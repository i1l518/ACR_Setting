// ACRAssigner.cs
using Firebase.Firestore;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class ACRAssigner : MonoBehaviour
{
    private FirebaseFirestore db;

    void Start()
    {
        FirebaseManager.OnFirebaseInitialized += () => {
            db = FirebaseManager.Instance.DB;
        };
    }

    /// <summary>
    /// TaskManager로부터 Task 할당 요청을 받는 공개 함수입니다.
    /// </summary>
    public void AssignTaskToIdleAcr(DocumentReference taskRef)
    {
        StartCoroutine(AssignTaskToIdleAcrCoroutine(taskRef));
    }

    /// <summary>
    /// 생성된 Task를 가용한 ACR에게 할당하는 코루틴입니다.
    /// </summary>
    private IEnumerator AssignTaskToIdleAcrCoroutine(DocumentReference taskRef)
    {
        if (taskRef == null)
        {
            Debug.LogError("[ACRAssigner] 할당할 Task가 null입니다.");
            yield break;
        }

        Query idleAcrQuery = db.Collection("ACRs").WhereEqualTo("status", "idle");
        Task<QuerySnapshot> getIdleAcrsTask = idleAcrQuery.GetSnapshotAsync();
        yield return new WaitUntil(() => getIdleAcrsTask.IsCompleted);

        if (getIdleAcrsTask.IsFaulted)
        {
            Debug.LogError("[ACRAssigner] 유휴 ACR 조회 실패!");
            yield break;
        }

        var idleAcrDocs = getIdleAcrsTask.Result.Documents.ToList();
        if (idleAcrDocs.Count == 0)
        {
            Debug.LogWarning("[ACRAssigner] 현재 가용한 유휴 ACR이 없습니다. Task가 대기 상태로 유지됩니다.");
            yield break;
        }

        // TODO: 향후 더 정교한 최적 ACR 선정 로직으로 개선 (예: 거리 기반)
        DocumentSnapshot selectedAcrDoc = idleAcrDocs.First();
        DocumentReference selectedAcrRef = selectedAcrDoc.Reference;

        Debug.Log($"[ACRAssigner] 최적 ACR 후보 선정: {selectedAcrDoc.Id}. 트랜잭션을 통해 할당을 시도합니다.");

        bool assignmentSuccess = false;
        Task transactionTask = db.RunTransactionAsync(async transaction =>
        {
            DocumentSnapshot latestAcrSnapshot = await transaction.GetSnapshotAsync(selectedAcrRef);
            if (latestAcrSnapshot.GetValue<string>("status") == "idle")
            {
                transaction.Update(selectedAcrRef, "assignedTask", taskRef.Id);
                assignmentSuccess = true;
            }
            else
            {
                assignmentSuccess = false;
            }
        });

        yield return new WaitUntil(() => transactionTask.IsCompleted);

        if (transactionTask.IsFaulted)
        {
            Debug.LogError($"[ACRAssigner] ACR '{selectedAcrDoc.Id}'에게 Task 할당 트랜잭션 중 오류 발생!");
            yield break;
        }

        if (assignmentSuccess)
        {
            Debug.Log($"[ACRAssigner] 성공적으로 ACR '{selectedAcrDoc.Id}'에게 Task '{taskRef.Id}'를 할당했습니다.");
        }
        else
        {
            Debug.LogWarning($"[ACRAssigner] ACR '{selectedAcrDoc.Id}'는 할당 직전 다른 작업을 받았습니다. 이 Task({taskRef.Id})는 다음 기회에 재할당됩니다.");
        }
    }
}