using System.Collections;
using UnityEngine;

public class AICustomerActions : MonoBehaviour
{
    #region Fields
    private AICustomer data;
    private CoffeeBrain brain;
    private CustomerController controller;
    private MovementController movController;
    public AICustomer aiCustomer;

    // Line references (Assign these via GM when spawning)
    public LineController featureLineController;
    public LineController waitLineController;
    public Transform exit;

    private static int SPEED = 10;
    private bool isDestroyed = false;
    private float waitTimeMultiplier = 1.0f;
    private bool coffeeApproved = false;
    private bool coffeeOrderPressed = false;
    private bool isProcessing = false;
    private bool isLeavingOrConsuming = false;
    public bool canAcceptCoffee = false;

    [Header("UI & FX")]
    [SerializeField] private GameObject moneyAnimationPrefab;
    [SerializeField] private Canvas targetCanvas;
    #endregion

    private void Start()
    {
        RegisterEventListeners();

        // 1. Auto-find the Target Canvas if it's null
        if (targetCanvas == null)
        {
            GameObject canvasObj = GameObject.Find("GameCanvas");
            if (canvasObj != null)
            {
                targetCanvas = canvasObj.GetComponent<Canvas>();
            }
            else
            {
                // Alternative: Just find any Canvas in the scene
                targetCanvas = FindObjectOfType<Canvas>();
            }
        }

        // 2. Auto-load the Money Animation Prefab if it's null
        // (This assumes the prefab is in a folder named 'Resources')
        if (moneyAnimationPrefab == null)
        {
            moneyAnimationPrefab = Resources.Load<GameObject>("MoneyPrefab");

            // If you don't use Resources, you'll need to assign this 
            // in the Prefab Asset in your Project window, not the hierarchy.
        }
    }

    private void RegisterEventListeners()
    {
        EventManager.current.onApproveCoffee += OnApproveClicked;
        EventManager.current.onQueueGotFirstItem += ShowUIIfFront;
        EventManager.current.onQueueBecameEmpty += HideUI;
        EventManager.current.onCoffeeOrderPressed += OnCoffeeOrderPressed;
        EventManager.current.onCoffeeProduced += OnCoffeeProduced;
        EventManager.current.onCoffeeReadyForCustomer += onCoffeeReadyForCustomer;

    }

    private void OnDisable()
    {
        if (EventManager.current != null)
        {
            EventManager.current.onApproveCoffee -= OnApproveClicked;
            EventManager.current.onQueueGotFirstItem -= ShowUIIfFront;
            EventManager.current.onQueueBecameEmpty -= HideUI;
            EventManager.current.onCoffeeOrderPressed -= OnCoffeeOrderPressed;
            EventManager.current.onCoffeeProduced -= OnCoffeeProduced;
            EventManager.current.onCoffeeReadyForCustomer -= onCoffeeReadyForCustomer;

        }
    }

    private void onCoffeeReadyForCustomer(Coffee coffee)
    {
        if (isLeavingOrConsuming || isDestroyed) return;

        // Check if I am the one who should take this coffee
        if (waitLineController != null && waitLineController.GetFrontCustomer() == movController)
        {
            Debug.Log($"{gameObject.name} (Front) saw coffee arrive. Updating flags.");

            // CRITICAL: Flip these flags so WaitForCoffeeApproval() exits its loop
            coffeeApproved = true;

            // If we aren't already in the middle of a transition, start the pickup
            if (!isLeavingOrConsuming)
            {
                isLeavingOrConsuming = true;
                StopCoroutine("WaitForCoffeeApproval");
                StartCoroutine(ProcessCoffeeOrder());
            }
        }
    }


    private void OnCoffeeProduced(Coffee coffee)
    {
        coffeeApproved = false;
        //StartCoroutine(TutorialHelper.ShowTutorialStepUntil(7, () => coffeeApproved));

        if (EventManager.current.HasNextCoffee())
        {
            //Debug.Log("Coffee in queue will be used for the customer");

            // Check if this approval was correct (customer wanted this coffee flavor)
            CoffeeOrder nextCoffee = EventManager.current.PeekNextCoffee();
            //if (nextCoffee.Coffee.flavor == aiCustomer.expectedFlavor)
            //{
            //    // This was a correct approval
            //    EventManager.current.CorrectlyApprovedCoffee();
            //}
            coffeeApproved = true;
        }


    }

