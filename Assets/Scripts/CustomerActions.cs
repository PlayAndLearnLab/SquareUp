using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class CustomerActions : MonoBehaviour
{
    #region Fields and Properties
    
    // References
    public GM gm;
    private AudioSource audioSource;
    private CustomerController controller;
    public LineController featureLineController;
    public LineController waitLineController;
    public UpgradeController orderStationUpgradeController;
    public Transform entrance;
    public Transform exit;
    public Customer customer;
    
    // Sprites
    public Sprite appleSprite;
    public Sprite candySprite;
    public Sprite pumpkinSprite;
    
    // Audio
    public AudioClip computerSound;
    
    // Animation
    [SerializeField] private GameObject moneyAnimationPrefab;
    [SerializeField] private Canvas targetCanvas;
    
    // State variables
    private static int SPEED = 10;
    private CoffeeOrder recievedCoffeeOrder;
    private string selectedFeature = "";
    private bool coffeeGiven = false;
    private bool coffeeApproved = false;
    private bool coffeeOrderPressed = false;
    private Coroutine tutorialCoroutine;
    private bool isDestroyed = false;
    private float waitTimeMultiplier = 1.0f;
    
    #endregion

    #region Initialization and Cleanup
    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        controller = GetComponent<CustomerController>();
    }

    void Start()
    {
        RegisterEventListeners();
    }

    void OnDestroy()
    {
        Debug.Log("OnDestroy");
        isDestroyed = true;
        CleanupCoroutines();
        UnregisterEventListeners();
    }
    
    private void RegisterEventListeners()
    {
        EventManager.current.onGiveCoffee += OnGiveCoffee;
        EventManager.current.onApproveCoffee += OnApproveCoffee;
        EventManager.current.onDenyCoffee += OnDenyCoffee;
        EventManager.current.onFeatureSelected += OnFeatureSelected;
        EventManager.current.onCoffeeOrderPressed += OnCoffeeOrderPressed;
        EventManager.current.onQueueBecameEmpty += OnQueueBecameEmpty;
        EventManager.current.onQueueGotFirstItem += OnQueueGotFirstItem;
        EventManager.current.onCoffeeProduced += OnCoffeeProduced;
    }
    
    private void UnregisterEventListeners()
    {
        EventManager.current.onFeatureSelected -= OnFeatureSelected;
        EventManager.current.onGiveCoffee -= OnGiveCoffee;
        EventManager.current.onApproveCoffee -= OnApproveCoffee;
        EventManager.current.onDenyCoffee -= OnDenyCoffee;
        EventManager.current.onCoffeeOrderPressed -= OnCoffeeOrderPressed;
        EventManager.current.onQueueBecameEmpty -= OnQueueBecameEmpty;
        EventManager.current.onQueueGotFirstItem -= OnQueueGotFirstItem;
        EventManager.current.onCoffeeProduced -= OnCoffeeProduced;
    }
    
    private void CleanupCoroutines()
    {
        if (tutorialCoroutine != null)
        {
            StopCoroutine(tutorialCoroutine);
            tutorialCoroutine = null;
        }
    }
    
    public void InitializeCustomer(Customer customer)
    {
        this.customer = customer;
        
        // Instantiate the customer prefab as a child of this object
        GameObject customerObject = Instantiate(customer.customerPrefab, transform);

        // Set the transform values
        customerObject.transform.localPosition = new Vector3(0.79f, 1.7f, 0.0001525647f);
        customerObject.transform.localRotation = Quaternion.Euler(0, 1, 0);
        customerObject.transform.localScale = new Vector3(-3, 3, 3);

        // Set the wait time multiplier based on the customer type
        switch (customer.customerType)
        {
            case CustomerType.Impatient:
                waitTimeMultiplier = 0.5f;
                break;
            case CustomerType.Patient:
                waitTimeMultiplier = 10.0f;
                break;
        }

        // Get child with name "UnitRoot" and set animator
        Transform unitRoot = customerObject.transform.Find("UnitRoot");
        controller.SetAnimator(unitRoot.GetComponent<Animator>());
    }
    
    #endregion

    #region Event Handlers
    
    private void OnFeatureSelected(string featureId)
    {
        selectedFeature = featureId;
    }
    
    private void OnQueueBecameEmpty()
    {
        EventManager.current.RequestCoffeeApprovalUI(false);
    }
    
    private void OnQueueGotFirstItem()
    {
        EventManager.current.RequestCoffeeApprovalUI(true);
    }

    private void OnCoffeeProduced(Coffee coffee) {
        coffeeApproved = false;
        StartCoroutine(TutorialHelper.ShowTutorialStepUntil(7, () => coffeeApproved));
        if (gm.GetQualityLevel() > 0) {
            if (EventManager.current.HasNextCoffee())
            {
                Debug.Log("Coffee in queue will be used for the customer");
                
                // Check if this approval was correct (customer wanted this coffee flavor)
                CoffeeOrder nextCoffee = EventManager.current.PeekNextCoffee();
                if (nextCoffee.Coffee.flavor == customer.expectedFlavor)
                {
                    // This was a correct approval
                    EventManager.current.CorrectlyApprovedCoffee();
                }
                coffeeApproved = true;
            } else {
                OnDenyCoffee();
            }
        }
    }
    private void OnApproveCoffee()
    {
        // --- DEBUG START ---
        string customerName = this.gameObject.name; // Assuming the GO name is useful
        MovementController thisController = this.controller; // Cache for logging
        Debug.Log($"[START-OnApproveCoffee-{customerName}] Event received by instance.");

        MovementController frontCustomer = waitLineController.GetFrontCustomer();
        int currentPosition = waitLineController.GetPositionInLine(thisController);

        Debug.Log($"[START-OnApproveCoffee-{customerName}] Current Front Customer reported by LineController: '{frontCustomer?.name}'. This customer's position: {currentPosition}");
        // --- DEBUG END ---

        // Check if this customer is at the front of the wait line
        if (frontCustomer == thisController)
        {
            Debug.Log($"[START-OnApproveCoffee-{customerName}] Matched front customer. Setting coffeeApproved=true.");
            if (EventManager.current.HasNextCoffee())
            {
                Debug.Log($"[START-OnApproveCoffee-{customerName}] Coffee in queue will be used.");

                // Check if this approval was correct (customer wanted this coffee flavor)
                CoffeeOrder nextCoffee = EventManager.current.PeekNextCoffee();
                if (nextCoffee.Coffee.flavor == customer.expectedFlavor)
                {
                    // This was a correct approval
                    EventManager.current.CorrectlyApprovedCoffee();
                }
                coffeeApproved = true;

            }
            else
            {
                Debug.LogWarning($"[START-OnApproveCoffee-{customerName}] No coffee in the queue to approve.");
            }
        }
        else
        {
            Debug.Log($"[START-OnApproveCoffee-{customerName}] This customer IS NOT the front customer reported by LineController. Ignoring approval.");
        }
    }
    
    private void OnDenyCoffee()
    {
        // Check if this customer is at the front of the wait line
        MovementController frontCustomer = waitLineController.GetFrontCustomer();
        
            
        // Explode the coffee at the top of the queue if there is one
        if (EventManager.current.HasNextCoffee())
        {
            // Check if this denial was correct (customer didn't want this coffee flavor)
            CoffeeOrder coffeeToExplode = EventManager.current.PeekNextCoffee();
            Debug.Log("Exploding coffee from the queue");
            if (frontCustomer == controller)
            {
                if (coffeeToExplode.Coffee.flavor != customer.expectedFlavor)
                {
                    // This was a correct denial
                    EventManager.current.CorrectlyDeniedCoffee();
                }
            }
        }
        else
        {
            Debug.LogWarning("No coffee in the queue to explode");
        }
            
        // Clear the received coffee order to ensure the customer waits for a new coffee
        ClearCoffee();
    }

    private void OnCoffeeOrderPressed()
    {
        coffeeOrderPressed = true;
    }
    
    #endregion

    #region Helper Methods
    
    private bool HasCorrectCoffee()
    {
        return recievedCoffeeOrder.Coffee.flavor == customer.expectedFlavor;
    }

    private bool HasCoffee()
    {
        return recievedCoffeeOrder != null;
    }

    private void ClearCoffee()
    {
        recievedCoffeeOrder = null;
    }

    private void OnGiveCoffee(Coffee coffee)
    {
        coffeeGiven = true;
    }

    private void ClearCoffeeGiven()
    {
        coffeeGiven = false;
    }


    private void ClearCoffeeOrderPressed()
    {
        coffeeOrderPressed = false;
    }

    private SpeechBubbleController.BubbleIcon GetBubbleIcon()
    {
        return SpeechBubbleController.BubbleIcon.ActiveSpeaker;
    }
    
    private IEnumerator WaitForFeatureSelectionWithSprite()
    {
        switch (customer.expectedFlavor)
        {
            case CoffeeFlavor.Candy:
                yield return controller.WaitForConditionWithSprite(candySprite, (int)(10 * waitTimeMultiplier), () => selectedFeature != "" || isDestroyed);
                break;
            case CoffeeFlavor.Apple:
                yield return controller.WaitForConditionWithSprite(appleSprite, (int)(10 * waitTimeMultiplier), () => selectedFeature != "" || isDestroyed);
                break;
            case CoffeeFlavor.Pumpkin:
                yield return controller.WaitForConditionWithSprite(pumpkinSprite, (int)(10 * waitTimeMultiplier), () => selectedFeature != "" || isDestroyed);
                break;
        }
    }
    
    private void ShowMoneyAnimation(int amount)
    {
        if (moneyAnimationPrefab == null || targetCanvas == null)
        {
            Debug.LogWarning("Money animation prefab or target canvas is not assigned!");
            return;
        }
        
        // Convert world position to screen position
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        
        // Instantiate the prefab as a child of the canvas
        GameObject moneyAnim = Instantiate(moneyAnimationPrefab, targetCanvas.transform);
        
        // Set the position using RectTransform
        RectTransform rectTransform = moneyAnim.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.position = screenPos;
        }
        
        MoneyAnimation moneyAnimation = moneyAnim.GetComponent<MoneyAnimation>();
        if (moneyAnimation != null)
        {
            moneyAnimation.SetAmount(amount);
        }
    }
    
    #endregion

    #region Customer Workflows
    
    /// <summary>
    /// Handles the main customer workflow in training mode
    /// </summary>
    public IEnumerator StartTrainingActions()
    {
        if (isDestroyed) yield break;

        // Wait in the feature line
        StartCoroutine(featureLineController.WaitInLine(SPEED, controller));
        if (isDestroyed) yield break;

        // Initial interaction based on feature level
        yield return HandleInitialInteraction();
        if (isDestroyed) yield break;
        
        // Customer immediately moves to wait line after ordering
        MoveToWaitLine();
        if (isDestroyed) yield break;
        
        // Wait for coffee to be given and approved
        yield return WaitForCoffeeApproval();
        if (isDestroyed) yield break;

        // Process the coffee order
        yield return ProcessCoffeeOrder();
    }

    private IEnumerator HandleInitialInteraction()
    {
        if (TutorialHelper.IsInTutorial) {
            Debug.Log("Pause timer");
            EventManager.current.PauseTimer();
        }
        // For level 0, need to be clicked first
        bool clicked = false;
        tutorialCoroutine = StartCoroutine(TutorialHelper.ShowTutorialStepUntil(1, () => clicked || isDestroyed));
        
        // Character waits to be clicked
        yield return controller.WaitForClickWithIcon(SpeechBubbleController.BubbleIcon.InactiveSpeaker, (int)(10 * waitTimeMultiplier), (bool wasClicked) =>
        {
            clicked = wasClicked;
        });
        
        if (isDestroyed) yield break;
        if (clicked)
        {
            if (gm.GetFeatureLevel() == 0) 
            {
                yield return controller.PlayClip(customer.expectedFlavorAudio);
                if (isDestroyed) yield break;
            }
            else
            {
                // Higher levels just play the computer sound
                controller.ShowBubbleWithSprite(customer.expectedFlavorSprite);
                yield return controller.PlayClip(computerSound);
            }
        }
        else
        {
            // Customer leaves if not clicked
            yield return LeaveEarly();
            yield break;
        }
    }

    // Used for customers leaving due to abandonment (not being clicked)
    private IEnumerator LeaveEarly()
    {
        if (featureLineController != null)
        {
            featureLineController.RemoveFromLine(controller);
        }
        if (waitLineController != null) {
            waitLineController.RemoveFromLine(controller);
        }
        if (!isDestroyed)
        {
            // Trigger the event before destroying the object
            if (EventManager.current != null)
            {
                EventManager.current.CustomerLeftEarly();
            }
            yield return controller.Walk(Vector3.left * 10, SPEED);
            Destroy(gameObject);
        }
    }

    // Generic method for customer leaving, without triggering early abandonment
    private IEnumerator LeaveBuilding()
    {
        if (featureLineController != null)
        {
            featureLineController.RemoveFromLine(controller);
        }
        if (waitLineController != null) {
            waitLineController.RemoveFromLine(controller);
        }
        if (!isDestroyed)
        {
            yield return controller.Walk(Vector3.left * 10, SPEED);
            Destroy(gameObject);
        }
    }

    private void MoveToWaitLine()
    {
        ClearCoffeeGiven();
        featureLineController.RemoveFromLine(controller);
        controller.HideSpeechBubble();
        
        // Wait in line
        StartCoroutine(waitLineController.WaitInLine(SPEED, controller));
    }
    
    private IEnumerator WaitForCoffeeApproval()
    {
        if (gm.GetFeatureLevel() == 0)
        {
            if (TutorialHelper.IsInTutorial) {
                controller.SetTimeFloor(10);
            } else {
                controller.SetTimeFloor(120);
            }
            // Wait for coffee to be given and approved
            yield return controller.WaitForConditionWithIcon(SpeechBubbleController.BubbleIcon.Timer, (int)(30 * waitTimeMultiplier), () => {
                // Check the CURRENT front customer each time inside the lambda
                MovementController currentFront = waitLineController.GetFrontCustomer();
                return (currentFront == controller && coffeeApproved) || isDestroyed;
            });
        }
        else
        {
            // Wait for coffee with appropriate sprite
            yield return controller.WaitForConditionWithSprite(customer.expectedFlavorSprite, (int)(30 * waitTimeMultiplier), () => {
                // Check the CURRENT front customer each time inside the lambda
                MovementController currentFront = waitLineController.GetFrontCustomer();
                return (currentFront == controller && coffeeApproved) || isDestroyed;
            });
        }

        // Re-check the condition properly before leaving, using the CURRENT front customer
        MovementController finalFront = waitLineController.GetFrontCustomer();
        if (!(finalFront == controller && coffeeApproved))
        {
             // Check if already destroyed before trying to leave
            if (!isDestroyed) {
                 // Use this.gameObject.name for potentially more specific logging
                 Debug.LogWarning($"[WaitForCoffeeApproval-{this.gameObject.name}] Condition not met after wait (Final Front: {finalFront?.name}, This: {controller?.name}, Approved: {coffeeApproved}). Leaving.");
                 // Customer is timing out or leaving the line without being served
                 yield return LeaveEarly();
            } else {
                 Debug.Log($"[WaitForCoffeeApproval-{this.gameObject.name}] Condition not met but object was already destroyed.");
            }
        }
        else
        {
             Debug.Log($"[WaitForCoffeeApproval-{this.gameObject.name}] Condition met successfully. Proceeding.");
        }
    }

    private IEnumerator ProcessCoffeeOrder()
    {
        if (isDestroyed) yield break;

        // Process the coffee normally
        recievedCoffeeOrder = EventManager.current.GetNextCoffee();
        if (recievedCoffeeOrder != null && recievedCoffeeOrder.CoffeeObject != null)
        {
            recievedCoffeeOrder.CoffeeObject.SetActive(false);
        }

        // Remove from wait line
        if (waitLineController != null)
        {
            waitLineController.RemoveFromLine(controller);
            Debug.Log("Customer removed from wait line after processing coffee order");
        }

        // Hide speech bubble
        if (controller != null)
        {
            controller.HideSpeechBubble();
        }

        // Handle customer reaction based on coffee correctness
        if (HasCoffee())
        {
            if (controller != null)
            {
                controller.ShowCoffee();
            }
            EventManager.current.CoffeeServed(HasCorrectCoffee());
            
            if (HasCorrectCoffee())
            {
                yield return HandleCorrectCoffeeReaction();
            }
            else
            {
                yield return HandleIncorrectCoffeeReaction();
            }
            EventManager.current.IncrementCustomerCount();
        }
    }

    private IEnumerator HandleCorrectCoffeeReaction()
    {
        if (isDestroyed) yield break;
        
        yield return controller.Happy();
        
        // Give money to the player
        EventManager.current.MoneyGained(customer.moneyReward);
        
        // Show money animation
        ShowMoneyAnimation(customer.moneyReward);
        
        // Leave the shop (directly, without calling LeaveBuilding)
        yield return controller.WalkTo(exit.position, SPEED);
    }

    private IEnumerator HandleIncorrectCoffeeReaction()
    {
        if (isDestroyed) yield break;
        
        // Our ProcessCoffeeOrder method already removes the customer from the wait line before calling
        // this method, but let's make sure it's removed to prevent the bug
        if (waitLineController != null)
        {
            waitLineController.RemoveFromLine(controller);
            Debug.Log("Customer removed from wait line after receiving incorrect coffee");
        }
        
        // Trigger the customer died event
        if (EventManager.current != null)
        {
            EventManager.current.CustomerDied();
        }
        
        // Play the death animation
        yield return controller.Die();
        
        // Ensure the parent GameObject is properly destroyed after the death animation
        // Die() destroys the controller's GameObject but not the parent CustomerActions GameObject
        if (!isDestroyed)
        {
            Destroy(gameObject);
        }
    }
    
    #endregion
}
