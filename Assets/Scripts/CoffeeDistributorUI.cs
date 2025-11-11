using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoffeeDistributorUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the CoffeeDistributorController")]
    public CoffeeDistributorController coffeeDistributor;
    
    [Header("Accuracy UI")]
    [Tooltip("Slider to control the accuracy")]
    public Slider accuracySlider;
    [Tooltip("Optional text to display the accuracy value")]
    public TextMeshProUGUI accuracyValueText;
    
    [Header("Speed UI")]
    [Tooltip("Slider to display the speed (read-only)")]
    public Slider speedSlider;
    [Tooltip("Optional text to display the speed value")]
    public TextMeshProUGUI speedValueText;
    
    [Header("Display Settings")]
    [Tooltip("Format string for displaying values (e.g. '{0}%' or '{0:F1}')")]
    public string valueFormat = "{0}%";
    
    private void OnEnable()
    {
        // Subscribe to the EventManager event
        if (EventManager.current != null)
        {
            EventManager.current.onCoffeeDistributorSpeedChanged += UpdateUI;
        }
        else
        {
            Debug.LogError("CoffeeDistributorUI: EventManager.current is null!");
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from the EventManager event
        if (EventManager.current != null)
        {
            EventManager.current.onCoffeeDistributorSpeedChanged -= UpdateUI;
        }
        
        // Remove slider listener (No longer needed as Accuracy slider is read-only)
        // if (accuracySlider != null)
        // {
        //     accuracySlider.onValueChanged.RemoveListener(OnAccuracySliderChanged);
        // }
    }
    
    private void Start()
    {
        if (coffeeDistributor == null)
        {
            Debug.LogError("CoffeeDistributorUI: No CoffeeDistributorController assigned!");
            return;
        }
        
        // Initialize UI elements
        InitializeUI();
        
        // Initial UI update with new properties
        UpdateUI(coffeeDistributor.CurrentSpeed, coffeeDistributor.CurrentAccuracy);
    }
    
    private void InitializeUI()
    {
        // Setup accuracy slider (now read-only)
        if (accuracySlider != null)
        {
            accuracySlider.minValue = 0f; // Simple 0-100 range
            accuracySlider.maxValue = 100f;
            accuracySlider.value = coffeeDistributor.CurrentAccuracy; // Use new property
            accuracySlider.interactable = false; // Make read-only
            
            // Remove listener registration (No longer needed)
            // accuracySlider.onValueChanged.AddListener(OnAccuracySliderChanged);
        }
        
        // Setup speed slider (read-only)
        if (speedSlider != null)
        { 
            speedSlider.minValue = 0f;
            speedSlider.maxValue = 100f;
            speedSlider.value = coffeeDistributor.CurrentSpeed; // Use new property
            
            // Disable the speed slider so it can't be manipulated
            speedSlider.interactable = false;
        }
    }
    
    // Removed OnAccuracySliderChanged method
    
    private void UpdateUI(float speed, float accuracy)
    {
        // Update accuracy UI
        if (accuracySlider != null)
        {
            // Removed minimum value update
            accuracySlider.value = accuracy;
        }
        
        if (accuracyValueText != null)
        {
            // Round to nearest whole number
            int roundedAccuracy = Mathf.RoundToInt(accuracy);
            accuracyValueText.text = string.Format(valueFormat, roundedAccuracy);
        }
        
        // Update speed UI
        if (speedSlider != null)
        { 
            speedSlider.value = speed;
        }
        
        if (speedValueText != null)
        {
            // Round to nearest whole number
            int roundedSpeed = Mathf.RoundToInt(speed);
            speedValueText.text = string.Format(valueFormat, roundedSpeed);
        }
    }
} 