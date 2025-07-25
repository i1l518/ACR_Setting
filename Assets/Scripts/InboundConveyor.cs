using UnityEngine;

public class ConveyorSensorControl : MonoBehaviour
{
    public GameObject inboundConveyor;      // ���� ��� �����̾�
    public float conveyorSpeed = 1.0f;       // �⺻ �ӵ�

    private bool isStopped = false;

    void Update()
    {
        if (!isStopped)
        {
            // ��Ʈ ������ (�⺻ �������� Translate)
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
