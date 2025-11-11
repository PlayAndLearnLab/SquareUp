using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitDoorController : MonoBehaviour
{
    AudioSource audioSource;
    public AudioClip noScream;
    public AudioClip cofffeeSold;
    public GameMasterScript gameMasterScript;

    public void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Play audio source when collider is triggered
    public void OnTriggerEnter2D(Collider2D other)
    {
        GameObject obj = other.gameObject;
        CustomerController customerController = obj.GetComponent<CustomerController>();
        if (!customerController)
        {
            return;
        }

        if (customerController.IsHappy())
        {
            audioSource.clip = cofffeeSold;
            gameMasterScript.CoffeeSold();
        }
        else
        {
            audioSource.clip = noScream;
            gameMasterScript.BadCoffeeSold();
        }
        gameMasterScript.ShowTutorialStep(5);
    }
}
