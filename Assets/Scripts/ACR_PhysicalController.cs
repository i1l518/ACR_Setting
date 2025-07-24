using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ACR_PhysicalController : MonoBehaviour
{
    [Header("ACR ���� ID")]
    public string acrId = "acr_01";
    [Header("���� ������Ʈ")]
    public GripperController gripperController;
    public GrabController grabController; // GrabController ���� �߰�

    void OnEnable() { ACREvents.OnArrivedForAction += HandleArrivedForAction; }
    void OnDisable() { ACREvents.OnArrivedForAction -= HandleArrivedForAction; }

    private void HandleArrivedForAction(string id, string action, Dictionary<string, object> stopData)
    {
        if (id != this.acrId) return;
        if (action == "pickup") StartCoroutine(PickupSequence(stopData));
        else ACREvents.RaiseOnActionCompleted(this.acrId);
    }

    private IEnumerator PickupSequence(Dictionary<string, object> stopData)
    {
        Debug.Log("--- ��ǰ ȸ��(Pickup) ������ ���� ---");

        // 1�ܰ�: ����Ʈ ���
        float targetLocalHeight = 0f;
        try
        {
            var locationMap = stopData["source"] as Dictionary<string, object>;
            var posMap = locationMap["position"] as Dictionary<string, object>;
            float targetWorldHeight = Convert.ToSingle(posMap["y"]);
            targetLocalHeight = targetWorldHeight - transform.position.y;
        }
        catch (Exception e) { Debug.LogError($"��ǥ ���� �Ľ� ����: {e.Message}"); }
        yield return StartCoroutine(gripperController.MoveLiftSequence(targetLocalHeight));
        Debug.Log($"1�ܰ� (����Ʈ ���) �Ϸ�!");
        yield return new WaitForSeconds(0.5f);

        // 2�ܰ�: �����̺� ȸ�� (�� ����)
        yield return StartCoroutine(gripperController.RotateTurntableSequence(90f));
        Debug.Log($"2�ܰ� (�����̺� ȸ��) �Ϸ�!");
        yield return new WaitForSeconds(0.5f);

        // 3�ܰ�: Gripper ���� (������)
        float slideDistanceToRack = 1.0f;
        yield return StartCoroutine(gripperController.SlideGripperSequence(slideDistanceToRack));
        Debug.Log($"3�ܰ� (�����̴� ����) �Ϸ�!");

        // 4�ܰ�: ����
        grabController.Grab(); // "���!" ���
        Debug.Log("4�ܰ� (����) �Ϸ�!");
        yield return new WaitForSeconds(1.0f); // ��� ����� ���� ������

        // 5�ܰ�: Gripper ���� (�ڽ��� ������)
        yield return StartCoroutine(gripperController.SlideGripperSequence(-slideDistanceToRack));
        Debug.Log("5�ܰ� (�����̴� ����) �Ϸ�!");
        yield return new WaitForSeconds(0.5f);

        // 6�ܰ�: �����̺� ����ġ (���� ����)
        yield return StartCoroutine(gripperController.RotateTurntableSequence(0f));
        Debug.Log("6�ܰ� (�����̺� ����ġ) �Ϸ�!");
        yield return new WaitForSeconds(0.5f);

        // 7�ܰ�: Gripper ���� (���� ���� ��������)
        float slideDistanceToStorage = 0.8f;
        yield return StartCoroutine(gripperController.SlideGripperSequence(slideDistanceToStorage));
        Debug.Log("7�ܰ� (���� ���� �������� ����) �Ϸ�!");

        // 8�ܰ�: ���� (�ڽ� ����)
        grabController.Release(); // "��!" ���
        Debug.Log("8�ܰ� (����) �Ϸ�!");
        yield return new WaitForSeconds(0.5f);

        // 9�ܰ�: Gripper ���� (����ġ��)
        yield return StartCoroutine(gripperController.SlideGripperSequence(-slideDistanceToStorage));
        Debug.Log("9�ܰ� (���� ���� �������� ����) �Ϸ�!");

        Debug.Log("--- ��� ���� �۾� �Ϸ�! ACRController���� �����մϴ�. ---");
        ACREvents.RaiseOnActionCompleted(this.acrId);
    }
}