using UnityEngine;
using System.Collections;

public class BoxSpawn : MonoBehaviour
{
    public GameObject boxPrefab;
    public float minInterval = 1f;  // �ּ� ���� ����
    public float maxInterval = 4f;  // �ִ� ���� ����
    public int totalBoxes = 10;

    private bool stopRequested = false;

    void Start()
    {
        StartCoroutine(SpawnBoxes());
    }

    public void StopSpawning()
    {
        stopRequested = true;
    }

    IEnumerator SpawnBoxes()
    {
        for (int i = 0; i < totalBoxes; i++)
        {
            if (stopRequested)
            {
                Debug.Log("�ڽ� ���� �ߴܵ� by ����");
                yield break;
            }

            Instantiate(boxPrefab, transform.position, Quaternion.identity);

            float randomDelay = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(randomDelay);
        }
    }
}