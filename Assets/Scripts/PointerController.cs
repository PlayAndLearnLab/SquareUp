using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointerController : MonoBehaviour
{
    private Vector3 direction;
    private Vector3 startPosition;
    private float speed = 30f;

    private int step = 0;
    private int maxSteps = 400;

    void Start()
    {
        direction = transform.up;
        startPosition = transform.position;
    }

    // move the pointer forward and backward
    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
        if (step % maxSteps == 0)
        {
            direction = -direction;
        }
        step++;
    }
}
