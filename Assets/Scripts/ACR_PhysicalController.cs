// ���ϸ�: ACR_PhysicalController.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ACR_PhysicalController : MonoBehaviour
{
    [System.Serializable] // �ν����Ϳ��� ������ �߰�
    public class StorageSlot
    {
        public int SlotId { get; private set; }
        public Transform SlotTransform { get; private set; }
        public GameObject StoredBoxObject { get; private set; }
        public string StoredBoxId { get; private set; } // �ڽ��� ���� ID (���� �߿�!)

        public bool IsEmpty => StoredBoxObject == null;

        public StorageSlot(int id, Transform transform)
        {
            this.SlotId = id;
            this.SlotTransform = transform;
        }

        public void StoreBox(GameObject boxObject, string boxId)
        {
            StoredBoxObject = boxObject;
            StoredBoxId = boxId;
            // ������ �θ�-�ڽ� ���� ����
            boxObject.transform.SetParent(SlotTransform, true);
            boxObject.transform.localPosition = Vector3.zero;
            boxObject.transform.localRotation = Quaternion.identity;
        }

        public GameObject ReleaseBox()
        {
            GameObject boxToRelease = StoredBoxObject;
            StoredBoxObject = null;
            StoredBoxId = null;
            return boxToRelease;
        }
    }

    [Header("ACR ���� ID")]
    public string acrId; // Inspector���� �� ACR���� ���� ID�� �ݵ�� ��������� �մϴ�. (��: acr_01, acr_02)

    [Header("���� ������Ʈ")]
    public GripperController gripperController;
    public GrabController grabController;

    // �ڡڡ� 1. ���ο� ���� �߰� �ڡڡ�
    [Header("������ ����")]
    [Tooltip("ACR ������ ������ ��ġ��. �Ʒ��� ���Ժ��� ������� �Ҵ��ϼ���.")]
    public List<Transform> storageSlotTransforms; // Inspector���� ���� Transform���� ����

    private StorageSlot[] internalStorage;

    [Tooltip("�� ������ ����ִ��� Ȯ���� �� ����� ���� ������ ũ��")]
    public Vector3 checkBoxSize = new Vector3(0.75f, 0.55f, 0.85f);

    [Tooltip("�ڽ�(Box) ������Ʈ���� ���� ���̾�")]
    public LayerMask boxLayer;

    void Awake()
    {
        InitializeStorage();
    }

    /// <summary>
    /// �ν����Ϳ��� �Ҵ��� Transform���� ������� ���� ������ �ý����� �ʱ�ȭ�մϴ�.
    /// </summary>
    private void InitializeStorage()
    {
        internalStorage = new StorageSlot[storageSlotTransforms.Count];
        for (int i = 0; i < storageSlotTransforms.Count; i++)
        {
            internalStorage[i] = new StorageSlot(i, storageSlotTransforms[i]);
        }
        Debug.Log($"[{acrId}] {internalStorage.Length}���� ���� ������ �ʱ�ȭ �Ϸ�.");
    }

    /// <summary>
    /// ����ִ� ���� ������ ������ ã���ϴ�. (�� �̻� Physics�� ������� ����)
    /// </summary>
    private StorageSlot FindEmptyStorageSlot(float preferredWorldY)
    {
        // Linq�� ����� �����ϰ� ǥ��
        var emptySlots = internalStorage.Where(slot => slot.IsEmpty).ToList();

        if (emptySlots.Count == 0)
        {
            Debug.LogError($"[{acrId}] ��� �����Ұ� �� á���ϴ�!");
            return null;
        }

        // ����ִ� ���� �� Y��ǥ�� ���� ����� ������ ã��
        StorageSlot closestEmptySlot = emptySlots.OrderBy(slot => Mathf.Abs(slot.SlotTransform.position.y - preferredWorldY)).First();

        Debug.Log($"[{acrId}] ����ִ� ���� �� ���� ����� ���� #{closestEmptySlot.SlotId}��(��) �����մϴ�.");
        return closestEmptySlot;
    }

    /// <summary>
    /// Ư�� ID�� ���� �ڽ��� ����� ������ ã���ϴ�.
    /// </summary>
    private StorageSlot FindSlotContainingBox(string boxId)
    {
        foreach (var slot in internalStorage)
        {
            if (!slot.IsEmpty && slot.StoredBoxId == boxId)
            {
                return slot;
            }
        }
        Debug.LogError($"[{acrId}] ID '{boxId}'�� ���� �ڽ��� ã�� �� �����ϴ�!");
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
        StorageSlot targetSlot = FindEmptyStorageSlot(pickupWorldHeight);

        // ���� �� ������ ã�� ���ߴٸ� �������� �ߴ��ϰ� ���� ó��
        if (targetSlot == null)
        {
            Debug.LogError($"[{acrId}] ���� ����: �� ������ ���� Pickup �������� �ߴ��մϴ�.");
            yield break;
        }

        float targetLiftHeight = targetSlot.SlotTransform.position.y - transform.position.y;
        yield return StartCoroutine(gripperController.MoveLiftSequence(targetLiftHeight));
        Debug.Log($"[{this.acrId}] 7�ܰ� (���� ���̷� ����Ʈ �̵�) �Ϸ�!");
        yield return new WaitForSeconds(0.5f);

        // 8�ܰ�: ������ ���� Gripper ����
        Vector3 localPosInSlider = gripperController.gripperSlider.InverseTransformPoint(targetSlot.SlotTransform.position);
        float slideDistanceToStorage = -localPosInSlider.z;
        yield return StartCoroutine(gripperController.SlideGripperSequence(slideDistanceToStorage));
        Debug.Log($"[{acrId}] 8�ܰ� (�������� �����̴� ����) �Ϸ�!");

        // 9�ܰ�: ���� (�ڽ� ����)
        GameObject releasedBox = grabController.Release();

        // �ڽ��� ���������� ���������� Ȯ���մϴ�.
        if (releasedBox != null)
        {
            // (1) �ڽ��� ���� ID ��������
            //    - �Ʒ� �ڵ�� �ڽ� ������Ʈ�� BoxData.cs ��ũ��Ʈ�� �ְ�,
            //      �� �ȿ� public string Id { get; } ������Ƽ�� �ִٰ� ������ �����Դϴ�.
            //    - ���� ������Ʈ�� �ڽ� ID ��å�� �°� �����ؾ� �մϴ�.
            //    - ���� BoxData ��ũ��Ʈ�� ���ٸ�, �ӽ÷� releasedBox.name ���� ����� �� �ֽ��ϴ�.

            string boxId = "unknown_id"; // �⺻��
            BoxData boxData = releasedBox.GetComponent<BoxData>();
            if (boxData != null)
            {
                boxId = boxData.Id;
            }
            else
            {
                Debug.LogWarning($"[{acrId}] �ڽ��� BoxData ������Ʈ�� ���� ID�� ã�� �� �����ϴ�. ������Ʈ �̸��� ID�� ����մϴ�.");
                boxId = releasedBox.name; // �ӽù���
            }

            // (2) ���� ������(StorageSlot)�� �ڽ� ���� ����
            targetSlot.StoreBox(releasedBox, boxId);

            Debug.Log($"[{acrId}] 9�ܰ� (����) �Ϸ�! ���� #{targetSlot.SlotId}�� �ڽ� '{boxId}'�� �����߽��ϴ�.");
        }
        else
        {
            // �� ��� ��ٸ�, Gripper�� �ڽ��� �������� ������ ������ ��� �ִ� ���� �����ٴ� �ǹ��Դϴ�.
            Debug.LogWarning($"[{acrId}] 9�ܰ� (����) ����: Release()�� ȣ��Ǿ����� ��ȯ�� �ڽ��� �����ϴ�. ���� �ܰ迡 ������ �־��� �� �ֽ��ϴ�.");
        }

        yield return new WaitForSeconds(0.5f); // ���� �ܰ踦 ���� ���

        // 10�ܰ�: Gripper ���� (����ġ��)
        yield return StartCoroutine(gripperController.SlideGripperSequence(-slideDistanceToStorage));
        Debug.Log($"[{this.acrId}] 10�ܰ� (���� ���� �������� ����) �Ϸ�!");

        Debug.Log($"--- [{this.acrId}] ��� ���� �۾� �Ϸ�! �߾� �����ҿ� �����մϴ�. ---");
        //// '���� �۾��� ������'�� ID�� ����Ͽ� �����ҿ� ����
        //ACREvents.RaiseActionCompleted(this.acrId);
    }

    public IEnumerator DropoffSequence(Dictionary<string, object> stopData)
    {
        Debug.Log($"--- [{this.acrId}] ��ü ��ǰ �Ͽ�(Dropoff) ������ ���� ---");

        // 1. ������(outbound_station) ���� �Ľ�
        Transform outboundStation = null;
        try
        {
            // �� �κ��� ���� `stopData` ������ �°� �����ؾ� �մϴ�.
            // ���⼭�� ���÷� "outbound_station_01" ���� �̸��� ������Ʈ�� ã�´ٰ� �����մϴ�.
            string stationName = stopData["destination_name"] as string;
            outboundStation = GameObject.Find(stationName)?.transform;

            if (outboundStation == null)
            {
                Debug.LogError($"������ �����̼� '{stationName}'�� ã�� �� �����ϴ�! Dropoff �������� �ߴ��մϴ�.");
                yield break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"������ ���� �Ľ� ����: {e.Message}");
            yield break;
        }

        // 2. �����ҿ� �ִ� ��� �ڽ��� ��ȸ�ϸ� �Ͽ� �۾� �ݺ�
        //    Linq�� ����� �ڽ��� �ִ� ���Ը� ���͸��մϴ�.
        var filledSlots = internalStorage.Where(slot => !slot.IsEmpty).ToList();

        if (filledSlots.Count == 0)
        {
            Debug.LogWarning($"[{acrId}] �����ҿ� �Ͽ��� �ڽ��� �����ϴ�. �������� �����մϴ�.");
            yield break;
        }

        Debug.Log($"[{acrId}] �� {filledSlots.Count}���� �ڽ��� �Ͽ��մϴ�.");

        foreach (StorageSlot sourceSlot in filledSlots)
        {
            Debug.Log($"--- ���� #{sourceSlot.SlotId}�� �ڽ� '{sourceSlot.StoredBoxId}' �Ͽ� ���� ---");

            // === ���� ������: �ڽ� ������ (Pickup�� ����) ===

            // A. �ڽ��� �ִ� ���� ���̷� ����Ʈ �̵�
            float liftHeightToGrab = sourceSlot.SlotTransform.position.y - transform.position.y;
            yield return StartCoroutine(gripperController.MoveLiftSequence(liftHeightToGrab));
            yield return new WaitForSeconds(0.5f);

            // B. ������ ���� Gripper ����
            Vector3 localPosToSlot = gripperController.gripperSlider.InverseTransformPoint(sourceSlot.SlotTransform.position);
            float slideDistToGrab = localPosToSlot.z;
            yield return StartCoroutine(gripperController.SlideGripperSequence(slideDistToGrab));

            // C. �ڽ� ����
            grabController.Grab(); // �� �������� ���Կ� �ִ� �ڽ��� �����Ǿ�� ��
            yield return new WaitForSeconds(1.0f);

            // D. ���� ���� ������Ʈ: �����ҿ��� �ڽ� ����
            GameObject boxToMove = sourceSlot.ReleaseBox();
            if (boxToMove == null)
            {
                Debug.LogError($"�ɰ��� ����: ���� #{sourceSlot.SlotId}�� ������� �ʴٰ� �Ǵ�������, ReleaseBox()�� null�� ��ȯ�߽��ϴ�. �������� �ߴ��մϴ�.");
                yield break;
            }
            Debug.Log($"[{acrId}] ���� #{sourceSlot.SlotId}���� �ڽ��� ���������� ���½��ϴ�.");


            // E. Gripper ����
            yield return StartCoroutine(gripperController.SlideGripperSequence(-slideDistToGrab));
            yield return new WaitForSeconds(0.5f);

            // === ���� ������: �ڽ� �������� (Pickup�� ����) ===

            // F. ������ �����̼� ���̷� ����Ʈ �̵�
            float liftHeightToDrop = outboundStation.position.y - transform.position.y;
            yield return StartCoroutine(gripperController.MoveLiftSequence(liftHeightToDrop));
            yield return new WaitForSeconds(0.5f);

            // G. ������ �����̼� �������� �����̺� ȸ�� (��: -90�� �Ǵ� 270��)
            //    �� ������ �����̼��� ��ġ�� ���� �޶����� �մϴ�. 
            //    ���⼭�� �ӽ÷� -90���� �����մϴ�.
            yield return StartCoroutine(gripperController.RotateTurntableSequence(-90f));
            yield return new WaitForSeconds(0.5f);

            // H. �������� ���� Gripper ����
            Vector3 localPosToStation = gripperController.gripperSlider.InverseTransformPoint(outboundStation.position);
            float slideDistToDrop = localPosToStation.z;
            yield return StartCoroutine(gripperController.SlideGripperSequence(slideDistToDrop));

            // I. �ڽ� ����
            grabController.Release();
            Debug.Log($"[{acrId}] ������ �����̼ǿ� �ڽ� '{boxToMove.name}'��(��) �������ҽ��ϴ�.");
            yield return new WaitForSeconds(0.5f);

            // J. Gripper ����
            yield return StartCoroutine(gripperController.SlideGripperSequence(-slideDistToDrop));
            yield return new WaitForSeconds(0.5f);

            // K. �����̺� ����ġ (���� �۾��� ����)
            yield return StartCoroutine(gripperController.RotateTurntableSequence(0f));
        }

        Debug.Log($"--- [{this.acrId}] ��� ��ǰ �Ͽ�(Dropoff) �Ϸ�! ---");
        //ACREvents.RaiseActionCompleted(this.acrId); // �����ҿ� �۾� �Ϸ� ����
    }

    //�������� �ð�ȭ
    private void OnDrawGizmos()
    {
        // Application.isPlaying�� ���� ���� ������ ���� ������ Ȯ���մϴ�.
        // ���� ���̰�, internalStorage�� ���������� �ʱ�ȭ�Ǿ��ٸ�...
        if (Application.isPlaying && internalStorage != null)
        {
            // ��Ÿ�� �����͸� ����Ͽ� �� ������ ���¸� �ð�ȭ�մϴ�.
            foreach (StorageSlot slot in internalStorage)
            {
                // ������ Transform�� ��ȿ���� Ȯ���մϴ�.
                if (slot.SlotTransform != null)
                {
                    // �ڡڡ� ������ ���������(IsEmpty) ���, �������� ���������� �׸��ϴ�. �ڡڡ�
                    Gizmos.color = slot.IsEmpty ? Color.green : Color.red;
                    Gizmos.DrawWireCube(slot.SlotTransform.position, checkBoxSize);
                }
            }
        }
        // ������ ���� ���� �ƴ� �� (Edit ����� ��)
        else
        {
            // �ν����Ϳ� �Ҵ�� Transform ������ ����� ��������� �׸��ϴ�.
            if (storageSlotTransforms == null) return;

            Gizmos.color = Color.yellow;
            foreach (Transform slotTransform in storageSlotTransforms)
            {
                if (slotTransform != null)
                {
                    Gizmos.DrawWireCube(slotTransform.position, checkBoxSize);
                }
            }
        }
    }
}