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
    /// TaskManager�κ��� Task �Ҵ� ��û�� �޴� ���� �Լ��Դϴ�.
    /// </summary>
    public void AssignTaskToIdleAcr(DocumentReference taskRef)
    {
        StartCoroutine(AssignTaskToIdleAcrCoroutine(taskRef));
    }

    /// <summary>
    /// ������ Task�� ������ ACR���� �Ҵ��ϴ� �ڷ�ƾ�Դϴ�.
    /// </summary>
    private IEnumerator AssignTaskToIdleAcrCoroutine(DocumentReference taskRef)
    {
        if (taskRef == null)
        {
            Debug.LogError("[ACRAssigner] �Ҵ��� Task�� null�Դϴ�.");
            yield break;
        }

        Query idleAcrQuery = db.Collection("ACRs").WhereEqualTo("status", "idle");
        Task<QuerySnapshot> getIdleAcrsTask = idleAcrQuery.GetSnapshotAsync();
        yield return new WaitUntil(() => getIdleAcrsTask.IsCompleted);

        if (getIdleAcrsTask.IsFaulted)
        {
            Debug.LogError("[ACRAssigner] ���� ACR ��ȸ ����!");
            yield break;
        }

        var idleAcrDocs = getIdleAcrsTask.Result.Documents.ToList();
        if (idleAcrDocs.Count == 0)
        {
            Debug.LogWarning("[ACRAssigner] ���� ������ ���� ACR�� �����ϴ�. Task�� ��� ���·� �����˴ϴ�.");
            yield break;
        }

        // TODO: ���� �� ������ ���� ACR ���� �������� ���� (��: �Ÿ� ���)
        DocumentSnapshot selectedAcrDoc = idleAcrDocs.First();
        DocumentReference selectedAcrRef = selectedAcrDoc.Reference;

        Debug.Log($"[ACRAssigner] ���� ACR �ĺ� ����: {selectedAcrDoc.Id}. Ʈ������� ���� �Ҵ��� �õ��մϴ�.");

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
            Debug.LogError($"[ACRAssigner] ACR '{selectedAcrDoc.Id}'���� Task �Ҵ� Ʈ����� �� ���� �߻�!");
            yield break;
        }

        if (assignmentSuccess)
        {
            Debug.Log($"[ACRAssigner] ���������� ACR '{selectedAcrDoc.Id}'���� Task '{taskRef.Id}'�� �Ҵ��߽��ϴ�.");
        }
        else
        {
            Debug.LogWarning($"[ACRAssigner] ACR '{selectedAcrDoc.Id}'�� �Ҵ� ���� �ٸ� �۾��� �޾ҽ��ϴ�. �� Task({taskRef.Id})�� ���� ��ȸ�� ���Ҵ�˴ϴ�.");
        }
    }
}