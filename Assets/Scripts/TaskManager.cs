// TaskManager.cs
using Firebase.Firestore;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    [Header("System References")]
    [Tooltip("씬에 있는 ACRAssigner 오브젝트를 연결해주세요.")]
    public ACRAssigner acrAssigner;

    private FirebaseFirestore db;

    public StationMonitor StationMonitor
    {
        get => default;
        set
        {
        }
    }

    public OutboundRequestUI OutboundRequestUI
    {
        get => default;
        set
        {
        }
    }

    void Start()
    {
        if (acrAssigner == null)
        {
            Debug.LogError("[TaskManager] ACRAssigner가 연결되지 않았습니다! Inspector에서 설정해주세요.");
            this.enabled = false;
            return;
        }

        FirebaseManager.OnFirebaseInitialized += () =>
        {
            db = FirebaseManager.Instance.DB;
        };
    }

    // --- Public Methods (외부 클래스에서 호출) ---

    public void CreateMultiInboundTask(DocumentSnapshot stationDoc)
    {
        StartCoroutine(CreateMultiInboundTaskCoroutine(stationDoc));
    }

    public void CreateMultiOutboundTask(List<string> requestedItemTypes)
    {
        StartCoroutine(CreateMultiOutboundTaskCoroutine(requestedItemTypes));
    }

    // --- Private Coroutines (실제 작업 수행) ---

    // TaskManager.cs

    // --- Inbound Task Coroutine (리팩토링된 버전) ---

    private IEnumerator CreateMultiInboundTaskCoroutine(DocumentSnapshot stationDoc)
    {
        string stationId = stationDoc.Id;
        var stationRef = stationDoc.Reference;

        // --- 1. 스테이션 예약 ---
        var reserveTask = stationRef.UpdateAsync("status", "reserved");
        yield return new WaitUntil(() => reserveTask.IsCompleted);
        if (reserveTask.IsFaulted)
        {
            Debug.LogError($"[TaskManager] 스테이션 '{stationId}' 예약 실패. 다른 프로세스가 먼저 처리했을 수 있습니다.");
            yield break;
        }

        // --- 2. 스테이션의 아이템 목록 가져오기 ---
        var getItemsTask = stationRef.Collection("items").GetSnapshotAsync();
        yield return new WaitUntil(() => getItemsTask.IsCompleted);
        if (getItemsTask.IsFaulted)
        {
            Debug.LogError($"[TaskManager] 스테이션 '{stationId}'의 하위 아이템 컬렉션 조회에 실패했습니다.");
            // TODO: 스테이션 상태 롤백
            yield break;
        }

        var itemsToPickup = getItemsTask.Result.Documents.ToList();
        if (itemsToPickup.Count == 0)
        {
            Debug.LogWarning($"[TaskManager] 스테이션 '{stationId}'에 아이템이 없어 작업을 생성하지 않습니다.");
            // TODO: 스테이션 상태 롤백
            yield break;
        }

        // --- 3. stops 배열 생성 및 목적지 랙 정보 조회/예약 준비 ---
        List<object> stops = new List<object>();
        List<DocumentReference> rackRefsToReserve = new List<DocumentReference>();
        Dictionary<string, RackData> rackDataCache = new Dictionary<string, RackData>(); // 랙 정보 임시 저장

        stops.Add(new Dictionary<string, object>
    {
        { "action", "pickup_multi" }, { "sourceStationId", stationId }, { "status", "pending" }
    });

        foreach (var itemDoc in itemsToPickup)
        {
            var itemData = itemDoc.ToDictionary();
            string destinationRackId = itemData["destinationRackId"].ToString();
            var rackRef = db.Collection("Gantries").Document(destinationRackId);

            rackRefsToReserve.Add(rackRef); // 예약할 랙 목록에 추가

            // 랙 정보를 가져와 캐시에 저장 (중복 조회 방지)
            var getRackDataTask = rackRef.GetSnapshotAsync();
            yield return new WaitUntil(() => getRackDataTask.IsCompleted);
            if (getRackDataTask.IsFaulted || !getRackDataTask.Result.Exists)
            {
                Debug.LogError($"[TaskManager] 목적지 랙 '{destinationRackId}' 정보를 가져오는 데 실패했습니다.");
                // TODO: 롤백 로직
                yield break;
            }
            rackDataCache[destinationRackId] = getRackDataTask.Result.ConvertTo<RackData>();
        }

        // --- 4. 헬퍼를 사용하여 모든 랙을 한 번에 예약 ---
        yield return StartCoroutine(ReserveRacks(rackRefsToReserve));

        // --- 5. 예약된 랙 정보를 바탕으로 dropoff 경유지 최종 생성 ---
        foreach (var itemDoc in itemsToPickup)
        {
            var itemData = itemDoc.ToDictionary();
            string destinationRackId = itemData["destinationRackId"].ToString();
            RackData rackData = rackDataCache[destinationRackId];

            stops.Add(new Dictionary<string, object>
        {
            { "action", "dropoff" },
            { "sourceSlotId", Convert.ToInt32(itemData["slotIndex"]) + 1 },
            { "destination", new Dictionary<string, object> {
                { "rackId", destinationRackId },
                { "position", rackData.position },
                { "rotation", new Dictionary<string, object> { { "y", rackData.angle } } }
            }},
            { "status", "pending" }
        });
        }

        // --- 6. 헬퍼를 사용하여 Task 문서 생성 및 ACR 할당 ---
        var newTaskData = new Dictionary<string, object>
    {
        { "type", "multi_inbound" }, { "stops", stops }, { "status", "pending" },
        { "assignedAcrId", null }, { "createdAt", Timestamp.GetCurrentTimestamp() }, { "completedAt", null }
    };

        yield return StartCoroutine(CreateTaskDocument(newTaskData, (newTaskRef) =>
        {
            if (newTaskRef != null)
            {
                acrAssigner.AssignTaskToIdleAcr(newTaskRef);
            }
            else
            {
                Debug.LogError("[TaskManager] Task 문서 생성에 실패하여 ACR 할당을 진행할 수 없습니다.");
            }
        }));
    }


    // --- Outbound Task Coroutine (신규 구현) ---

    /// <summary>
    /// 요청된 아이템 목록을 기반으로 다중 출고 Task를 생성합니다.
    /// </summary>
    private IEnumerator CreateMultiOutboundTaskCoroutine(List<string> requestedItemTypes)
    {
        Debug.Log($"[TaskManager] {requestedItemTypes.Count}개 아이템에 대한 다중 출고 Task 생성을 시작합니다.");

        List<object> stops = new List<object>();
        List<DocumentReference> rackRefsToReserve = new List<DocumentReference>();

        // --- 1. 재고 탐색 및 픽업 경유지 생성 ---
        List<string> alreadySelectedRackIds = new List<string>(); // 동일 요청 내 중복 픽업 방지
        int currentSlotId = 1;

        foreach (var itemType in requestedItemTypes)
        {
            // 1-1. 재고 쿼리 (itemType 일치, status 0, 낮은 번호 우선)
            Query rackQuery = db.Collection("Gantries")
                .WhereEqualTo("itemType", itemType)
                .WhereEqualTo("status", 0);

            var getRacksTask = rackQuery.GetSnapshotAsync();
            yield return new WaitUntil(() => getRacksTask.IsCompleted);

            if (getRacksTask.IsFaulted)
            {
                // <<<--- 에러의 상세 내용을 출력하도록 수정 ---
                Debug.LogError($"'{itemType}' 재고 검색 중 오류 발생!");
                yield break;
            }

            if (getRacksTask.Result.Count == 0)
            {
                Debug.LogError($"'{itemType}' 타입의 재고를 찾을 수 없습니다! 작업 생성 중단.");
                yield break;
            }

            // 1-2. 사용 가능한 첫 번째 재고 선택
            DocumentSnapshot selectedRackDoc = null;
            // Firestore에서 가져온 문서를 ID 기준으로 정렬합니다.
            var sortedDocs = getRacksTask.Result.Documents.OrderBy(d => d.Id);

            foreach (var doc in getRacksTask.Result.Documents)
            {
                if (!alreadySelectedRackIds.Contains(doc.Id))
                {
                    selectedRackDoc = doc;
                    break;
                }
            }

            if (selectedRackDoc == null)
            {
                Debug.LogError($"'{itemType}' 타입의 가용 재고를 찾을 수 없습니다! 작업 생성 중단.");
                // TODO: 롤백 로직
                yield break;
            }

            alreadySelectedRackIds.Add(selectedRackDoc.Id);
            rackRefsToReserve.Add(selectedRackDoc.Reference);
            var rackData = selectedRackDoc.ConvertTo<RackData>();

            // 1-3. `pickup` 경유지 추가
            stops.Add(new Dictionary<string, object>
            {
                { "action", "pickup" },
                { "source", new Dictionary<string, object> {
                    { "rackId", rackData.DocumentId },
                    { "itemType", itemType },
                    { "position", rackData.position },
                    { "rotation", new Dictionary<string, object> { { "y", rackData.angle } } }
                }},
                { "targetSlotId", currentSlotId++ },
                { "status", "pending" }
            });
        }

        // --- 2. 리소스 예약 ---
        yield return StartCoroutine(ReserveRacks(rackRefsToReserve));

        // --- 3. 최종 `dropoff_multi` 경유지 추가 ---
        stops.Add(new Dictionary<string, object>
        {
            { "action", "dropoff_multi" },
            { "destinationStationId", "outbound_station_01" }, // 고정된 출고 스테이션 ID
            { "destinationStationRotation", new Dictionary<string, object> { { "y", 0 } } },
            { "status", "pending" }
        });

        // --- 4. Task 문서 생성 및 ACR 할당 ---
        var newTaskData = new Dictionary<string, object>
        {
            { "type", "multi_outbound" }, { "stops", stops }, { "status", "pending" },
            { "assignedAcrId", null }, { "createdAt", Timestamp.GetCurrentTimestamp() }, { "completedAt", null }
        };

        // 헬퍼를 사용하여 Task 생성 후, 콜백으로 ACR 할당 함수 호출
        yield return StartCoroutine(CreateTaskDocument(newTaskData, (newTaskRef) =>
        {
            if (newTaskRef != null)
            {
                acrAssigner.AssignTaskToIdleAcr(newTaskRef);
            }
            else
            {
                Debug.LogError("[TaskManager] Task 문서 생성에 실패하여 ACR 할당을 진행할 수 없습니다.");
                // TODO: 롤백 로직
            }
        }));
    }

    private IEnumerator RollbackStationStatus(DocumentReference stationRef, string statusToRestore)
    {
        Task rollbackTask = stationRef.UpdateAsync("status", statusToRestore);
        yield return new WaitUntil(() => rollbackTask.IsCompleted);
        if (rollbackTask.IsFaulted)
        {
            Debug.LogError($"[TaskManager] 심각한 오류: 스테이션 '{stationRef.Id}' 상태 롤백 실패!");
        }
    }

    //================================================================
    // 헬퍼 함수들
    //================================================================

    /// <summary>
    /// 여러 개의 랙 문서를 동시에 'reserved' 상태로 업데이트합니다.
    /// </summary>
    private IEnumerator ReserveRacks(List<DocumentReference> rackRefs)
    {
        var reservationTasks = new List<Task>();
        foreach (var rackRef in rackRefs)
        {
            reservationTasks.Add(rackRef.UpdateAsync("status", 2));
        }

        var allReservationsTask = Task.WhenAll(reservationTasks);
        yield return new WaitUntil(() => allReservationsTask.IsCompleted);

        if (allReservationsTask.IsFaulted)
        {
            Debug.LogError("[TaskManager] 하나 이상의 랙을 예약하는 데 실패했습니다.");
            // TODO: 이미 성공한 예약들을 롤백하는 로직 필요
        }
    }

    /// <summary>
    /// 최종 Task 데이터를 기반으로 Firestore에 문서를 생성합니다.
    /// </summary>
    private IEnumerator CreateTaskDocument(Dictionary<string, object> taskData, Action<DocumentReference> onComplete)
    {
        var createTask = db.Collection("tasks").AddAsync(taskData);
        yield return new WaitUntil(() => createTask.IsCompleted);

        if (createTask.IsFaulted)
        {
            Debug.LogError("[TaskManager] Task 문서 생성 실패!");
            onComplete?.Invoke(null);
            yield break;
        }

        onComplete?.Invoke(createTask.Result);
    }
}