using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoffeeApprovalUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the panel containing approval buttons")]
    public GameObject approvalPanel;
    
    [Tooltip("Button to approve coffee")]
    public Button approveButton;
    
    [Tooltip("Button to deny coffee")]
    public Button denyButton;
    
    private void OnEnable()
    {
        // Subscribe to events
        if (EventManager.current != null)
        {
            EventManager.current.onCoffeeApprovalUIRequested += SetButtonsActive;
        }
        else
        {
            Debug.LogError("CoffeeApprovalUI: EventManager.current is null!");
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events
        if (EventManager.current != null)
        {
            EventManager.current.onCoffeeApprovalUIRequested -= SetButtonsActive;
        }
    }
    
    private void Start()
    {
        // Initialize UI - keep panel visible but disable buttons
        if (approvalPanel != null)
        {
            approvalPanel.SetActive(true);
        }
        
        // Add button listeners and ensure buttons start disabled
        if (approveButton != null)
        {
            approveButton.onClick.AddListener(OnApproveClicked);
            approveButton.interactable = false;
        }
        
        if (denyButton != null)
        {
            denyButton.onClick.AddListener(OnDenyClicked);
            denyButton.interactable = false;
        }
        
        // Log initialization
        Debug.Log("CoffeeApprovalUI initialized - panel visible, buttons disabled");
    }
    
    private void SetButtonsActive(bool active)
    {
        Debug.Log("SetButtonsActive called with active: " + active);
        if (approveButton != null)
        {
            approveButton.interactable = active;
        }
        
        if (denyButton != null)
        {
            denyButton.interactable = active;
        }
    }
    
    private void OnApproveClicked()
    {
        // Disable buttons after clicking
        // SetButtonsActive(false);
        EventManager.current.ApproveCoffee();
    }
    
    private void OnDenyClicked()
    {
        // Disable buttons after clicking
        // SetButtonsActive(false);
        EventManager.current.DenyCoffee();
    }
} 