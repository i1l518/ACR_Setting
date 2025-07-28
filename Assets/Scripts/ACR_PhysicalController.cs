// ���ϸ�: ACR_PhysicalController.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/*
// Pickup �۾��� �����ϴ� Ŭ���� (IPhysicalAction �������̽��� ����)
public class PickupAction : IPhysicalAction
{
    private ACR_PhysicalController physicalController;

    public PickupAction(ACR_PhysicalController controller)
    {
        this.physicalController = controller;
    }

    public IEnumerator Execute(Dictionary<string, object> stopData)
    {
        // ���� PickupSequence �ڵ带 �״�� ���⿡ �ٿ��ֽ��ϴ�.
        // ��, ������ ���� �����մϴ�.
        Debug.Log($"--- [{physicalController.acrId}] ��ǰ ȸ��(Pickup) ������ ���� ---");
        // ... (1�ܰ���� 9�ܰ������ ��� ����) ...
        yield return new WaitForSeconds(0.5f);
        Debug.Log($"--- [{physicalController.acrId}] ��� ���� �۾� �Ϸ�! ---");
        // �� �ڷ�ƾ�� ������ �ڵ����� ������� ACRController���� ���ư��ϴ�.
    }
}*/

public class ACR_PhysicalController : MonoBehaviour
{
    [Header("ACR ���� ID")]
    public string acrId; // Inspector���� �� ACR���� ���� ID�� �ݵ�� ��������� �մϴ�. (��: acr_01, acr_02)

    [Header("���� ������Ʈ")]
    public GripperController gripperController;
    public GrabController grabController;

    // �ڡڡ� 1. ���ο� ���� �߰� �ڡڡ�
    [Header("������ ����")]
    [Tooltip("ACR ������ ������ ��ġ��. �Ʒ��� ���Ժ��� ������� �Ҵ��ϼ���.")]
    public List<Transform> storageSlots; // Inspector���� ���� Transform���� ����

    [Tooltip("�� ������ ����ִ��� Ȯ���� �� ����� ���� ������ ũ��")]
    public Vector3 checkBoxSize = new Vector3(0.5f, 0.5f, 0.5f);

    [Tooltip("�ڽ�(Box) ������Ʈ���� ���� ���̾�")]
    public LayerMask boxLayer;

    // �ڡڡ� 2. �ű� �Լ� �ۼ�: FindEmptyStorageSlot �ڡڡ�
    /// <summary>
    /// ����ִ� ������ ������ ã���ϴ�.
    /// </summary>
    /// <param name="preferredSlotIndex">�켱������ Ȯ���ϰ� ���� ������ �ε���</param>
    /// <returns>����ִ� ������ Transform. ��� á���� null�� ��ȯ�մϴ�.</returns>
    private Transform FindEmptyStorageSlot(float preferredWorldY)
    {
        // 1. ���� ����� ���� ã��
        Transform closestSlot = null;
        float minDistance = float.MaxValue;

        // ��� ������ ��ȸ�ϸ� preferredWorldY�� Y��ǥ ���̰� ���� ���� ������ ã���ϴ�.
        foreach (Transform slot in storageSlots)
        {
            float distance = Mathf.Abs(slot.position.y - preferredWorldY);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestSlot = slot;
            }
        }

        // 2. ���� ����� ����(�켱���� ����)�� ����ִ��� Ȯ��
        if (closestSlot != null)
        {
            Collider[] colliders = new Collider[1];
            int count = Physics.OverlapBoxNonAlloc(closestSlot.position, checkBoxSize / 2, colliders, closestSlot.rotation, boxLayer);

            if (count == 0) // ����ִٸ�
            {
                Debug.Log($"[{acrId}] �ڽ��� ���� ���̿� ���� ����� ����(Y={closestSlot.position.y})�� ����־� �����մϴ�.");
                return closestSlot;
            }
        }

        // 3. �켱���� ������ �� �ִٸ�, 0������ ������� �� ������ �ٽ� ã���ϴ�.
        Debug.Log($"[{acrId}] �켱���� ������ �� �־, ���� �Ʒ����� �� ������ �ٽ� �˻��մϴ�.");
        for (int i = 0; i < storageSlots.Count; i++)
        {
            Transform currentSlot = storageSlots[i];
            Collider[] colliders = new Collider[1];
            int count = Physics.OverlapBoxNonAlloc(currentSlot.position, checkBoxSize / 2, colliders, currentSlot.rotation, boxLayer);

            if (count == 0) // ó������ �߰ߵ� �� ����
            {
                Debug.Log($"[{acrId}] ����ִ� ���� ���� #{i}��(��) ã�� �����մϴ�.");
                return currentSlot;
            }
        }

