using System.Collections;
using UnityEngine;

public class InboundConveyorController : MonoBehaviour
{
    public bool isRunning = true;
    public Vector3 moveDirection = Vector3.right;  // 이동 방향
    public float speed = 2f;                        // 이동 속도

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

    //public void StopForSeconds(float duration)
    //{
    //    if (gameObject.activeInHierarchy)
    //        StartCoroutine(StopCoroutine(duration));
    //}

    //private IEnumerator StopCoroutine(float duration)
    //{
    //    Debug.Log("⛔ Bay1 컨베이어 멈춤 시작");
    //    isRunning = false;
    //    yield return new WaitForSeconds(duration);
    //    isRunning = true;
    //    Debug.Log("✅ Bay1 컨베이어 다시 시작");
    //}
}
