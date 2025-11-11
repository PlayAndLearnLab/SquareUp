using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GM : MonoBehaviour, ShopController.IGameState
{
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private GameObject ShopUI;
    [SerializeField] private GameObject QuizUI;
    [SerializeField] private Transform customerEntrance;
    [SerializeField] private Transform customerExit;
    [SerializeField] private Level[] levels;
    [SerializeField] private ShopUpgrade[] availableUpgrades;  // Reference to all possible upgrades
    private int currentLevelIndex;
    private bool inDay = true;
    private bool coffeeGiven = false;
    private bool coffeeDenied = false;
    private bool speedChanged = false;
    public int day = 0;
    private int money = 0;  // Starting money amount
    private List<GameObject> activeCustomers = new List<GameObject>();
    
    // Customer tracking
    private int customersServedToday = 0;

    private bool customerSaved = false;
    public int CustomersServedToday => customersServedToday;

    // Upgrade system

    private bool nextDayClicked = false;  // Add this field at the top with other private fields

    #region Upgrade System Methods
    private Dictionary<string, ShopUpgrade> activeUpgrades = new Dictionary<string, ShopUpgrade>();

    private void InitializeUpgrades()
    {
        Debug.Log($"Starting InitializeUpgrades. Available upgrades count: {availableUpgrades?.Length ?? 0}");

        if (availableUpgrades == null || availableUpgrades.Length == 0)
        {
            Debug.LogError("No available upgrades assigned in the Inspector!");
            return;
        }

        // Clear existing upgrades to prevent duplicates
        activeUpgrades.Clear();
        Debug.Log("Cleared existing active upgrades");

        // Create runtime copies of all available upgrades
        foreach (var upgrade in availableUpgrades)
        {
            if (upgrade != null)
            {
                var runtimeCopy = upgrade.CreateRuntimeCopy();
                runtimeCopy.currentLevel = 0;  // Explicitly reset the level
                activeUpgrades[upgrade.upgradeName] = runtimeCopy;
                Debug.Log($"Added upgrade: {upgrade.upgradeName} to active upgrades. Current level: {runtimeCopy.currentLevel}");
            }
            else
            {
                Debug.LogWarning("Null upgrade found in availableUpgrades array!");
            }
        }

        Debug.Log($"Finished InitializeUpgrades. Total active upgrades: {activeUpgrades.Count}");
    }

    public ShopUpgrade GetUpgrade(string upgradeName)
    {
        return activeUpgrades.ContainsKey(upgradeName) ? activeUpgrades[upgradeName] : null;
    }

    public ShopUpgrade[] GetActiveUpgrades()
    {
        return activeUpgrades.Values.ToArray();
    }

    private bool upgradeApplied = false;

    public void ApplyUpgrade(ShopUpgrade upgrade)
    {
        // Safety check: Ensure currentLevel is within the bounds of valuePerLevel
        if (upgrade.currentLevel <= 0 || upgrade.currentLevel > upgrade.valuePerLevel.Length)
        {
            Debug.LogError($"Attempted to apply upgrade '{upgrade.upgradeName}' with invalid level {upgrade.currentLevel}. valuePerLevel length is {upgrade.valuePerLevel.Length}. Aborting apply.");
            // Optionally, reset the level if it was incorrectly incremented
            // upgrade.currentLevel--; 
            return; // Stop processing this invalid application
        }

        // Apply the upgrade effects (Now safe to access)
        float currentValue = upgrade.valuePerLevel[upgrade.currentLevel - 1];

        // print the keys of the activeUpgrades dictionary
        // print the length of the activeUpgrades dictionary
        Debug.Log("Active upgrades: " + activeUpgrades.Count);
        foreach (var key in activeUpgrades.Keys)
        {
            Debug.Log("Active upgrade: " + key);
        }
        Debug.Log("Upgrade name: " + upgrade.upgradeName);

        switch (upgrade.category)
        {
            case UpgradeCategory.Quality:
                Debug.Log("Applying quality upgrade: " + upgrade.upgradeName);
                activeUpgrades[upgrade.upgradeName].currentLevel++;
                break;
            case UpgradeCategory.Feature:
                Debug.Log("Applying feature upgrade: " + upgrade.upgradeName);
                activeUpgrades[upgrade.upgradeName].currentLevel++;
                // log the updgrade name and current level
                Debug.Log("Feature upgrade: " + upgrade.upgradeName + " current level: " + activeUpgrades[upgrade.upgradeName].currentLevel);
                break;
        }

        // Fire the upgrade applied event
        EventManager.current.ApplyUpgrade(upgrade.upgradeName, upgrade.category, activeUpgrades[upgrade.upgradeName].currentLevel);
        upgradeApplied = true;
    }

    public int GetFeatureLevel()    
    {
        return activeUpgrades["Speech-to-Text"].currentLevel;
    }
    public int GetQualityLevel()    
    {
        Debug.Log("Qual level" + activeUpgrades["Quality Check"].currentLevel);
        return activeUpgrades["Quality Check"].currentLevel;
    }

    public int GetAccuracyLevel()
    {
        return activeUpgrades["Accuracy"].currentLevel;
    }

    public int GetSpeedLevel()
    {
        return activeUpgrades["Speed"].currentLevel;
    }

    #endregion

    private bool TryWalkToExit(CustomerController controller, Vector3 exitPosition)
    {
        try
        {
            controller.WalkTo(exitPosition, 10);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not walk customer to exit: {e.Message}");
            return false;
        }
    }

    private IEnumerator RemoveCustomer(GameObject customer)
    {
        if (customer == null) yield break;

        var customerActions = customer.GetComponent<CustomerActions>();
        if (customerActions != null)
        {
            var controller = customer.GetComponent<CustomerController>();
            if (controller != null)
            {
                // Try to remove from both lines - the LineController will handle if they're not in the line
                if (customerActions.featureLineController != null)
                {
                    try
                    {
                        customerActions.featureLineController.RemoveFromLine(controller);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Could not remove customer from feature line: {e.Message}");
                    }
                }
                if (customerActions.waitLineController != null)
                {
                    try
                    {
                        customerActions.waitLineController.RemoveFromLine(controller);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Could not remove customer from wait line: {e.Message}");
                    }
                }

                // Only try to walk to exit if we have one assigned and the customer still exists
                if (customerExit != null && customer != null)
                {
                    Vector3 exitPosition = customerExit.position;
                    if (controller != null && controller.objTransform != null)
                    {
                        yield return controller.WalkTo(exitPosition, 10);
                    }
                }
            }
        }

        // Make sure the customer still exists before trying to destroy it
        if (customer != null)
        {
            Destroy(customer);
        }
    }

    #region Money System Methods

    public int GetMoney() => money;

    public bool RemoveMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            return true;
        }
        return false;
    }

    public void AddMoney(int amount)
    {
        money += amount;
    }

    #endregion

    void Awake()
    {
        ShopUI.SetActive(false);
        InitializeUpgrades();
    }

    void Start()
    {
        InitializeUpgrades();
        EventManager.current.onDayCompleted += onDayCompleted;
        EventManager.current.onTutorialStepReady += OnTutorialStepReady;
        EventManager.current.onTutorialStepCompleted += TutorialHelper.OnTutorialStepCompleted;
        EventManager.current.onMoneyGained += AddMoney;
        EventManager.current.onStartNextDay += OnStartNextDay;
        EventManager.current.onGiveCoffee += OnGiveCoffee;
        EventManager.current.onDenyCoffee += onDenyCoffee;
        EventManager.current.onCoffeeDistributorSpeedChanged += onCoffeeDistributorSpeedChanged;
        EventManager.current.onCustomerServed += OnCustomerServed;
        EventManager.current.onCustomerSaved += OnCustomerSaved;
        EventManager.current.onGameOver += HandleGameOver;
        TutorialHelper.StartTutorial();
        StartCoroutine(GameLoop());
    }

    private void OnStartNextDay()
    {
        nextDayClicked = true;  // Set the flag when next day is clicked
        inDay = true;  // Reset the day state
        day++;
        customersServedToday = 0;  // Reset customer count for new day
        StartCoroutine(GameLoop());  // Restart from level 1
    }

    private void onCoffeeDistributorSpeedChanged(float speed, float accuracy) 
    {
        speedChanged = true;
    }

    private bool shopClosed = false;

    IEnumerator GameLoop()
    {
        yield return StartLevel(currentLevelIndex++);
        yield return ShowShop();
        shopClosed = true;
        if (TutorialHelper.IsInTutorial) {
            Debug.Log("Ending Tutorial");
            TutorialHelper.EndTutorial();
        }
        
        // Check if all levels are completed
        if (currentLevelIndex >= levels.Length) {
            Debug.Log("All levels completed! Loading Good Ending scene.");
            SceneManager.LoadScene("Good Ending");
        }
    }

    private IEnumerator CleanupCustomers()
    {
        List<Coroutine> cleanupRoutines = new List<Coroutine>();

        foreach (var customer in activeCustomers.ToArray())
        {
            if (customer != null)
            {
                cleanupRoutines.Add(StartCoroutine(RemoveCustomer(customer)));
            }
        }

        // Wait for all cleanup routines to finish
        foreach (var routine in cleanupRoutines)
        {
            yield return routine;
        }

        activeCustomers.Clear();
    }

    void onDayCompleted()
    {
        // Only process if we're still in day
        if (!inDay) return;

        inDay = false;
        StartCoroutine(CleanupCustomers());
    }

    private void OnGiveCoffee(Coffee coffee)
    {
        coffeeGiven = true;
    }

    private void onDenyCoffee()
    {
        coffeeDenied = true;   
        CoffeeOrder coffeeToExplode = EventManager.current.PeekNextCoffee();

        coffeeToExplode = EventManager.current.GetNextCoffee(); // Remove from queue
        if (coffeeToExplode != null && coffeeToExplode.CoffeeObject != null)
        {
            // Explode the coffee
            StartCoroutine(coffeeToExplode.CoffeeObject.GetComponent<CoffeeCupController>().Explode());
            Debug.Log("Coffee exploded successfully");
        }
        else
        {
            Debug.LogWarning("Coffee order or coffee object was null");
        } 
    }

    private void OnCustomerSaved()
    {
        customerSaved = true;
        Debug.Log("Customer saved");
    }

    void OnTutorialStepReady(int step)
    {
        Debug.Log("Tutorial step ready: " + step);
        if (step == 2)
        {
            StartCoroutine(TutorialHelper.ShowTutorialStepUntil(2, () => coffeeGiven));
        }
        else if (step == 3) {
            StartCoroutine(TutorialHelper.ShowTutorialStepUntil(3, () => customerSaved));
        }
        else if (step == 5)
        {
            StartCoroutine(TutorialHelper.ShowTutorialStepUntil(5, () => coffeeDenied));
        }
        else if (step == 6)
        {
            coffeeGiven = false;
            StartCoroutine(TutorialHelper.ShowTutorialStepUntil(6, () => coffeeGiven));
        }
        else if (step == 8)
        {
            StartCoroutine(TutorialHelper.WaitForTutorialStep(8));
        }
        else if (step == 9)
        {
            StartCoroutine(TutorialHelper.WaitForTutorialStep(9));
        }
        else if (step == 10)
        {
            IEnumerator finishDayAfterStep10() {
                yield return TutorialHelper.WaitForTutorialStep(10);
                EventManager.current.DayCompleted();
            }
            StartCoroutine(finishDayAfterStep10());
        }
        else if (step == 11)
        {
            IEnumerator waitBeforeStep11() {
                yield return new WaitForSeconds(0.1f);
                StartCoroutine(TutorialHelper.ShowTutorialStepUntil(11, () => upgradeApplied));
            }
            StartCoroutine(waitBeforeStep11());
        }
        else if (step == 12)
        {
            StartCoroutine(TutorialHelper.WaitForTutorialStep(12));
        }
        else if (step == 13)
        {
            StartCoroutine(TutorialHelper.ShowTutorialStepUntil(13, () => shopClosed));

        }
    }

    IEnumerator StartLevel(int levelIndex)
    {
        yield return TutorialHelper.WaitForTutorialStep(0);
        EventManager.current.DayStarted(levels[levelIndex].trainingTime);
        StartCoroutine(StartWave(levels[levelIndex].trainingWave, false));
        yield return new WaitUntil(() => !inDay);
    }

    IEnumerator ShowShop()
    {
        ShopUI.SetActive(true);
        EventManager.current.ShopOpened();
        EventManager.current.ClearQueueRequested();
        nextDayClicked = false;  // Reset the flag when shop opens
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Escape) || nextDayClicked);
        EventManager.current.ShopClosed();
        ShopUI.SetActive(false);
    }

    IEnumerator StartWave(Wave wave, bool isTesting = false)
    {
        foreach (Customer customer in wave.customers)
        {
            // Stop spawning if the day has ended
            if (!inDay) break;

            SpawnCustomer(customer, isTesting);
            yield return new WaitForSeconds(wave.timeBetweenCustomers);
        }
    }

    void SpawnCustomer(Customer customer, bool isTesting = false)
    {
        GameObject customerObject = Instantiate(customerPrefab, customerEntrance.position, Quaternion.identity);
        CustomerActions customerActions = customerObject.GetComponent<CustomerActions>();
        customerActions.InitializeCustomer(customer);
        StartCoroutine(customerActions.StartTrainingActions());
        activeCustomers.Add(customerObject);
    }

    private void OnCustomerServed()
    {
        customersServedToday++;
        Debug.Log($"Customer served! Total today: {customersServedToday}");
        Wave currentWave = levels[currentLevelIndex-1].trainingWave;
        Debug.Log($"Customer served! Need {currentWave.customers.Length} customers");
        if (customersServedToday >= currentWave.customers.Length && !TutorialHelper.IsInTutorial) {
            Debug.Log("Completed day");
            EventManager.current.DayCompleted();
        }
    }

    void OnDisable()
    {
        if (EventManager.current != null)
        {
            EventManager.current.onDayCompleted -= onDayCompleted;
            EventManager.current.onTutorialStepReady -= OnTutorialStepReady;
            EventManager.current.onTutorialStepCompleted -= TutorialHelper.OnTutorialStepCompleted;
            EventManager.current.onMoneyGained -= AddMoney;
            EventManager.current.onStartNextDay -= OnStartNextDay;
            EventManager.current.onGiveCoffee -= OnGiveCoffee;
            EventManager.current.onDenyCoffee -= onDenyCoffee;
            EventManager.current.onCoffeeDistributorSpeedChanged -= onCoffeeDistributorSpeedChanged;
            EventManager.current.onCustomerServed -= OnCustomerServed;
            EventManager.current.onCustomerSaved -= OnCustomerSaved;
            EventManager.current.onGameOver -= HandleGameOver;
        }
    }

    // Handler for the game over event
    private void HandleGameOver()
    {
        Debug.Log("Game Over! Loading Bad Ending scene.");
        SceneManager.LoadScene("Bad Ending");
    }
}
