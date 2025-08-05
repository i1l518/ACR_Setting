using UnityEngine;

public class CrossRouter2 : MonoBehaviour
{
    [Header("���� �б� ����")]
    [Tooltip("���� �ڽ��� ���ư� ������ ����")]
    public Vector3 redDirection; // ������: (0, 0, -1)

    [Tooltip("��� �ڽ��� ���ư� ���� ����")]
    public Vector3 yellowDirection; // ����: (0, 0, 1)

    [Header("�̵� �ӵ�")]
    public float moveSpeed = 2.0f;

    // �� Ʈ���Ŵ� ������ ����� ó���մϴ�.
    private void OnTriggerStay(Collider other)
    {
        // "Box" �±װ� ���� ������Ʈ�� ��� �����մϴ�.
        if (!other.CompareTag("Box")) return;

        Rigidbody rb = other.attachedRigidbody;
        Renderer renderer = other.GetComponent<Renderer>();
        if (rb == null || renderer == null) return;

        string matName = renderer.material.name.ToLower();

        // 1. ���������� Ȯ���ϰ�, �´ٸ� �̵���ŵ�ϴ�.
        if (matName.Contains("red"))
        {
            Vector3 movement = redDirection.normalized * moveSpeed * Time.deltaTime;
            rb.MovePosition(rb.position + movement);
        }
        // 2. ��������� Ȯ���ϰ�, �´ٸ� �̵���ŵ�ϴ�.
        else if (matName.Contains("yellow"))
        {
            Vector3 movement = yellowDirection.normalized * moveSpeed * Time.deltaTime;
            rb.MovePosition(rb.position + movement);
        }
        // 3. �Ķ���, �ʷϻ� �� �� �� ��� �ڽ��� �� ��ũ��Ʈ�� �ǵ帮�� �ʰ� ������ �����մϴ�.
    }
}