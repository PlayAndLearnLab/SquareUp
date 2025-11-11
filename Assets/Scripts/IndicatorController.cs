using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Added for UI.Image

public class IndicatorController : MonoBehaviour
{
    public enum IndicatorType
    {
        Speed,
        Accuracy,
    }

    public IndicatorType indicatorType;
    public Color[] tierColors = new Color[] { 
        new Color(0.5f, 0.5f, 0.5f, 0.5f),    // Inactive color
        new Color(1.0f, 0.5f, 0.0f, 0.5f),    // Orange
        new Color(1.0f, 1.0f, 0.0f, 0.5f),    // Yellow
        new Color(0.0f, 1.0f, 0.0f, 0.5f),    // Green
        new Color(0.5f, 0.0f, 0.5f, 0.5f)     // Purple
    };
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Default inactive color
    public int maxLevel = 4; // Maximum upgrade level (to normalize the value)

    private List<Image> indicatorBubbles = new List<Image>();

    public void Start()
    {
        // indicators are the children of the gameobject
        foreach (Transform child in transform)
        {
            Image image = child.GetComponent<Image>();
            if (image != null)
            {
                indicatorBubbles.Add(image);
                image.color = inactiveColor; // Start all as inactive
            }
            else
            {
                Debug.LogWarning($"Child object {child.name} is missing an Image component.", child.gameObject);
            }
        }

        // Subscribe to the upgrade event
        if (EventManager.current != null)
        {
            EventManager.current.onUpgradeApplied += OnUpgradeApplied;
        }
        else
        {
            Debug.LogError("EventManager.current is null! Make sure EventManager exists in the scene.", this);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from the event when this object is destroyed
        if (EventManager.current != null)
        {
            EventManager.current.onUpgradeApplied -= OnUpgradeApplied;
        }
    }

    private void OnUpgradeApplied(string upgradeName, UpgradeCategory category, int newLevel)
    {
        // Check if this upgrade category matches our indicator type
        bool isRelevantUpgrade = false;

        Debug.Log("IndicatorController: OnUpgradeApplied called with upgradeName: " + upgradeName + ", category: " + category + ", newLevel: " + newLevel);
        
        switch (indicatorType)
        {
            case IndicatorType.Speed:
                isRelevantUpgrade = (category == UpgradeCategory.Speed);
                break;
            case IndicatorType.Accuracy:
                isRelevantUpgrade = (category == UpgradeCategory.Accuracy);
                break;
        }

        if (isRelevantUpgrade)
        {
            UpdateIndicator(newLevel);
        }
    }

    public void UpdateIndicator(float value)
    {
        // Round to nearest int since we're working with discrete levels
        int level = Mathf.RoundToInt(value);
        
        for (int i = 0; i < indicatorBubbles.Count; i++)
        {
            if (indicatorBubbles[i] != null)
            {
                if (i < level && i < tierColors.Length - 1)
                {
                    // Light up with the appropriate tier color (offset by 1 since index 0 is inactive)
                    indicatorBubbles[i].color = tierColors[i + 1];
                }
                else
                {
                    // Not achieved yet, use inactive color
                    indicatorBubbles[i].color = inactiveColor;
                }
            }
        }
    }
} 