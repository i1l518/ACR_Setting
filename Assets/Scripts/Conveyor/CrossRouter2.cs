using UnityEngine;

public class CrossRouter2 : MonoBehaviour
{
    [Header("최종 분기 방향")]
    [Tooltip("빨간 박스가 나아갈 오른쪽 방향")]
    public Vector3 redDirection; // 오른쪽: (0, 0, -1)

    [Tooltip("노란 박스가 나아갈 왼쪽 방향")]
    public Vector3 yellowDirection; // 왼쪽: (0, 0, 1)

    [Header("이동 속도")]
    public float moveSpeed = 2.0f;

    // 이 트리거는 빨강과 노랑만 처리합니다.
    private void OnTriggerStay(Collider other)
    {
        // "Box" 태그가 없는 오브젝트는 즉시 무시합니다.
        if (!other.CompareTag("Box")) return;

        Rigidbody rb = other.attachedRigidbody;
        Renderer renderer = other.GetComponent<Renderer>();
        if (rb == null || renderer == null) return;

        string matName = renderer.material.name.ToLower();

        // 1. 빨간색인지 확인하고, 맞다면 이동시킵니다.
        if (matName.Contains("red"))
        {
            Vector3 movement = redDirection.normalized * moveSpeed * Time.deltaTime;
            rb.MovePosition(rb.position + movement);
        }
        // 2. 노란색인지 확인하고, 맞다면 이동시킵니다.
        else if (matName.Contains("yellow"))
        {
            Vector3 movement = yellowDirection.normalized * moveSpeed * Time.deltaTime;
            rb.MovePosition(rb.position + movement);
        }
        // 3. 파란색, 초록색 등 그 외 모든 박스는 이 스크립트가 건드리지 않고 완전히 무시합니다.
    }
}