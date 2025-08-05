using Firebase.Firestore;
using System;
using UnityEngine;
using UnityEngine.AI;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

// NavMeshAgent�� NavMeshObstacle ������Ʈ�� �� ��ũ��Ʈ�� �Բ� �ݵ�� �����ϵ��� �����մϴ�.
[RequireComponent(typeof(NavMeshAgent), typeof(NavMeshObstacle))]
public class ACRController : MonoBehaviour
{
    // �ڡڡ� �ٽ� 1: Delegate Ÿ�� ���� �ڡڡ�
    // Dictionary�� IEnumerator�� �޴� �޼ҵ带 ���� �� �ִ� 'Ʋ'�� �����մϴ�.
    public delegate IEnumerator PhysicalActionDelegate(Dictionary<string, object> stopData);

    // �ڡڡ� �ٽ� 2: �׼� ����� ���� Dictionary ���� �ڡڡ�
    // ���ڿ� Ű("pickup")�� ���� ����� �޼ҵ带 ¦���� �����մϴ�.
    private Dictionary<string, PhysicalActionDelegate> actionRegistry;

    // --- ������Ʈ ��ũ ---
    [Header("Component Links")]
    public ACR_PhysicalController physicalController; // Inspector���� ����

    // --- ������Ʈ ���� ---
    private NavMeshAgent agent;         // ACR�� �̵� �� ��� Ž���� ����ϴ� ������Ʈ
    private NavMeshObstacle obstacle;       // ACR�� �������� �� �ٸ� ACR�� ���ذ����� �ϴ� ��ֹ� ������Ʈ

    // --- Inspector ���� ���� ---
    [Header("Firebase Settings")]
    public string acrId = "acr_01";     // Firebase 'ACRs' �÷����� ���� ID�� ��ġ�ؾ� �ϴ� �� ACR�� ���� ID

    [Header("ACR Settings")]
    public Transform homePosition;      // �۾��� ���� �� ��� �� ������ ��ġ
    public float rotationSpeed = 120f;  // ��ǥ �������� ȸ���� ���� �ӵ� (�ʴ� ����)

    // --- ���� ���� ���� ---
    private DocumentReference acrDocRef;    // �� ACR�� Firebase ������ ���� ����
    private ListenerRegistration listener;      // Firebase ������ ������ �ǽð����� �����ϴ� ������
    private bool isWorking = false;             // ���� �۾�(Task)�� ���� ������ ���θ� ��Ÿ���� �÷���

    //private bool isFirstMove = true;
    private string currentTaskId;               // ���� ���� ���� Task�� ID
    private bool isPhysicalActionInProgress = false; // <<<--- ���� �۾��� ���� ������ Ȯ���ϴ� �÷���

    [Header("Navigation Settings")]
    [Tooltip("NavMeshObstacle�� Carving Time�� ������ ������ �����ϼ���. NavMesh ������ ��ٸ��� �ð��Դϴ�.")]
    public float navMeshCarveTime = 0.1f;

    public ACRAssigner ACRAssigner
    {
        get => default;
        set
        {
        }
    }

    //================================================================
    // 1. Unity �����ֱ� �Լ���: ��ũ��Ʈ�� �ʱ�ȭ �� ���� ���
    //================================================================

    /// <summary>
    /// ���� ������Ʈ�� ó�� Ȱ��ȭ�� �� �� �� ȣ��˴ϴ�.
    /// �ַ� ������Ʈ�� ã�ƿ��� �̺�Ʈ�� �����ϴ� �� ���˴ϴ�.
    /// </summary>
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();
        physicalController = GetComponentInChildren<ACR_PhysicalController>(); // �ڽĿ��� ã�ƿ�

        // Inspector ������ Ȯ���ϰ�, �׼� ��Ϻθ� �ʱ�ȭ�մϴ�.
        if (physicalController == null)
        {
            Debug.LogError($"[{acrId}] ACR_PhysicalController�� Inspector�� ������� �ʾҽ��ϴ�!");
        }
        InitializeActionRegistry();

