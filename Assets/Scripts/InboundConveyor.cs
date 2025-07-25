using UnityEngine;

public class ConveyorSensorControl : MonoBehaviour
{
    public GameObject inboundConveyor;      // 제어 대상 컨베이어
    public float conveyorSpeed = 1.0f;       // 기본 속도

    private bool isStopped = false;

    void Update()
    {
        if (!isStopped)
        {
            // 벨트 움직임 (기본 방향으로 Translate)
            inboundConveyor.transform.Translate(Vector3.forward * conveyorSpeed * Time.deltaTime);
        }
    }

    public void StopConveyor()
    {
        isStopped = true;
    }

    public void ResumeConveyor()
    {
        isStopped = false;
    }
}
