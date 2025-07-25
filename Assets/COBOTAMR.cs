using UnityEngine;

public class AMRController : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 1.5f;
    private int currentIndex = 0;

    void Update()
    {
        if (currentIndex >= waypoints.Length) return;

        Transform target = waypoints[currentIndex];
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentIndex++;
        }
    }
}