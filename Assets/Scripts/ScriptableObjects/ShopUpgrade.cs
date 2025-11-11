using UnityEngine;

[CreateAssetMenu(fileName = "New Shop Upgrade", menuName = "Shop/ShopUpgrade")]
public class ShopUpgrade : ScriptableObject
{
    [Header("Basic Info")]
    public string upgradeName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;
    public Quiz prerequisiteQuiz;
    public bool prerequisiteQuizCompleted = false;

    [Header("Upgrade Properties")]
    public UpgradeCategory category;
    public int[] costPerLevel;  // Array of costs for each level
    public float[] valuePerLevel;  // Array of actual upgrade values per level
    public int maxLevel = 3;
    [SerializeField] private int defaultLevel = 0;  // Store the default level
    [SerializeField] private int baseCost = 10;
    private int _currentLevel;  // Runtime-only field
    public int currentLevel
    {
        get => _currentLevel;
        set => _currentLevel = value;
    }

    [Header("UI Display")]
    public string valueFormat = "{0}";  // Format string for how the value should be displayed (e.g. "+{0}% Speed")

    // Create a runtime copy of the upgrade
    public ShopUpgrade CreateRuntimeCopy()
    {
        var copy = Instantiate(this);
        copy._currentLevel = defaultLevel;
        return copy;
    }

    public string GetValueDisplay(int level)
    {
        return string.Format(valueFormat, valuePerLevel[level]);
    }

    public bool IsMaxLevel => currentLevel >= maxLevel;

    public int GetNextLevelCost => baseCost * (int)Mathf.Pow(2, currentLevel);
}

public enum UpgradeCategory
{
    Feature,  // New gameplay features or mechanics
    Quality,   // Improvements to existing features
    Accuracy,  // Improvements to accuracy
    Speed,     // Improvements to speed
}