using UnityEngine;
using System.Collections;


public class GrabController : MonoBehaviour
{
    [Header("파지 위치 기준점")]
    [Tooltip("박스가 파지되었을 때 위치할 기준점 오브젝트 (Grab_Anchor)")]
    public Transform grabAnchor; // <<<--- 이 변수를 추가합니다.

    private Transform detectedBox = null;

    // 기존의 heldBox를 외부에서 직접 접근 못 하도록 private _heldObject로 변경합니다. (이름에 _를 붙이는 건 관례)
    private GameObject _heldObject = null;

    // 외부에 '읽기 전용'으로 _heldObject를 노출하는 프로퍼티입니다.
    // "HeldObject의 값을 물어보면 _heldObject 값을 알려줄게" 라는 의미의 축약형 문법입니다.
    public GameObject HeldObject => _heldObject;

    public GripperController GripperController
    {
        get => default;
        set
        {
        }
    }

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
        if (detectedBox != null && _heldObject == null)
        {
            Debug.Log($"[GrabController] {detectedBox.name} 파지 실행!");
            _heldObject = detectedBox.gameObject;

            // 부모 설정 및 위치/회전 조정 시에는 .transform을 사용합니다.
            _heldObject.transform.SetParent(this.transform.parent);

            //// 1. 파지하기 직전, 박스의 원래 월드 스케일을 기억합니다.
            //Vector3 originalScale = _heldObject.lossyScale;

            //// 2. 박스의 부모를 변경합니다.
            //_heldObject.SetParent(this.transform.parent);
            

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
                _heldObject.transform.position = grabAnchor.position;
            }
            else
            {
                _heldObject.transform.localPosition = Vector3.zero;
            }
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
            _heldObject.transform.localRotation = Quaternion.Euler(90, 0, 0);
            detectedBox = null;
        }
    }

    /// <summary>
    /// 잡고 있던 오브젝트를 놓고, 그 오브젝트의 참조를 반환합니다.
    /// </summary>
    /// <returns>방금 놓은 GameObject. 아무것도 잡고 있지 않았다면 null.</returns>
    public GameObject Release()
    {
        if (_heldObject != null)
        {
            Debug.Log($"[GrabController] {_heldObject.name} 놓기 실행!");
            GameObject releasedObject = _heldObject; // 반환할 오브젝트를 임시 변수에 저장

            releasedObject.transform.SetParent(null, true); // 부모 분리
            _heldObject = null; // 내부 상태 업데이트

            return releasedObject; // 방금 놓은 오브젝트 반환
        }
        return null;
    }
}