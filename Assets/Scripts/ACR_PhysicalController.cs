using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Firebase.Firestore;
using System.Threading.Tasks;

public class ACR_PhysicalController : MonoBehaviour
{
    [Header("ACR 고유 ID")]
    public string acrId = "acr_01";
    [Header("연결 컴포넌트")]
    public GripperController gripperController;
    public GrabController grabController;

    private FirebaseFirestore db;

    void Start()
    {
        FirebaseManager.OnFirebaseInitialized += () => { db = FirebaseManager.Instance.DB; };
    }

    void OnEnable() { ACREvents.OnArrivedForAction += HandleArrivedForAction; }
    void OnDisable() { ACREvents.OnArrivedForAction -= HandleArrivedForAction; }

    private void HandleArrivedForAction(string id, string action, Dictionary<string, object> stopData)
    {
        if (id != this.acrId) return;
        Debug.Log($"[{acrId}] 물리 제어기: '{action}' 작업을 위한 도착 신호 수신!");

        if (action == "pickup") StartCoroutine(PickupSequence(stopData));
        else if (action == "dropoff") StartCoroutine(DropoffSequence(stopData));
        else ACREvents.RaiseOnActionCompleted(this.acrId);
    }

    private IEnumerator PickupSequence(Dictionary<string, object> stopData)
    {
        Debug.Log("--- 물품 회수(Pickup) 시퀀스 시작 ---");

        var sourceMap = stopData["source"] as Dictionary<string, object>;
        string gantryId = GetValueFromMap(sourceMap, "gantryId");
        float targetLocalHeight = 0f;
        try
        {
            var posMap = sourceMap["position"] as Dictionary<string, object>;
            targetLocalHeight = Convert.ToSingle(posMap["y"]) - transform.position.y;
        }
        catch { }

        // --- 물리적 작업 수행 ---
        yield return StartCoroutine(gripperController.MoveLiftSequence(targetLocalHeight));
        yield return StartCoroutine(gripperController.RotateTurntableSequence(90f));
        yield return StartCoroutine(gripperController.SlideGripperSequence(1.0f));
        grabController.Grab();
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gripperController.SlideGripperSequence(-1.0f));
        yield return StartCoroutine(gripperController.RotateTurntableSequence(0f));
        yield return StartCoroutine(gripperController.SlideGripperSequence(0.8f));
        grabController.Release(gripperController.liftMechanism); // liftfloor에 놓기
        yield return StartCoroutine(gripperController.SlideGripperSequence(-0.8f));

        // --- Firebase 상태 업데이트 ---
        Debug.Log($"Firebase의 {gantryId} 상태를 '비어있음(1)'으로 업데이트합니다.");
        Task updateTask = UpdateGantryStatus_Full(gantryId, 1, "NONE"); // 1: Empty
        yield return new WaitUntil(() => updateTask.IsCompleted);

        Debug.Log("--- 모든 물리 작업 완료! ACRController에게 보고합니다. ---");
        ACREvents.RaiseOnActionCompleted(this.acrId);
    }

    private IEnumerator DropoffSequence(Dictionary<string, object> stopData) { /* ... 향후 구현 ... */ yield return null; }

    private Task UpdateGantryStatus_Full(string gantryDocId, int newStatus, string newItemType)
    {
        if (db == null) return Task.CompletedTask;
        DocumentReference gantryRef = db.Collection("Gantries").Document(gantryDocId);
        Dictionary<string, object> updates = new Dictionary<string, object> {
            { "status", newStatus },
            { "itemType", newItemType }
        };
        return gantryRef.SetAsync(updates, SetOptions.MergeAll);
    }
    private string GetValueFromMap(Dictionary<string, object> dataMap, string key) => dataMap.TryGetValue(key, out object valueObj) ? valueObj.ToString() : string.Empty;
}