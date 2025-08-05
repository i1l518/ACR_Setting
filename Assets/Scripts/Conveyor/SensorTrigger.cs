using UnityEngine;

public class SensorTrigger : MonoBehaviour
{
    public Renderer sensorRenderer;
    private Color originalColor;

    // Bay1 제어용 변수 추가
    public Bay1SensorTrigger bay1SensorTrigger;

    private void Start()
    {
        if (sensorRenderer == null)
            sensorRenderer = GetComponent<Renderer>();

        originalColor = sensorRenderer.material.color;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Box"))
        {
            Debug.Log("📦 박스 감지됨 → 센서 색상 노란색으로 변경");

            // 박스 상태 처리
            BoxState state = other.GetComponent<BoxState>();
            if (state != null)
                state.isProcessed = true;

            sensorRenderer.material.color = Color.yellow;

            // 📛 센서가 노란색으로 바뀌면 Bay1 정지
            if (bay1SensorTrigger != null && bay1SensorTrigger.bay1Conveyor != null)
            {
                bay1SensorTrigger.StopImmediately();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Box"))
        {
            Debug.Log("📦 박스 이탈 → 센서 색상 원래대로 복원");
            sensorRenderer.material.color = originalColor;
        }
    }
}