        // 4. ��� ������ �� �� ���
        Debug.LogError($"[{acrId}] ��� �����Ұ� �� á���ϴ�! ������ ������ �����ϴ�.");
        return null;
    }

    public IEnumerator PickupSequence(Dictionary<string, object> stopData)
    {
        Debug.Log($"--- [{this.acrId}] ��ǰ ȸ��(Pickup) ������ ���� ---");

        // 1�ܰ�: ����Ʈ ���
        float targetLocalHeight = 0f;
        float pickupWorldHeight = 0f; // �� �ڽ��� ���� ���� ���̸� ������ ����
        try
        {
            var locationMap = stopData["source"] as Dictionary<string, object>;
            var posMap = locationMap["position"] as Dictionary<string, object>;
            pickupWorldHeight = Convert.ToSingle(posMap["y"]);
            targetLocalHeight = pickupWorldHeight - transform.position.y;
        }
        catch (Exception e) { Debug.LogError($"��ǥ ���� �Ľ� ����: {e.Message}"); }
        yield return StartCoroutine(gripperController.MoveLiftSequence(targetLocalHeight));
        Debug.Log($"[{this.acrId}] 1�ܰ� (����Ʈ ���) �Ϸ�!");
        yield return new WaitForSeconds(0.5f);

        // 2�ܰ�: �����̺� ȸ�� (�� ����)
        yield return StartCoroutine(gripperController.RotateTurntableSequence(90f));
        Debug.Log($"[{this.acrId}] 2�ܰ� (�����̺� ȸ��) �Ϸ�!");
        yield return new WaitForSeconds(0.5f);

        // 3�ܰ�: Gripper ���� (������)
        float slideDistanceToRack = 1.0f;
        yield return StartCoroutine(gripperController.SlideGripperSequence(slideDistanceToRack));
        Debug.Log($"[{this.acrId}] 3�ܰ� (�����̴� ����) �Ϸ�!");

        // 4�ܰ�: ����
        grabController.Grab();
        Debug.Log($"[{this.acrId}] 4�ܰ� (����) �Ϸ�!");
        yield return new WaitForSeconds(1.0f);

        // 5�ܰ�: Gripper ���� (�ڽ��� ������)
        yield return StartCoroutine(gripperController.SlideGripperSequence(-slideDistanceToRack));
        Debug.Log($"[{this.acrId}] 5�ܰ� (�����̴� ����) �Ϸ�!");
        yield return new WaitForSeconds(0.5f);

        // 6�ܰ�: �����̺� ����ġ (���� ����)
        yield return StartCoroutine(gripperController.RotateTurntableSequence(0f));
        Debug.Log($"[{this.acrId}] 6�ܰ� (�����̺� ����ġ) �Ϸ�!");
        yield return new WaitForSeconds(0.5f);

        // 7�ܰ�: ����ִ� ������ ���� ã�� �� ��ġ�� �̵�
        // �ڡڡ� �ڽ��� ���� �ø� ���� ����(pickupWorldHeight)�� ���ڷ� ���� �ڡڡ�
        Transform targetSlot = FindEmptyStorageSlot(pickupWorldHeight);

        // ���� �� ������ ã�� ���ߴٸ� �������� �ߴ��ϰ� ���� ó��
        if (targetSlot == null)
        {
            Debug.LogError($"[{acrId}] ���� ����: �� ������ ���� Pickup �������� �ߴ��մϴ�.");
            yield break;
        }

        float targetLiftHeight = targetSlot.position.y - transform.position.y;
        yield return StartCoroutine(gripperController.MoveLiftSequence(targetLiftHeight));
        Debug.Log($"[{acrId}] 7�ܰ� (���� ���̷� ����Ʈ �̵�) �Ϸ�!");
        yield return new WaitForSeconds(0.5f);

        // 8�ܰ�: ������ ���� Gripper ����
        Vector3 localPosInSlider = gripperController.transform.parent.InverseTransformPoint(targetSlot.position);
        float slideDistanceToStorage = localPosInSlider.z;
        yield return StartCoroutine(gripperController.SlideGripperSequence(slideDistanceToStorage));
        Debug.Log($"[{acrId}] 8�ܰ� (�������� �����̴� ����) �Ϸ�!");

        // 9�ܰ�: ���� (�ڽ� ����)
        grabController.Release();
        Debug.Log($"[{this.acrId}] 9�ܰ� (����) �Ϸ�!");
        yield return new WaitForSeconds(0.5f);

        // 10�ܰ�: Gripper ���� (����ġ��)
        yield return StartCoroutine(gripperController.SlideGripperSequence(-slideDistanceToStorage));
        Debug.Log($"[{this.acrId}] 10�ܰ� (���� ���� �������� ����) �Ϸ�!");

        Debug.Log($"--- [{this.acrId}] ��� ���� �۾� �Ϸ�! �߾� �����ҿ� �����մϴ�. ---");
        //// '���� �۾��� ������'�� ID�� ����Ͽ� �����ҿ� ����
        //ACREvents.RaiseActionCompleted(this.acrId);
    }

    // ���߿� dropoff ����� �ʿ��ϸ� ���⿡ �߰��ϸ� �˴ϴ�.
    public IEnumerator DropoffSequence(Dictionary<string, object> stopData)
    {
        Debug.Log($"--- [{this.acrId}] ��ǰ �Ͽ�(Dropoff) ������ ���� ---");
        // ... Dropoff ���� ...
        yield return new WaitForSeconds(1.0f);
        Debug.Log($"--- [{this.acrId}] Dropoff �۾� �Ϸ�! ---");
    }



}