using UnityEngine;

public class SensorTrigger : MonoBehaviour
{
    public Renderer sensorRenderer;
    private Color originalColor;

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
            Debug.Log("✅ 박스 감지됨 → 센서 색상 노란색으로 변경");

            // 박스 상태 기록 (추후용)
            BoxState state = other.GetComponent<BoxState>();
            if (state != null)
                state.isProcessed = true;

            sensorRenderer.material.color = Color.yellow;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Box"))
        {
            Debug.Log("🔄 박스 이탈 → 센서 색상 원래대로 복원");
            sensorRenderer.material.color = originalColor;
        }
    }
}
