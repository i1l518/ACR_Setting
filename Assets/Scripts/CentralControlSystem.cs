// CentralControlSystem.cs (새로운 C# 스크립트 파일)
using Firebase.Firestore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CentralControlSystem : MonoBehaviour
{
    //================================================================
    // 0. 변수
    //================================================================
    private FirebaseFirestore db;

    [Header("Task Trigger Settings")]
    [Tooltip("작업 할당 조건을 확인하는 주기 (초)")]
    public float checkIntervalSeconds = 10.0f;

    [Tooltip("아이템 수량이 이 값 이상이면 작업을 생성합니다.")]
    public int itemCountTrigger = 5;

    [Tooltip("마지막 아이템 추가 후 이 시간(분)이 지나면 작업을 생성합니다.")]
    public float timeoutMinutes = 3.0f;

    //================================================================
    // 1. Unity 생명주기 함수들
    //================================================================
    void Start()
    {
        // FirebaseManager로부터 DB 인스턴스를 받아옵니다.
        FirebaseManager.OnFirebaseInitialized += () =>
        {
            db = FirebaseManager.Instance.DB;
            // Firebase가 초기화되면, 주기적으로 스테이션 상태를 확인하는 코루틴을 시작합니다.
            StartCoroutine(CheckStationsPeriodically());
        };
    }

    /*
    // UI 버튼이나 다른 트리거에서 이 함수를 호출한다고 가정합니다.
    public void OnInboundRequest(string itemType)
    {
        Debug.Log($"'{itemType}' 타입 물품 입고 요청 접수. 최적 랙 탐색 시작...");
        StartCoroutine(CreateInboundTaskCoroutine(itemType));
    }
    */

    //===========================================================================================================================================================
    //===========================================================================================================================================================
    //===========================================================================================================================================================

    //================================================================
    // 1. Inbound_Station에서 조건에 따라 task를 만드는 과정
    //================================================================

    /// <summary>
    /// 정해진 시간(checkIntervalSeconds)마다 스테이션 상태를 확인하는 메인 루프입니다.
    /// </summary>
    private IEnumerator CheckStationsPeriodically()
    {
        while (true)
        {
            Debug.Log("[CentralControlSystem] 주기적인 인바운드 스테이션 상태 확인을 시작합니다...");
            yield return StartCoroutine(CheckAndCreateInboundTasks());

            // 다음 확인 시간까지 대기합니다.
            yield return new WaitForSeconds(checkIntervalSeconds);
        }
    }

    /// <summary>
    /// 모든 인바운드 스테이션을 확인하고, 조건(수량/시간)에 맞는 스테이션에 대해 Task를 생성합니다.
    /// </summary>
    private IEnumerator CheckAndCreateInboundTasks()
    {
        // 1. 조건에 맞는 스테이션들을 쿼리합니다.(firestore에 연결만 한 상태)
        var stationsRef = db.Collection("inbound_stations");
        var now = Timestamp.GetCurrentTimestamp();
        var timeoutTimestamp = Timestamp.FromDateTime(now.ToDateTime().AddMinutes(-timeoutMinutes));

        // 쿼리 1: 수량 조건 (5개 이상)
        Query quantityQuery = stationsRef
            .WhereEqualTo("status", "waiting")
            .WhereGreaterThanOrEqualTo("itemCount", itemCountTrigger);

        // 쿼리 2: 타임아웃 조건 (3분 이상 경과)
        Query timeoutQuery = stationsRef
            .WhereEqualTo("status", "waiting")
            .WhereLessThanOrEqualTo("lastItemAddedAt", timeoutTimestamp);

        // 두 쿼리를 동시에 실행합니다.(firestore에서 데이터를 가져오기)
        var getQuantityTask = quantityQuery.GetSnapshotAsync();
        var getTimeoutTask = timeoutQuery.GetSnapshotAsync();

        yield return new WaitUntil(() => getQuantityTask.IsCompleted && getTimeoutTask.IsCompleted);

        if (getQuantityTask.IsFaulted || getTimeoutTask.IsFaulted)
        {
            Debug.LogError("[CentralControlSystem] 스테이션 상태 조회 중 오류가 발생했습니다.");
            yield break;
        }

        // 2. 두 쿼리 결과를 합치고 중복을 제거합니다.
        var stationsToProcess = new Dictionary<string, DocumentSnapshot>();
        foreach (var doc in getQuantityTask.Result.Documents)
        {
            if (!stationsToProcess.ContainsKey(doc.Id)) stationsToProcess.Add(doc.Id, doc);
        }
        foreach (var doc in getTimeoutTask.Result.Documents)
        {
            if (!stationsToProcess.ContainsKey(doc.Id)) stationsToProcess.Add(doc.Id, doc);
        }

        if (stationsToProcess.Count == 0)
        {
            Debug.Log("[CentralControlSystem] 현재 작업을 생성할 조건의 스테이션이 없습니다.");
            yield break;
        }

        // 3. 조건에 맞는 각 스테이션에 대해 Task 생성 코루틴을 실행합니다.
        foreach (var stationDoc in stationsToProcess.Values)
        {
            Debug.Log($"[CentralControlSystem] 조건 충족 스테이션 발견: {stationDoc.Id}. 다중 입고 Task 생성을 시작합니다.");
            yield return StartCoroutine(CreateMultiInboundTaskForStation(stationDoc));
        }
    }

    /// <summary>
    /// 특정 Inbound 스테이션의 아이템들을 기반으로 다중 입고 Task를 생성
    /// </summary>
    private IEnumerator CreateMultiInboundTaskForStation(DocumentSnapshot stationDoc)
    {
        string stationId = stationDoc.Id;
        var stationRef = stationDoc.Reference;

        // --- 1. 중복 생성을 막기 위해 스테이션 상태를 'reserved'로 즉시 변경 ---
        var reserveTask = stationRef.UpdateAsync("status", "reserved");
        yield return new WaitUntil(() => reserveTask.IsCompleted);
        if (reserveTask.IsFaulted)
        {
            Debug.LogError($"스테이션 '{stationId}' 예약 실패. 다른 프로세스가 먼저 처리했을 수 있습니다.");
            yield break;
        }

        // --- 2. 스테이션의 하위 컬렉션 'items'에서 모든 아이템 정보 가져오기 ---
        var getItemsTask = stationRef.Collection("items").GetSnapshotAsync();
        yield return new WaitUntil(() => getItemsTask.IsCompleted);
        if (getItemsTask.IsFaulted)
        {
            Debug.LogError($"스테이션 '{stationId}'의 하위 아이템 컬렉션 조회에 실패했습니다.");
            yield return RollbackStationStatus(stationRef, "waiting"); // 실패 시 스테이션 상태 롤백
            yield break;
        }

        var itemsToPickup = getItemsTask.Result.Documents.ToList();

        // --- 3. 각 아이템에 대해 최적의 빈 랙 찾기 및 'stops' 배열 생성 ---
        List<object> stops = new List<object>();
        List<Task> rackReservationTasks = new List<Task>();
        List<DocumentReference> reservedRackRefs = new List<DocumentReference>();

        // 3-1. 픽업 경유지 추가
        stops.Add(new Dictionary<string, object>
        {
            { "action", "pickup_multi" },
            { "sourceStationId", stationId },
            { "status", "pending" } 
            // items_to_pickup 정보는 ACR이 아닌 중앙 시스템이 알면 되므로 Task에선 생략 가능
        });

        // 3-2. 각 아이템별 드랍오프 경유지 추가
        foreach (var itemDoc in itemsToPickup)
        {
            var itemData = itemDoc.ToDictionary();
            string destinationRackId = itemData["destinationRackId"].ToString();
            var rackRef = db.Collection("Gantries").Document(destinationRackId);

            // 해당 랙의 상태를 'reserved(2)'로 업데이트하는 Task를 리스트에 추가
            rackReservationTasks.Add(rackRef.UpdateAsync("status", 2));
            reservedRackRefs.Add(rackRef); // 롤백을 위해 참조 저장

            // Firestore에서 랙의 상세 정보(좌표, 각도)를 다시 가져와야 함
            var getRackDataTask = rackRef.GetSnapshotAsync();
            yield return new WaitUntil(() => getRackDataTask.IsCompleted);
            if (getRackDataTask.IsFaulted || !getRackDataTask.Result.Exists)
            {
                Debug.LogError($"목적지 랙 '{destinationRackId}' 정보를 가져오는 데 실패했습니다.");
                // TODO: 롤백 로직
                yield break;
            }

            var rackData = getRackDataTask.Result.ConvertTo<RackData>();

            stops.Add(new Dictionary<string, object>
            {
                { "action", "dropoff" },
                { "sourceSlotId", Convert.ToInt32(itemData["slotIndex"]) + 1 }, // 슬롯 인덱스는 1부터 시작한다고 가정
                { "destination", new Dictionary<string, object> {
                    { "rackId", destinationRackId },
                    { "position", rackData.position },
                    { "rotation", new Dictionary<string, object> { { "y", rackData.angle } } }
                }},
                { "status", "pending" }
            });

            // 모든 랙 예약 Task를 병렬로 실행
            Task allRackReservations = Task.WhenAll(rackReservationTasks);
            yield return new WaitUntil(() => allRackReservations.IsCompleted);

            // 랙 예약 중 하나라도 실패했다면 롤백 로직 필요 (여기서는 간단히 에러 로그만 출력)
            if (allRackReservations.IsFaulted)
            {
                Debug.LogError($"스테이션 '{stationId}'의 랙들을 예약하는 데 실패했습니다. 롤백이 필요합니다.");
                // TODO: 이미 예약된 랙들의 상태를 다시 'empty(1)'로 되돌리는 로직 추가
                yield break;
            }

            // ==========================================================
            // ---  최종 Task 문서 생성 ---
            // ==========================================================

            // Task 문서에 저장할 모든 데이터를 Dictionary 형태로 구성합니다.
            var newTaskData = new Dictionary<string, object>
            {
                // Task의 종류를 명시합니다. ACRController가 이 값을 보고 작업 흐름을 결정합니다.
                { "type", "multi_inbound" },
        
                // 위에서 동적으로 생성한 경유지(stops) 리스트를 저장합니다.
                { "stops", stops },
        
                // Task의 초기 상태는 'pending'(할당 대기 중)입니다.
                { "status", "pending" },
        
                // 아직 어떤 ACR에도 할당되지 않았으므로 null로 초기화합니다.
                { "assignedAmrId", null },
        
                // Firebase 서버의 현재 시간을 기준으로 생성 시간을 기록합니다.
                { "createdAt", Timestamp.GetCurrentTimestamp() },
        
                // 아직 완료되지 않았으므로 null로 초기화합니다.
                { "completedAt", null }
            };

            // 'tasks' 컬렉션에 위에서 구성한 데이터로 새로운 문서를 추가(생성)하는 비동기 작업을 시작합니다.
            // .AddAsync()는 Firebase가 자동으로 고유한 ID를 생성하여 문서를 만듭니다.
            Task<DocumentReference> createTask = db.Collection("tasks").AddAsync(newTaskData);

            // Task 생성 작업이 완료될 때까지 코루틴을 잠시 멈추고 기다립니다.
            yield return new WaitUntil(() => createTask.IsCompleted);

            // --- Task 생성 실패 시 복구(롤백) 로직 ---
            if (createTask.IsFaulted)
            {
                Debug.LogError($"Task 생성 실패! 예약했던 스테이션({stationId})과 랙들의 상태를 원래대로 복구해야 합니다.");

                yield break;
            }

            // --- Task 생성 성공 시 ---

            // 생성된 문서의 참조(경로 및 ID 포함)를 가져옵니다.
            DocumentReference newTaskRef = createTask.Result;

            Debug.Log($"다중 입고 Task '{newTaskRef.Id}' 생성 완료. 이제 유휴 ACR을 탐색합니다.");

            // 5. 유휴 ACR에게 Task 할당 (기존 로직 재사용)
            yield return StartCoroutine(AssignTaskToIdleAcr(newTaskRef));
        }
    }


    //=======================================================================================
    //=======================================================================================
    //=======================================================================================

    // ==========================================================
    // ---  ACR에게 Task 할당 ---
    // ==========================================================
    /// <summary>
    /// 생성된 Task를 가용한 ACR에게 할당합니다.
    /// </summary>
    private IEnumerator AssignTaskToIdleAcr(DocumentReference taskRef)
    {
        Query idleAcrQuery = db.Collection("ACRs").WhereEqualTo("status", "idle");
        Task<QuerySnapshot> getIdleAcrsTask = idleAcrQuery.GetSnapshotAsync();
        yield return new WaitUntil(() => getIdleAcrsTask.IsCompleted);

        if (getIdleAcrsTask.IsFaulted)
        {
            Debug.LogError("유휴 ACR 조회 실패!");
            // TODO: Task 상태를 'failed'로 변경하고 예약된 리소스 롤백
            yield break;
        }

        var idleAcrDocs = getIdleAcrsTask.Result.Documents.ToList();
        if (idleAcrDocs.Count == 0)
        {
            Debug.LogWarning("현재 가용한 유휴 ACR이 없습니다. Task가 대기 상태로 유지됩니다.");
            yield break;
        }

        if (getIdleAcrsTask.Result.Count == 0)
        {
            Debug.LogWarning("현재 가용한 유휴 ACR이 없습니다. Task가 대기 상태로 유지됩니다.");
            yield break;
        }

        DocumentSnapshot selectedAcrDoc = getIdleAcrsTask.Result.Documents.First();//문서 데이터 가져오기
        DocumentReference selectedAcrRef = selectedAcrDoc.Reference;//문서 위치 지정
        Debug.Log($"최적 ACR 후보 선정: {selectedAcrDoc.Id}. 트랜잭션을 통해 할당을 시도합니다.");

        // 2. 트랜잭션을 통해 안전하게 Task를 할당합니다.
        bool assignmentSuccess = false; // 트랜잭션 성공 여부를 저장할 변수

        // 트랜잭션은 비동기 Task이므로, 완료될 때까지 기다려야 합니다.
        Task transactionTask = db.RunTransactionAsync(async transaction =>
        {
            // 2-1. [읽기] 트랜잭션 내에서 ACR의 '최신' 상태를 다시 읽습니다.
            // 이것이 동시성 문제를 해결하는 핵심입니다.
            DocumentSnapshot latestAcrSnapshot = await transaction.GetSnapshotAsync(selectedAcrRef);

            // 2-2. [조건 확인] 최신 상태가 여전히 'idle'인지 최종 확인합니다.
            if (latestAcrSnapshot.GetValue<string>("status") == "idle")
            {
                // 2-3. [쓰기] 'idle'이 맞다면, Task를 할당하고 성공 플래그를 true로 설정합니다.
                transaction.Update(selectedAcrRef, "assignedTask", taskRef.Id);
                assignmentSuccess = true;
            }
            else
            {
                // 그 사이에 다른 프로세스가 이 ACR을 채갔다면, 실패 플래그를 false로 유지합니다.
                assignmentSuccess = false;
            }
        });

        // 코루틴에서 비동기 Task가 완료될 때까지 기다립니다.
        yield return new WaitUntil(() => transactionTask.IsCompleted);

        // 3. 트랜잭션 결과를 확인하고 후속 조치를 취합니다.
        if (transactionTask.IsFaulted)
        {
            Debug.LogError($"ACR '{selectedAcrDoc.Id}'에게 Task 할당 트랜잭션 중 오류 발생!");
            // TODO: 복구 로직
            yield break;
        }

        if (assignmentSuccess)
        {
            // 트랜잭션이 성공적으로 ACR에게 Task를 할당했을 경우
            Debug.Log($"성공적으로 ACR '{selectedAcrDoc.Id}'에게 Task '{taskRef.Id}'를 할당했습니다.");
        }
        else
        {
            // 트랜잭션은 성공적으로 실행됐지만, ACR이 이미 다른 작업을 하고 있어서 할당하지 못했을 경우
            Debug.LogWarning($"ACR '{selectedAcrDoc.Id}'는 할당 직전 다른 작업을 받았습니다. 이 Task({taskRef.Id})는 다음 기회에 재할당됩니다.");
            // TODO: 대기 중인 Task 큐에 다시 넣는 로직이 필요할 수 있습니다.
        }
    }

    /// <summary>
    /// 작업 실패 시 스테이션의 상태를 되돌리는 헬퍼 코루틴입니다.
    /// </summary>
    private IEnumerator RollbackStationStatus(DocumentReference stationRef, string statusToRestore)
    {
        Task rollbackTask = stationRef.UpdateAsync("status", statusToRestore);
        yield return new WaitUntil(() => rollbackTask.IsCompleted);
        if (rollbackTask.IsFaulted)
        {
            Debug.LogError($"심각한 오류: 스테이션 '{stationRef.Id}' 상태 롤백 실패!");
        }
    }
}
