using UnityEngine;

public class GrabController : MonoBehaviour
{
    [Header("파지 위치 기준점")]
    [Tooltip("박스가 파지되었을 때 위치할 기준점 오브젝트 (Grab_Anchor)")]
    public Transform grabAnchor; // <<<--- 이 변수를 추가합니다.

    private Transform detectedBox = null;
    private Transform heldBox = null;

    private void OnTriggerEnter(Collider other)
    { /* 이전과 동일 */
        if (other.CompareTag("Box")) { detectedBox = other.transform; }
    }
    private void OnTriggerExit(Collider other)
    { /* 이전과 동일 */
        if (other.CompareTag("Box") && other.transform == detectedBox) { detectedBox = null; }
    }

    public void Grab()
    {
        if (detectedBox != null && heldBox == null)
        {
            Debug.Log($"[GrabController] {detectedBox.name} 파지 실행!");
            heldBox = detectedBox;

            // ▼▼▼▼▼ 스케일 문제를 완벽하게 해결하는 최종 코드입니다 ▼▼▼▼▼

            // 1. 파지하기 직전, 박스의 원래 월드 스케일을 기억합니다.
            Vector3 originalScale = heldBox.lossyScale;

            // 2. 박스의 부모를 변경합니다.
            heldBox.SetParent(this.transform.parent);
            

            // 3. 부모의 월드 스케일(lossyScale)을 가져옵니다.
            Vector3 parentScale = this.transform.lossyScale;

            // 4. 박스의 새로운 로컬 스케일을 계산합니다.
            //    (원하는 월드 스케일) / (부모의 월드 스케일) = (설정해야 할 로컬 스케일)
            //    Vector3는 컴포넌트별 나눗셈을 지원합니다.
            //heldBox.localScale = new Vector3(
            //    originalScale.x / parentScale.x,
            //    originalScale.y / parentScale.y,
            //    originalScale.z / parentScale.z
            //);

            // 5. 위치와 회전을 기준점에 맞춥니다.
            if (grabAnchor != null)
            {
                heldBox.position = grabAnchor.position;
                //heldBox.rotation = grabAnchor.rotation;
            }
            else
            {
                heldBox.localPosition = Vector3.zero;
                //heldBox.localRotation = Quaternion.identity;
            }
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
            heldBox.transform.localRotation = Quaternion.Euler(90, 0, 0);
            detectedBox = null;
        }
        // ... (else 부분은 이전과 동일) ...
    }

    // GrabController.cs
    public void Release() // 파라미터를 받지 않도록 변경
    {
        if (heldBox != null)
        {
            Debug.Log($"[GrabController] {heldBox.name} 놓기 실행!");

            // 부모를 월드(null)로 설정하여 독립적인 오브젝트로 만듭니다.
            heldBox.SetParent(null, true);

            heldBox = null;
        }
    }
}