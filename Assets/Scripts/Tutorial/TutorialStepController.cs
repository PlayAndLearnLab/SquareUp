using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
public class TutorialStepController : MonoBehaviour
{
    public int step;

    public bool useButton = false;
    public GameObject button;
    public bool isGameButton = false;

    private Image tutorialImage;
    private CanvasGroup canvasGroup;
    private bool isShowing = false;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private float diagnosticTimer = 0f;
    private const float DIAGNOSTIC_INTERVAL = 0.5f; // Check every half second

    public void SetStepActive(bool active)
    {
        //isShowing = active;
        //gameObject.SetActive(active);
        isShowing = active;
        if (active)
        {
            ApplyVisibleState(); // This sets Alpha to 1 and Button to Active
        }
        else
        {
            ApplyHiddenState();  // This sets Alpha to 0 and Button to Inactive
        }
    }

    void Awake()
    {
        Debug.Log($"[TutorialStep {step}] Awake - GameObject: {gameObject.name}, Active: {gameObject.activeSelf}, Visible in Hierarchy: {gameObject.activeInHierarchy}");
        
        // Check hierarchy
        Transform current = transform;
        string hierarchy = gameObject.name;
        while (current.parent != null)
        {
            current = current.parent;
            hierarchy = current.name + " > " + hierarchy;
            
            // Check for canvas parent
            Canvas canvas = current.GetComponent<Canvas>();
            if (canvas != null)
            {
                parentCanvas = canvas;
                Debug.Log($"[TutorialStep {step}] Found parent canvas: {canvas.name}, RenderMode: {canvas.renderMode}, WorldCamera: {canvas.worldCamera?.name ?? "null"}");
            }
        }
        Debug.Log($"[TutorialStep {step}] Full hierarchy: {hierarchy}");
        
        // Subscribe to events
        if (EventManager.current != null)
        {
            EventManager.current.onTutorialStepStarted += OnTutorialStepStarted;
            EventManager.current.onTutorialStepCompleted += OnTutorialStepCompleted;
            Debug.Log($"[TutorialStep {step}] Successfully subscribed to EventManager events");
        }
        else
        {
            Debug.LogError($"[TutorialStep {step}] ERROR: EventManager.current is null");
        }

        // Get components
        tutorialImage = GetComponent<Image>();
        Debug.Log($"[TutorialStep {step}] Image component: {(tutorialImage != null ? "Found" : "Not found")}");
        if (tutorialImage != null)
        {
            Debug.Log($"[TutorialStep {step}] Image properties - Color: {tutorialImage.color}, Enabled: {tutorialImage.enabled}, Raycast: {tutorialImage.raycastTarget}");
        }
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            //Debug.Log($"[TutorialStep {step}] Adding CanvasGroup component");
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        Debug.Log($"[TutorialStep {step}] CanvasGroup - Alpha: {canvasGroup.alpha}, Interactable: {canvasGroup.interactable}, BlocksRaycasts: {canvasGroup.blocksRaycasts}");
        
        rectTransform = transform as RectTransform;
        if (rectTransform != null) 
        {
            Debug.Log($"[TutorialStep {step}] RectTransform - Pos: {rectTransform.anchoredPosition}, Size: {rectTransform.sizeDelta}, Scale: {rectTransform.localScale}");
            Debug.Log($"[TutorialStep {step}] RectTransform - AnchorMin: {rectTransform.anchorMin}, AnchorMax: {rectTransform.anchorMax}, Pivot: {rectTransform.pivot}");
        }
        else 
        {
            Debug.LogError($"[TutorialStep {step}] ERROR: No RectTransform component found");
        }

        if (button != null)
        {
            Debug.Log($"[TutorialStep {step}] Button found: {button.name}, Active: {button.activeSelf}");
            Button btnComponent = button.GetComponent<Button>();
            if (btnComponent != null)
            {
                btnComponent.onClick.RemoveAllListeners();
                btnComponent.onClick.AddListener(() => {
                    if (isShowing)
                    {
                        Debug.Log($"[TutorialStep {step}] Button clicked, completing step");
                        EventManager.current.TutorialStepCompleted(step);
                    }
                        
                });
                Debug.Log($"[TutorialStep {step}] Button click listener added");
            }
            else
            {
                Debug.LogError($"[TutorialStep {step}] ERROR: Button component not found on {button.name}");
            }
            //button.SetActive(false);
        }
        else
        {
            Debug.Log($"[TutorialStep {step}] No button assigned");
        }
        
        // Validate UI state immediately
        // ValidateUIState("Awake");
    }

