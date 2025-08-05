using Firebase.Firestore;
using System;
using UnityEngine;
using UnityEngine.AI;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

// NavMeshAgent와 NavMeshObstacle 컴포넌트가 이 스크립트와 함께 반드시 존재하도록 강제합니다.
[RequireComponent(typeof(NavMeshAgent), typeof(NavMeshObstacle))]
public class ACRController : MonoBehaviour
{
    // ★★★ 핵심 1: Delegate 타입 선언 ★★★
    // Dictionary와 IEnumerator를 받는 메소드를 담을 수 있는 '틀'을 정의합니다.
    public delegate IEnumerator PhysicalActionDelegate(Dictionary<string, object> stopData);

    // ★★★ 핵심 2: 액션 등록을 위한 Dictionary 선언 ★★★
    // 문자열 키("pickup")와 실제 실행될 메소드를 짝지어 저장합니다.
    private Dictionary<string, PhysicalActionDelegate> actionRegistry;

    // --- 컴포넌트 링크 ---
    [Header("Component Links")]
    public ACR_PhysicalController physicalController; // Inspector에서 연결

    // --- 컴포넌트 변수 ---
    private NavMeshAgent agent;         // ACR의 이동 및 경로 탐색을 담당하는 컴포넌트
    private NavMeshObstacle obstacle;       // ACR이 멈춰있을 때 다른 ACR이 피해가도록 하는 장애물 컴포넌트

    // --- Inspector 설정 변수 ---
    [Header("Firebase Settings")]
    public string acrId = "acr_01";     // Firebase 'ACRs' 컬렉션의 문서 ID와 일치해야 하는 이 ACR의 고유 ID

    [Header("ACR Settings")]
    public Transform homePosition;      // 작업이 없을 때 대기 및 복귀할 위치
    public float rotationSpeed = 120f;  // 목표 방향으로 회전할 때의 속도 (초당 각도)

    // --- 내부 상태 변수 ---
    private DocumentReference acrDocRef;    // 이 ACR의 Firebase 문서에 대한 참조
    private ListenerRegistration listener;      // Firebase 데이터 변경을 실시간으로 감지하는 리스너
    private bool isWorking = false;             // 현재 작업(Task)을 수행 중인지 여부를 나타내는 플래그

    //private bool isFirstMove = true;
    private string currentTaskId;               // 현재 수행 중인 Task의 ID
    private bool isPhysicalActionInProgress = false; // <<<--- 물리 작업이 진행 중인지 확인하는 플래그

    [Header("Navigation Settings")]
    [Tooltip("NavMeshObstacle의 Carving Time과 동일한 값으로 설정하세요. NavMesh 복구를 기다리는 시간입니다.")]
    public float navMeshCarveTime = 0.1f;

    public ACRAssigner ACRAssigner
    {
        get => default;
        set
        {
        }
    }

    //================================================================
    // 1. Unity 생명주기 함수들: 스크립트의 초기화 및 해제 담당
    //================================================================

    /// <summary>
    /// 게임 오브젝트가 처음 활성화될 때 한 번 호출됩니다.
    /// 주로 컴포넌트를 찾아오고 이벤트를 구독하는 데 사용됩니다.
    /// </summary>
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();
        physicalController = GetComponentInChildren<ACR_PhysicalController>(); // 자식에서 찾아옴

        // Inspector 연결을 확인하고, 액션 등록부를 초기화합니다.
        if (physicalController == null)
        {
            Debug.LogError($"[{acrId}] ACR_PhysicalController가 Inspector에 연결되지 않았습니다!");
        }
        InitializeActionRegistry();

