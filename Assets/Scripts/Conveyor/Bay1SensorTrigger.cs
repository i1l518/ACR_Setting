using UnityEngine;
using System.Collections;

public class Bay1SensorTrigger : MonoBehaviour
{
    public InboundConveyorController bay1Conveyor;
    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        if (other.CompareTag("Box"))
        {
            Debug.Log("📦 센서 감지됨 → Bay1 컨베이어 정지");
            StartCoroutine(StopAndResume());
        }
    }

    private IEnumerator StopAndResume()
    {
        isTriggered = true;

        if (bay1Conveyor != null)
            bay1Conveyor.isRunning = false;

        yield return new WaitForSeconds(5f);

        if (bay1Conveyor != null)
            bay1Conveyor.isRunning = true;

        Debug.Log("✅ Bay1 컨베이어 재작동");
        isTriggered = false;
    }

    // ✅ SensorTrigger에서 직접 호출하는 즉시 정지용 메서드
    public void StopImmediately()
    {
        StopAllCoroutines();  // 현재 진행 중인 StopAndResume 중단
        isTriggered = true;

        if (bay1Conveyor != null)
            bay1Conveyor.isRunning = false;

        Debug.Log("⛔ SensorTrigger에 의해 Bay1 컨베이어 정지됨");

        // 필요 시 자동 재작동 다시 시작하려면 다음 줄 추가
        StartCoroutine(StopAndResume());
    }
}