    public void Start()
    {
        Debug.Log($"[TutorialStep {step}] Start - Active: {gameObject.activeSelf}, ActiveInHierarchy: {gameObject.activeInHierarchy}");
        // Hide all tutorials at start
        //HideTutorialStep(false);
        Debug.Log($"[TutorialStep {step}] Initial state set to hidden");
        // ValidateUIState("Start");
    }
    
    private void Update()
    {
        // Perform periodic UI state validation in WebGL builds
        #if UNITY_WEBGL
        diagnosticTimer += Time.deltaTime;
        if (diagnosticTimer >= DIAGNOSTIC_INTERVAL)
        {
            diagnosticTimer = 0f;
            // ValidateUIState("Update");
        }
        #endif
    }

    private void HideTutorialStep(bool useCoroutine = true)
    {
        Debug.Log($"[TutorialStep {step}] HideTutorialStep called with useCoroutine={useCoroutine}");
        isShowing = false;
        if (useCoroutine)
        {
            Debug.Log($"[TutorialStep {step}] Starting visibility coroutine for hiding");
            StartCoroutine(ApplyVisibilityChanges(false));
        }
        else
        {
            Debug.Log($"[TutorialStep {step}] Directly applying hidden state");
            ApplyHiddenState();
        }
    }

    private void ApplyHiddenState()
    {
        Debug.Log($"[TutorialStep {step}] ApplyHiddenState - GameObject Active: {gameObject.activeSelf}");
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            Debug.Log($"[TutorialStep {step}] Set CanvasGroup - Alpha: 0, Interactable: false, BlocksRaycasts: false");
        }
        else
        {
            Debug.LogError($"[TutorialStep {step}] ERROR: CanvasGroup is null when trying to hide");
        }
        
        if (tutorialImage != null)
        {
            tutorialImage.enabled = false;
            Debug.Log($"[TutorialStep {step}] Set Image.enabled = false");
        }
        else
        {
            Debug.LogWarning($"[TutorialStep {step}] WARNING: tutorialImage is null when trying to hide");
        }
        
        if (button != null && !isGameButton)
        {
            button.SetActive(false);
            Debug.Log($"[TutorialStep {step}] Set button.active = false");
        }
        
        Debug.Log($"[TutorialStep {step}] Hidden state applied. Final state - Alpha: {canvasGroup?.alpha}, Image.enabled: {tutorialImage?.enabled}, Active: {gameObject.activeSelf}");
        
