using UnityEngine;

public class ConveyorController : MonoBehaviour
{
    public bool isRunning = true;
    public Vector3 moveDirection = Vector3.right;  // �̵� ����
    public float speed = 2f;                        // �̵� �ӵ�

    private void OnTriggerStay(Collider other)
    {
        if (!isRunning) return;

        if (other.CompareTag("Box"))
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null)
            {
                Vector3 movement = moveDirection.normalized * speed * Time.deltaTime;
                rb.MovePosition(rb.position + movement);
            }
        }
    }
}