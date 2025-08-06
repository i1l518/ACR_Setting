using UnityEngine;
using System.Collections;


public class GrabController : MonoBehaviour
{
    [Header("���� ��ġ ������")]
    [Tooltip("�ڽ��� �����Ǿ��� �� ��ġ�� ������ ������Ʈ (Grab_Anchor)")]
    public Transform grabAnchor; // <<<--- �� ������ �߰��մϴ�.

    private Transform detectedBox = null;

    // ������ heldBox�� �ܺο��� ���� ���� �� �ϵ��� private _heldObject�� �����մϴ�. (�̸��� _�� ���̴� �� ����)
    private GameObject _heldObject = null;

    // �ܺο� '�б� ����'���� _heldObject�� �����ϴ� ������Ƽ�Դϴ�.
    // "HeldObject�� ���� ����� _heldObject ���� �˷��ٰ�" ��� �ǹ��� ����� �����Դϴ�.
    public GameObject HeldObject => _heldObject;

    public GripperController GripperController
    {
        get => default;
        set
        {
        }
    }

    private void OnTriggerEnter(Collider other)
    { /* ������ ���� */
        if (other.CompareTag("Box")) { detectedBox = other.transform; }
    }
    private void OnTriggerExit(Collider other)
    { /* ������ ���� */
        if (other.CompareTag("Box") && other.transform == detectedBox) { detectedBox = null; }
    }

    public void Grab()
    {
        if (detectedBox != null && _heldObject == null)
        {
            Debug.Log($"[GrabController] {detectedBox.name} ���� ����!");
            _heldObject = detectedBox.gameObject;

            // �θ� ���� �� ��ġ/ȸ�� ���� �ÿ��� .transform�� ����մϴ�.
            _heldObject.transform.SetParent(this.transform.parent);

            //// 1. �����ϱ� ����, �ڽ��� ���� ���� �������� ����մϴ�.
            //Vector3 originalScale = _heldObject.lossyScale;

<<<<<<< HEAD
            // 2. �ڽ��� �θ� �����մϴ�.
            heldBox.SetParent(this.transform.parent);
=======
            //// 2. �ڽ��� �θ� �����մϴ�.
            //_heldObject.SetParent(this.transform.parent);
>>>>>>> 3aeb94fa4e3f8765644417a539cdd19ca9f1e24c
            

            // 3. �θ��� ���� ������(lossyScale)�� �����ɴϴ�.
            Vector3 parentScale = this.transform.lossyScale;

            // 4. �ڽ��� ���ο� ���� �������� ����մϴ�.
            //    (���ϴ� ���� ������) / (�θ��� ���� ������) = (�����ؾ� �� ���� ������)
            //    Vector3�� ������Ʈ�� �������� �����մϴ�.
            //heldBox.localScale = new Vector3(
            //    originalScale.x / parentScale.x,
            //    originalScale.y / parentScale.y,
            //    originalScale.z / parentScale.z
            //);

            // 5. ��ġ�� ȸ���� �������� ����ϴ�.
            if (grabAnchor != null)
            {
<<<<<<< HEAD
                heldBox.position = grabAnchor.position;
                //heldBox.rotation = grabAnchor.rotation;
            }
            else
            {
                heldBox.localPosition = Vector3.zero;
                //heldBox.localRotation = Quaternion.identity;
            }
            // ���������������������������������������������
            heldBox.transform.localRotation = Quaternion.Euler(90, 0, 0);
=======
                _heldObject.transform.position = grabAnchor.position;
            }
            else
            {
                _heldObject.transform.localPosition = Vector3.zero;
            }
            // ���������������������������������������������
            _heldObject.transform.localRotation = Quaternion.Euler(90, 0, 0);
>>>>>>> 3aeb94fa4e3f8765644417a539cdd19ca9f1e24c
            detectedBox = null;
        }
    }

<<<<<<< HEAD
  public void Release(Transform newParent)
    {
        if (heldBox != null)
        {
            Debug.Log($"[GrabController] {heldBox.name} ���� ����! ���ο� �θ�: {(newParent != null ? newParent.name : "World")}");
            
            // �ڽ��� �θ� ���޹��� newParent�� �����մϴ�.
            // worldPositionStays�� true�� �Ͽ�, ���� ���� ��ġ�� �״�� �����ϸ� �θ� �ٲߴϴ�.
            heldBox.SetParent(newParent, true);
            

            // ��� �ִ� �ڽ� ������ ���ϴ�.
            heldBox = null;
=======
    /// <summary>
    /// ��� �ִ� ������Ʈ�� ����, �� ������Ʈ�� ������ ��ȯ�մϴ�.
    /// </summary>
    /// <returns>��� ���� GameObject. �ƹ��͵� ��� ���� �ʾҴٸ� null.</returns>
    public GameObject Release()
    {
        if (_heldObject != null)
        {
            Debug.Log($"[GrabController] {_heldObject.name} ���� ����!");
            GameObject releasedObject = _heldObject; // ��ȯ�� ������Ʈ�� �ӽ� ������ ����

            releasedObject.transform.SetParent(null, true); // �θ� �и�
            _heldObject = null; // ���� ���� ������Ʈ

            return releasedObject; // ��� ���� ������Ʈ ��ȯ
>>>>>>> 3aeb94fa4e3f8765644417a539cdd19ca9f1e24c
        }
        return null;
    }
}