        // Validate UI state after change
        // ValidateUIState("AfterHide");
    }

    private void ShowTutorialStep()
    {
        Debug.Log($"[TutorialStep {step}] ShowTutorialStep called. Current GameObject.active: {gameObject.activeSelf}");
        isShowing = true;
        Debug.Log($"[TutorialStep {step}] Starting visibility coroutine for showing");
        StartCoroutine(ApplyVisibilityChanges(true));
    }
    
    private void ApplyVisibleState()
    {
        Debug.Log($"[TutorialStep {step}] ApplyVisibleState - GameObject Active: {gameObject.activeSelf}");
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = useButton;
            canvasGroup.blocksRaycasts = useButton;
            Debug.Log($"[TutorialStep {step}] Set CanvasGroup - Alpha: 1, Interactable: {useButton}, BlocksRaycasts: {useButton}");
        }
        else
        {
            Debug.LogError($"[TutorialStep {step}] ERROR: CanvasGroup is null when trying to show");
        }
        gameObject.SetActive(true);

        if (tutorialImage != null)
        {
            tutorialImage.enabled = true;
            Debug.Log($"[TutorialStep {step}] Set Image.enabled = true");
        }
        else
        {
            Debug.LogWarning($"[TutorialStep {step}] WARNING: tutorialImage is null when trying to show");
        }
        
        if (button != null && useButton)
        {
            button.SetActive(true);
            PromoteButton();
            Debug.Log($"[TutorialStep {step}] Set button.active = true");
        }
        
        // Force a layout refresh
        if (rectTransform != null)
        {
            try {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                Debug.Log($"[TutorialStep {step}] Layout rebuild forced");
            } catch (System.Exception e) {
                Debug.LogError($"[TutorialStep {step}] ERROR: Layout rebuild failed: {e.Message}");
            }
        }
        
        Debug.Log($"[TutorialStep {step}] Visible state applied. Final state - Alpha: {canvasGroup?.alpha}, Image.enabled: {tutorialImage?.enabled}, Active: {gameObject.activeSelf}");
        
        // Extra debug for web builds - check if we're actually visible in screen space
        if (rectTransform != null && parentCanvas != null)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            
            bool isInView = false;
            foreach (Vector3 corner in corners)
            {
                if (corner.x >= 0 && corner.x <= Screen.width && corner.y >= 0 && corner.y <= Screen.height)
                {
                    isInView = true;
                    break;
                }
            }
            
             Debug.Log($"[TutorialStep {step}] Visibility check - In screen view: {isInView}, Screen size: {Screen.width}x{Screen.height}");
             Debug.Log($"[TutorialStep {step}] World corners - TL: {corners[0]}, TR: {corners[1]}, BR: {corners[2]}, BL: {corners[3]}");
        }
        
        // Validate UI state after change
        // ValidateUIState("AfterShow");
    }
    
    private IEnumerator ApplyVisibilityChanges(bool shouldShow)
    {
         Debug.Log($"[TutorialStep {step}] ApplyVisibilityChanges coroutine started. shouldShow={shouldShow}, isShowing={isShowing}");
        
        // Store visibility locally to avoid state corruption in WebGL
        bool targetVisibility = shouldShow;
        
        // First wait for the end of frame
        yield return new WaitForEndOfFrame();
         Debug.Log($"[TutorialStep {step}] After first WaitForEndOfFrame. Target visibility: {targetVisibility}");
        
        // Apply the correct state based on parameter
        if (targetVisibility)
        {
            Debug.Log($"[TutorialStep {step}] Applying visible state from coroutine");
            ApplyVisibleState();
        }
        else
        {
            Debug.Log($"[TutorialStep {step}] Applying hidden state from coroutine");
            ApplyHiddenState();
        }
            
        // Sometimes WebGL needs an extra frame
        yield return new WaitForEndOfFrame();
        // Debug.Log($"[TutorialStep {step}] After second WaitForEndOfFrame");
        
        // Double-check that our changes were applied
        if (targetVisibility)
        {
            // Debug.Log($"[TutorialStep {step}] Double-checking visible state - Alpha: {canvasGroup?.alpha}, Image.enabled: {tutorialImage?.enabled}, GameObject.active: {gameObject.activeSelf}");
            
            // Extra safety in case something isn't working
            if (canvasGroup != null && canvasGroup.alpha < 0.9f)
            {
                // Debug.LogWarning($"[TutorialStep {step}] WARNING: Alpha is still low ({canvasGroup.alpha}), forcing to 1");
                canvasGroup.alpha = 1f;
            }
            
            if (tutorialImage != null && !tutorialImage.enabled)
            {
                // Debug.LogWarning($"[TutorialStep {step}] WARNING: Image is still disabled, forcing enabled");
                tutorialImage.enabled = true;
            }
            
            // Try direct renderer approach as backup
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                // Debug.Log($"[TutorialStep {step}] Found renderer: {r.name}, Enabled: {r.enabled}");
                r.enabled = true;
            }
            
            // Last resort - force canvas update
            if (parentCanvas != null)
            {
                // Debug.Log($"[TutorialStep {step}] Forcing Canvas update: {parentCanvas.name}");
                parentCanvas.enabled = false;
                yield return new WaitForEndOfFrame();
                parentCanvas.enabled = true;
            }
            
            // Validate UI state after visibility changes
            // ValidateUIState("AfterCoroutine");
            
            // Add a final visibility check a few frames later to confirm WebGL rendering
            yield return new WaitForSeconds(0.2f);
            // ValidateUIState("DelayedCheck");
        }
        
        // Debug.Log($"[TutorialStep {step}] Visibility coroutine complete");
    }
    
    private void ValidateUIState(string checkpoint)
    {
        StringBuilder validationLog = new StringBuilder();
        // validationLog.AppendLine($"[TutorialStep {step}] UI STATE VALIDATION ({checkpoint}):");
        
        // Basic GameObject state
        // validationLog.AppendLine($"- GameObject: {gameObject.name}");
        // validationLog.AppendLine($"- Active: {gameObject.activeSelf}, ActiveInHierarchy: {gameObject.activeInHierarchy}");
        // validationLog.AppendLine($"- Layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer})");
        // validationLog.AppendLine($"- Tag: {gameObject.tag}");
        
        // Canvas state
        if (parentCanvas != null)
        {
            // validationLog.AppendLine($"- Parent Canvas: {parentCanvas.name}");
            // validationLog.AppendLine($"- Canvas RenderMode: {parentCanvas.renderMode}");
            // validationLog.AppendLine($"- Canvas Enabled: {parentCanvas.enabled}");
            // validationLog.AppendLine($"- Canvas OverrideSorting: {parentCanvas.overrideSorting}, SortingOrder: {parentCanvas.sortingOrder}");
            
            // Check if any parent CanvasGroup might be affecting visibility
            CanvasGroup[] parentGroups = parentCanvas.GetComponentsInParent<CanvasGroup>(true);
            foreach (CanvasGroup cg in parentGroups)
            {
                // validationLog.AppendLine($"- Parent CanvasGroup on {cg.gameObject.name}: Alpha={cg.alpha}, Interactable={cg.interactable}, BlocksRaycasts={cg.blocksRaycasts}");
            }
            
            // Canvas scaler info if available
            CanvasScaler scaler = parentCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                // validationLog.AppendLine($"- CanvasScaler: ScaleFactor={scaler.scaleFactor}, DynamicPixelsPerUnit={scaler.dynamicPixelsPerUnit}");
                // validationLog.AppendLine($"- CanvasScaler: ReferenceResolution={scaler.referenceResolution}, ScreenMatchMode={scaler.screenMatchMode}");
            }
        }
        
        // RectTransform state
        if (rectTransform != null)
        {
            // validationLog.AppendLine($"- RectTransform Valid: {rectTransform != null}");
            // validationLog.AppendLine($"- AnchoredPosition: {rectTransform.anchoredPosition}");
            // validationLog.AppendLine($"- SizeDelta: {rectTransform.sizeDelta}");
            // validationLog.AppendLine($"- LocalScale: {rectTransform.localScale}");
            // validationLog.AppendLine($"- AnchorMin: {rectTransform.anchorMin}, AnchorMax: {rectTransform.anchorMax}");
            // validationLog.AppendLine($"- Pivot: {rectTransform.pivot}");
            // validationLog.AppendLine($"- Rotation: {rectTransform.localRotation.eulerAngles}");
            
            // Calculate actual rendered rect in screen space
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Rect screenRect = RectTransformUtility.PixelAdjustRect(rectTransform, parentCanvas);
            // validationLog.AppendLine($"- Screen Space Rect: Pos=({screenRect.x}, {screenRect.y}), Size=({screenRect.width}, {screenRect.height})");
            
            // Check if this rect is outside the screen bounds
            bool isOffScreen = screenRect.x > Screen.width || screenRect.y > Screen.height || 
                              screenRect.x + screenRect.width < 0 || screenRect.y + screenRect.height < 0;
            // validationLog.AppendLine($"- Possibly Off Screen: {isOffScreen}");
            
            // Check if size is too small to be visible
            bool isTooSmall = screenRect.width < 1 || screenRect.height < 1;
            // validationLog.AppendLine($"- Too Small To Render: {isTooSmall}");
        }
        
        // Image component state
        if (tutorialImage != null)
        {
            // validationLog.AppendLine($"- Image Valid: {tutorialImage != null}");
            // validationLog.AppendLine($"- Image Enabled: {tutorialImage.enabled}");
            // validationLog.AppendLine($"- Image Color: {tutorialImage.color} (Alpha: {tutorialImage.color.a})");
            // validationLog.AppendLine($"- Image RaycastTarget: {tutorialImage.raycastTarget}");
            // validationLog.AppendLine($"- Image Material: {(tutorialImage.material != null ? tutorialImage.material.name : "Default")}");
            // validationLog.AppendLine($"- Image Type: {tutorialImage.type}");
            // validationLog.AppendLine($"- Image Sprite: {(tutorialImage.sprite != null ? tutorialImage.sprite.name : "None")}");
            
            #if UNITY_WEBGL
            // WebGL specific checks
            if (tutorialImage.enabled && tutorialImage.color.a > 0 && !tutorialImage.sprite)
            {
                validationLog.AppendLine($"- WARNING: Image enabled with no sprite in WebGL");
            }
            #endif
            
            // Check if canvas renderer is valid
            CanvasRenderer renderer = GetComponent<CanvasRenderer>();
            if (renderer != null)
            {
                // validationLog.AppendLine($"- CanvasRenderer Alpha: {renderer.GetAlpha()}");
                // validationLog.AppendLine($"- CanvasRenderer Cull: {renderer.cull}");
                // validationLog.AppendLine($"- CanvasRenderer Hidden: {renderer.GetInheritedAlpha() <= 0.01f}");
            }
        }
        
        // CanvasGroup state
        if (canvasGroup != null)
        {
            // validationLog.AppendLine($"- CanvasGroup Valid: {canvasGroup != null}");
            // validationLog.AppendLine($"- CanvasGroup Alpha: {canvasGroup.alpha}");
            // validationLog.AppendLine($"- CanvasGroup Interactable: {canvasGroup.interactable}");
            // validationLog.AppendLine($"- CanvasGroup BlocksRaycasts: {canvasGroup.blocksRaycasts}");
            // validationLog.AppendLine($"- CanvasGroup IgnoreParentGroups: {canvasGroup.ignoreParentGroups}");
        }
        
        // Layout component states if present
        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            // validationLog.AppendLine($"- LayoutElement: Flexible={layoutElement.flexibleWidth}x{layoutElement.flexibleHeight}, Min={layoutElement.minWidth}x{layoutElement.minHeight}");
            // validationLog.AppendLine($"- LayoutElement Ignored: {!layoutElement.IsActive()}");
        }
        
        // Check for any layout groups that might be affecting this object
        LayoutGroup[] layoutGroups = GetComponentsInParent<LayoutGroup>(true);
        if (layoutGroups.Length > 0)
        {
            // validationLog.AppendLine($"- Found {layoutGroups.Length} parent layout groups:");
            foreach (LayoutGroup lg in layoutGroups)
            {
                // validationLog.AppendLine($"  - {lg.GetType().Name} on {lg.gameObject.name} (Enabled: {lg.enabled}, Active: {lg.gameObject.activeInHierarchy})");
            }
        }
        
        // Current expected visibility state
        // validationLog.AppendLine($"- Expected to be visible: {isShowing}");
        
        // Special WebGL checks
        #if UNITY_WEBGL
        // validationLog.AppendLine($"- WEBGL BUILD - Time: {Time.time}");
        // validationLog.AppendLine($"- Screen Info: Width={Screen.width}, Height={Screen.height}, DPI={Screen.dpi}");
        #endif
        
        Debug.Log(validationLog.ToString());
    }

    void OnTutorialStepStarted(int stepTriggered)
    {
        // Debug.Log($"[TutorialStep {step}] OnTutorialStepStarted received for step {stepTriggered}, my step is {step}, match: {stepTriggered == step}");
        if (stepTriggered == this.step)
        {
            // Debug.Log($"[TutorialStep {step}] Step matches, calling ShowTutorialStep");
            ShowTutorialStep();
            // Debug.Log($"[TutorialStep {step}] ShowTutorialStep called");

            if (!useButton && tutorialImage != null)
            {
                tutorialImage.raycastTarget = false;
                // Debug.Log($"[TutorialStep {step}] Raycast target set to false");
            }
        } else {
            // Debug.Log($"[TutorialStep {step}] Step doesn't match my step ({step}), ignoring");
        }
    }

    void onClick()
    {
        // Debug.Log($"[TutorialStep {step}] onClick method called");
        EventManager.current.TutorialStepCompleted(step);
    }

    void OnTutorialStepCompleted(int stepCompleted)
    {
        // Debug.Log($"[TutorialStep {step}] OnTutorialStepCompleted received for step {stepCompleted}, my step is {step}, match: {stepCompleted == step}");
        if (stepCompleted == this.step)
        {
            // Debug.Log($"[TutorialStep {step}] Step matches, calling HideTutorialStep");
            HideTutorialStep();
        }
    }
    
    void OnEnable()
    {
        // Debug.Log($"[TutorialStep {step}] OnEnable - GameObject: {gameObject.name}");
        // ValidateUIState("OnEnable");
    }
    
    void OnDisable()
    {
        // Debug.Log($"[TutorialStep {step}] OnDisable - GameObject: {gameObject.name}");
        // Ensure we don't leave any coroutines running
        StopAllCoroutines();
    }
    
    void OnDestroy() 
    {
        // Debug.Log($"[TutorialStep {step}] OnDestroy - Unsubscribing from events");
        if (EventManager.current != null)
        {
            EventManager.current.onTutorialStepStarted -= OnTutorialStepStarted;
            EventManager.current.onTutorialStepCompleted -= OnTutorialStepCompleted;
        }
    }

    public void PromoteButton()
    {
        if (button == null) return;

        // 1. Add Canvas if it doesn't exist
        Canvas btnCanvas = button.GetComponent<Canvas>();
        if (btnCanvas == null) btnCanvas = button.AddComponent<Canvas>();

        // 2. Set sorting to be above the Blocker Mask
        btnCanvas.overrideSorting = true;
        btnCanvas.sortingOrder = 100; // Ensure this is higher than your Blocker Mask's sorting

        // 3. Add GraphicRaycaster so the button can still be clicked
        if (button.GetComponent<GraphicRaycaster>() == null)
            button.AddComponent<GraphicRaycaster>();
    }

    public void DemoteButton()
    {
        if (button == null) return;

        // Remove the temporary components to return the button to its normal state
        Destroy(button.GetComponent<GraphicRaycaster>());
        Destroy(button.GetComponent<Canvas>());
    }
}