    private void ShowUIIfFront()
    {
        // Only show the buttons if THIS customer is actually at the front of the wait line
        // AND there is actually a coffee ready.
        if (waitLineController.GetFrontCustomer() == movController && EventManager.current.HasNextCoffee())
        {
            EventManager.current.RequestCoffeeApprovalUI(true);
        }
    }

    private void HideUI()
    {
        EventManager.current.RequestCoffeeApprovalUI(false);
    }

    private void OnCoffeeOrderPressed()
    {
        //coffeeOrderPressed = true;
        if (featureLineController.GetFrontCustomer() == movController)
        {
            coffeeOrderPressed = true;
        }
    }

    private void CheckIfThisCoffeeIsForMe(Coffee coffee)
    {
        // In a museum exhibit with one customer at a time, 
        // the first customer in the 'WaitLine' is the one who gets the coffee.
        MovementController movController = GetComponent<MovementController>();

        if (movController != null && waitLineController != null)
        {
            if (waitLineController.GetPositionInLine(movController) == 1)
            {
                //HandleAIResult(coffee);
                //Debug.Log("AI predicts");
            }
        }
    }

    private void OnApproveClicked()
    {
        if (waitLineController.GetFrontCustomer() == movController)
        {
            coffeeApproved = true;
        }
    }




    public void InitializeCustomer(AICustomer customerData)
    {
        this.data = customerData;
        this.brain = FindObjectOfType<CoffeeBrain>();
        this.controller = GetComponent<CustomerController>();

        this.coffeeApproved = false;
        this.coffeeOrderPressed = false;
        this.isDestroyed = false;

        if (controller == null)
        {
            Debug.Log("Error, controller == null");
        }
        this.movController = GetComponent<MovementController>();

        // 1. Instantiate visuals (Same as traditional)
        GameObject customerObject = Instantiate(data.customerPrefab, transform);
        customerObject.transform.localPosition = new Vector3(0.79f, 1.7f, 0.0001525647f);
        customerObject.transform.localRotation = Quaternion.Euler(0, 1, 0);
        customerObject.transform.localScale = new Vector3(-3, 3, 3);

        controller.RefreshReferences();

        // 2. Set wait logic based on your new AICustomerType
        switch (data.customerType)
        {
            case AICustomerType.Impatient: waitTimeMultiplier = 0.5f; break;
            case AICustomerType.Patient: waitTimeMultiplier = 2.0f; break;
            case AICustomerType.Grumpy: waitTimeMultiplier = 0.8f; break;
        }

        Transform unitRoot = customerObject.transform.Find("UnitRoot");
        controller.SetAnimator(unitRoot.GetComponent<Animator>());


    }

