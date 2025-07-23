using UnityEngine;
using System.Collections;

public class GripperController : MonoBehaviour
{
    [Header("���� ���")]
    public Transform liftMechanism;      // liftfloor
    public Transform turntableMechanism; // TurnTable_Pivot
    public Transform gripperSlider;      // Gripper_Slider
    public Transform gripperLeft;        // Gripper
    public Transform gripperRight;       // Gripper (1)

    [Header("���� �Ķ����")]
    public float liftSpeed = 0.5f;
    public float rotationSpeed = 90.0f;
    public float gripperSlideSpeed = 0.5f;
    public float gripperExtendSpeed = 0.5f;

    // ������ ���� ���� �ɼ��� �߰��մϴ� ������
    [Tooltip("üũ�ϸ� �����̴��� ����/���� ������ �ݴ�� �ٲ�ϴ�.")]
    public bool invertSlideDirection = false;

    private const float GRIPPER_EXTEND_DISTANCE = 0.5f;

    // --- LIFT ���� ---
    public IEnumerator MoveLiftSequence(float targetLocalY)
    { /* ������ ���� */
        if (liftMechanism == null) { yield break; }
        Vector3 startPos = liftMechanism.localPosition;
        Vector3 targetPos = new Vector3(startPos.x, targetLocalY, startPos.z);
        yield return StartCoroutine(MoveLocalCoroutine(liftMechanism, targetPos, liftSpeed));
    }

    // --- �����̺� ���� ---
    public IEnumerator RotateTurntableSequence(float angle)
    { /* ������ ���� */
        if (turntableMechanism == null) { yield break; }
        Quaternion startRot = turntableMechanism.localRotation;
        Quaternion targetRot = Quaternion.Euler(0, angle, 0);
        yield return StartCoroutine(RotateCoroutine(turntableMechanism, targetRot, rotationSpeed));
    }

    // --- Gripper �����̴� ��/���� ���� (������) ---
    public IEnumerator SlideGripperSequence(float distance)
    {
        if (gripperSlider == null) { Debug.LogError("Gripper Slider ���� �ȵ�!"); yield break; }
        Debug.Log($"�����̴� �̵� ����! �Ÿ�: {distance}");

        // ������ üũ�ڽ� ���� ���� �̵� ������ �����մϴ� ������
        // invertSlideDirection�� false�̸� Vector3.forward, true�̸� Vector3.back�� ����մϴ�.
        Vector3 moveDirection = invertSlideDirection ? Vector3.back : Vector3.forward;

        Vector3 startPos = gripperSlider.localPosition;
        Vector3 targetPos = startPos + moveDirection * distance;

        yield return StartCoroutine(MoveLocalCoroutine(gripperSlider, targetPos, gripperSlideSpeed));
        Debug.Log("�����̴� �̵� �Ϸ�.");
    }

    // --- Gripper ���� Ȯ��/��� ���� ---
    public IEnumerator ExtendGrippersSequence(bool extend)
    { /* ������ ���� */
        if (gripperLeft == null || gripperRight == null) { yield break; }
        Vector3 leftTarget = extend ? new Vector3(-GRIPPER_EXTEND_DISTANCE, 0, 0) : Vector3.zero;
        Vector3 rightTarget = extend ? new Vector3(GRIPPER_EXTEND_DISTANCE, 0, 0) : Vector3.zero;
        StartCoroutine(MoveLocalCoroutine(gripperLeft, leftTarget, gripperExtendSpeed));
        yield return StartCoroutine(MoveLocalCoroutine(gripperRight, rightTarget, gripperExtendSpeed));
    }


    // --- ���� Coroutines ---
    private IEnumerator MoveLocalCoroutine(Transform target, Vector3 targetLocalPos, float speed)
    { /* ������ ���� */
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
    { /* ������ ���� */
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