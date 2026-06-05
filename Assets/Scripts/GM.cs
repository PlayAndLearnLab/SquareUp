using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
    [SerializeField] private AILevel[] ailevels;
    private int TotalLevelsCount => (ailevels != null && ailevels.Length > 0) ? ailevels.Length : levels.Length;

    //[SerializeField] private ShopUpgrade[] availableUpgrades;  // Reference to all possible upgrades
    [SerializeField] private LineController featureLineController;
    [SerializeField] private LineController waitLineController;
    private int currentLevelIndex;
    private bool inDay = true;
    private bool coffeeGiven = false;
    private bool bubbleClicked = false;
    private bool coffeeDenied = false;
    private bool speedChanged = false;
    private bool customersServed = false;
    private bool customerDestroyed = false;
    public int day = 0;
    private int money = 0;  // Starting money amount
    private List<GameObject> activeCustomers = new List<GameObject>();
    private int currentWaveTargetCount = 0;

    private bool canShowShop = false;

    // Customer tracking
    private int customersServedToday = 0;

    private bool customerSaved = false;
    public int CustomersServedToday => customersServedToday;
    public bool ai_levels_ongoing;

    // Upgrade system

    private bool nextDayClicked = false;  // Add this field at the top with other private fields
    private bool isIntroStep0Complete = false;

    #region Upgrade System Methods
    private Dictionary<string, ShopUpgrade> activeUpgrades = new Dictionary<string, ShopUpgrade>();
    public bool IsAILevel => (ailevels != null && currentLevelIndex < ailevels.Length);

    public void InitializeUpgrades(ShopUpgrade[] availableUpgrades)
    {
        if (activeUpgrades == null)
        {
            Debug.Log("[GM Lifecycle] activeUpgrades dictionary was NULL. Creating fresh tracking collection.");
            activeUpgrades = new Dictionary<string, ShopUpgrade>();
        }
        else
        {
            Debug.Log($"[GM Lifecycle] activeUpgrades persistent dictionary exists! Count: {activeUpgrades.Count} items.");
            foreach (var kvp in activeUpgrades)
            {
                Debug.Log($"[GM Lifecycle Memory Dump] Key tracked in RAM: '{kvp.Key}' | Level: {kvp.Value.currentLevel}");
            }
        }

        //string[] coreKeys = { "Accuracy", "Speed", "Speech-to-Text", "Quality Check" };

        foreach (var upgradeTemplate in availableUpgrades)
        {
            // Guard against clean string comparisons (strip spaces, enforce formatting matches)
            string trackingKey = upgradeTemplate.upgradeName.Trim();

            if (!activeUpgrades.ContainsKey(trackingKey))
            {
                // First time loading this upgrade into memory context
                ShopUpgrade runtimeCopy = upgradeTemplate.CreateRuntimeCopy();
                activeUpgrades[trackingKey] = runtimeCopy;
                Debug.Log($"[GM Lifecycle] Loaded fresh runtime instance for category '{runtimeCopy.category}': Key='{trackingKey}' initialized at level: {runtimeCopy.currentLevel}");
            }
            else
            {
                // Crucial block: State already exists! 
                // We DO NOT overwrite the instance entirely, but we print out what it currently is.
                Debug.Log($"[GM Lifecycle SUCCESS] Preserved existing state for upgrade key '{trackingKey}'. Active game runtime level remains: {activeUpgrades[trackingKey].currentLevel}");
            }
        }

        //string[] coreKeys = { "Accuracy", "Speed", "Speech-to-Text", "Quality Check" };

        //foreach (string coreKey in coreKeys)
        //{
        //    if (!activeUpgrades.ContainsKey(coreKey))
        //    {
        //        // If a level configuration forgot to include it, create a mock or empty entry 
        //        // so components like QualityCameraController don't crash the game thread.
        //        ShopUpgrade fallbackUpgrade = ScriptableObject.CreateInstance<ShopUpgrade>();
        //        fallbackUpgrade.upgradeName = coreKey;
        //        fallbackUpgrade.currentLevel = 0; // Default locked state

        //        activeUpgrades[coreKey] = fallbackUpgrade;
        //        Debug.LogWarning($"[GM Initializer] Core key '{coreKey}' was completely missing from scene layout setup! Created fallback level-0 entry.");
        //    }
        //}
    }

    private void old_InitializeUpgrades(ShopUpgrade[] upgradesToLoad)
    {
        if (upgradesToLoad == null || upgradesToLoad.Length == 0)
            return;

        if (activeUpgrades == null)
        {
            activeUpgrades = new Dictionary<string, ShopUpgrade>();
        }

        //activeUpgrades.Clear();

        foreach (var upgrade in upgradesToLoad)
        {
            if (upgrade == null)
                continue;

            var runtimeCopy = upgrade.CreateRuntimeCopy();

            string cleanName = upgrade.upgradeName.Replace("(Clone)", "").Trim();

            runtimeCopy.upgradeName = cleanName;
            runtimeCopy.currentLevel = 0;

            activeUpgrades[cleanName] = runtimeCopy;

            Debug.Log($"Loaded Upgrade: {cleanName}");
        }
    }

    //private void InitializeUpgrades()
    //{
    //    //Debug.Log($"Starting InitializeUpgrades. Available upgrades count: {availableUpgrades?.Length ?? 0}");

    //    if (availableUpgrades == null || availableUpgrades.Length == 0)
    //    {
    //        //Debug.LogError("No available upgrades assigned in the Inspector!");
    //        return;
    //    }

    //    // Clear existing upgrades to prevent duplicates
    //    activeUpgrades.Clear();
    //    //Debug.Log("Cleared existing active upgrades");

    //    // Create runtime copies of all available upgrades
    //    foreach (var upgrade in availableUpgrades)
    //    {
    //        if (upgrade != null)
    //        {
    //            var runtimeCopy = upgrade.CreateRuntimeCopy();
    //            runtimeCopy.currentLevel = 0;  // Explicitly reset the level
    //            activeUpgrades[upgrade.upgradeName] = runtimeCopy;
    //            Debug.Log($"Added upgrade: {upgrade.upgradeName} to active upgrades. Current level: {runtimeCopy.currentLevel}");
    //        }
    //        else
    //        {
    //            Debug.LogWarning("Null upgrade found in availableUpgrades array!");
    //        }
    //    }

    //    //Debug.Log($"Finished InitializeUpgrades. Total active upgrades: {activeUpgrades.Count}");
    //}

    // Inside GM.cs
    public void PurchaseUpgrade(string upgradeKey)
    {
        if (activeUpgrades != null && activeUpgrades.ContainsKey(upgradeKey))
        {
            ShopUpgrade runtimeUpgrade = activeUpgrades[upgradeKey];

            if (!runtimeUpgrade.IsMaxLevel)
            {
                runtimeUpgrade.currentLevel++;
                Debug.Log($"[GM] Successfully upgraded {upgradeKey} to level {runtimeUpgrade.currentLevel}!");
            }
            else
            {
                Debug.LogWarning($"[GM] {upgradeKey} is already at max level!");
            }
        }
        else
        {
            Debug.LogError($"[GM] Cannot upgrade! Key '{upgradeKey}' not found in activeUpgrades dictionary.");
        }
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
        // FIX 1: Adjust bounds checking to handle features cleanly, 
        // or log explicitly so it never fails silently again if configurations are empty
        if (upgrade.currentLevel <= 0)
        {
            Debug.LogError($"[GM] '{upgrade.upgradeName}' has an invalid level of {upgrade.currentLevel}. Aborting.");
            return;
        }

        // Safely check valuePerLevel bounds only if values are populated
        float currentValue = 0f;
        if (upgrade.valuePerLevel != null && upgrade.currentLevel <= upgrade.valuePerLevel.Length)
        {
            currentValue = upgrade.valuePerLevel[upgrade.currentLevel - 1];
        }
        else
        {
            Debug.LogWarning($"[GM] '{upgrade.upgradeName}' level {upgrade.currentLevel} doesn't have a corresponding value in valuePerLevel array. Proceeding with state sync anyway.");
        }

        // Safety check: verify key exists in the active state dictionary before modifying it
        if (!activeUpgrades.ContainsKey(upgrade.upgradeName))
        {
            Debug.LogError($"[GM] Key '{upgrade.upgradeName}' was not initialized in activeUpgrades dictionary!");
            return;
        }

        // FIX 2: Explicitly assign the level value directly for ALL categories.
        // Doing activeUpgrades[...].currentLevel++ here double-increments it 
        // because ShopController already added +1 to the source object reference!
        switch (upgrade.category)
        {
            case UpgradeCategory.Quality:
            case UpgradeCategory.Feature:
            case UpgradeCategory.Accuracy:
            case UpgradeCategory.Speed:
            case UpgradeCategory.Learning:
                activeUpgrades[upgrade.upgradeName].currentLevel = upgrade.currentLevel;
                break;
        }

        Debug.Log($"[GM Success] '{upgrade.upgradeName}' synced to master dictionary. Level is now: {activeUpgrades[upgrade.upgradeName].currentLevel}");

        // Fire the upgrade applied event
        EventManager.current.ApplyUpgrade(upgrade.upgradeName, upgrade.category, activeUpgrades[upgrade.upgradeName].currentLevel);
        upgradeApplied = true;
    }

    public void old_ApplyUpgrade(ShopUpgrade upgrade)
    {
        // Safety check: Ensure currentLevel is within the bounds of valuePerLevel
        if (upgrade.currentLevel <= 0 || upgrade.currentLevel > upgrade.valuePerLevel.Length)
        {
            //Debug.LogError($"Attempted to apply upgrade '{upgrade.upgradeName}' with invalid level {upgrade.currentLevel}. valuePerLevel length is {upgrade.valuePerLevel.Length}. Aborting apply.");
            // Optionally, reset the level if it was incorrectly incremented
            // upgrade.currentLevel--; 
            return; // Stop processing this invalid application
        }

        // Apply the upgrade effects (Now safe to access)
        float currentValue = upgrade.valuePerLevel[upgrade.currentLevel - 1];

        // print the keys of the activeUpgrades dictionary
        // print the length of the activeUpgrades dictionary
        //Debug.Log("Active upgrades: " + activeUpgrades.Count);
        //foreach (var key in activeUpgrades.Keys)
        //{
        //    Debug.Log("Active upgrade: " + key);
        //}
        //Debug.Log("Upgrade name: " + upgrade.upgradeName);

        switch (upgrade.category)
        {
            case UpgradeCategory.Quality:
                //Debug.Log("Applying quality upgrade: " + upgrade.upgradeName);
                activeUpgrades[upgrade.upgradeName].currentLevel++;
                break;
            case UpgradeCategory.Feature:
                //Debug.Log("Applying feature upgrade: " + upgrade.upgradeName);
                activeUpgrades[upgrade.upgradeName].currentLevel++;
                // log the updgrade name and current level
                //Debug.Log("Feature upgrade: " + upgrade.upgradeName + " current level: " + activeUpgrades[upgrade.upgradeName].currentLevel);
                break;

            case UpgradeCategory.Accuracy:
                activeUpgrades[upgrade.upgradeName].currentLevel = upgrade.currentLevel;
                break;
            case UpgradeCategory.Speed:
                activeUpgrades[upgrade.upgradeName].currentLevel = upgrade.currentLevel;
                break;
        }

        // Fire the upgrade applied event
        EventManager.current.ApplyUpgrade(upgrade.upgradeName, upgrade.category, activeUpgrades[upgrade.upgradeName].currentLevel);
        upgradeApplied = true;
    }

    public int GetML_Level()
    {
        return activeUpgrades["Learning Boost"].currentLevel;
    }

    public int old_GetFeatureLevel()    
    {
        return activeUpgrades["Speech-to-Text"].currentLevel;
    }

    public int GetFeatureLevel()
    {
        // 1. Safety check: make sure the dictionary exists and contains the requested key
        if (activeUpgrades != null && activeUpgrades.ContainsKey("Speech-to-Text"))
        {
            return activeUpgrades["Speech-to-Text"].currentLevel;
        }

        // 2. Safe fallback: log a warning in the editor instead of crashing the game
        Debug.LogWarning($"[GM] Feature key '{"Speech-to-Text"}' not found in activeUpgrades dictionary! Defaulting to level 0.");
        return 0;
    }

    public int GetQualityLevel()
    {
        if (activeUpgrades == null)
        {
            Debug.LogWarning("[GM] activeUpgrades dictionary is null when checking Quality Level! Defaulting to 0.");
            return 0;
        }

        // FIX: Check if the key exists before indexing into it
        if (activeUpgrades.ContainsKey("Quality Check"))
        {
            return activeUpgrades["Quality Check"].currentLevel;
        }

        // Fallback if the level configurations didn't load the asset or if it's not initialized yet
        Debug.LogWarning("[GM] 'Quality Check' key was missing from activeUpgrades. Returning default level 0.");
        return 0;
    }
    public int old_GetQualityLevel()    
    {
        //Debug.Log("Qual level" + activeUpgrades["Quality Check"].currentLevel);
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
            //Debug.LogWarning($"Could not walk customer to exit: {e.Message}");
            return false;
        }
    }

    public void CompleteIntroStep()
    {
        Debug.Log("[GM] Intro Step 0 Custom Release Triggered!");
        isIntroStep0Complete = true;
    }

    private IEnumerator RemoveCustomer(GameObject customer)
    {
        if (customer == null) yield break;

        var traditionalActions = customer.GetComponent<CustomerActions>();
        var aiActions = customer.GetComponent<AICustomerActions>();
        var controller = customer.GetComponent<CustomerController>();

        if (controller != null)
        {

            LineController fLine = traditionalActions != null ? traditionalActions.featureLineController : aiActions?.featureLineController;
            LineController wLine = traditionalActions != null ? traditionalActions.waitLineController : aiActions?.waitLineController;

            if (fLine != null) fLine.RemoveFromLine(controller);
            if (wLine != null) wLine.RemoveFromLine(controller);

            if (customerExit != null)
            {
                yield return controller.WalkTo(customerExit.position, 10);
                
                customerDestroyed = true;
            }


        }

        // Make sure the customer still exists before trying to destroy it
        if (customer != null)
        {
            Destroy(customer);
            customerDestroyed = true;
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
        //InitializeUpgrades();

        if (ai_levels_ongoing)
        {
            currentLevelIndex = 0;
            TutorialHelper.Reset();
            Debug.Log("[GM] AI Scene detected. Resetting currentLevelIndex to 0.");
            if (activeUpgrades != null)
            {
                activeUpgrades.Clear();
                Debug.Log("[GM Clean Lifecycle] Flushed persistent upgrades cache for AI isolation.");
            }
        }
    }

    void Start()
    {
        //InitializeUpgrades();
        EventManager.current.onDayCompleted += onDayCompleted;
        EventManager.current.onTutorialStepReady += OnTutorialStepReady;
        EventManager.current.onTutorialStepCompleted += TutorialHelper.OnTutorialStepCompleted;
        EventManager.current.onMoneyGained += AddMoney;
        EventManager.current.onStartNextDay += OnStartNextDay;
        EventManager.current.onGiveCoffee += OnGiveCoffee;
        EventManager.current.onDenyCoffee += onDenyCoffee;
        EventManager.current.onCoffeeDistributorSpeedChanged += onCoffeeDistributorSpeedChanged;
        EventManager.current.onCustomerServed += OnCustomerServed;
        EventManager.current.onCustomerLeftEarly += OnCustomerFinishedInteraction;
        EventManager.current.onCustomerDied += OnCustomerFinishedInteraction;
        EventManager.current.onCustomerSaved += OnCustomerSaved;
        EventManager.current.onBubbleClicked += OnBubbleClicked;
        EventManager.current.onGameOver += HandleGameOver;
        EventManager.current.onAnalyticsClosed += OnAnalyticsFinished;
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

    private void OnBubbleClicked()
    {
        bubbleClicked = true;
        Debug.Log("GM: Bubble click detected");

        if (TutorialHelper.IsInTutorial)
        {
            EventManager.current.TutorialStepCompleted(1);
        }
    }

    private void onCoffeeDistributorSpeedChanged(float speed, float accuracy) 
    {
        speedChanged = true;
    }

    private bool shopClosed = false;

    IEnumerator GameLoop()
    {
        Debug.Log($"[GM GAMELOOP] Starting GameLoop. currentLevelIndex={currentLevelIndex}, inDay={inDay}, ai_levels_ongoing={ai_levels_ongoing}");
        yield return StartLevel(currentLevelIndex++);
        Debug.Log($"[GM GAMELOOP] StartLevel finished. Waiting for canShowShop. currentLevelIndex now={currentLevelIndex}");
        yield return new WaitUntil(() => canShowShop);
        Debug.Log("[GM GAMELOOP] canShowShop is true. Showing shop.");
        yield return ShowShop();
        Debug.Log("[GM GAMELOOP] Shop closed. shopClosed=true");
        shopClosed = true;
        if (TutorialHelper.IsInTutorial) {
            Debug.Log("Ending Tutorial");
            TutorialHelper.EndTutorial();
        }

        //TO REMOVE WHEN TESTING DONE
        //if (!ai_levels_ongoing && currentLevelIndex == 3)
        //{
        //    SceneManager.LoadScene("Transition");
        //}

        // Check if all levels are completed
        if (currentLevelIndex >= TotalLevelsCount || Input.GetKeyDown(KeyCode.A)) {
            Debug.Log("All levels completed! Loading Good Ending scene.");
            Debug.Log(TotalLevelsCount);
            //if normal levels then transition else ai levels so good ending
            if (ai_levels_ongoing)
            {
                SceneManager.LoadScene("Good Ending");
            }
            else
            {
                SceneManager.LoadScene("Transition");
            }
            
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

    private void onDayCompleted()
    {
        if (!inDay) return;

        inDay = false;
        StartCoroutine(CleanupCustomers());

        // 1. Show the Brain Analytics first
        CoffeeBrain brain = FindObjectOfType<CoffeeBrain>();
        if (brain != null)
        {
            Debug.Log("Brain trying to show stats");
            brain.ShowEndOfDayStats(); // This turns on your Analytics Canvas
        }
        else
        {
            Debug.Log("No CoffeeBrain found, ending day.");
            // If no brain (maybe a non-AI level), go straight to Shop
            canShowShop = true;
        }
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
        //Debug.Log("Customer saved");
    }

    void OnTutorialStepReady(int step)
    {
        Debug.Log($"[GM OnTutorialStepReady] Called with step={step}, ai_levels_ongoing={ai_levels_ongoing}");

        if (ai_levels_ongoing == false)
        {
            //Debug.Log("Tutorial step ready: " + step);
            if (step == 2)
            {
                StartCoroutine(TutorialHelper.ShowTutorialStepUntil(2, () => coffeeGiven));
            }
            else if (step == 3)
            {
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
                IEnumerator finishDayAfterStep10()
                {
                    yield return TutorialHelper.WaitForTutorialStep(10);
                    EventManager.current.DayCompleted();
                }
                StartCoroutine(finishDayAfterStep10());
            }
            else if (step == 11)
            {
                IEnumerator waitBeforeStep11()
                {
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
        } else
        {
            //if (step == 2)
            //{
            //    //StartCoroutine(TutorialHelper.WaitForTutorialStep(2));
            Debug.Log("GM: ai levels, tutorial time");
            //    StartCoroutine(TutorialHelper.ShowTutorialStepUntil(2, () => bubbleClicked));
            //}
            //if (step == 0)
            //{
            //    if (day == 0) // Or whenever this tutorial happens
            //    {
            //        StartCoroutine(StartLevel());
            //    }
            //}

            //if (step == 1)
            //{
            //    bubbleClicked = false;
            //    StartCoroutine(TutorialHelper.ShowTutorialStepUntil(1, () =>
            //    {
            //        if (bubbleClicked)
            //        {
            //            // 1. Tell the Tutorial Manager the step is officially finished
            //            EventManager.current.TutorialStepCompleted(1);

            //            // 2. Return true so the UI image hides itself
            //            return true;
            //        }
            //        return false;
            //    }));
            //}
            if (step == 0)
            {
                //var confirmBtn = GameObject.Find("Confirmation Button")?.GetComponent<UnityEngine.UI.Button>();
                Debug.Log($"[GM TUTORIAL] Step 0");

                //if (confirmBtn != null)
                //{
                //    confirmBtn.onClick.AddListener(CompleteIntroStep);
                //}
            }

            if (step == 1)
            {
                //SpawnCustomer(ailevels[currentLevelIndex].trainingWave.customers[0], true);
                //Debug.Log("Step 1 in motion without any OnTutorialStepReady Coroutine");
                Debug.Log("[GM] Step 1 ready → marking isIntroStep0Complete = true");
                isIntroStep0Complete = true; // ← ADD THIS
                Debug.Log("Step 1 in motion without any OnTutorialStepReady Coroutine");
            }
            else if (step == 2)
            {
                Debug.Log("Step 2 special session starting");
                // Find the component directly or find the GameObject first
                //AICustomerActions code = GameObject.FindObjectOfType<AICustomerActions>();
                AICustomerActions code = Object.FindObjectOfType<AICustomerActions>();
                Debug.Log($"code exists? = {code}");

                if (code != null)
                {
                    Debug.Log("code is not null");
                    StartCoroutine(TutorialHelper.ShowTutorialStepUntil(2, () => code.canAcceptCoffee == true));
                }
            }
            //else if (step == 3)
            //{



            //    //AICustomer tutorialCustomer = AICustomer.Instantiate(activeCustomers[0].name, transform.position, transform.rotation).GetComponent<AICustomer>();


            //    AICustomer tutorialCustomer = GameObject.FindObjectOfType<AICustomer>();




            //    Debug.Log($"Step 3: activeCustomers[0] = {activeCustomers[0]}");
            //    //AICustomer tutorialCustomer = activeCustomers[0];
            //    //AICustomerActions code = tutorialCustomer.GetComponent<AICustomerActions>();
            //    AICustomerActions code = tutorialCustomer.gameObject.GetComponent<AICustomerActions>();
            //    Debug.Log($"code.canAcceptCoffee for step 3 = {code.canAcceptCoffee}");
            //    StartCoroutine(TutorialHelper.ShowTutorialStepUntil(3, () => code.canAcceptCoffee == true));


            //}
            //else if (step == 5)
            //{
            //    StartCoroutine(TutorialHelper.ShowTutorialStepUntil(5, () => coffeeDenied));
            //}
            //else if (step == 6)
            //{
            //    coffeeGiven = false;
            //    StartCoroutine(TutorialHelper.ShowTutorialStepUntil(6, () => coffeeGiven));
            //}
            else if (step == 8)
            {
                StartCoroutine(TutorialHelper.WaitForTutorialStep(8));
            }
            else if (step == 9)
            {
                StartCoroutine(TutorialHelper.WaitForTutorialStep(9));
            }
            else if (step == 16)
            {
                Debug.Log($"customers that are active = {activeCustomers.Count}");
                Debug.Log($"customersServed = {customersServed}");
                customersServed = false;
                StartCoroutine(TutorialHelper.ShowTutorialStepUntil(16, () => customersServed));
                Debug.Log($"customersServed = {customersServed}");
            }
            else if (step == 17)
            {
                //customerDestroyed = false;
                //StartCoroutine(TutorialHelper.ShowTutorialStepUntil(17, () => customerDestroyed));
                //yield return new WaitForSeconds(5f);
                //Debug.Log($"customers that are active = {activeCustomers.Count}");
                //StartCoroutine(TutorialHelper.ShowTutorialStepUntil(17, () => (activeCustomers.Count == 0)));
                inDay = true;
                IEnumerator finishDayAfterStep17()
                {
                    yield return new WaitForSeconds(5f);
                    Debug.Log("Seconds have passed for tutorial 17");
                    //StartCoroutine(TutorialHelper.WaitForTutorialStep(17));
                    
                    StartCoroutine(TutorialHelper.ShowTutorialStepUntil(17, () => (inDay == false)));
                    EventManager.current.DayCompleted();

                    //Debug.Log($"customers that are active = {activeCustomers.Count}");
                    //StartCoroutine(TutorialHelper.ShowTutorialStepUntil(17, () => (activeCustomers.Count == 0)));
                }
                StartCoroutine(finishDayAfterStep17());
            }

            //else if (step == 12)
            //{
            //    StartCoroutine(TutorialHelper.WaitForTutorialStep(12));
            //}
            //else if (step == 13)
            //{
            //    StartCoroutine(TutorialHelper.ShowTutorialStepUntil(13, () => shopClosed));

            //}

        }
    }

    private object GetCurrentLevelObject(int index)
    {
        //if (ailevels != null && ailevels.Length > index) return ailevels[index];
        if (ai_levels_ongoing)
        {
            // Prevent index out of bounds if scenes transition with high indices
            int safeAIIndex = index % ailevels.Length;
            if (ailevels != null && ailevels.Length > safeAIIndex) return ailevels[safeAIIndex];
        }
        if (levels != null && levels.Length > index) return levels[index];
        return null;
    }

    IEnumerator StartLevel(int levelIndex)
    {
        Debug.Log($"[GM STARTLEVEL] Entered. levelIndex={levelIndex}, inDay={inDay}, isIntroStep0Complete={isIntroStep0Complete}");

        if (ai_levels_ongoing)
        {
            // Instead of waiting on the shared TutorialHelper, wait for our local flag
            yield return new WaitUntil(() => isIntroStep0Complete);
        }
        else
        {
            // Traditional levels can keep their old behavior
            yield return TutorialHelper.WaitForTutorialStep(0);
        }

        inDay = true;
        Debug.Log($"[GM STARTLEVEL] After inDay reset: inDay={inDay}");


        Debug.Log("[GM PROGRESSION] Breaking free from Step 0 window! Initializing wave configurations.");

        //yield return TutorialHelper.WaitForTutorialStep(0);

        object currentLevel = GetCurrentLevelObject(levelIndex);
        int trainingTime = 0;
        object waveData = null;

        try
        {
            if (currentLevel is AILevel al)
            {
                InitializeUpgrades(al.levelUpgrades);
                canShowShop = false;
                trainingTime = al.trainingTime;
                waveData = al.trainingWave;
                currentWaveTargetCount = al.trainingWave.customers.Length;
            }
            else if (currentLevel is Level l)
            {
                InitializeUpgrades(l.levelUpgrades);
                canShowShop = true;
                trainingTime = l.trainingTime;
                waveData = l.trainingWave;
                currentWaveTargetCount = l.trainingWave.customers.Length;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CRITICAL GM SETUP EXCEPTION] Stalled setup phase: {ex.Message}\n{ex.StackTrace}");
        }

        //if (currentLevel is AILevel al)
        //{
        //    InitializeUpgrades(al.levelUpgrades);

        //    canShowShop = false;
        //    trainingTime = al.trainingTime;
        //    waveData = al.trainingWave; // This is an AIWave
        //    currentWaveTargetCount = al.trainingWave.customers.Length;
        //}
        //else if (currentLevel is Level l)
        //{
        //    InitializeUpgrades(l.levelUpgrades);

        //    canShowShop = true;
        //    trainingTime = l.trainingTime;
        //    waveData = l.trainingWave; // This is a traditional Wave
        //    currentWaveTargetCount = l.trainingWave.customers.Length;
        //}

        EventManager.current.DayStarted(trainingTime);

        Debug.Log($"[GM] Reached Spawning Milestone. Wave Data valid? {waveData != null}");

        Debug.Log($"[GM STARTLEVEL] Calling StartWave. waveData={waveData?.GetType().Name ?? "NULL"}, inDay={inDay}");

        StartCoroutine(StartWave(waveData, false));

        Debug.Log("[GM STARTLEVEL] Now waiting for !inDay...");

        yield return new WaitUntil(() => !inDay);

        Debug.Log("[GM STARTLEVEL] !inDay resolved. Level done.");

    }

    private void OnCustomerFinishedInteraction()
    {
        customersServed = true;
        customersServedToday++;
        CheckForEndOfDay();
    }

    //IEnumerator StartLevel_old(int levelIndex)
    //{
    //    yield return TutorialHelper.WaitForTutorialStep(0);
    //    EventManager.current.DayStarted(levels[levelIndex].trainingTime);
    //    StartCoroutine(StartWave(levels[levelIndex].trainingWave, false));
    //    yield return new WaitUntil(() => !inDay);
    //}

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

    IEnumerator StartWave_old(Wave wave, bool isTesting = false)
    {
        foreach (Customer customer in wave.customers)
        {
            // Stop spawning if the day has ended
            if (!inDay) break;

            SpawnCustomer(customer, isTesting);
            yield return new WaitForSeconds(wave.timeBetweenCustomers);
        }
    }

    IEnumerator StartWave(object waveData, bool isTesting = false)
    {
        //Debug.Log("Start wave initialized");
        Debug.Log($"[GM STARTWAVE] Entered. inDay={inDay}, waveData type={waveData?.GetType().Name ?? "NULL"}");

        IEnumerable customers = null;
        float timeBetween = 0;

        if (waveData is AIWave aiW)
        {
            customers = aiW.customers;
            timeBetween = aiW.timeBetweenCustomers;
        }
        else if (waveData is Wave w)
        {
            customers = w.customers;
            timeBetween = w.timeBetweenCustomers;
        }

        if (customers == null)
        {
            Debug.LogError($"[GM BUG] StartWave was passed invalid or null waveData! waveData type: {waveData?.GetType().Name ?? "NULL"}");
            yield break;
        }

        int spawnCount = 0;

        foreach (object customer in customers)
        {
            Debug.Log($"[GM STARTWAVE] Loop iteration. inDay={inDay}, spawnCount={spawnCount}");

            //if (!inDay) break;
            if (!inDay) { Debug.Log("[GM STARTWAVE] inDay is FALSE — breaking out of spawn loop!"); break; }


            SpawnCustomer(customer, isTesting);
            spawnCount++;
            yield return new WaitForSeconds(timeBetween);
        }

        Debug.Log($"[GM STARTWAVE] Finished. Total spawned: {spawnCount}");
    }

    void SpawnCustomer(object customerData, bool isTesting = false)
    {
        GameObject customerObject = Instantiate(customerPrefab, customerEntrance.position, Quaternion.identity);
        customerObject.name = $"Customer_{System.Guid.NewGuid().ToString().Substring(0, 5)}";

        if (customerData is Customer traditionalData)
        {
            // If the prefab ALREADY has CustomerActions, don't AddComponent!
            CustomerActions actions = customerObject.GetComponent<CustomerActions>();

            // If it doesn't have it, then add it
            if (actions == null) actions = customerObject.AddComponent<CustomerActions>();

            // CRITICAL: Ensure the GM passes the line references to traditional customers too!
            actions.gm = this;
            actions.featureLineController = this.featureLineController;
            actions.waitLineController = this.waitLineController;

            actions.entrance = this.customerEntrance;
            actions.exit = this.customerExit;

            actions.InitializeCustomer(traditionalData);
            StartCoroutine(actions.StartTrainingActions());
            activeCustomers.Add(customerObject);
        }
        // 2. Check if it's an AI Customer
        else if (customerData is AICustomer aiData)
        {
            AICustomerActions aiActions = customerObject.GetComponent<AICustomerActions>();
            if (aiActions == null) aiActions = customerObject.AddComponent<AICustomerActions>();

            Debug.Log($"AICustomerActions count on {customerObject.name}: " + customerObject.GetComponents<AICustomerActions>().Length);

            

            aiActions.featureLineController = this.featureLineController;
            aiActions.waitLineController = this.waitLineController;
            aiActions.exit = this.customerExit;

            aiActions.InitializeCustomer(aiData, this.customerEntrance);
            StartCoroutine(aiActions.StartTrainingActions());
            activeCustomers.Add(customerObject);
            //AICustomerActions aiActions = customerObject.AddComponent<AICustomerActions>();
            //aiActions.featureLineController = this.featureLineController; // Make sure GM has these fields
            //aiActions.waitLineController = this.waitLineController;
            //aiActions.exit = this.customerExit;
            //aiActions.InitializeCustomer(aiData);
            //StartCoroutine(aiActions.StartTrainingActions());
            //activeCustomers.Add(customerObject);
        }
    }

    public void OnCustomerServed()
    {
        
        OnCustomerFinishedInteraction();

    }

    private void CheckForEndOfDay()
    {
        if (customersServedToday >= currentWaveTargetCount && !TutorialHelper.IsInTutorial)
        {
            Debug.Log("Wave Finished: All customers have either been served or left.");
            IEnumerator waitBeforeEndingLevel()
            {
                Debug.Log("waiting end of day buffer");
                yield return new WaitForSeconds(2f);
                EventManager.current.DayCompleted();
            }
            StartCoroutine(waitBeforeEndingLevel());
            
        }
    }

    private void OnAnalyticsFinished()
    {
        // Now that the player closed the brain view, 
        // we can proceed to the Shop or the next day logic
        Debug.Log("Analytics closed. Proceeding...");
        canShowShop = true;

        // Usually, you'd show the Shop UI here or wait for StartNextDay
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
            EventManager.current.onCustomerLeftEarly -= OnCustomerFinishedInteraction;
            EventManager.current.onCustomerDied -= OnCustomerFinishedInteraction;
            EventManager.current.onCustomerSaved -= OnCustomerSaved;
            EventManager.current.onBubbleClicked -= OnBubbleClicked;
            EventManager.current.onGameOver -= HandleGameOver;
            EventManager.current.onAnalyticsClosed -= OnAnalyticsFinished;
        }
    }

    // Handler for the game over event
    private void HandleGameOver()
    {
        Debug.Log("Game Over! Loading Bad Ending scene.");
        SceneManager.LoadScene("Bad Ending");
    }
}
