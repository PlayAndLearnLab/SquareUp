using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUpgradeButton : MonoBehaviour
{
    public ShopUpgrade upgrade;
    public TextMeshProUGUI titleText;   // The main text showing upgrade name
    public TextMeshProUGUI costText;    // The text showing the cost
    public GameObject doneUpgradeButton;
    public GameObject upgradeButton;
    public GameObject lockedUpgradeButton;
    public Button button;
    public Button lockedButton;  // Reference to the locked button component
    public Image iconImage;             // Reference to the icon image component
    [SerializeField] private AudioSource audioSource;  // Reference to the AudioSource component

    public void Initialize(ShopUpgrade upgrade, System.Action<ShopUpgrade> onPurchase, System.Action<ShopUpgrade> onUnlock)
    {
        this.upgrade = upgrade;
        button.onClick.AddListener(() =>
        {
            if (audioSource != null)
            {
                audioSource.Play();
            }
            onPurchase(upgrade);
        });

        // Add listener for the locked button
        if (lockedButton != null)
        {
            lockedButton.onClick.AddListener(() =>
            {
                if (audioSource != null)
                {
                    audioSource.Play();
                }
                onUnlock(upgrade);
            });
        }

        // Set the icon if available
        if (iconImage != null && upgrade.icon != null)
        {
            iconImage.sprite = upgrade.icon;
        }

        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        titleText.text = upgrade.upgradeName;
        costText.text = $"${upgrade.GetNextLevelCost:N0}";

        // Show appropriate button state based on conditions
        bool isLocked = !upgrade.prerequisiteQuizCompleted;
        bool isMaxLevel = upgrade.IsMaxLevel;

        doneUpgradeButton.SetActive(isMaxLevel && !isLocked);
        upgradeButton.SetActive(!isMaxLevel && !isLocked);
        lockedUpgradeButton.SetActive(isLocked);
    }
}