using Firebase.Firestore;
using System;
using UnityEngine;
using UnityEngine.AI;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent), typeof(NavMeshObstacle))]
public class ACRController : MonoBehaviour
{
    // --- 컴포넌트 변수 ---
    private NavMeshAgent agent;
    private NavMeshObstacle obstacle;
    private FirebaseFirestore db;

    // --- Inspector 설정 변수 ---
    [Header("Firebase Settings")]
    public string acrId = "acr_01";
    [Header("ACR Settings")]
    public Transform homePosition;
    public float rotationSpeed = 120f;

    // --- 내부 상태 변수 ---
    private DocumentReference acrDocRef;
    private ListenerRegistration listener;
    private bool isWorking = false;
    private string currentTaskId;
    private bool isPhysicalActionInProgress = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();
        FirebaseManager.OnFirebaseInitialized += () => {
            db = FirebaseManager.Instance.DB;
            SetupFirestoreListener();
        };
        ACREvents.OnActionCompleted += HandleActionCompleted;
    }

    void Start()
    {
        if (homePosition == null) { Debug.LogError($"[{acrId}] Home Position이 설정되지 않았습니다!"); }
        SwitchToObstacle();
    }

    void OnDestroy()
    {
        listener?.Stop();
        ACREvents.OnActionCompleted -= HandleActionCompleted;
    }

    private void SetupFirestoreListener()
    {
        if (db == null) { Debug.LogError("Firestore DB is not initialized."); return; }
        acrDocRef = db.Collection("ACRs").Document(acrId);
        listener = acrDocRef.Listen(snapshot =>
        {
            if (isWorking) return;
            if (snapshot.Exists && snapshot.ToDictionary().TryGetValue("assignedTask", out object taskIdObj) && taskIdObj != null)
            {
                currentTaskId = taskIdObj.ToString();
                Debug.Log($"[{acrId}] 새로운 작업({currentTaskId})을 할당 받았습니다.");
                isWorking = true;
                StartCoroutine(ProcessMultiStopTaskCoroutine(currentTaskId));
            }
        });
    }

    private IEnumerator ProcessMultiStopTaskCoroutine(string taskId)
    {
        DocumentReference taskDocRef = db.Collection("tasks").Document(taskId);

        // await 대신 Task를 받고 코루틴에서 기다리는 방식으로 수정
        Task<DocumentSnapshot> getTask = taskDocRef.GetSnapshotAsync();
        yield return new WaitUntil(() => getTask.IsCompleted);

        if (getTask.IsFaulted || !getTask.Result.Exists)
        {
            Debug.LogError($"Task 읽기 실패: {getTask.Exception}");
            isWorking = false;
            yield break;
        }

        var taskData = getTask.Result.ToDictionary();
        if (taskData.TryGetValue("stops", out object stopsObj) && stopsObj is List<object> stopsList)
        {
            for (int i = 0; i < stopsList.Count; i++)
            {
                var stop = stopsList[i] as Dictionary<string, object>;
                if (GetValueFromMap(stop, "status") == "completed") continue;
                string action = GetValueFromMap(stop, "action");

                string gantryIdToUpdate = GetGantryIdFromStop(stop);
                if (!string.IsNullOrEmpty(gantryIdToUpdate))
                {
                    Debug.Log($"[{acrId}] 작업 시작 전, {gantryIdToUpdate} 상태를 '작업 중(2)'으로 변경합니다.");
                    Task updateStatusTask = UpdateGantryStatus_OnlyStatus(gantryIdToUpdate, 2);
                    yield return new WaitUntil(() => updateStatusTask.IsCompleted);
                }

                yield return StartCoroutine(MoveAndRotateForAction(stop, action));

                SwitchToObstacle();
                isPhysicalActionInProgress = true;
                ACREvents.RaiseOnArrivedForAction(this.acrId, action, stop);
                yield return new WaitUntil(() => !isPhysicalActionInProgress);
                Debug.Log($"[{acrId}] 물리 작업 완료 신호를 받았습니다.");

                // await 대신 Task를 받고 코루틴에서 기다리는 방식으로 수정
                Task updateStopStatusTask_local = taskDocRef.UpdateAsync($"stops.{i}.status", "completed");
                yield return new WaitUntil(() => updateStopStatusTask_local.IsCompleted);
            }
        }

        // await 대신 Task를 받고 코루틴에서 기다리는 방식으로 수정
        Task completeTask = CompleteTask(taskDocRef);
        yield return new WaitUntil(() => completeTask.IsCompleted);

        yield return GoHome();
        isWorking = false;
    }

    private void HandleActionCompleted(string completedAcrId) { if (completedAcrId == this.acrId) isPhysicalActionInProgress = false; }

    private IEnumerator MoveAndRotateForAction(Dictionary<string, object> stopData, string action) { /* ... 이전과 동일 ... */ yield return null; }
    private IEnumerator MoveToTarget(Vector3 targetPosition) { /* ... 이전과 동일 ... */ yield return null; }
    private IEnumerator GoHome() { /* ... 이전과 동일 ... */ yield return null; }

    // async 키워드를 제거하고 Task를 반환하도록 수정
    private Task CompleteTask(DocumentReference taskDocRef)
    {
        Task t1 = acrDocRef.UpdateAsync("assignedTask", null);
        Dictionary<string, object> taskUpdates = new Dictionary<string, object>
        {
            { "status", "completed" },
            { "completedAt", Timestamp.GetCurrentTimestamp() }
        };
        Task t2 = taskDocRef.UpdateAsync(taskUpdates);
        Debug.Log($"[{acrId}] Task '{taskDocRef.Id}' 완료 보고.");
        return Task.WhenAll(t1, t2);
    }

    private Task UpdateStatus(string newStatus) { if (acrDocRef == null) return Task.CompletedTask; return acrDocRef.UpdateAsync("status", newStatus); }
    private void SwitchToAgent() { if (obstacle != null && obstacle.enabled) obstacle.enabled = false; if (agent != null && !agent.enabled) agent.enabled = true; }
    private void SwitchToObstacle() { if (agent != null && agent.enabled) { if (agent.hasPath) agent.ResetPath(); agent.enabled = false; } if (obstacle != null && !obstacle.enabled) obstacle.enabled = true; }
    private bool HasArrived() { if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance) { return !agent.hasPath || agent.velocity.sqrMagnitude == 0f; } return false; }
    private string GetValueFromMap(Dictionary<string, object> dataMap, string key) { return dataMap.TryGetValue(key, out object valueObj) ? valueObj.ToString() : string.Empty; }
    private float GetRotationFromTask(Dictionary<string, object> taskData, string key) { if (taskData.TryGetValue(key, out object rotObj) && rotObj is Dictionary<string, object> rotMap) { return GetRotationFromMap(rotMap, "y"); } return transform.eulerAngles.y; }
    private float GetRotationFromMap(Dictionary<string, object> dataMap, string key) { if (dataMap.TryGetValue(key, out object rotObj) && rotObj is Dictionary<string, object> rotMap) { if (rotMap.TryGetValue("y", out object yObj)) return Convert.ToSingle(yObj); } else if (dataMap.TryGetValue(key, out object yObj)) { return Convert.ToSingle(yObj); } return transform.eulerAngles.y; }
    private IEnumerator RotateTowards(float targetYAngle) { if (agent.enabled) agent.isStopped = true; Quaternion targetRotation = Quaternion.Euler(0, targetYAngle, 0); while (Quaternion.Angle(transform.rotation, targetRotation) > 1.0f) { transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime); yield return null; } transform.rotation = targetRotation; if (agent.enabled) agent.isStopped = false; }
    private string GetGantryIdFromStop(Dictionary<string, object> stopData) { string locationKey = stopData.ContainsKey("source") ? "source" : "destination"; if (stopData.TryGetValue(locationKey, out object locObj) && locObj is Dictionary<string, object> locationMap) return GetValueFromMap(locationMap, "gantryId"); return string.Empty; }
    private Task UpdateGantryStatus_OnlyStatus(string gantryDocId, int newStatus) { if (db == null) return Task.CompletedTask; DocumentReference gantryRef = db.Collection("Gantries").Document(gantryDocId); return gantryRef.UpdateAsync("status", newStatus); }
}