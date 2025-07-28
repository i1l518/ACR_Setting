using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Firebase.Firestore;
using System.Threading.Tasks;

public class ACR_PhysicalController : MonoBehaviour
{
    [Header("ACR ���� ID")]
    public string acrId = "acr_01";
    [Header("���� ������Ʈ")]
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
        Debug.Log($"[{acrId}] ���� �����: '{action}' �۾��� ���� ���� ��ȣ ����!");

        if (action == "pickup") StartCoroutine(PickupSequence(stopData));
        else if (action == "dropoff") StartCoroutine(DropoffSequence(stopData));
        else ACREvents.RaiseOnActionCompleted(this.acrId);
    }

    private IEnumerator PickupSequence(Dictionary<string, object> stopData)
    {
        Debug.Log("--- ��ǰ ȸ��(Pickup) ������ ���� ---");

        var sourceMap = stopData["source"] as Dictionary<string, object>;
        string gantryId = GetValueFromMap(sourceMap, "gantryId");
        float targetLocalHeight = 0f;
        try
        {
            var posMap = sourceMap["position"] as Dictionary<string, object>;
            targetLocalHeight = Convert.ToSingle(posMap["y"]) - transform.position.y;
        }
        catch { }

        // --- ������ �۾� ���� ---
        yield return StartCoroutine(gripperController.MoveLiftSequence(targetLocalHeight));
        yield return StartCoroutine(gripperController.RotateTurntableSequence(90f));
        yield return StartCoroutine(gripperController.SlideGripperSequence(1.0f));
        grabController.Grab();
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(gripperController.SlideGripperSequence(-1.0f));
        yield return StartCoroutine(gripperController.RotateTurntableSequence(0f));
        yield return StartCoroutine(gripperController.SlideGripperSequence(0.8f));
        grabController.Release(gripperController.liftMechanism); // liftfloor�� ����
        yield return StartCoroutine(gripperController.SlideGripperSequence(-0.8f));

        // --- Firebase ���� ������Ʈ ---
        Debug.Log($"Firebase�� {gantryId} ���¸� '�������(1)'���� ������Ʈ�մϴ�.");
        Task updateTask = UpdateGantryStatus_Full(gantryId, 1, "NONE"); // 1: Empty
        yield return new WaitUntil(() => updateTask.IsCompleted);

        Debug.Log("--- ��� ���� �۾� �Ϸ�! ACRController���� �����մϴ�. ---");
        ACREvents.RaiseOnActionCompleted(this.acrId);
    }

    private IEnumerator DropoffSequence(Dictionary<string, object> stopData) { /* ... ���� ���� ... */ yield return null; }

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