// CentralControlSystem.cs (���ο� C# ��ũ��Ʈ ����)
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
    // 0. ����
    //================================================================
    private FirebaseFirestore db;

    [Header("Task Trigger Settings")]
    [Tooltip("�۾� �Ҵ� ������ Ȯ���ϴ� �ֱ� (��)")]
    public float checkIntervalSeconds = 10.0f;

    [Tooltip("������ ������ �� �� �̻��̸� �۾��� �����մϴ�.")]
    public int itemCountTrigger = 5;

    [Tooltip("������ ������ �߰� �� �� �ð�(��)�� ������ �۾��� �����մϴ�.")]
    public float timeoutMinutes = 3.0f;

    //================================================================
    // 1. Unity �����ֱ� �Լ���
    //================================================================
    void Start()
    {
        // FirebaseManager�κ��� DB �ν��Ͻ��� �޾ƿɴϴ�.
        FirebaseManager.OnFirebaseInitialized += () =>
        {
            db = FirebaseManager.Instance.DB;
            // Firebase�� �ʱ�ȭ�Ǹ�, �ֱ������� �����̼� ���¸� Ȯ���ϴ� �ڷ�ƾ�� �����մϴ�.
            StartCoroutine(CheckStationsPeriodically());
        };
    }

    /*
    // UI ��ư�̳� �ٸ� Ʈ���ſ��� �� �Լ��� ȣ���Ѵٰ� �����մϴ�.
    public void OnInboundRequest(string itemType)
    {
        Debug.Log($"'{itemType}' Ÿ�� ��ǰ �԰� ��û ����. ���� �� Ž�� ����...");
        StartCoroutine(CreateInboundTaskCoroutine(itemType));
    }
    */

    //===========================================================================================================================================================
    //===========================================================================================================================================================
    //===========================================================================================================================================================

    //================================================================
    // 1. Inbound_Station���� ���ǿ� ���� task�� ����� ����
    //================================================================

    /// <summary>
    /// ������ �ð�(checkIntervalSeconds)���� �����̼� ���¸� Ȯ���ϴ� ���� �����Դϴ�.
    /// </summary>
    private IEnumerator CheckStationsPeriodically()
    {
        while (true)
        {
            Debug.Log("[CentralControlSystem] �ֱ����� �ιٿ�� �����̼� ���� Ȯ���� �����մϴ�...");
            yield return StartCoroutine(CheckAndCreateInboundTasks());

            // ���� Ȯ�� �ð����� ����մϴ�.
            yield return new WaitForSeconds(checkIntervalSeconds);
        }
    }

    /// <summary>
    /// ��� �ιٿ�� �����̼��� Ȯ���ϰ�, ����(����/�ð�)�� �´� �����̼ǿ� ���� Task�� �����մϴ�.
    /// </summary>
    private IEnumerator CheckAndCreateInboundTasks()
    {
        // 1. ���ǿ� �´� �����̼ǵ��� �����մϴ�.(firestore�� ���Ḹ �� ����)
        var stationsRef = db.Collection("inbound_stations");
        var now = Timestamp.GetCurrentTimestamp();
        var timeoutTimestamp = Timestamp.FromDateTime(now.ToDateTime().AddMinutes(-timeoutMinutes));

        // ���� 1: ���� ���� (5�� �̻�)
        Query quantityQuery = stationsRef
            .WhereEqualTo("status", "waiting")
            .WhereGreaterThanOrEqualTo("itemCount", itemCountTrigger);

        // ���� 2: Ÿ�Ӿƿ� ���� (3�� �̻� ���)
        Query timeoutQuery = stationsRef
            .WhereEqualTo("status", "waiting")
            .WhereLessThanOrEqualTo("lastItemAddedAt", timeoutTimestamp);

        // �� ������ ���ÿ� �����մϴ�.(firestore���� �����͸� ��������)
        var getQuantityTask = quantityQuery.GetSnapshotAsync();
        var getTimeoutTask = timeoutQuery.GetSnapshotAsync();

        yield return new WaitUntil(() => getQuantityTask.IsCompleted && getTimeoutTask.IsCompleted);

        if (getQuantityTask.IsFaulted || getTimeoutTask.IsFaulted)
        {
            Debug.LogError("[CentralControlSystem] �����̼� ���� ��ȸ �� ������ �߻��߽��ϴ�.");
            yield break;
        }

        // 2. �� ���� ����� ��ġ�� �ߺ��� �����մϴ�.
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
            Debug.Log("[CentralControlSystem] ���� �۾��� ������ ������ �����̼��� �����ϴ�.");
            yield break;
        }

        // 3. ���ǿ� �´� �� �����̼ǿ� ���� Task ���� �ڷ�ƾ�� �����մϴ�.
        foreach (var stationDoc in stationsToProcess.Values)
        {
            Debug.Log($"[CentralControlSystem] ���� ���� �����̼� �߰�: {stationDoc.Id}. ���� �԰� Task ������ �����մϴ�.");
            yield return StartCoroutine(CreateMultiInboundTaskForStation(stationDoc));
        }
    }

    /// <summary>
    /// Ư�� Inbound �����̼��� �����۵��� ������� ���� �԰� Task�� ����
    /// </summary>
    private IEnumerator CreateMultiInboundTaskForStation(DocumentSnapshot stationDoc)
    {
        string stationId = stationDoc.Id;
        var stationRef = stationDoc.Reference;

        // --- 1. �ߺ� ������ ���� ���� �����̼� ���¸� 'reserved'�� ��� ���� ---
        var reserveTask = stationRef.UpdateAsync("status", "reserved");
        yield return new WaitUntil(() => reserveTask.IsCompleted);
        if (reserveTask.IsFaulted)
        {
            Debug.LogError($"�����̼� '{stationId}' ���� ����. �ٸ� ���μ����� ���� ó������ �� �ֽ��ϴ�.");
            yield break;
        }

        // --- 2. �����̼��� ���� �÷��� 'items'���� ��� ������ ���� �������� ---
        var getItemsTask = stationRef.Collection("items").GetSnapshotAsync();
        yield return new WaitUntil(() => getItemsTask.IsCompleted);
        if (getItemsTask.IsFaulted)
        {
            Debug.LogError($"�����̼� '{stationId}'�� ���� ������ �÷��� ��ȸ�� �����߽��ϴ�.");
            yield return RollbackStationStatus(stationRef, "waiting"); // ���� �� �����̼� ���� �ѹ�
            yield break;
        }

        var itemsToPickup = getItemsTask.Result.Documents.ToList();

        // --- 3. �� �����ۿ� ���� ������ �� �� ã�� �� 'stops' �迭 ���� ---
        List<object> stops = new List<object>();
        List<Task> rackReservationTasks = new List<Task>();
        List<DocumentReference> reservedRackRefs = new List<DocumentReference>();

        // 3-1. �Ⱦ� ������ �߰�
        stops.Add(new Dictionary<string, object>
        {
            { "action", "pickup_multi" },
            { "sourceStationId", stationId },
            { "status", "pending" } 
            // items_to_pickup ������ ACR�� �ƴ� �߾� �ý����� �˸� �ǹǷ� Task���� ���� ����
        });

        // 3-2. �� �����ۺ� ������� ������ �߰�
        foreach (var itemDoc in itemsToPickup)
        {
            var itemData = itemDoc.ToDictionary();
            string destinationRackId = itemData["destinationRackId"].ToString();
            var rackRef = db.Collection("Gantries").Document(destinationRackId);

            // �ش� ���� ���¸� 'reserved(2)'�� ������Ʈ�ϴ� Task�� ����Ʈ�� �߰�
            rackReservationTasks.Add(rackRef.UpdateAsync("status", 2));
            reservedRackRefs.Add(rackRef); // �ѹ��� ���� ���� ����

            // Firestore���� ���� �� ����(��ǥ, ����)�� �ٽ� �����;� ��
            var getRackDataTask = rackRef.GetSnapshotAsync();
            yield return new WaitUntil(() => getRackDataTask.IsCompleted);
            if (getRackDataTask.IsFaulted || !getRackDataTask.Result.Exists)
            {
                Debug.LogError($"������ �� '{destinationRackId}' ������ �������� �� �����߽��ϴ�.");
                // TODO: �ѹ� ����
                yield break;
            }

            var rackData = getRackDataTask.Result.ConvertTo<RackData>();

            stops.Add(new Dictionary<string, object>
            {
                { "action", "dropoff" },
                { "sourceSlotId", Convert.ToInt32(itemData["slotIndex"]) + 1 }, // ���� �ε����� 1���� �����Ѵٰ� ����
                { "destination", new Dictionary<string, object> {
                    { "rackId", destinationRackId },
                    { "position", rackData.position },
                    { "rotation", new Dictionary<string, object> { { "y", rackData.angle } } }
                }},
                { "status", "pending" }
            });

            // ��� �� ���� Task�� ���ķ� ����
            Task allRackReservations = Task.WhenAll(rackReservationTasks);
            yield return new WaitUntil(() => allRackReservations.IsCompleted);

            // �� ���� �� �ϳ��� �����ߴٸ� �ѹ� ���� �ʿ� (���⼭�� ������ ���� �α׸� ���)
            if (allRackReservations.IsFaulted)
            {
                Debug.LogError($"�����̼� '{stationId}'�� ������ �����ϴ� �� �����߽��ϴ�. �ѹ��� �ʿ��մϴ�.");
                // TODO: �̹� ����� ������ ���¸� �ٽ� 'empty(1)'�� �ǵ����� ���� �߰�
                yield break;
            }

            // ==========================================================
            // ---  ���� Task ���� ���� ---
            // ==========================================================

            // Task ������ ������ ��� �����͸� Dictionary ���·� �����մϴ�.
            var newTaskData = new Dictionary<string, object>
            {
                // Task�� ������ ����մϴ�. ACRController�� �� ���� ���� �۾� �帧�� �����մϴ�.
                { "type", "multi_inbound" },
        
                // ������ �������� ������ ������(stops) ����Ʈ�� �����մϴ�.
                { "stops", stops },
        
                // Task�� �ʱ� ���´� 'pending'(�Ҵ� ��� ��)�Դϴ�.
                { "status", "pending" },
        
                // ���� � ACR���� �Ҵ���� �ʾ����Ƿ� null�� �ʱ�ȭ�մϴ�.
                { "assignedAmrId", null },
        
                // Firebase ������ ���� �ð��� �������� ���� �ð��� ����մϴ�.
                { "createdAt", Timestamp.GetCurrentTimestamp() },
        
                // ���� �Ϸ���� �ʾ����Ƿ� null�� �ʱ�ȭ�մϴ�.
                { "completedAt", null }
            };

            // 'tasks' �÷��ǿ� ������ ������ �����ͷ� ���ο� ������ �߰�(����)�ϴ� �񵿱� �۾��� �����մϴ�.
            // .AddAsync()�� Firebase�� �ڵ����� ������ ID�� �����Ͽ� ������ ����ϴ�.
            Task<DocumentReference> createTask = db.Collection("tasks").AddAsync(newTaskData);

            // Task ���� �۾��� �Ϸ�� ������ �ڷ�ƾ�� ��� ���߰� ��ٸ��ϴ�.
            yield return new WaitUntil(() => createTask.IsCompleted);

            // --- Task ���� ���� �� ����(�ѹ�) ���� ---
            if (createTask.IsFaulted)
            {
                Debug.LogError($"Task ���� ����! �����ߴ� �����̼�({stationId})�� ������ ���¸� ������� �����ؾ� �մϴ�.");

                yield break;
            }

            // --- Task ���� ���� �� ---

            // ������ ������ ����(��� �� ID ����)�� �����ɴϴ�.
            DocumentReference newTaskRef = createTask.Result;

            Debug.Log($"���� �԰� Task '{newTaskRef.Id}' ���� �Ϸ�. ���� ���� ACR�� Ž���մϴ�.");

            // 5. ���� ACR���� Task �Ҵ� (���� ���� ����)
            yield return StartCoroutine(AssignTaskToIdleAcr(newTaskRef));
        }
    }


    //=======================================================================================
    //=======================================================================================
    //=======================================================================================

    // ==========================================================
    // ---  ACR���� Task �Ҵ� ---
    // ==========================================================
    /// <summary>
    /// ������ Task�� ������ ACR���� �Ҵ��մϴ�.
    /// </summary>
    private IEnumerator AssignTaskToIdleAcr(DocumentReference taskRef)
    {
        Query idleAcrQuery = db.Collection("ACRs").WhereEqualTo("status", "idle");
        Task<QuerySnapshot> getIdleAcrsTask = idleAcrQuery.GetSnapshotAsync();
        yield return new WaitUntil(() => getIdleAcrsTask.IsCompleted);

        if (getIdleAcrsTask.IsFaulted)
        {
            Debug.LogError("���� ACR ��ȸ ����!");
            // TODO: Task ���¸� 'failed'�� �����ϰ� ����� ���ҽ� �ѹ�
            yield break;
        }

        var idleAcrDocs = getIdleAcrsTask.Result.Documents.ToList();
        if (idleAcrDocs.Count == 0)
        {
            Debug.LogWarning("���� ������ ���� ACR�� �����ϴ�. Task�� ��� ���·� �����˴ϴ�.");
            yield break;
        }

        if (getIdleAcrsTask.Result.Count == 0)
        {
            Debug.LogWarning("���� ������ ���� ACR�� �����ϴ�. Task�� ��� ���·� �����˴ϴ�.");
            yield break;
        }

        DocumentSnapshot selectedAcrDoc = getIdleAcrsTask.Result.Documents.First();//���� ������ ��������
        DocumentReference selectedAcrRef = selectedAcrDoc.Reference;//���� ��ġ ����
        Debug.Log($"���� ACR �ĺ� ����: {selectedAcrDoc.Id}. Ʈ������� ���� �Ҵ��� �õ��մϴ�.");

        // 2. Ʈ������� ���� �����ϰ� Task�� �Ҵ��մϴ�.
        bool assignmentSuccess = false; // Ʈ����� ���� ���θ� ������ ����

        // Ʈ������� �񵿱� Task�̹Ƿ�, �Ϸ�� ������ ��ٷ��� �մϴ�.
        Task transactionTask = db.RunTransactionAsync(async transaction =>
        {
            // 2-1. [�б�] Ʈ����� ������ ACR�� '�ֽ�' ���¸� �ٽ� �н��ϴ�.
            // �̰��� ���ü� ������ �ذ��ϴ� �ٽ��Դϴ�.
            DocumentSnapshot latestAcrSnapshot = await transaction.GetSnapshotAsync(selectedAcrRef);

            // 2-2. [���� Ȯ��] �ֽ� ���°� ������ 'idle'���� ���� Ȯ���մϴ�.
            if (latestAcrSnapshot.GetValue<string>("status") == "idle")
            {
                // 2-3. [����] 'idle'�� �´ٸ�, Task�� �Ҵ��ϰ� ���� �÷��׸� true�� �����մϴ�.
                transaction.Update(selectedAcrRef, "assignedTask", taskRef.Id);
                assignmentSuccess = true;
            }
            else
            {
                // �� ���̿� �ٸ� ���μ����� �� ACR�� ä���ٸ�, ���� �÷��׸� false�� �����մϴ�.
                assignmentSuccess = false;
            }
        });

        // �ڷ�ƾ���� �񵿱� Task�� �Ϸ�� ������ ��ٸ��ϴ�.
        yield return new WaitUntil(() => transactionTask.IsCompleted);

        // 3. Ʈ����� ����� Ȯ���ϰ� �ļ� ��ġ�� ���մϴ�.
        if (transactionTask.IsFaulted)
        {
            Debug.LogError($"ACR '{selectedAcrDoc.Id}'���� Task �Ҵ� Ʈ����� �� ���� �߻�!");
            // TODO: ���� ����
            yield break;
        }

        if (assignmentSuccess)
        {
            // Ʈ������� ���������� ACR���� Task�� �Ҵ����� ���
            Debug.Log($"���������� ACR '{selectedAcrDoc.Id}'���� Task '{taskRef.Id}'�� �Ҵ��߽��ϴ�.");
        }
        else
        {
            // Ʈ������� ���������� ���������, ACR�� �̹� �ٸ� �۾��� �ϰ� �־ �Ҵ����� ������ ���
            Debug.LogWarning($"ACR '{selectedAcrDoc.Id}'�� �Ҵ� ���� �ٸ� �۾��� �޾ҽ��ϴ�. �� Task({taskRef.Id})�� ���� ��ȸ�� ���Ҵ�˴ϴ�.");
            // TODO: ��� ���� Task ť�� �ٽ� �ִ� ������ �ʿ��� �� �ֽ��ϴ�.
        }
    }

    /// <summary>
    /// �۾� ���� �� �����̼��� ���¸� �ǵ����� ���� �ڷ�ƾ�Դϴ�.
    /// </summary>
    private IEnumerator RollbackStationStatus(DocumentReference stationRef, string statusToRestore)
    {
        Task rollbackTask = stationRef.UpdateAsync("status", statusToRestore);
        yield return new WaitUntil(() => rollbackTask.IsCompleted);
        if (rollbackTask.IsFaulted)
        {
            Debug.LogError($"�ɰ��� ����: �����̼� '{stationRef.Id}' ���� �ѹ� ����!");
        }
    }
}
