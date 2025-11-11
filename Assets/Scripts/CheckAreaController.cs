using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CheckAreaController : MonoBehaviour
{
    public bool stationFilled;
    public PickUpAreaController pickUpAreaController;
    public GameMasterScript gameMaster;
    public GameObject pumpkinExpected;
    public GameObject candyExpected;
    public GameObject appleExpected;
    public GameObject drinkCheckSpot;
    public TrashController trashController;
    public UpgradeController upgradeController;
    private AudioSource audioSource;
    public GameObject cameras;
    public GameObject noFlashCamera;
    public GameObject flashCamera;
    public GameObject ui;
    enum CheckState
    {
        WaitingForCoffee,
        WaitingForInput,
        Good,
        Bad
    }

    private CheckState checkState = CheckState.WaitingForCoffee;
    private OnColliderClicked onColliderClicked;
    public void Awake()
    {
        stationFilled = false;
        // set all expected to inactive
        SetAllExpectedInactive();
        ui.SetActive(false);
        audioSource = GetComponent<AudioSource>();
        onColliderClicked = GetComponent<OnColliderClicked>();
        onColliderClicked.clickAction = OnCheckAreaClicked;
        noFlashCamera.SetActive(true);
        flashCamera.SetActive(false);
        cameras.SetActive(false);
    }

    void Update()
    {
        if (upgradeController.level == 1 && !cameras.activeSelf)
        {
            cameras.SetActive(true);
        }
    }

    private void SetAllExpectedInactive()
    {
        pumpkinExpected.SetActive(false);
        candyExpected.SetActive(false);
        appleExpected.SetActive(false);
    }

    private void SetExpectedType(CoffeeFlavor type)
    {
        // SetAllExpectedInactive();

        // switch (type)
        // {
        //     // case CoffeeFlavor.PumpkinSpice:
        //     //     pumpkinExpected.SetActive(true);
        //     //     break;
        //     // case CoffeeFlavor.Candy:
        //     //     candyExpected.SetActive(true);
        //     //     break;
        //     // case CoffeeFlavor.Apple:
        //     //     appleExpected.SetActive(true);
        //     //     break;
        // }
    }

    private IEnumerator PlayShutterSound()
    {
        float clipLength = audioSource.clip.length;
        audioSource.Play();
        noFlashCamera.SetActive(false);
        flashCamera.SetActive(true);
        yield return new WaitForSeconds(clipLength);
        noFlashCamera.SetActive(true);
        flashCamera.SetActive(false);
    }

    private IEnumerator CheckCoffee(GameObject coffee)
    {
        // Set coffee area to check area
        coffee.GetComponent<CoffeeController>().currentArea = CoffeeArea.CheckArea;
        gameMaster.ShowTutorialStep(3);
        stationFilled = true;
        coffee.transform.position = drinkCheckSpot.transform.position;
        CoffeeController coffeeController = coffee.GetComponent<CoffeeController>();
        SetExpectedType(coffeeController.type);
        if (upgradeController.level == 0)
        {
            checkState = CheckState.WaitingForInput;
            yield return new WaitUntil(() => checkState != CheckState.WaitingForInput);
        }
        else
        {
            yield return PlayShutterSound();
            checkState = coffeeController.type == coffeeController.filledType ? CheckState.Good : CheckState.Bad;
        }
        if (checkState == CheckState.Bad)
        {
            StartCoroutine(trashController.ThrowAwayObject(coffee));
        }
        else if (!pickUpAreaController.IsPickUpAreaFull())
        {
            pickUpAreaController.FindSpotAndOccupy(coffee);
        }
        else
        {
            Debug.Log("Pick up area is full");
        }

        // reset
        checkState = CheckState.WaitingForCoffee;
        stationFilled = false;
        SetAllExpectedInactive();


    }

    public void QueueCoffeeForCheck(GameObject coffee)
    {
        StartCoroutine(CheckCoffee(coffee));
    }

    public void OnCheckAreaClicked()
    {
        if (checkState == CheckState.WaitingForInput)
        {
            ui.SetActive(true);
        }
    }

    public void OnValidationInputClicked(string type)
    {
        if (checkState == CheckState.WaitingForInput)
        {
            if (type.Equals("good"))
            {
                checkState = CheckState.Good;
            }
            else
            {
                checkState = CheckState.Bad;
            }
            ui.SetActive(false);
        }
    }

}
