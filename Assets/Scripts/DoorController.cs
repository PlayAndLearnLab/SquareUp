using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    AudioSource audioSource;
    public void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Play audio source when collider is triggered
    public void OnTriggerEnter2D(Collider2D other)
    {
        audioSource.Play();
    }
}
