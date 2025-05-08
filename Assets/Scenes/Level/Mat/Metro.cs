using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Metro : MonoBehaviour
{
    public Transform startWaypoint;
    public Transform endWaypoint;
    public float speed = 5f;
    public float delayTime = 2f;

    private Vector3 targetPosition;
    private bool movingToEnd = true;

    void Start()
    {
        targetPosition = endWaypoint.position; // Start by moving to the end waypoint
        StartCoroutine(MoveCar());
    }

    IEnumerator MoveCar()
    {
        while (true)
        {
            // Move the car toward the target
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
                yield return null; // Wait for the next frame
            }

            // Wait for some time at the waypoint
            yield return new WaitForSeconds(delayTime);

            // Toggle direction
            movingToEnd = !movingToEnd;
            targetPosition = movingToEnd ? endWaypoint.position : startWaypoint.position;
        }
    }
}
