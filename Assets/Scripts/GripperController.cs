using UnityEngine;
using System.Collections;

public class GripperController : MonoBehaviour
{
    [Header("제어 대상")]
    public Transform liftMechanism;      // liftfloor
    public Transform turntableMechanism; // TurnTable_Pivot
    public Transform gripperSlider;      // Gripper_Slider
    public Transform gripperLeft;        // Gripper
    public Transform gripperRight;       // Gripper (1)
    public Transform boxparents;

    [Header("제어 파라미터")]
    public float liftSpeed = 0.5f;
    public float rotationSpeed = 90.0f;
    public float gripperSlideSpeed = 0.5f;
    public float gripperExtendSpeed = 0.5f;

    // ▼▼▼▼▼ 방향 반전 옵션을 추가합니다 ▼▼▼▼▼
    [Tooltip("체크하면 슬라이더의 전진/후진 방향이 반대로 바뀝니다.")]
    public bool invertSlideDirection = false;

    private const float GRIPPER_EXTEND_DISTANCE = 0.5f;

    public ACR_PhysicalController ACR_PhysicalController
    {
        get => default;
        set
        {
        }
    }

    // --- LIFT 제어 ---
    public IEnumerator MoveLiftSequence(float targetLocalY)
    { /* 이전과 동일 */
        if (liftMechanism == null) { yield break; }
        Vector3 startPos = liftMechanism.localPosition;
        Vector3 targetPos = new Vector3(startPos.x, targetLocalY, startPos.z);
        yield return StartCoroutine(MoveLocalCoroutine(liftMechanism, targetPos, liftSpeed));
    }

    // --- 턴테이블 제어 ---
    public IEnumerator RotateTurntableSequence(float angle)
    { /* 이전과 동일 */
        if (turntableMechanism == null) { yield break; }
        Quaternion startRot = turntableMechanism.localRotation;
        Quaternion targetRot = Quaternion.Euler(0, angle, 0);
        yield return StartCoroutine(RotateCoroutine(turntableMechanism, targetRot, rotationSpeed));
    }

    // --- Gripper 슬라이더 전/후진 제어 (수정됨) ---
    public IEnumerator SlideGripperSequence(float distance)
    {
        if (gripperSlider == null) { Debug.LogError("Gripper Slider 연결 안됨!"); yield break; }
        Debug.Log($"슬라이더 이동 시작! 거리: {distance}");

        // ▼▼▼▼▼ 체크박스 값에 따라 이동 방향을 결정합니다 ▼▼▼▼▼
        // invertSlideDirection이 false이면 Vector3.forward, true이면 Vector3.back을 사용합니다.
        Vector3 moveDirection = invertSlideDirection ? Vector3.back : Vector3.forward;

        Vector3 startPos = gripperSlider.localPosition;
        Vector3 targetPos = startPos + moveDirection * distance;

        yield return StartCoroutine(MoveLocalCoroutine(gripperSlider, targetPos, gripperSlideSpeed));
        Debug.Log("슬라이더 이동 완료.");
    }

    /// <summary>
    /// 특정 월드 좌표를 향해 턴테이블을 회전시킵니다.
    /// </summary>
    public IEnumerator RotateTowards(Vector3 worldPosition)
    {
        // 로봇의 위치에서 목표 지점을 바라보는 방향 벡터를 계산합니다. (Y축은 무시)
        Vector3 directionToTarget = worldPosition - transform.position;
        directionToTarget.y = 0;

        // 턴테이블의 '앞쪽' 방향(local z-axis)이 이 방향 벡터와 일치하도록 하는 회전값을 계산합니다.
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        // 이미 구현된 RotateTurntableSequence를 재사용하거나,
        // 아래처럼 직접 Lerp를 구현할 수 있습니다.
        // 여기서는 기존 함수를 재사용하기 위해 목표 각도(Y-euler)만 추출합니다.
        float targetYAngle = targetRotation.eulerAngles.y;

        // 턴테이블 메커니즘의 현재 로컬 회전값을 기준으로 목표 각도를 계산해야 할 수도 있습니다.
        // 아래는 간단한 버전입니다.
        yield return StartCoroutine(RotateTurntableSequence(targetYAngle));
    }

    // --- Gripper 집게 확장/축소 제어 ---
    public IEnumerator ExtendGrippersSequence(bool extend)
    { /* 이전과 동일 */
        if (gripperLeft == null || gripperRight == null) { yield break; }
        Vector3 leftTarget = extend ? new Vector3(-GRIPPER_EXTEND_DISTANCE, 0, 0) : Vector3.zero;
        Vector3 rightTarget = extend ? new Vector3(GRIPPER_EXTEND_DISTANCE, 0, 0) : Vector3.zero;
        StartCoroutine(MoveLocalCoroutine(gripperLeft, leftTarget, gripperExtendSpeed));
        yield return StartCoroutine(MoveLocalCoroutine(gripperRight, rightTarget, gripperExtendSpeed));
    }


    // --- 공용 Coroutines ---
    private IEnumerator MoveLocalCoroutine(Transform target, Vector3 targetLocalPos, float speed)
    { /* 이전과 동일 */
        Vector3 startPos = target.localPosition;
        float dist = Vector3.Distance(startPos, targetLocalPos);
        if (dist < 0.01f) yield break;
        float time = dist / speed;
        float elapsed = 0f;
        while (elapsed < time)
        {
            target.localPosition = Vector3.Lerp(startPos, targetLocalPos, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.localPosition = targetLocalPos;
    }
    private IEnumerator RotateCoroutine(Transform target, Quaternion targetLocalRot, float speed)
    { /* 이전과 동일 */
        Quaternion startRot = target.localRotation;
        float angle = Quaternion.Angle(startRot, targetLocalRot);
        if (angle < 1.0f) yield break;
        float time = angle / speed;
        float elapsed = 0f;
        while (elapsed < time)
        {
            target.localRotation = Quaternion.Slerp(startRot, targetLocalRot, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.localRotation = targetLocalRot;
    }
}