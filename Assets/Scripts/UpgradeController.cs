using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeController : MonoBehaviour
{
    public List<Upgrade> upgrades;
    public Upgrade fullUpgrade;
    public int level = 0;

    public TextMeshProUGUI Title;
    public TextMeshProUGUI Description;

    public RawImage Icon;
    public TextMeshProUGUI Cost;
    public Button UpgradeButton;
    public GameMasterScript gameMaster;
    private Upgrade currentUpgrade;

    void Start()
    {
        UpgradeButton.onClick.AddListener(() => StartCoroutine(Upgrade()));
        SetUpgrade(upgrades[level]);
    }

    // Disable upgrade button if not enough money
    void Update()
    {
        bool hasEnoughMoney = gameMaster.GetMoney() >= currentUpgrade.cost;
        bool isMaxLevel = level >= upgrades.Count;
        UpgradeButton.interactable = hasEnoughMoney && !isMaxLevel;
    }

    public void IncrementUpgrade()
    {
        level++;
        if (level >= upgrades.Count)
        {
            SetUpgrade(fullUpgrade);
        }
        else
        {
            SetUpgrade(upgrades[level]);
        }
    }

    public void SetUpgrade(Upgrade upgrade)
    {
        Title.text = upgrade.title;
        Description.text = upgrade.description;
        Icon.texture = upgrade.icon.texture;
        Cost.text = upgrade.cost.ToString();
        currentUpgrade = upgrade;
    }

    public IEnumerator Upgrade()
    {
        if (!gameMaster.RemoveMoney(currentUpgrade.cost))
        {
            yield break;
        }
        bool passed = true;
        if (currentUpgrade.quiz != null)
        {
            yield return gameMaster.GiveQuiz(currentUpgrade.quiz, (bool _passed) =>
            {
                passed = _passed;
            });
        }
        if (passed)
        {
            IncrementUpgrade();
        }
    }
}
