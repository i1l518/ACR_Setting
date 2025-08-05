using UnityEngine;

public class CrossRouter : MonoBehaviour
{
    [Header("이동 방향 (월드 좌표 기준)")]
    public Vector3 rightDirection = new Vector3(1, 0, 0);
    public Vector3 leftDirection = new Vector3(-1, 0, 0);
    public Vector3 forwardDirection = new Vector3(0, 0, -1); // 기본값을 올바르게 수정

    [Header("이동 속도")]
    public float moveSpeed = 2.0f;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Box")) return;
        Rigidbody rb = other.attachedRigidbody;
        Renderer renderer = other.GetComponent<Renderer>();
        if (rb == null || renderer == null) return;

        Vector3 targetDirection;
        string matName = renderer.material.name.ToLower();

        // ★★★ 디버깅을 위해 어떤 박스가 어떤 색으로 인식되는지 로그를 추가합니다 ★★★
        if (matName.Contains("blue"))
        {
            targetDirection = rightDirection;
            Debug.Log($"[라우터] {other.name}: 'blue' 인식 -> 오른쪽 {targetDirection}으로 설정");
        }
        else if (matName.Contains("green"))
        {
            targetDirection = leftDirection;
            Debug.Log($"[라우터] {other.name}: 'green' 인식 -> 왼쪽 {targetDirection}으로 설정");
        }
        else
        {
            targetDirection = forwardDirection;
            Debug.Log($"[라우터] {other.name}: '기타 색상' 인식 -> 직진 {targetDirection}으로 설정");
        }

        Vector3 movement = targetDirection.normalized * moveSpeed * Time.deltaTime;
        rb.MovePosition(rb.position + movement);
    }
}