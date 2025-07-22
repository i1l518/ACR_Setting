// StationMonitor.cs
using Firebase.Firestore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StationMonitor : MonoBehaviour
{
    [Header("System References")]
    [Tooltip("���� �ִ� TaskManager ������Ʈ�� �������ּ���.")]
    public TaskManager taskManager;

    [Header("Monitoring Settings")]
    [Tooltip("�����̼� ���¸� Ȯ���ϴ� �ֱ� (��)")]
    public float checkIntervalSeconds = 10.0f;
    [Tooltip("������ ������ �� �� �̻��̸� �۾��� Ʈ�����մϴ�.")]
    public int itemCountTrigger = 5;
    [Tooltip("������ ������ �߰� �� �� �ð�(��)�� ������ �۾��� Ʈ�����մϴ�.")]
    public float timeoutMinutes = 3.0f;

    private FirebaseFirestore db;

    void Start()
    {
        if (taskManager == null)
        {
            Debug.LogError("[StationMonitor] TaskManager�� ������� �ʾҽ��ϴ�! Inspector���� �������ּ���.");
            this.enabled = false; // ��ũ��Ʈ ��Ȱ��ȭ
            return;
        }

        FirebaseManager.OnFirebaseInitialized += () => {
            db = FirebaseManager.Instance.DB;
            StartCoroutine(CheckStationsPeriodically());
        };
    }

    /// <summary>
    /// ������ �ð����� �ιٿ�� �����̼� ���¸� Ȯ���ϴ� ���� �����Դϴ�.
    /// </summary>
    private IEnumerator CheckStationsPeriodically()
    {
        while (true)
        {
            Debug.Log("[StationMonitor] �ֱ����� �ιٿ�� �����̼� ���� Ȯ���� �����մϴ�...");
            yield return StartCoroutine(CheckAndTriggerInboundTasks());
            yield return new WaitForSeconds(checkIntervalSeconds);
        }
    }

    /// <summary>
    /// ��� �ιٿ�� �����̼��� Ȯ���ϰ�, ����(����/�ð�)�� �´� �����̼ǿ� ���� Task ������ ��û�մϴ�.
    /// </summary>
    private IEnumerator CheckAndTriggerInboundTasks()
    {
        var stationsRef = db.Collection("inbound_stations");
        var now = Timestamp.GetCurrentTimestamp();
        var timeoutTimestamp = Timestamp.FromDateTime(now.ToDateTime().AddMinutes(-timeoutMinutes));

        Query quantityQuery = stationsRef.WhereEqualTo("status", "waiting").WhereGreaterThanOrEqualTo("itemCount", itemCountTrigger);
        Query timeoutQuery = stationsRef.WhereEqualTo("status", "waiting").WhereLessThanOrEqualTo("lastItemAddedAt", timeoutTimestamp);

        var getQuantityTask = quantityQuery.GetSnapshotAsync();
        var getTimeoutTask = timeoutQuery.GetSnapshotAsync();
        yield return new WaitUntil(() => getQuantityTask.IsCompleted && getTimeoutTask.IsCompleted);

        if (getQuantityTask.IsFaulted || getTimeoutTask.IsFaulted)
        {
            Debug.LogError("[StationMonitor] �����̼� ���� ��ȸ �� ������ �߻��߽��ϴ�.");
            yield break;
        }

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
            Debug.Log("[StationMonitor] ���� �۾��� ������ ������ �����̼��� �����ϴ�.");
            yield break;
        }

        foreach (var stationDoc in stationsToProcess.Values)
        {
            Debug.Log($"[StationMonitor] ���� ���� �����̼� �߰�: {stationDoc.Id}. TaskManager���� Task ������ ��û�մϴ�.");

            // TaskManager���� Task ������ �����մϴ�.
            taskManager.CreateMultiInboundTask(stationDoc);
        }
    }
}