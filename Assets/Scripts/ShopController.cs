using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class ShopController : MonoBehaviour
{
    // Interface for GM to implement
    public interface IGameState
    {
        int GetMoney();
        bool RemoveMoney(int amount);
        void AddMoney(int amount);
        ShopUpgrade GetUpgrade(string name);
        void ApplyUpgrade(ShopUpgrade upgrade);
        ShopUpgrade[] GetActiveUpgrades();  // New method to get available upgrades
    }

    [Header("References")]
    [SerializeField] private Transform upgradeContainer;
    [SerializeField] private GameObject upgradeButtonPrefab;
    [SerializeField] private GameObject nextDayButton;
    [SerializeField] private GM gm;  // Reference GM directly instead of IGameState

    [Header("Shop Configuration")]
    [SerializeField] private Vector2 spacing = new Vector2(250f, 110f);  // X=200, Y=10 from your settings
    [SerializeField] private Vector2 buttonSize = new Vector2(300f, 350f);  // X=300, Y=350 from your settings
    [SerializeField] private int columnsCount = 3;  // 3 columns as shown

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI dayText;

    private Dictionary<UpgradeCategory, List<ShopUpgrade>> categorizedUpgrades;
    private List<ShopUpgradeButton> upgradeButtons = new List<ShopUpgradeButton>();

    public List<TieredUpgradeController> tieredUpgradeControllers = new List<TieredUpgradeController>();
    private GridLayoutGroup gridLayout;
    private ShopUpgrade[] runtimeUpgrades;  // Store runtime copies
    private IGameState gameState;    // Private field for interface access

    // Track which upgrade's quiz needs to be started after slideshow
    private ShopUpgrade pendingUpgradeAfterSlides;

    private void Awake()
    {
        // Convert GM reference to IGameState
        gameState = gm;
        Debug.Assert(gameState != null, "GM reference is required");

        // Get runtime copies of upgrades from the game state
        runtimeUpgrades = gameState.GetActiveUpgrades();
        Debug.Log($"Retrieved {runtimeUpgrades.Length} available upgrades from game state");

        SetupGridLayout();
        categorizedUpgrades = new Dictionary<UpgradeCategory, List<ShopUpgrade>>();
        OrganizeUpgrades();
        InitializeUI();
    }

    private void SetupGridLayout()
    {
        // Get or add GridLayoutGroup component
        gridLayout = upgradeContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = upgradeContainer.gameObject.AddComponent<GridLayoutGroup>();
        }

        // Force update the grid layout settings
        gridLayout.padding = new RectOffset(0, 0, 0, 0);  // Reset padding
        gridLayout.cellSize = new Vector2(300f, 350f);    // Exact values from inspector
        gridLayout.spacing = new Vector2(200f, 10f);      // Exact values from inspector
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        // Force layout rebuild
        LayoutRebuilder.ForceRebuildLayoutImmediate(upgradeContainer.GetComponent<RectTransform>());
    }

    private void OnEnable()
    {
        if (EventManager.current != null)
        {
            EventManager.current.onShopOpened += OnShopOpened;
            EventManager.current.onShopClosed += OnShopClosed;
            EventManager.current.onQuizCompleted += OnQuizCompleted;
            EventManager.current.onSlideshowCompleted += OnSlideshowCompleted;
        }
    }

    private void OnDisable()
    {
        // if (EventManager.current != null)
        // {
        //     EventManager.current.onShopOpened -= OnShopOpened;
        //     EventManager.current.onShopClosed -= OnShopClosed;
        //     EventManager.current.onQuizCompleted -= OnQuizCompleted;
        // }
    }

    private void Start()
    {
        // Subscribe to events here as well in case EventManager wasn't ready in OnEnable
        if (EventManager.current != null)
        {
            EventManager.current.onShopOpened += OnShopOpened;
            EventManager.current.onShopClosed += OnShopClosed;
            EventManager.current.onQuizCompleted += OnQuizCompleted;
            EventManager.current.onSlideshowCompleted += OnSlideshowCompleted;
        }

        // Set up next day button click handler
        if (nextDayButton != null)
        {
            nextDayButton.GetComponent<Button>().onClick.AddListener(OnNextDayButtonClicked);
        }

        UpdateMoneyDisplay();
        RefreshUpgradeButtons();

        // Initialize the speed upgrade
        // find the speed upgrade in the runtimeUpgrades array
        ShopUpgrade speedUpgrade = runtimeUpgrades.FirstOrDefault(upgrade => upgrade.category == UpgradeCategory.Speed);
        if (speedUpgrade != null)
        {
            tieredUpgradeControllers[0].Initialize(speedUpgrade, PurchaseUpgrade);
        }
        else
        {
            Debug.LogWarning("Speed upgrade not found in runtimeUpgrades");
        }

        ShopUpgrade accuracyUpgrade = runtimeUpgrades.FirstOrDefault(upgrade => upgrade.category == UpgradeCategory.Accuracy);
        if (accuracyUpgrade != null) {
            tieredUpgradeControllers[1].Initialize(accuracyUpgrade, PurchaseUpgrade);
        }
        else
        {
            Debug.LogWarning("Accuracy upgrade not found in runtimeUpgrades");
        }

    }

    private void OrganizeUpgrades()
    {
        foreach (ShopUpgrade upgrade in runtimeUpgrades)  // Use runtime copies instead
        {
            if (!categorizedUpgrades.ContainsKey(upgrade.category))
            {
                categorizedUpgrades[upgrade.category] = new List<ShopUpgrade>();
            }
            categorizedUpgrades[upgrade.category].Add(upgrade);
        }
    }

    private void InitializeUI()
    {
        Debug.Log("Initializing UI");
        // Clear existing upgrade buttons
        foreach (Transform child in upgradeContainer)
        {
            Destroy(child.gameObject);
        }
        upgradeButtons.Clear();

        // Create upgrade buttons for each category
        foreach (var category in categorizedUpgrades)
        {
            Debug.Log($"Creating buttons for category {category.Key} with {category.Value.Count} upgrades");
            foreach (var upgrade in category.Value)
            {
                if (upgrade.category != UpgradeCategory.Speed && upgrade.category != UpgradeCategory.Accuracy)
                {
                    CreateUpgradeButton(upgrade);
                }
            }
        }


        Debug.Log($"Created {upgradeButtons.Count} total upgrade buttons");
    }

    private void CreateUpgradeButton(ShopUpgrade upgrade)
    {
        GameObject buttonObj = Instantiate(upgradeButtonPrefab, upgradeContainer);
        var upgradeButton = buttonObj.GetComponent<ShopUpgradeButton>();

        if (upgradeButton != null)
        {
            upgradeButton.Initialize(upgrade, PurchaseUpgrade, UnlockUpgrade);
            upgradeButtons.Add(upgradeButton);
        }
    }

    private void PurchaseUpgrade(ShopUpgrade upgrade)
    {
        Debug.Log($"Purchasing upgrade {upgrade.name}");
        int cost = upgrade.GetNextLevelCost;
        if (gameState.RemoveMoney(cost))
        {
            upgrade.currentLevel++;
            UpdateMoneyDisplay();
            RefreshUpgradeButtons();
            gameState.ApplyUpgrade(upgrade);
        }
    }

    private void UnlockUpgrade(ShopUpgrade upgrade)
    {
        if (upgrade.prerequisiteQuiz != null)
        {
            Debug.Log($"ShopController: Starting quiz '{upgrade.prerequisiteQuiz.quizName}', EventManager is {(EventManager.current != null ? "available" : "null")}");
            
            // Check if the quiz has intro slides to show first
            if (upgrade.prerequisiteQuiz.introSlides != null)
            {
                // Start by showing the slideshow
                Debug.Log($"ShopController: Showing intro slides before quiz '{upgrade.prerequisiteQuiz.quizName}'");
                pendingUpgradeAfterSlides = upgrade;
                EventManager.current.RequestSlideDisplay(upgrade.prerequisiteQuiz.introSlides);
            }
            else
            {
                // No slides, start quiz directly
                EventManager.current.StartQuiz(upgrade.prerequisiteQuiz);
            }
        }
        else
        {
            Debug.LogWarning("ShopController: Trying to unlock upgrade but prerequisiteQuiz is null!");
        }
    }

    private void OnQuizCompleted(Quiz quiz, bool passed)
    {
        // Find the upgrade that has this quiz as a prerequisite
        var upgradeWithQuiz = upgradeButtons
            .Select(button => button.upgrade)
            .FirstOrDefault(upgrade => upgrade.prerequisiteQuiz == quiz);

        if (upgradeWithQuiz != null && passed)
        {
            upgradeWithQuiz.prerequisiteQuizCompleted = true;
            RefreshUpgradeButtons();
        }
    }

    private void OnSlideshowCompleted(IntroSlidesData slidesData)
    {
        if (pendingUpgradeAfterSlides != null && 
            pendingUpgradeAfterSlides.prerequisiteQuiz != null && 
            pendingUpgradeAfterSlides.prerequisiteQuiz.introSlides == slidesData)
        {
            Debug.Log($"ShopController: Slideshow completed, now starting quiz '{pendingUpgradeAfterSlides.prerequisiteQuiz.quizName}'");
            EventManager.current.StartQuiz(pendingUpgradeAfterSlides.prerequisiteQuiz);
            pendingUpgradeAfterSlides = null;
        }
    }

    private void RefreshUpgradeButtons()
    {
        foreach (var upgradeButton in upgradeButtons)
        {
            bool canAfford = gameState.GetMoney() >= upgradeButton.upgrade.GetNextLevelCost;
            upgradeButton.button.interactable = canAfford && !upgradeButton.upgrade.IsMaxLevel;
            upgradeButton.UpdateDisplay();
        }

        foreach (var tieredUpgradeController in tieredUpgradeControllers)
        {
            tieredUpgradeController.RefreshDisplay();
        }
    }

    private void UpdateMoneyDisplay()
    {
        if (moneyText != null)
        {
            moneyText.text = $"${gameState.GetMoney():N0}";
        }
    }

    private void UpdateDayDisplay()
    {
        if (dayText != null)
        {
            dayText.text = $"{gm.day}";
        }
    }

    private void OnNextDayButtonClicked()
    {
        if (EventManager.current != null)
        {
            EventManager.current.StartNextDay();
        }
    }

    private void OnShopOpened()
    {
        UpdateMoneyDisplay();
        RefreshUpgradeButtons();
        UpdateDayDisplay();
    }

    private void OnShopClosed()
    {
        // Nothing needed here
    }

    // update function to check if the player has enough money to purchase the upgrade
    public void Update() {
        foreach (var tieredUpgradeController in tieredUpgradeControllers)
        {
            int price = int.Parse(tieredUpgradeController.priceText.text);
            if (gameState.GetMoney() >= price)
            {
                tieredUpgradeController.EnablePurchaseButton();
            }
            else
            {
                tieredUpgradeController.DisablePurchaseButton();
            }
        }
    }
}