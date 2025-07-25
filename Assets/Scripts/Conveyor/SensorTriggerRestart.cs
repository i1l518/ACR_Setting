using UnityEngine;

public class SensorTriggerStart : MonoBehaviour
{
    public ConveyorController targetConveyor;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Box"))
        {
            Debug.Log("✅ 센서 B 감지됨 → Bay1 작동 재개");
            if (targetConveyor != null)
                targetConveyor.isRunning = true;
        }
    }
}