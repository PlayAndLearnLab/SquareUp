using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using System;

public class GameMasterScript : MonoBehaviour
{
    public GameObject orderStationArea;
    public TextMeshProUGUI gameMoneyText;
    public TextMeshProUGUI quizMoneyText;
    private GameObject coffeePrepObject;

    public JanetController janetController;

    public GameObject QuizUI;
    public GameObject GameUI;
    public GameObject UpgradeUI;

    //Customer prefab
    public GameObject customerPrefab;
    public GameObject coffeeCupPrefab;

    public Transform customerEntrance;

    public LineController coffeeLineController;
    public LineController waitLineController;
    public CoffeeMachineController coffeeMachineController;
    public PickUpAreaController pickUpAreaController;
    public TrashController trashController;
    public Order orders;
    public GameObject pickUpUI;
    public QuizController quizController;
    public TutorialController tutorialController;
    public GameObject tutorialUI;
    private bool tutorialFinished = false;
    public GameObject SelectedCoffee = null;
    public GameObject nextWaveButton;
    public Wave[] waves;


    public int money = 10000;
    public void Awake()
    {
        coffeePrepObject = GameObject.Find("unmade_coffee_prepped");

        // game ui to active
        ShowGameUI();


        pickUpUI.SetActive(false);
        nextWaveButton.SetActive(false);
    }

    public void Start()
    {
        tutorialController.HideAllTutorialSteps();
        tutorialController.ShowTutorialStep(10);

        // Subscribe to money events
        if (EventManager.current != null)
        {
            EventManager.current.onMoneyGained += AddMoney;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (EventManager.current != null)
        {
            EventManager.current.onMoneyGained -= AddMoney;
        }
    }

    public void AddMoney(int amount)
    {
        money += amount;
        waveCount++;
        gameMoneyText.text = money.ToString();
        quizMoneyText.text = money.ToString();
    }

    public bool RemoveMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            gameMoneyText.text = money.ToString();
            quizMoneyText.text = money.ToString();
            return true;
        }
        return false;
    }

    public void BadCoffeeSold()
    {
        waveCount++;
        RemoveMoney(5);
        janetController.CoffeeFail();
    }

    public int GetMoney()
    {
        return money;
    }

    private int waveIndex = 0;
    private IEnumerator ExecuteWave()
    {
        foreach (Customer customer in waves[waveIndex].customers)
        {
            SpawnCustomer(customer);
            yield return new WaitForSeconds(waves[waveIndex].timeBetweenCustomers);
        }
        yield return new WaitUntil(() => waitLineController.GetLineLength() == 0 && waveCount >= waves[waveIndex].customers.Length);
        yield return new WaitForSeconds(2);
        waveIndex++;
        if (tutorialFinished)
        {
            nextWaveButton.SetActive(true);
        }
    }
    private int waveCount = 0;
    public void StartNextWave()
    {
        janetController.Reset();
        waveCount = 0;
        Debug.Log("Starting next wave");
        StartCoroutine(ExecuteWave());
    }
    private void SpawnCustomer(Customer customer)
    {
        GameObject customerObject = Instantiate(customerPrefab, customerEntrance.position, Quaternion.identity);
        CustomerActions customerActions = customerObject.GetComponent<CustomerActions>();
        customerActions.featureLineController = customerPrefab.GetComponent<CustomerActions>().featureLineController;
        customerActions.InitializeCustomer(customer);
        StartCoroutine(customerActions.StartTrainingActions());
    }


    public void SpawnCoffee(string coffeeType)
    {
        // CoffeeFlavor type;
        // switch (coffeeType)
        // {

        // }
        // Vector3 spawnLocation = coffeeLineController.GetLineStartPosition();
        // GameObject coffeeCup = Instantiate(coffeeCupPrefab, spawnLocation, Quaternion.identity);
        // CoffeeController coffeeController = coffeeCup.GetComponent<CoffeeController>();
        // coffeeController.objTransform.position = spawnLocation;
        // coffeeController.type = type;
        // StartCoroutine(QueueCoffee(coffeeCup, coffeeController));
    }

    // public IEnumerator QueueCoffee(GameObject coffeeCup)
    // {
    //     // yield return coffeeLineController.WaitInLine(2, coffeeController);
    //     // yield return new WaitUntil(() => !coffeeMachineController.isWorking);
    //     // coffeeLineController.RemoveFromLine(coffeeController);
    //     // StartCoroutine(coffeeMachineController.MakeCoffee(coffeeCup));
    // }

    public void SelectCoffee(GameObject coffeeCup)
    {
        pickUpUI.SetActive(true);
        SelectedCoffee = coffeeCup;
    }

    public void SendSelectedCoffeeToTrash()
    {
        pickUpUI.SetActive(false);
        if (SelectedCoffee != null)
        {
            Debug.Log("Sending coffee to trash");
            pickUpAreaController.UnoccupySpot(SelectedCoffee);
            StartCoroutine(trashController.ThrowAwayObject(SelectedCoffee));
            SelectedCoffee = null;
        }
    }

    public void GiveFrontCustomerCoffee()
    {
        pickUpUI.SetActive(false);
        if (SelectedCoffee != null)
        {
            // CoffeeFlavor type = SelectedCoffee.GetComponent<CoffeeController>().filledType;
            MovementController frontCustomerMovementController = waitLineController.GetFrontCustomer();
            if (frontCustomerMovementController != null)
            {
                // frontCustomerMovementController.GetComponent<CustomerActions>().GiveCoffee(type);
            }
            else
            {
                SelectedCoffee = null;
                return;
            }
            pickUpAreaController.UnoccupySpot(SelectedCoffee);
            Destroy(SelectedCoffee);
            SelectedCoffee = null;
        }
    }

    public void CoffeeSold()
    {
        AddMoney(15);
        janetController.CoffeeSuccess();
    }

    public void ShowQuizUI()
    {
        QuizUI.SetActive(true);
        GameUI.SetActive(false);
        UpgradeUI.SetActive(false);
    }

    public void ShowGameUI()
    {
        QuizUI.SetActive(false);
        GameUI.SetActive(true);
        UpgradeUI.SetActive(false);
    }

    public void ShowUpgradeUI()
    {
        QuizUI.SetActive(false);
        GameUI.SetActive(false);
        UpgradeUI.SetActive(true);
    }

    public IEnumerator GiveQuiz(Quiz quiz, Action<bool> callback)
    {
        // QuizUI.SetActive(true);
        // yield return quizController.GiveQuiz(quiz, callback);
        // ShowUpgradeUI();
        return null;
    }

    private int tutorialStep = -1;
    public void ShowTutorialStep(int step)
    {
        if (tutorialFinished)
        {
            return;
        }
        tutorialStep = step;
        if (!tutorialUI.activeSelf)
        {
            tutorialUI.SetActive(true);
        }
        tutorialController.ShowTutorialStep(step);
    }

    public void FinishTutorial()
    {
        tutorialFinished = true;
        tutorialController.HideAllTutorialSteps();
    }
}
