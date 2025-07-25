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
}