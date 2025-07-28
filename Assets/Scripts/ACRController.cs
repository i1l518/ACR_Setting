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
    // --- ������Ʈ ���� ---
    private NavMeshAgent agent;
    private NavMeshObstacle obstacle;
    private FirebaseFirestore db;

    // --- Inspector ���� ���� ---
    [Header("Firebase Settings")]
    public string acrId = "acr_01";
    [Header("ACR Settings")]
    public Transform homePosition;
    public float rotationSpeed = 120f;

    // --- ���� ���� ���� ---
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
        if (homePosition == null) { Debug.LogError($"[{acrId}] Home Position�� �������� �ʾҽ��ϴ�!"); }
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
                Debug.Log($"[{acrId}] ���ο� �۾�({currentTaskId})�� �Ҵ� �޾ҽ��ϴ�.");
                isWorking = true;
                StartCoroutine(ProcessMultiStopTaskCoroutine(currentTaskId));
            }
        });
    }

    private IEnumerator ProcessMultiStopTaskCoroutine(string taskId)
    {
        DocumentReference taskDocRef = db.Collection("tasks").Document(taskId);

        // await ��� Task�� �ް� �ڷ�ƾ���� ��ٸ��� ������� ����
        Task<DocumentSnapshot> getTask = taskDocRef.GetSnapshotAsync();
        yield return new WaitUntil(() => getTask.IsCompleted);

        if (getTask.IsFaulted || !getTask.Result.Exists)
        {
            Debug.LogError($"Task �б� ����: {getTask.Exception}");
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
                    Debug.Log($"[{acrId}] �۾� ���� ��, {gantryIdToUpdate} ���¸� '�۾� ��(2)'���� �����մϴ�.");
                    Task updateStatusTask = UpdateGantryStatus_OnlyStatus(gantryIdToUpdate, 2);
                    yield return new WaitUntil(() => updateStatusTask.IsCompleted);
                }

                yield return StartCoroutine(MoveAndRotateForAction(stop, action));

                SwitchToObstacle();
                isPhysicalActionInProgress = true;
                ACREvents.RaiseOnArrivedForAction(this.acrId, action, stop);
                yield return new WaitUntil(() => !isPhysicalActionInProgress);
                Debug.Log($"[{acrId}] ���� �۾� �Ϸ� ��ȣ�� �޾ҽ��ϴ�.");

                // await ��� Task�� �ް� �ڷ�ƾ���� ��ٸ��� ������� ����
                Task updateStopStatusTask_local = taskDocRef.UpdateAsync($"stops.{i}.status", "completed");
                yield return new WaitUntil(() => updateStopStatusTask_local.IsCompleted);
            }
        }

        // await ��� Task�� �ް� �ڷ�ƾ���� ��ٸ��� ������� ����
        Task completeTask = CompleteTask(taskDocRef);
        yield return new WaitUntil(() => completeTask.IsCompleted);

        yield return GoHome();
        isWorking = false;
    }

    private void HandleActionCompleted(string completedAcrId) { if (completedAcrId == this.acrId) isPhysicalActionInProgress = false; }

    private IEnumerator MoveAndRotateForAction(Dictionary<string, object> stopData, string action) { /* ... ������ ���� ... */ yield return null; }
    private IEnumerator MoveToTarget(Vector3 targetPosition) { /* ... ������ ���� ... */ yield return null; }
    private IEnumerator GoHome() { /* ... ������ ���� ... */ yield return null; }

    // async Ű���带 �����ϰ� Task�� ��ȯ�ϵ��� ����
    private Task CompleteTask(DocumentReference taskDocRef)
    {
        Task t1 = acrDocRef.UpdateAsync("assignedTask", null);
        Dictionary<string, object> taskUpdates = new Dictionary<string, object>
        {
            { "status", "completed" },
            { "completedAt", Timestamp.GetCurrentTimestamp() }
        };
        Task t2 = taskDocRef.UpdateAsync(taskUpdates);
        Debug.Log($"[{acrId}] Task '{taskDocRef.Id}' �Ϸ� ����.");
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