using UnityEngine;
using System.Collections;

public class BoxSpawn : MonoBehaviour
{
    public GameObject boxPrefab;
    public float minInterval = 1f;  // 최소 생성 간격
    public float maxInterval = 4f;  // 최대 생성 간격
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
                Debug.Log("박스 생성 중단됨 by 센서");
                yield break;
            }

            Instantiate(boxPrefab, transform.position, Quaternion.identity);

            float randomDelay = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(randomDelay);
        }
    }
}