    public IEnumerator StartTrainingActions()
    {
        if (isDestroyed) yield break;

        isProcessing = false;
        canAcceptCoffee = false;
        controller.RefreshReferences();

        Debug.Log($"[Action-Step 1] {gameObject.name} entering Order Line.");

        //if (TutorialHelper.IsInTutorial)
        //{
        //    Debug.Log("Pause timer");
        //    EventManager.current.PauseTimer();
        //}

        // 1. JOIN THE ORDER LINE — enqueue and wait inline (avoids cross-MonoBehaviour coroutine yield)
        featureLineController.EnqueueCustomer(SPEED, movController);
        float lineWaitStart = Time.time;
        yield return new WaitUntil(() => {
            if (isDestroyed) return true;
            if (Time.time - lineWaitStart > 120f) return true; // safety timeout
            int pos = featureLineController.GetPositionInLine(movController);
            return pos == 1 || pos == -1;
        });

        if (isDestroyed) yield break;
        if (featureLineController.GetPositionInLine(movController) != 1) yield break;

        Debug.Log($"[Action-Step 2] {gameObject.name} reached front of order line.");

        // 2. Wait until physically arrived (distance check)
        float arrivalTimeout = 5f;
        while (arrivalTimeout > 0)
        {
            float dist = Vector3.Distance(transform.position, featureLineController.GetPositionVector(1));
            if (dist < 0.5f) break;
            arrivalTimeout -= 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        // 3. Snap to position
        movController.StopMovement(featureLineController.GetPositionVector(1));
        float stopTimeout = 2f;
        while (movController.IsMoving && stopTimeout > 0f)
        {
            stopTimeout -= Time.deltaTime;
            yield return null;
        }

        if (isProcessing) yield break;

        // 4. Show speech bubble
        int retries = 0;
        while (retries < 10)
        {
            controller.RefreshReferences();
            if (controller.IsSpeechBubbleReady()) break;
            yield return new WaitForSeconds(0.1f);
            retries++;
        }

        Debug.Log($"[Action] {gameObject.name} showing speech bubble.");

        if (TutorialHelper.IsInTutorial)
        {
            Debug.Log("Tutorial detected: Pausing timer for bubble click.");
            EventManager.current.PauseTimer();
        }

        controller.ShowSpeechBubble(SpeechBubbleController.BubbleIcon.InactiveSpeaker);

        //int bubbleWaitTime = (int)(10 * waitTimeMultiplier);
        int bubbleWaitTime = TutorialHelper.IsInTutorial ? 999999 : (int)(10 * waitTimeMultiplier);
        bool clicked = false;

        if (TutorialHelper.IsInTutorial)
        {
            StartCoroutine(TutorialHelper.ShowTutorialStepUntil(1, () => clicked || isDestroyed));
        }

        yield return controller.WaitForClickWithIcon(SpeechBubbleController.BubbleIcon.InactiveSpeaker, bubbleWaitTime, (bool wasClicked) =>
        {
            clicked = wasClicked;
        });

        if (!clicked)
        {
            if (!isProcessing) { isProcessing = true; yield return LeaveEarly(); }
            yield break;
        }

        if (data.orderAudio != null) yield return controller.PlayClip(data.orderAudio);
        if (brain != null) brain.StartAILogic(data.orderDialogue, data);
        canAcceptCoffee = true;

        // 5. TRANSITION TO WAIT LINE
        if (featureLineController.IsCustomerInLine(movController))
            featureLineController.RemoveFromLine(movController);

        controller.HideSpeechBubble();

        // Enqueue in wait line inline (same safe pattern)
        waitLineController.EnqueueCustomer(SPEED, movController);

        yield return WaitForCoffeeApproval();
        yield return ProcessCoffeeOrder();
    }

    

    

    private void CheckForWaitingCoffee()
    {
        if (isLeavingOrConsuming || isDestroyed) return;

        // If I'm at the front of the wait line and there's a coffee on the counter...
        if (waitLineController != null &&
            waitLineController.GetFrontCustomer() == movController &&
            EventManager.current.HasNextCoffee())
        {
            Debug.Log($"[Fix] {gameObject.name} found a coffee already waiting. Picking up.");
            isLeavingOrConsuming = true;
            StartCoroutine(ProcessCoffeeOrder());
        }
    }

    private IEnumerator WaitForCoffeeApproval()
    {
        //int maxWaitTime = (int)(30 * waitTimeMultiplier);
        //controller.SetTimeFloor(maxWaitTime);

        int maxWaitTime = TutorialHelper.IsInTutorial ? 999999 : (int)(30 * waitTimeMultiplier);
        controller.SetTimeFloor(maxWaitTime);

        yield return controller.WaitForConditionWithIcon(SpeechBubbleController.BubbleIcon.Timer, maxWaitTime, () => {
            if (coffeeOrderPressed)
            {
                coffeeOrderPressed = false;
                return false; // Resets timer
            }

            // Check BOTH the local flag and the global EventManager queue
            bool coffeeIsReadyAtCounter = EventManager.current.HasNextCoffee();
            bool isAtFront = waitLineController.GetFrontCustomer() == movController;

            return (isAtFront && (coffeeApproved || coffeeIsReadyAtCounter)) || isDestroyed;
        });

        if (!(waitLineController.GetFrontCustomer() == movController && (coffeeApproved || EventManager.current.HasNextCoffee())))
        {
            yield return LeaveEarly();
        }
    }

    

    private IEnumerator ProcessCoffeeOrder()
    {
        if (isDestroyed || isProcessing) yield break;
        isProcessing = true;
        //isLeavingOrConsuming = true;

        yield return new WaitForEndOfFrame();
        

        if (!EventManager.current.HasNextCoffee())
        {
            isProcessing = false; // Reset if it was a false alarm
            yield break;
        }

        if (isDestroyed || !EventManager.current.HasNextCoffee()) yield break;


        CoffeeOrder servedOrder = EventManager.current.GetNextCoffee();

        if (servedOrder != null && servedOrder.CoffeeObject != null)
        {
            servedOrder.CoffeeObject.SetActive(false);
        }

        

        //if (waitLineController != null) waitLineController.RemoveFromLine(controller);
        controller.HideSpeechBubble();
        controller.ShowCoffee(); // Visually "carry" the drink

        //if (brain != null)
        //{
        //    brain.UpdateBrainFromResult(data.orderDialogue, servedOrder.Coffee);
        //}

        HandleAIResult(servedOrder.Coffee);

        if (waitLineController != null && waitLineController.IsCustomerInLine(movController))
        {
            waitLineController.RemoveFromLine(movController);
        }
        else if (featureLineController != null && featureLineController.IsCustomerInLine(movController))
        {
            featureLineController.RemoveFromLine(movController);
        }
    }

    public void HandleAIResult(Coffee servedCoffee)
    {
        bool isFlavorCorrect = (servedCoffee.flavor == data.expectedFlavor);
        bool servedIsHot = (servedCoffee.temperature <= 0.5f || servedCoffee.temperature > 50f);
        bool customerWantedHot = (data.expectedTemp == TempPreference.Hot);

        if (isFlavorCorrect && (servedIsHot == customerWantedHot))
            StartCoroutine(HandleCorrectAIReaction());
        else
            StartCoroutine(HandleIncorrectAIReaction());
    }




    private IEnumerator HandleCorrectAIReaction()
    {
        if (isDestroyed) yield break;

        yield return controller.Happy();

        ShowMoneyAnimation(data.moneyReward);

        EventManager.current.MoneyGained(data.moneyReward);

        // Trigger traditional events so the GM knows to count this customer
        EventManager.current.CoffeeServed(true);
        EventManager.current.IncrementCustomerCount();

        // Walk to exit
        yield return controller.WalkTo(exit.position, SPEED);
        Destroy(gameObject);
    }

    private void ShowMoneyAnimation(int amount)
    {
        if (moneyAnimationPrefab == null || targetCanvas == null)
        {
            //Debug.Log($"No money animation because moneyAnimationPrefab = {moneyAnimationPrefab} and targetCanvas = {targetCanvas}");
            return;
        }



        // Convert world to screen
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        //Debug.Log($"Spawning money at {screenPos} on canvas {targetCanvas.name}");

        GameObject moneyAnim = Instantiate(moneyAnimationPrefab, targetCanvas.transform);
        RectTransform rectTransform = moneyAnim.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.position = screenPos;

            // CRITICAL FIX: Overlay Canvas ignores Z for rendering, 
            // but high Z values can still cause the object to be culled 
            // by the UI system if it's not careful.
            rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y, 0f);
        }

