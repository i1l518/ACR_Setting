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
    [Tooltip("���� �ִ� ACRAssigner ������Ʈ�� �������ּ���.")]
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
            Debug.LogError("[TaskManager] ACRAssigner�� ������� �ʾҽ��ϴ�! Inspector���� �������ּ���.");
            this.enabled = false;
            return;
        }

        FirebaseManager.OnFirebaseInitialized += () =>
        {
            db = FirebaseManager.Instance.DB;
        };
    }

    // --- Public Methods (�ܺ� Ŭ�������� ȣ��) ---

    public void CreateMultiInboundTask(DocumentSnapshot stationDoc)
    {
        StartCoroutine(CreateMultiInboundTaskCoroutine(stationDoc));
    }

    public void CreateMultiOutboundTask(List<string> requestedItemTypes)
    {
        StartCoroutine(CreateMultiOutboundTaskCoroutine(requestedItemTypes));
    }

    // --- Private Coroutines (���� �۾� ����) ---

    // TaskManager.cs

    // --- Inbound Task Coroutine (�����丵�� ����) ---

    private IEnumerator CreateMultiInboundTaskCoroutine(DocumentSnapshot stationDoc)
    {
        string stationId = stationDoc.Id;
        var stationRef = stationDoc.Reference;

        // --- 1. �����̼� ���� ---
        var reserveTask = stationRef.UpdateAsync("status", "reserved");
        yield return new WaitUntil(() => reserveTask.IsCompleted);
        if (reserveTask.IsFaulted)
        {
            Debug.LogError($"[TaskManager] �����̼� '{stationId}' ���� ����. �ٸ� ���μ����� ���� ó������ �� �ֽ��ϴ�.");
            yield break;
        }

        // --- 2. �����̼��� ������ ��� �������� ---
        var getItemsTask = stationRef.Collection("items").GetSnapshotAsync();
        yield return new WaitUntil(() => getItemsTask.IsCompleted);
        if (getItemsTask.IsFaulted)
        {
            Debug.LogError($"[TaskManager] �����̼� '{stationId}'�� ���� ������ �÷��� ��ȸ�� �����߽��ϴ�.");
            // TODO: �����̼� ���� �ѹ�
            yield break;
        }

        var itemsToPickup = getItemsTask.Result.Documents.ToList();
        if (itemsToPickup.Count == 0)
        {
            Debug.LogWarning($"[TaskManager] �����̼� '{stationId}'�� �������� ���� �۾��� �������� �ʽ��ϴ�.");
            // TODO: �����̼� ���� �ѹ�
            yield break;
        }

        // --- 3. stops �迭 ���� �� ������ �� ���� ��ȸ/���� �غ� ---
        List<object> stops = new List<object>();
        List<DocumentReference> rackRefsToReserve = new List<DocumentReference>();
        Dictionary<string, RackData> rackDataCache = new Dictionary<string, RackData>(); // �� ���� �ӽ� ����

        stops.Add(new Dictionary<string, object>
    {
        { "action", "pickup_multi" }, { "sourceStationId", stationId }, { "status", "pending" }
    });

        foreach (var itemDoc in itemsToPickup)
        {
            var itemData = itemDoc.ToDictionary();
            string destinationRackId = itemData["destinationRackId"].ToString();
            var rackRef = db.Collection("Gantries").Document(destinationRackId);

            rackRefsToReserve.Add(rackRef); // ������ �� ��Ͽ� �߰�

            // �� ������ ������ ĳ�ÿ� ���� (�ߺ� ��ȸ ����)
            var getRackDataTask = rackRef.GetSnapshotAsync();
            yield return new WaitUntil(() => getRackDataTask.IsCompleted);
            if (getRackDataTask.IsFaulted || !getRackDataTask.Result.Exists)
            {
                Debug.LogError($"[TaskManager] ������ �� '{destinationRackId}' ������ �������� �� �����߽��ϴ�.");
                // TODO: �ѹ� ����
                yield break;
            }
            rackDataCache[destinationRackId] = getRackDataTask.Result.ConvertTo<RackData>();
        }

        // --- 4. ���۸� ����Ͽ� ��� ���� �� ���� ���� ---
        yield return StartCoroutine(ReserveRacks(rackRefsToReserve));

        // --- 5. ����� �� ������ �������� dropoff ������ ���� ���� ---
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

        // --- 6. ���۸� ����Ͽ� Task ���� ���� �� ACR �Ҵ� ---
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
                Debug.LogError("[TaskManager] Task ���� ������ �����Ͽ� ACR �Ҵ��� ������ �� �����ϴ�.");
            }
        }));
    }


    // --- Outbound Task Coroutine (�ű� ����) ---

    /// <summary>
    /// ��û�� ������ ����� ������� ���� ��� Task�� �����մϴ�.
    /// </summary>
    private IEnumerator CreateMultiOutboundTaskCoroutine(List<string> requestedItemTypes)
    {
        Debug.Log($"[TaskManager] {requestedItemTypes.Count}�� �����ۿ� ���� ���� ��� Task ������ �����մϴ�.");

        List<object> stops = new List<object>();
        List<DocumentReference> rackRefsToReserve = new List<DocumentReference>();

        // --- 1. ��� Ž�� �� �Ⱦ� ������ ���� ---
        List<string> alreadySelectedRackIds = new List<string>(); // ���� ��û �� �ߺ� �Ⱦ� ����
        int currentSlotId = 1;

        foreach (var itemType in requestedItemTypes)
        {
            // 1-1. ��� ���� (itemType ��ġ, status 0, ���� ��ȣ �켱)
            Query rackQuery = db.Collection("Gantries")
                .WhereEqualTo("itemType", itemType)
                .WhereEqualTo("status", 0);

            var getRacksTask = rackQuery.GetSnapshotAsync();
            yield return new WaitUntil(() => getRacksTask.IsCompleted);

            if (getRacksTask.IsFaulted)
            {
                // <<<--- ������ �� ������ ����ϵ��� ���� ---
                Debug.LogError($"'{itemType}' ��� �˻� �� ���� �߻�!");
                yield break;
            }

            if (getRacksTask.Result.Count == 0)
            {
                Debug.LogError($"'{itemType}' Ÿ���� ��� ã�� �� �����ϴ�! �۾� ���� �ߴ�.");
                yield break;
            }

            // 1-2. ��� ������ ù ��° ��� ����
            DocumentSnapshot selectedRackDoc = null;
            // Firestore���� ������ ������ ID �������� �����մϴ�.
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
                Debug.LogError($"'{itemType}' Ÿ���� ���� ��� ã�� �� �����ϴ�! �۾� ���� �ߴ�.");
                // TODO: �ѹ� ����
                yield break;
            }

            alreadySelectedRackIds.Add(selectedRackDoc.Id);
            rackRefsToReserve.Add(selectedRackDoc.Reference);
            var rackData = selectedRackDoc.ConvertTo<RackData>();

            // 1-3. `pickup` ������ �߰�
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

        // --- 2. ���ҽ� ���� ---
        yield return StartCoroutine(ReserveRacks(rackRefsToReserve));

        // --- 3. ���� `dropoff_multi` ������ �߰� ---
        stops.Add(new Dictionary<string, object>
        {
            { "action", "dropoff_multi" },
            { "destinationStationId", "outbound_station_01" }, // ������ ��� �����̼� ID
            { "destinationStationRotation", new Dictionary<string, object> { { "y", 0 } } },
            { "status", "pending" }
        });

        // --- 4. Task ���� ���� �� ACR �Ҵ� ---
        var newTaskData = new Dictionary<string, object>
        {
            { "type", "multi_outbound" }, { "stops", stops }, { "status", "pending" },
            { "assignedAcrId", null }, { "createdAt", Timestamp.GetCurrentTimestamp() }, { "completedAt", null }
        };

        // ���۸� ����Ͽ� Task ���� ��, �ݹ����� ACR �Ҵ� �Լ� ȣ��
        yield return StartCoroutine(CreateTaskDocument(newTaskData, (newTaskRef) =>
        {
            if (newTaskRef != null)
            {
                acrAssigner.AssignTaskToIdleAcr(newTaskRef);
            }
            else
            {
                Debug.LogError("[TaskManager] Task ���� ������ �����Ͽ� ACR �Ҵ��� ������ �� �����ϴ�.");
                // TODO: �ѹ� ����
            }
        }));
    }

    private IEnumerator RollbackStationStatus(DocumentReference stationRef, string statusToRestore)
    {
        Task rollbackTask = stationRef.UpdateAsync("status", statusToRestore);
        yield return new WaitUntil(() => rollbackTask.IsCompleted);
        if (rollbackTask.IsFaulted)
        {
            Debug.LogError($"[TaskManager] �ɰ��� ����: �����̼� '{stationRef.Id}' ���� �ѹ� ����!");
        }
    }

    //================================================================
    // ���� �Լ���
    //================================================================

    /// <summary>
    /// ���� ���� �� ������ ���ÿ� 'reserved' ���·� ������Ʈ�մϴ�.
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
            Debug.LogError("[TaskManager] �ϳ� �̻��� ���� �����ϴ� �� �����߽��ϴ�.");
            // TODO: �̹� ������ ������� �ѹ��ϴ� ���� �ʿ�
        }
    }

    /// <summary>
    /// ���� Task �����͸� ������� Firestore�� ������ �����մϴ�.
    /// </summary>
    private IEnumerator CreateTaskDocument(Dictionary<string, object> taskData, Action<DocumentReference> onComplete)
    {
        var createTask = db.Collection("tasks").AddAsync(taskData);
        yield return new WaitUntil(() => createTask.IsCompleted);

        if (createTask.IsFaulted)
        {
            Debug.LogError("[TaskManager] Task ���� ���� ����!");
            onComplete?.Invoke(null);
            yield break;
        }

        onComplete?.Invoke(createTask.Result);
    }
}