        FirebaseManager.OnFirebaseInitialized += HandleFirebaseInitialized;
    }

    /// <summary>
    /// �׼� �̸��� ���� ������ �޼ҵ带 �����ϴ� '��ȭ��ȣ��'�� ����ϴ�.
    /// </summary>
    private void InitializeActionRegistry()
    {
        actionRegistry = new Dictionary<string, PhysicalActionDelegate>();

        // "pickup"�̶�� Ű���尡 ����, physicalController�� PickupSequence �޼ҵ带 �����ϵ��� �����մϴ�.
        if (physicalController != null)
        {
            actionRegistry["pickup"] = physicalController.PickupSequence;
            actionRegistry["dropoff_multi"] = physicalController.DropoffSequence; //
            // ���߿� �ٸ� �׼��� �߰��Ǹ� ���⿡ ��� ����ϸ� �˴ϴ�.
            // actionRegistry["charge"] = physicalController.ChargeSequence;
        }
    }


    /// <summary>
    /// ù ��° ������ ������Ʈ ���� �� �� ȣ��˴ϴ�.
    /// Awake ���Ŀ� ȣ��Ǹ�, �ٸ� ��ũ��Ʈ���� ��ȣ�ۿ��� �ʿ��� �� ���˴ϴ�.
    /// </summary>
    IEnumerator Start()
    {
        if (homePosition == null)
        {
            Debug.LogError($"[{acrId}] Home Position�� �������� �ʾҽ��ϴ�! Inspector���� �Ҵ����ּ���.");
            // homePosition�� ������ �� �̻� �������� �ʵ��� �մϴ�.
            yield break;
        }

        agent.enabled = true;
        obstacle.enabled = false;

        // 3. (���û���, ������ ����) Obstacle�� ������ �ڸ��� ���� ������ �����ϰ� ��ٸ��ϴ�.
        // ������ "ù ����" ���׸� �����ϴ� �������� ���ܵδ� ���� �����ϴ�.
        yield return new WaitForSeconds(navMeshCarveTime);
    }

    /// <summary>
    /// ���� ������Ʈ�� �ı��� �� ȣ��˴ϴ�.
    /// �޸� ������ �����ϱ� ���� �����ߴ� �̺�Ʈ�� �ݵ�� �����ؾ� �մϴ�.
    /// </summary>
    void OnDestroy()
    {
        FirebaseManager.OnFirebaseInitialized -= HandleFirebaseInitialized;
        listener?.Stop();
    }
    //================================================================
    // 2. Firebase ������ ����: ACR�� '��' ����
    //================================================================

    /// <summary>
    /// Firebase�� �غ�Ǹ� ȣ��Ǿ�, Firestore ������ ������ �����մϴ�.
    /// </summary>
    private void HandleFirebaseInitialized()
    {
        SetupFirestoreListener();
    }

    /// <summary>
    /// �ڽ��� Firebase ������ �ǽð����� ����(Listen)�ϴ� �����ʸ� �����մϴ�.
    /// 'assignedTask' �ʵ尡 ����Ǹ� ���ο� �۾��� �����մϴ�.
    /// </summary>
    private void SetupFirestoreListener()
    {
        acrDocRef = FirebaseManager.Instance.DB.Collection("ACRs").Document(acrId);
        listener = acrDocRef.Listen(snapshot =>
        {
            if (isWorking) return; // �̹� �ٸ� �۾��� �ϰ� �ִٸ� �� ����� �����մϴ�.

            if (snapshot.Exists && snapshot.ToDictionary().TryGetValue("assignedTask", out object taskIdObj) && taskIdObj != null)
            {
                currentTaskId = taskIdObj.ToString();
                Debug.Log($"[{acrId}] ���ο� �۾�({currentTaskId})�� �Ҵ� �޾ҽ��ϴ�.");
                isWorking = true;
                StartCoroutine(ProcessMultiStopTaskCoroutine(currentTaskId)); // ���ο� ���� �ڷ�ƾ�� �����մϴ�.
            }
        });
    }

    //================================================================
    // 3. ���� �۾� ó�� �ڷ�ƾ: �۾��� ��ü �帧�� �����ϴ� '��������' ����
    //================================================================

    /// <summary>
    /// ���� ������(Multi-Stop) Task�� ���������� ó���ϴ� ���� �ڷ�ƾ�Դϴ�.
    /// </summary>
    private IEnumerator ProcessMultiStopTaskCoroutine(string taskId)
    {
        // --- �۾� �غ� �ܰ� ---
        DocumentReference taskDocRef = FirebaseManager.Instance.DB.Collection("tasks").Document(taskId);
        Task<DocumentSnapshot> getTask = taskDocRef.GetSnapshotAsync();
        yield return new WaitUntil(() => getTask.IsCompleted);

        if (getTask.IsFaulted || !getTask.Result.Exists)
        {
            Debug.LogError($"[{acrId}] Task Error: Task ID '{taskId}'�� ã�ų� �д� �� �����߽��ϴ�!");
            isWorking = false;
            yield break;
        }

        var taskData = getTask.Result.ToDictionary();

        // --- 'stops' �迭�� ��ȸ�ϸ� �۾� ���� ---
        if (taskData.TryGetValue("stops", out object stopsObj) && stopsObj is List<object> stopsList)
        {
            Debug.Log($"[{acrId}] �� {stopsList.Count}���� �������� �ִ� �۾��� �����մϴ�.");

            for (int i = 0; i < stopsList.Count; i++)
            {
                var stop = stopsList[i] as Dictionary<string, object>; //firebase���� ���� data������ Dictionary<string, object> �������� Ȯ�� �� stop�� ����

                if (GetValueFromMap(stop, "status") == "completed") continue; // ACR�� ����ġ ���ؼ� ������ �����

                string action = GetValueFromMap(stop, "action");
                Debug.Log($"--- ������ {i + 1}/{stopsList.Count} ����: �׼� = {action} ---");

                // --- 1. �������� �̵� �� ȸ�� ---
                yield return MoveAndRotateForAction(stop, action);

                // --- 2. '���� ��ȣ' ������ '�Ϸ� ��ȣ' ��� ---
                yield return SwitchToObstacleCoroutine(); // ���� �۾� �߿��� ��ֹ� ����

                // �ڡڡ� �ٽ� 3: Dictionary���� �޼ҵ带 ã�� ���� ���� �ڡڡ�
                // �� �̻� �̺�Ʈ�� ���� ����, WaitUntil�� ������� �ʽ��ϴ�.
                if (actionRegistry.TryGetValue(action, out PhysicalActionDelegate actionToExecute))
                {
                    Debug.Log($"[{acrId}] ���� �۾�({action})�� �����մϴ�...");
                    // ã�ƿ� �޼ҵ�(Delegate)�� �ڷ�ƾ���� �����ϰ� ���� ������ ��ٸ��ϴ�.
                    yield return actionToExecute(stop);
                    Debug.Log($"[{acrId}] ���� �۾�({action})�� �Ϸ�Ǿ����ϴ�.");
                }
                else
                {
                    Debug.Log($"[{acrId}] ��ϵ��� �ʾҰų� ���� �۾��� ���� �׼�({action})�Դϴ�.");
                }

                Debug.Log($"[{acrId}] ���� �۾� ������� �ѱ�� �Ϸ� ��ȣ�� ����մϴ�...");
                yield return new WaitUntil(() => !isPhysicalActionInProgress); // ��� ����
                Debug.Log($"[{acrId}] ���� �۾� �Ϸ� ��ȣ�� �޾ҽ��ϴ�. ���� �������� �̵��մϴ�.");

                // --- 3. ������ �Ϸ� ó�� ---
                Task updateStopStatusTask = taskDocRef.UpdateAsync($"stops.{i}.status", "completed");
                yield return new WaitUntil(() => updateStopStatusTask.IsCompleted);
            }
        }

        // --- �۾� �Ϸ� �� ���� �ܰ� ---
        yield return CompleteTask(taskDocRef);
        yield return GoHome();

        isWorking = false; // ��� �۾��� �������Ƿ� ���ο� �۾��� ���� �� �ִ� ���·� ��ȯ�մϴ�.
    }

    //================================================================
    // 4. ���� �۾� �帧(�׼�) �ڷ�ƾ: �� ���������� ������ ��ü���� �ൿ��
    //================================================================

    /*
    /// <summary>
    /// PhysicalController�κ��� �۾� �Ϸ� ��ȣ�� ������ ȣ��˴ϴ�.
    /// </summary>
    private void HandleActionCompleted(string completedAcrId)
    {
        // ��ȣ�� ���� ACR�� �� �ڽ��� ���� �����մϴ�.
        if (completedAcrId == this.acrId)
        {
            isPhysicalActionInProgress = false; // ��� ���¸� �����մϴ�.
        }
    }
    */

    /// <summary>
    /// �� �������� �������� �̵��ϰ� ȸ���ϴ� ������ ����ϴ� ���� �ڷ�ƾ�Դϴ�.
    /// </summary>
    private IEnumerator MoveAndRotateForAction(Dictionary<string, object> stopData, string action)
    {
        Vector3 targetPosition = Vector3.zero;
        float targetRotation = transform.eulerAngles.y;

        // action Ÿ�Կ� ���� ������ ��ǥ�� ȸ������ �Ľ��մϴ�.
        switch (action)
        {
            case "pickup":
            case "dropoff":
                string locationKey = (action == "pickup") ? "source" : "destination";

                if (stopData.TryGetValue(locationKey, out object locObj) && locObj is Dictionary<string, object> locationMap)
                {
                    if (locationMap.TryGetValue("position", out object posObj) && posObj is Dictionary<string, object> posMap)
                    {
                        targetPosition = new Vector3(Convert.ToSingle(posMap["x"]), 0, Convert.ToSingle(posMap["z"]));
                    }
                    targetRotation = GetRotationFromMap(locationMap, "rotation");
                }
                else
                {
                    Debug.LogError($"[{acrId}] Action '{action}'�� �ʿ��� '{locationKey}' ���� ã�� �� �����ϴ�!");
                    // ���⼭ ó���� �ߴ��ؾ� �� ���� �ֽ��ϴ�.
                    yield break;
                }
                break;

            case "pickup_multi":
            case "dropoff_multi":
                string stationIdKey = action == "pickup_multi" ? "sourceStationId" : "destinationStationId";
                string rotationKey = action == "pickup_multi" ? "sourceStationRotation" : "destinationStationRotation";
                string stationId = GetValueFromMap(stopData, stationIdKey);

                // StationManager���� null�� ��ȯ�� �� �����Ƿ� ��� �ڵ� �߰�
                Transform stationTransform = StationManager.Instance.GetStationTransform(stationId);
                if (stationTransform != null)
                {
                    targetPosition = stationTransform.position;
                }
                else
                {
                    Debug.LogError($"[{acrId}] StationManager���� ID '{stationId}'�� ã�� �� �����ϴ�!");
                    yield break;
                }
                targetRotation = GetRotationFromTask(stopData, rotationKey);
                break;
        }

        // ���� �������� �̵��ϰ� ȸ���մϴ�.
        yield return MoveToTarget(targetPosition);
        yield return StartCoroutine(RotateTowards(targetRotation));
    }

    //================================================================
    // 5. ����(Helper) �Լ���: �ݺ��Ǵ� �ڵ带 ���� ����
    //================================================================

    /// <summary>
    /// ������ ���� ��ǥ�� �̵��ϴ� ��ü ������ ����մϴ�. (ȸ�� -> �̵� -> ���� ���)
    /// </summary>
    private IEnumerator MoveToTarget(Vector3 targetPosition)
    {
        // <<<--- ���� 1: ��ǥ ��ġ�� y���� 0���� �����մϴ�. ---
        Vector3 finalTargetPosition = new Vector3(targetPosition.x, 0, targetPosition.z);

        //// �ڡڡ� �ٽ� 2: ù ��° �̵��� ��쿡�� Ư�� �ʱ�ȭ ���� ���� �ڡڡ�
        //if (isFirstMove)
        //{
        //    Debug.Log($"[{acrId}] ù ��° �̵��� �����߽��ϴ�. Ư�� ����ȭ�� �����մϴ�.");
        //    // Agent�� Ȱ��ȭ�ϰ�, Warp�� ���� ���� ��ġ�� ���� ����ȭ�մϴ�.
        //    // �� ������ Start()���� �ξ� ���̹Ƿ� NavMesh �ý����� �غ�Ǿ��� Ȯ���� �����ϴ�.
        //    if (!agent.enabled) agent.enabled = true;
        //    agent.Warp(transform.position);

        //    // ���� �̵����ʹ� �� ������ �������� �ʵ��� �÷��׸� �����մϴ�.
        //    isFirstMove = false;

        //    // ������ ���� �� ������ ����Ͽ� Warp�� ������ ����ǵ��� �մϴ�.
        //    yield return null;
        //}
        // 1. �̵��� �����ϱ� ���� Agent ���� ���� ��ȯ�մϴ�.
        yield return StartCoroutine(SwitchToAgentCoroutine());

        // 2. ���� �������� ���� �ε巴�� ȸ���մϴ�.
        // ȸ�� ���� ��꿡�� y���� 0�� ��ǥ�� ����մϴ�.
        Vector3 direction = (finalTargetPosition - transform.position).normalized;
        if (direction.sqrMagnitude > 0.001f)
        {
            float targetYAngle = Quaternion.LookRotation(direction).eulerAngles.y;
            yield return StartCoroutine(RotateTowards(targetYAngle));
        }

        // 3. ���¸� 'moving'���� ������Ʈ�մϴ�.
        Task updateStatusTask = UpdateStatus("moving");
        yield return new WaitUntil(() => updateStatusTask.IsCompleted);

        // 4. ȸ���� ���� ��, �̵��� �����մϴ�.
        agent.isStopped = false;
        // SetDestination���� y���� 0�� ��ǥ�� �����մϴ�.
        agent.SetDestination(finalTargetPosition);

        // 5. ������ ������ ����մϴ�.
        yield return new WaitUntil(() => HasArrived());
    }

    /// <summary>
    /// ��� �۾��� ���� �� Home���� �����ϴ� ������ ����մϴ�.
    /// </summary>
    private IEnumerator GoHome()
    {
        Debug.Log($"[{acrId}] ���� �������� �����մϴ�.");
        yield return MoveToTarget(homePosition.position);

        // �ʿ��ϴٸ� Ȩ���� Ư�� ������ �ٶ󺸵��� ȸ�� ���� �߰� ����
        // yield return StartCoroutine(RotateTowards(0));

        Debug.Log($"[{acrId}] ���� ���� ����. ��� ���·� ��ȯ�մϴ�.");
        yield return StartCoroutine(SwitchToObstacleCoroutine());
        Task idleTask = UpdateStatus("idle");
        yield return new WaitUntil(() => idleTask.IsCompleted);
    }

    /// <summary>
    /// Task �Ϸ� ���¸� Firebase�� �����մϴ�. (�ڽ��� assignedTask ����, tasks ���� ���� ����)
    /// </summary>
    private async Task CompleteTask(DocumentReference taskDocRef)
    {
        // 1. �ڽ��� 'assignedTask' �ʵ带 null�� ����� ���ο� �۾��� ���� �� �ֵ��� ��
        await acrDocRef.UpdateAsync("assignedTask", null);

        // 2. ���� �ʵ带 �� ���� ������Ʈ�ϱ� ���� Dictionary�� ����
        Dictionary<string, object> taskUpdates = new Dictionary<string, object>
    {
        { "status", "completed" },
        { "completedAt", Timestamp.GetCurrentTimestamp() }
    };

        // 3. Dictionary�� ���ڷ� �����Ͽ� tasks ������ ������Ʈ
        await taskDocRef.UpdateAsync(taskUpdates);

        Debug.Log($"[{acrId}] Task '{taskDocRef.Id}' �Ϸ� ����.");
    }

    /// <summary>
    /// �� ACR�� ���� ����(status)�� Firebase�� ������Ʈ�մϴ�.
    /// </summary>
    private Task UpdateStatus(string newStatus)
    {
        if (acrDocRef == null) return Task.CompletedTask;
        return acrDocRef.UpdateAsync("status", newStatus);
    }

    /// <summary>
    /// AMR�� '�̵� ������ Agent' ���� ��ȯ�մϴ�.
    /// </summary>
    private IEnumerator SwitchToAgentCoroutine()
    {
        // 1. Obstacle�� ���� ��Ȱ��ȭ�Ͽ� NavMesh ������ �����մϴ�.
        if (obstacle.enabled)
        {
            obstacle.enabled = false;
        }

        // NavMesh ���� �ð��� Obstacle�� Carving Time�� �����ָ� ���� �������Դϴ�.
        // Carving Time�� 0�̸� �� �����Ӹ� ��ٷ��� ����մϴ�.
        if (navMeshCarveTime > 0)
        {
            yield return new WaitForSeconds(navMeshCarveTime);
        }
        else
        {
            yield return new WaitForEndOfFrame();
        }

        if (!agent.enabled)
        {
            agent.enabled = true;
        }
    }

    /// <summary>
    /// AMR�� '������ ��ֹ�' ���� ��ȯ�մϴ�.
    /// </summary>
    private IEnumerator SwitchToObstacleCoroutine()
    {
        // 1. Agent�� �������� ���߰� ��Ȱ��ȭ�մϴ�.
        if (agent.enabled)
        {
            if (agent.hasPath) agent.ResetPath();
            agent.isStopped = true; // isStopped�� ���� �����ϴ� ���� �����մϴ�.
            agent.enabled = false;
        }

        // 2. �ڡڡ� �ٽ�: Agent�� �ý��ۿ��� ������ ���ŵ� �ð��� �ݴϴ�. �ڡڡ�
        yield return new WaitForEndOfFrame();

        // 3. Agent�� ����� ��, Obstacle�� �����ϰ� Ȱ��ȭ�մϴ�.
        if (!obstacle.enabled)
        {
            obstacle.enabled = true;
        }
    }

    /// <summary>
    /// NavMeshAgent�� �������� �����ߴ��� Ȯ���մϴ�.
    /// </summary>
    private bool HasArrived()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            return !agent.hasPath || agent.velocity.sqrMagnitude == 0f;
        }
        return false;
    }

    /// <summary>
    /// ��ųʸ����� Ư�� Ű�� ���� �����ϰ� ���ڿ��� �����ɴϴ�.
    /// </summary>
    private string GetValueFromMap(Dictionary<string, object> dataMap, string key)
    {
        return dataMap.TryGetValue(key, out object valueObj) ? valueObj.ToString() : string.Empty;
    }

    /// <summary>
    /// Task ������(�ֻ��� ��ųʸ�)���� ȸ�� ������ �����ϰ� �Ľ��մϴ�.
    /// </summary>
    private float GetRotationFromTask(Dictionary<string, object> taskData, string key)
    {
        if (taskData.TryGetValue(key, out object rotObj) && rotObj is Dictionary<string, object> rotMap)
        {
            return GetRotationFromMap(rotMap, "y");
        }
        return transform.eulerAngles.y;
    }

    /// <summary>
    /// ���� ��(destination, source ��)���� ȸ�� ������ �����ϰ� �Ľ��մϴ�.
    /// </summary>
    private float GetRotationFromMap(Dictionary<string, object> dataMap, string key)
    {
        if (dataMap.TryGetValue(key, out object rotObj) && rotObj is Dictionary<string, object> rotMap)
        {
            if (rotMap.TryGetValue("y", out object yObj)) return Convert.ToSingle(yObj);
        }
        else if (dataMap.TryGetValue(key, out object yObj))
        {
            return Convert.ToSingle(yObj);
        }
        return transform.eulerAngles.y;
    }

    /// <summary>
    /// ������ Y�� ������ �ε巴�� ȸ���ϴ� �ڷ�ƾ�Դϴ�.
    /// </summary>
    private IEnumerator RotateTowards(float targetYAngle)
    {
        if (agent.enabled)
        {
            agent.isStopped = true;
        }

        yield return null;

        Quaternion targetRotation = Quaternion.Euler(0, targetYAngle, 0);

        while (Quaternion.Angle(transform.rotation, targetRotation) > 1.0f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            yield return null;
        }

        transform.rotation = targetRotation;
        Debug.Log($"��ǥ ����({targetYAngle}��)���� ȸ�� �Ϸ�.");
    }
}