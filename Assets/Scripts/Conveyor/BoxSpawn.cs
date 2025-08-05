using UnityEngine;
using System.Collections;

public class BoxSpawn : MonoBehaviour
{
    public GameObject boxPrefab;
    public float spawnInterval = 2f;  // 박스 생성 간격 (초)
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
                Debug.Log("📦 박스 생성 중단됨 by 센서");
                yield break;
            }

            Instantiate(boxPrefab, transform.position, Quaternion.identity);
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}