        FirebaseManager.OnFirebaseInitialized += HandleFirebaseInitialized;
    }

    /// <summary>
    /// 액션 이름과 실제 실행할 메소드를 연결하는 '전화번호부'를 만듭니다.
    /// </summary>
    private void InitializeActionRegistry()
    {
        actionRegistry = new Dictionary<string, PhysicalActionDelegate>();

        // "pickup"이라는 키워드가 오면, physicalController의 PickupSequence 메소드를 실행하도록 연결합니다.
        if (physicalController != null)
        {
            actionRegistry["pickup"] = physicalController.PickupSequence;
            actionRegistry["dropoff_multi"] = physicalController.DropoffSequence; //
            // 나중에 다른 액션이 추가되면 여기에 계속 등록하면 됩니다.
            // actionRegistry["charge"] = physicalController.ChargeSequence;
        }
    }


    /// <summary>
    /// 첫 번째 프레임 업데이트 전에 한 번 호출됩니다.
    /// Awake 이후에 호출되며, 다른 스크립트와의 상호작용이 필요할 때 사용됩니다.
    /// </summary>
    IEnumerator Start()
    {
        if (homePosition == null)
        {
            Debug.LogError($"[{acrId}] Home Position이 설정되지 않았습니다! Inspector에서 할당해주세요.");
            // homePosition이 없으면 더 이상 진행하지 않도록 합니다.
            yield break;
        }

        agent.enabled = true;
        obstacle.enabled = false;

        // 3. (선택사항, 하지만 권장) Obstacle이 완전히 자리를 잡을 때까지 안전하게 기다립니다.
        // 이전의 "첫 실행" 버그를 예방하는 차원에서 남겨두는 것이 좋습니다.
        yield return new WaitForSeconds(navMeshCarveTime);
    }

    /// <summary>
    /// 게임 오브젝트가 파괴될 때 호출됩니다.
    /// 메모리 누수를 방지하기 위해 예약했던 이벤트를 반드시 해제해야 합니다.
    /// </summary>
    void OnDestroy()
    {
        FirebaseManager.OnFirebaseInitialized -= HandleFirebaseInitialized;
        listener?.Stop();
    }
    //================================================================
    // 2. Firebase 리스너 설정: ACR의 '귀' 역할
    //================================================================

    /// <summary>
    /// Firebase가 준비되면 호출되어, Firestore 리스너 설정을 시작합니다.
    /// </summary>
    private void HandleFirebaseInitialized()
    {
        SetupFirestoreListener();
    }

    /// <summary>
    /// 자신의 Firebase 문서를 실시간으로 감시(Listen)하는 리스너를 설정합니다.
    /// 'assignedTask' 필드가 변경되면 새로운 작업을 시작합니다.
    /// </summary>
    private void SetupFirestoreListener()
    {
        acrDocRef = FirebaseManager.Instance.DB.Collection("ACRs").Document(acrId);
        listener = acrDocRef.Listen(snapshot =>
        {
            if (isWorking) return; // 이미 다른 작업을 하고 있다면 새 명령을 무시합니다.

            if (snapshot.Exists && snapshot.ToDictionary().TryGetValue("assignedTask", out object taskIdObj) && taskIdObj != null)
            {
                currentTaskId = taskIdObj.ToString();
                Debug.Log($"[{acrId}] 새로운 작업({currentTaskId})을 할당 받았습니다.");
                isWorking = true;
                StartCoroutine(ProcessMultiStopTaskCoroutine(currentTaskId)); // 새로운 메인 코루틴을 시작합니다.
            }
        });
    }

    //================================================================
    // 3. 메인 작업 처리 코루틴: 작업의 전체 흐름을 지휘하는 '교통정리' 역할
    //================================================================

    /// <summary>
    /// 다중 경유지(Multi-Stop) Task를 순차적으로 처리하는 메인 코루틴입니다.
    /// </summary>
    private IEnumerator ProcessMultiStopTaskCoroutine(string taskId)
    {
        // --- 작업 준비 단계 ---
        DocumentReference taskDocRef = FirebaseManager.Instance.DB.Collection("tasks").Document(taskId);
        Task<DocumentSnapshot> getTask = taskDocRef.GetSnapshotAsync();
        yield return new WaitUntil(() => getTask.IsCompleted);

        if (getTask.IsFaulted || !getTask.Result.Exists)
        {
            Debug.LogError($"[{acrId}] Task Error: Task ID '{taskId}'를 찾거나 읽는 데 실패했습니다!");
            isWorking = false;
            yield break;
        }

        var taskData = getTask.Result.ToDictionary();

        // --- 'stops' 배열을 순회하며 작업 수행 ---
        if (taskData.TryGetValue("stops", out object stopsObj) && stopsObj is List<object> stopsList)
        {
            Debug.Log($"[{acrId}] 총 {stopsList.Count}개의 경유지가 있는 작업을 시작합니다.");

            for (int i = 0; i < stopsList.Count; i++)
            {
                var stop = stopsList[i] as Dictionary<string, object>; //firebase에서 받은 data형식이 Dictionary<string, object> 형식인지 확인 후 stop에 저장

                if (GetValueFromMap(stop, "status") == "completed") continue; // ACR이 예기치 못해서 꺼졌을 때대비

                string action = GetValueFromMap(stop, "action");
                Debug.Log($"--- 경유지 {i + 1}/{stopsList.Count} 시작: 액션 = {action} ---");

                // --- 1. 목적지로 이동 및 회전 ---
                yield return MoveAndRotateForAction(stop, action);

                // --- 2. '도착 신호' 보내고 '완료 신호' 대기 ---
                yield return SwitchToObstacleCoroutine(); // 물리 작업 중에는 장애물 역할

                // ★★★ 핵심 3: Dictionary에서 메소드를 찾아 직접 실행 ★★★
                // 더 이상 이벤트나 상태 변수, WaitUntil을 사용하지 않습니다.
                if (actionRegistry.TryGetValue(action, out PhysicalActionDelegate actionToExecute))
                {
                    Debug.Log($"[{acrId}] 물리 작업({action})을 시작합니다...");
                    // 찾아온 메소드(Delegate)를 코루틴으로 실행하고 끝날 때까지 기다립니다.
                    yield return actionToExecute(stop);
                    Debug.Log($"[{acrId}] 물리 작업({action})이 완료되었습니다.");
                }
                else
                {
                    Debug.Log($"[{acrId}] 등록되지 않았거나 물리 작업이 없는 액션({action})입니다.");
                }

                Debug.Log($"[{acrId}] 물리 작업 제어권을 넘기고 완료 신호를 대기합니다...");
                yield return new WaitUntil(() => !isPhysicalActionInProgress); // 대기 종료
                Debug.Log($"[{acrId}] 물리 작업 완료 신호를 받았습니다. 다음 경유지로 이동합니다.");

                // --- 3. 경유지 완료 처리 ---
                Task updateStopStatusTask = taskDocRef.UpdateAsync($"stops.{i}.status", "completed");
                yield return new WaitUntil(() => updateStopStatusTask.IsCompleted);
            }
        }

        // --- 작업 완료 및 복귀 단계 ---
        yield return CompleteTask(taskDocRef);
        yield return GoHome();

        isWorking = false; // 모든 작업이 끝났으므로 새로운 작업을 받을 수 있는 상태로 전환합니다.
    }

    //================================================================
    // 4. 세부 작업 흐름(액션) 코루틴: 각 경유지에서 수행할 구체적인 행동들
    //================================================================

    /*
    /// <summary>
    /// PhysicalController로부터 작업 완료 신호를 받으면 호출됩니다.
    /// </summary>
    private void HandleActionCompleted(string completedAcrId)
    {
        // 신호를 보낸 ACR이 나 자신일 때만 반응합니다.
        if (completedAcrId == this.acrId)
        {
            isPhysicalActionInProgress = false; // 대기 상태를 해제합니다.
        }
    }
    */

    /// <summary>
    /// 각 경유지의 목적지로 이동하고 회전하는 역할을 담당하는 통합 코루틴입니다.
    /// </summary>
    private IEnumerator MoveAndRotateForAction(Dictionary<string, object> stopData, string action)
    {
        Vector3 targetPosition = Vector3.zero;
        float targetRotation = transform.eulerAngles.y;

        // action 타입에 따라 목적지 좌표와 회전값을 파싱합니다.
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
                    Debug.LogError($"[{acrId}] Action '{action}'에 필요한 '{locationKey}' 맵을 찾을 수 없습니다!");
                    // 여기서 처리를 중단해야 할 수도 있습니다.
                    yield break;
                }
                break;

            case "pickup_multi":
            case "dropoff_multi":
                string stationIdKey = action == "pickup_multi" ? "sourceStationId" : "destinationStationId";
                string rotationKey = action == "pickup_multi" ? "sourceStationRotation" : "destinationStationRotation";
                string stationId = GetValueFromMap(stopData, stationIdKey);

                // StationManager에서 null을 반환할 수 있으므로 방어 코드 추가
                Transform stationTransform = StationManager.Instance.GetStationTransform(stationId);
                if (stationTransform != null)
                {
                    targetPosition = stationTransform.position;
                }
                else
                {
                    Debug.LogError($"[{acrId}] StationManager에서 ID '{stationId}'를 찾을 수 없습니다!");
                    yield break;
                }
                targetRotation = GetRotationFromTask(stopData, rotationKey);
                break;
        }

        // 계산된 목적지로 이동하고 회전합니다.
        yield return MoveToTarget(targetPosition);
        yield return StartCoroutine(RotateTowards(targetRotation));
    }

    //================================================================
    // 5. 헬퍼(Helper) 함수들: 반복되는 코드를 묶어 관리
    //================================================================

    /// <summary>
    /// 지정된 월드 좌표로 이동하는 전체 과정을 담당합니다. (회전 -> 이동 -> 도착 대기)
    /// </summary>
    private IEnumerator MoveToTarget(Vector3 targetPosition)
    {
        // <<<--- 수정 1: 목표 위치의 y값을 0으로 강제합니다. ---
        Vector3 finalTargetPosition = new Vector3(targetPosition.x, 0, targetPosition.z);

        //// ★★★ 핵심 2: 첫 번째 이동일 경우에만 특별 초기화 로직 수행 ★★★
        //if (isFirstMove)
        //{
        //    Debug.Log($"[{acrId}] 첫 번째 이동을 감지했습니다. 특별 동기화를 시작합니다.");
        //    // Agent를 활성화하고, Warp를 통해 현재 위치를 강제 동기화합니다.
        //    // 이 시점은 Start()보다 훨씬 뒤이므로 NavMesh 시스템이 준비되었을 확률이 높습니다.
        //    if (!agent.enabled) agent.enabled = true;
        //    agent.Warp(transform.position);

        //    // 다음 이동부터는 이 로직을 실행하지 않도록 플래그를 변경합니다.
        //    isFirstMove = false;

        //    // 안전을 위해 한 프레임 대기하여 Warp가 완전히 적용되도록 합니다.
        //    yield return null;
        //}
        // 1. 이동을 시작하기 전에 Agent 모드로 먼저 전환합니다.
        yield return StartCoroutine(SwitchToAgentCoroutine());

        // 2. 다음 목적지를 향해 부드럽게 회전합니다.
        // 회전 방향 계산에도 y값이 0인 좌표를 사용합니다.
        Vector3 direction = (finalTargetPosition - transform.position).normalized;
        if (direction.sqrMagnitude > 0.001f)
        {
            float targetYAngle = Quaternion.LookRotation(direction).eulerAngles.y;
            yield return StartCoroutine(RotateTowards(targetYAngle));
        }

        // 3. 상태를 'moving'으로 업데이트합니다.
        Task updateStatusTask = UpdateStatus("moving");
        yield return new WaitUntil(() => updateStatusTask.IsCompleted);

        // 4. 회전이 끝난 후, 이동을 시작합니다.
        agent.isStopped = false;
        // SetDestination에도 y값이 0인 좌표를 전달합니다.
        agent.SetDestination(finalTargetPosition);

        // 5. 도착할 때까지 대기합니다.
        yield return new WaitUntil(() => HasArrived());
    }

    /// <summary>
    /// 모든 작업이 끝난 후 Home으로 복귀하는 과정을 담당합니다.
    /// </summary>
    private IEnumerator GoHome()
    {
        Debug.Log($"[{acrId}] 시작 지점으로 복귀합니다.");
        yield return MoveToTarget(homePosition.position);

        // 필요하다면 홈에서 특정 방향을 바라보도록 회전 로직 추가 가능
        // yield return StartCoroutine(RotateTowards(0));

        Debug.Log($"[{acrId}] 시작 지점 도착. 대기 상태로 전환합니다.");
        yield return StartCoroutine(SwitchToObstacleCoroutine());
        Task idleTask = UpdateStatus("idle");
        yield return new WaitUntil(() => idleTask.IsCompleted);
    }

    /// <summary>
    /// Task 완료 상태를 Firebase에 보고합니다. (자신의 assignedTask 비우기, tasks 문서 상태 변경)
    /// </summary>
    private async Task CompleteTask(DocumentReference taskDocRef)
    {
        // 1. 자신의 'assignedTask' 필드를 null로 만들어 새로운 작업을 받을 수 있도록 함
        await acrDocRef.UpdateAsync("assignedTask", null);

        // 2. 여러 필드를 한 번에 업데이트하기 위해 Dictionary를 생성
        Dictionary<string, object> taskUpdates = new Dictionary<string, object>
    {
        { "status", "completed" },
        { "completedAt", Timestamp.GetCurrentTimestamp() }
    };

        // 3. Dictionary를 인자로 전달하여 tasks 문서를 업데이트
        await taskDocRef.UpdateAsync(taskUpdates);

        Debug.Log($"[{acrId}] Task '{taskDocRef.Id}' 완료 보고.");
    }

    /// <summary>
    /// 이 ACR의 현재 상태(status)를 Firebase에 업데이트합니다.
    /// </summary>
    private Task UpdateStatus(string newStatus)
    {
        if (acrDocRef == null) return Task.CompletedTask;
        return acrDocRef.UpdateAsync("status", newStatus);
    }

    /// <summary>
    /// AMR을 '이동 가능한 Agent' 모드로 전환합니다.
    /// </summary>
    private IEnumerator SwitchToAgentCoroutine()
    {
        // 1. Obstacle을 먼저 비활성화하여 NavMesh 복구를 시작합니다.
        if (obstacle.enabled)
        {
            obstacle.enabled = false;
        }

        // NavMesh 복구 시간을 Obstacle의 Carving Time에 맞춰주면 더욱 안정적입니다.
        // Carving Time이 0이면 한 프레임만 기다려도 충분합니다.
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
    /// AMR을 '정지한 장애물' 모드로 전환합니다.
    /// </summary>
    private IEnumerator SwitchToObstacleCoroutine()
    {
        // 1. Agent의 움직임을 멈추고 비활성화합니다.
        if (agent.enabled)
        {
            if (agent.hasPath) agent.ResetPath();
            agent.isStopped = true; // isStopped를 먼저 설정하는 것이 안전합니다.
            agent.enabled = false;
        }

        // 2. ★★★ 핵심: Agent가 시스템에서 완전히 제거될 시간을 줍니다. ★★★
        yield return new WaitForEndOfFrame();

        // 3. Agent가 사라진 후, Obstacle을 안전하게 활성화합니다.
        if (!obstacle.enabled)
        {
            obstacle.enabled = true;
        }
    }

    /// <summary>
    /// NavMeshAgent가 목적지에 도착했는지 확인합니다.
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
    /// 딕셔너리에서 특정 키의 값을 안전하게 문자열로 가져옵니다.
    /// </summary>
    private string GetValueFromMap(Dictionary<string, object> dataMap, string key)
    {
        return dataMap.TryGetValue(key, out object valueObj) ? valueObj.ToString() : string.Empty;
    }

    /// <summary>
    /// Task 데이터(최상위 딕셔너리)에서 회전 정보를 안전하게 파싱합니다.
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
    /// 하위 맵(destination, source 등)에서 회전 정보를 안전하게 파싱합니다.
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
    /// 지정된 Y축 각도로 부드럽게 회전하는 코루틴입니다.
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
        Debug.Log($"목표 방향({targetYAngle}도)으로 회전 완료.");
    }
}