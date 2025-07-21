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
    private string currentTaskId;               // 현재 수행 중인 Task의 ID
    private bool isPhysicalActionInProgress = false; // <<<--- 물리 작업이 진행 중인지 확인하는 플래그

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
        FirebaseManager.OnFirebaseInitialized += HandleFirebaseInitialized;

        // <<<--- '작업 완료' 이벤트를 구독합니다. ---
        ACREvents.OnActionCompleted += HandleActionCompleted;
    }
    /// <summary>
    /// 첫 번째 프레임 업데이트 전에 한 번 호출됩니다.
    /// Awake 이후에 호출되며, 다른 스크립트와의 상호작용이 필요할 때 사용됩니다.
    /// </summary>
    void Start()
    {
        if (homePosition == null)
        {
            Debug.LogError($"[{acrId}] Home Position이 설정되지 않았습니다! Inspector에서 할당해주세요.");
        }
        // 시작 시에는 움직이지 않는 장애물(Obstacle) 역할만 하도록 설정합니다.
        SwitchToObstacle();
    }

    /// <summary>
    /// 게임 오브젝트가 파괴될 때 호출됩니다.
    /// 메모리 누수를 방지하기 위해 예약했던 이벤트를 반드시 해제해야 합니다.
    /// </summary>
    void OnDestroy()
    {
        FirebaseManager.OnFirebaseInitialized -= HandleFirebaseInitialized;
        listener?.Stop();

        // <<<--- 이벤트 구독을 반드시 해제합니다. ---
        ACREvents.OnActionCompleted -= HandleActionCompleted;
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
                yield return StartCoroutine(MoveAndRotateForAction(stop, action));

                // --- 2. '도착 신호' 보내고 '완료 신호' 대기 ---
                SwitchToObstacle(); // 물리 작업 중에는 장애물 역할

                isPhysicalActionInProgress = true; // 대기 시작
                ACREvents.RaiseOnArrivedForAction(this.acrId, action, stop); // "도착했으니 작업 시작해!" 신호 전송

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
                var locationMap = stopData[action == "pickup" ? "source" : "destination"] as Dictionary<string, object>;
                var posMap = locationMap["position"] as Dictionary<string, object>;
                targetPosition = new Vector3(Convert.ToSingle(posMap["x"]), 0, Convert.ToSingle(posMap["z"]));
                targetRotation = GetRotationFromMap(locationMap, "rotation");
                break;

            case "pickup_multi":
            case "dropoff_multi":
                string stationIdKey = action == "pickup_multi" ? "sourceStationId" : "destinationStationId";
                string rotationKey = action == "pickup_multi" ? "sourceStationRotation" : "destinationStationRotation";
                string stationId = GetValueFromMap(stopData, stationIdKey);
                targetPosition = StationManager.Instance.GetStationTransform(stationId).position;
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
        SwitchToAgent();

        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction.sqrMagnitude > 0.001f)
        {
            float targetYAngle = Quaternion.LookRotation(direction).eulerAngles.y;
            yield return StartCoroutine(RotateTowards(targetYAngle));
        }

        Task updateStatusTask = UpdateStatus("moving");
        yield return new WaitUntil(() => updateStatusTask.IsCompleted);

        agent.SetDestination(targetPosition);
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
        SwitchToObstacle();
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
    private void SwitchToAgent()
    {
        if (obstacle != null && obstacle.enabled) obstacle.enabled = false;
        if (agent != null && !agent.enabled) agent.enabled = true;
    }

    /// <summary>
    /// AMR을 '정지한 장애물' 모드로 전환합니다.
    /// </summary>
    private void SwitchToObstacle()
    {
        if (agent != null && agent.enabled)
        {
            if (agent.hasPath) agent.ResetPath();
            agent.enabled = false;
        }
        if (obstacle != null && !obstacle.enabled) obstacle.enabled = true;
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
        agent.isStopped = true; 

        Quaternion targetRotation = Quaternion.Euler(0, targetYAngle, 0);

        while (Quaternion.Angle(transform.rotation, targetRotation) > 1.0f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            yield return null;
        }

        transform.rotation = targetRotation;
        Debug.Log($"목표 방향({targetYAngle}도)으로 회전 완료.");

        agent.isStopped = false; // 이동을 다시 허용합니다.
    }
}