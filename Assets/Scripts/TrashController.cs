using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashController : MonoBehaviour
{
    private AudioSource audioSource;

    public void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public IEnumerator ThrowAwayObject(GameObject obj)
    {
        MovementController movementController = obj.GetComponent<MovementController>();

        if (movementController != null)
        {
            yield return movementController.WalkToInSecs(transform.position, 1);
        }
        audioSource.Play();
        Destroy(obj);
    }
}