        MoneyAnimation moneyAnimation = moneyAnim.GetComponent<MoneyAnimation>();
        if (moneyAnimation != null)
        {
            moneyAnimation.SetAmount(amount);
        }
    }



    private IEnumerator HandleIncorrectAIReaction()
    {
        EventManager.current.CoffeeServed(false);
        EventManager.current.CustomerDied(); // Your traditional penalty

        yield return controller.Die();
        Destroy(gameObject);
    }

    private IEnumerator LeaveEarly()
    {
        if (isLeavingOrConsuming) yield break; // Guard check
        isLeavingOrConsuming = true;

        //if (featureLineController != null) featureLineController.RemoveFromLine(controller);
        //if (waitLineController != null) waitLineController.RemoveFromLine(controller);

        if (waitLineController != null && waitLineController.IsCustomerInLine(movController))
        {
            waitLineController.RemoveFromLine(movController);
        }
        else if (featureLineController != null && featureLineController.IsCustomerInLine(movController))
        {
            featureLineController.RemoveFromLine(movController);
        }
        EventManager.current.CustomerLeftEarly();
        yield return controller.Walk(Vector3.left * 10, SPEED);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        Debug.Log($"[CRITICAL] {gameObject.name} AICustomerActions DISABLED");
        isDestroyed = true;

        if (EventManager.current != null)
        {
            EventManager.current.onApproveCoffee -= OnApproveClicked;
            EventManager.current.onQueueGotFirstItem -= ShowUIIfFront;
            EventManager.current.onQueueBecameEmpty -= HideUI;
            EventManager.current.onCoffeeOrderPressed -= OnCoffeeOrderPressed;
            EventManager.current.onCoffeeProduced -= OnCoffeeProduced;
            EventManager.current.onCoffeeReadyForCustomer -= onCoffeeReadyForCustomer;

        }

        if (featureLineController != null && featureLineController.IsCustomerInLine(movController))
            featureLineController.RemoveFromLine(movController);

        if (waitLineController != null && waitLineController.IsCustomerInLine(movController))
            waitLineController.RemoveFromLine(movController);
    }
}
