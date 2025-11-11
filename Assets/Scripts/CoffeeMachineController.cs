using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class CoffeeMachineController : MonoBehaviour
{
    public Transform rightGear;
    public Transform leftGear;
    public Transform intakeLocation;
    public Transform outLocation;
    public CheckAreaController checkAreaController;
    public MachineConfigController machineConfigController;
    private AudioSource audioSource;
    public bool isWorking = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public IEnumerator TurnGears(float seconds)
    {
        float time = 0;
        while (time < seconds)
        {
            rightGear.Rotate(Vector3.forward * 100 * Time.deltaTime);
            leftGear.Rotate(Vector3.forward * -100 * Time.deltaTime);
            time += Time.deltaTime;
            yield return null;
        }
    }

    public IEnumerator MakeCoffee(GameObject coffeeCup)
    {
        isWorking = true;
        audioSource.pitch = machineConfigController.GetSpeed();
        float totalTime = audioSource.clip.length / audioSource.pitch;
        StartCoroutine(TurnGears(totalTime));
        audioSource.Play();
        // set position to be intake loc
        coffeeCup.transform.position = intakeLocation.position;
        CoffeeController coffeeController = coffeeCup.GetComponent<CoffeeController>();
        Vector3 intakePos = intakeLocation.position;
        Vector3 outPos = outLocation.position;
        Vector3 halfPos = (intakePos + outPos) / 2;
        yield return coffeeController.WalkToInSecs(halfPos, totalTime / 2);
        coffeeController.FillCoffee(machineConfigController.GetAccuracy());
        yield return coffeeController.WalkToInSecs(outLocation.position, totalTime / 2);
        yield return new WaitUntil(() => !checkAreaController.stationFilled);
        checkAreaController.QueueCoffeeForCheck(coffeeCup);
        isWorking = false;
    }
}
