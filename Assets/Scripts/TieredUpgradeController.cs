using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class TieredUpgradeController : MonoBehaviour
{
    public GameObject bubbleContainer;
    public Color[] tierColors = new Color[] { 
        new Color(0.5f, 0.5f, 0.5f, 0.5f),    // Inactive color
        new Color(1.0f, 0.5f, 0.0f, 0.5f),    // Orange
        new Color(1.0f, 1.0f, 0.0f, 0.5f),    // Yellow
        new Color(0.0f, 1.0f, 0.0f, 0.5f),    // Green
        new Color(0.5f, 0.0f, 0.5f, 0.5f)     // Purple
    };

    public AudioClip purchaseSound;
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Default inactive color

    public Button purchaseButton;
    public TextMeshProUGUI priceText;
    private ShopUpgrade upgrade;
    private GameObject[] tierBubbles;
    private Image[] bubbleImages;
    private int upgradeLevel = 0;
    private const int BASE_PRICE = 10;
    
    void Start()
    {
        tierBubbles = new GameObject[bubbleContainer.transform.childCount];
        bubbleImages = new Image[bubbleContainer.transform.childCount];
        
        for (int i = 0; i < bubbleContainer.transform.childCount; i++)
        {
            tierBubbles[i] = bubbleContainer.transform.GetChild(i).gameObject;
            bubbleImages[i] = tierBubbles[i].GetComponent<Image>();
            if (bubbleImages[i] == null)
            {
                Debug.LogWarning("Bubble " + i + " has no Image component");
            }
        }
        
        UpdatePriceText();
    }

    public void Initialize(ShopUpgrade _upgrade, Action<ShopUpgrade> onPurchaseButtonClick)
    {
        upgrade = _upgrade;
        purchaseButton.onClick.AddListener(() => {
            onPurchaseButtonClick(upgrade);
            upgradeLevel++;
            RefreshDisplay();
            AudioSource.PlayClipAtPoint(purchaseSound, transform.position);
        });
        RefreshDisplay();
    }

    private void UpdatePriceText()
    {
        if (priceText != null)
        {
            int nextPrice = BASE_PRICE * (int)Mathf.Pow(2, upgradeLevel);
            priceText.text = nextPrice.ToString();
        }
    }

    public void RefreshDisplay()
    {
        if (upgrade == null) return;
        if (tierBubbles == null)
        { 
            Debug.LogWarning("TierBubbles is null");
            return;
        }

        UpdatePriceText();

        for (int i = 0; i < tierBubbles.Length; i++)
        {
            // Keep all bubbles active
            tierBubbles[i].SetActive(true);
            
            // Change color based on level
            if (bubbleImages[i] != null)
            {
                if (i < upgradeLevel && i < tierColors.Length - 1)
                {
                    // Light up with the color at the same index
                    bubbleImages[i].color = tierColors[i + 1]; // +1 because index 0 is inactive color
                }
                else
                {
                    // Not achieved yet, use inactive color
                    bubbleImages[i].color = inactiveColor;
                }
            }
        }
    }

    // public function to enable the purchase button
    public void EnablePurchaseButton()
    {
        purchaseButton.interactable = true;
    }

    // public function to disable the purchase button
    public void DisablePurchaseButton()
    {
        purchaseButton.interactable = false;
    }
}
