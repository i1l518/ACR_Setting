using UnityEngine;

public class GrabController : MonoBehaviour
{
    [Header("���� ��ġ ������")]
    [Tooltip("�ڽ��� �����Ǿ��� �� ��ġ�� ������ ������Ʈ (Grab_Anchor)")]
    public Transform grabAnchor; // <<<--- �� ������ �߰��մϴ�.

    private Transform detectedBox = null;
    private Transform heldBox = null;

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
        if (detectedBox != null && heldBox == null)
        {
            Debug.Log($"[GrabController] {detectedBox.name} ���� ����!");
            heldBox = detectedBox;

            // ������ ������ ������ �Ϻ��ϰ� �ذ��ϴ� ���� �ڵ��Դϴ� ������

            // 1. �����ϱ� ����, �ڽ��� ���� ���� �������� ����մϴ�.
            Vector3 originalScale = heldBox.lossyScale;

            // 2. �ڽ��� �θ� �����մϴ�.
            heldBox.SetParent(this.transform);

            // 3. �θ��� ���� ������(lossyScale)�� �����ɴϴ�.
            Vector3 parentScale = this.transform.lossyScale;

            // 4. �ڽ��� ���ο� ���� �������� ����մϴ�.
            //    (���ϴ� ���� ������) / (�θ��� ���� ������) = (�����ؾ� �� ���� ������)
            //    Vector3�� ������Ʈ�� �������� �����մϴ�.
            heldBox.localScale = new Vector3(
                originalScale.x / parentScale.x,
                originalScale.y / parentScale.y,
                originalScale.z / parentScale.z
            );

            // 5. ��ġ�� ȸ���� �������� ����ϴ�.
            if (grabAnchor != null)
            {
                heldBox.position = grabAnchor.position;
                heldBox.rotation = grabAnchor.rotation;
            }
            else
            {
                heldBox.localPosition = Vector3.zero;
                heldBox.localRotation = Quaternion.identity;
            }
            // ���������������������������������������������

            detectedBox = null;
        }
        // ... (else �κ��� ������ ����) ...
    }

    public void Release()
    { /* ������ ���� */
        if (heldBox != null)
        {
            heldBox.SetParent(null, true);
            heldBox = null;
        }
    }
}