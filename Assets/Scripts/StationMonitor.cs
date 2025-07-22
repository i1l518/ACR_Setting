// StationMonitor.cs
using Firebase.Firestore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StationMonitor : MonoBehaviour
{
    [Header("System References")]
    [Tooltip("씬에 있는 TaskManager 오브젝트를 연결해주세요.")]
    public TaskManager taskManager;

    [Header("Monitoring Settings")]
    [Tooltip("스테이션 상태를 확인하는 주기 (초)")]
    public float checkIntervalSeconds = 10.0f;
    [Tooltip("아이템 수량이 이 값 이상이면 작업을 트리거합니다.")]
    public int itemCountTrigger = 5;
    [Tooltip("마지막 아이템 추가 후 이 시간(분)이 지나면 작업을 트리거합니다.")]
    public float timeoutMinutes = 3.0f;

    private FirebaseFirestore db;

    void Start()
    {
        if (taskManager == null)
        {
            Debug.LogError("[StationMonitor] TaskManager가 연결되지 않았습니다! Inspector에서 설정해주세요.");
            this.enabled = false; // 스크립트 비활성화
            return;
        }

        FirebaseManager.OnFirebaseInitialized += () => {
            db = FirebaseManager.Instance.DB;
            StartCoroutine(CheckStationsPeriodically());
        };
    }

    /// <summary>
    /// 정해진 시간마다 인바운드 스테이션 상태를 확인하는 메인 루프입니다.
    /// </summary>
    private IEnumerator CheckStationsPeriodically()
    {
        while (true)
        {
            Debug.Log("[StationMonitor] 주기적인 인바운드 스테이션 상태 확인을 시작합니다...");
            yield return StartCoroutine(CheckAndTriggerInboundTasks());
            yield return new WaitForSeconds(checkIntervalSeconds);
        }
    }

    /// <summary>
    /// 모든 인바운드 스테이션을 확인하고, 조건(수량/시간)에 맞는 스테이션에 대해 Task 생성을 요청합니다.
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
            Debug.LogError("[StationMonitor] 스테이션 상태 조회 중 오류가 발생했습니다.");
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
            Debug.Log("[StationMonitor] 현재 작업을 생성할 조건의 스테이션이 없습니다.");
            yield break;
        }

        foreach (var stationDoc in stationsToProcess.Values)
        {
            Debug.Log($"[StationMonitor] 조건 충족 스테이션 발견: {stationDoc.Id}. TaskManager에게 Task 생성을 요청합니다.");

            // TaskManager에게 Task 생성을 위임합니다.
            taskManager.CreateMultiInboundTask(stationDoc);
        }
    }
}