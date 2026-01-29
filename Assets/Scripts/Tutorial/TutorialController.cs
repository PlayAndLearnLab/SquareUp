//using System.Collections;
//using System.Collections.Generic;
//using System.Runtime.InteropServices;
//using UnityEngine;
//using UnityEngine.AI;

//public class TutorialController : MonoBehaviour
//{
//    public GameObject[] tutorialSteps;
//    public GameObject upgradePanel;
//    public GameObject blockerMask;
//    private int currentStep = -1;
//    private int[] stepsWithButton = { 0, 5, 6, 7, 8, 9, 10 };

//    void Start()
//    {
//        // hide upgrade panel
//        upgradePanel.SetActive(false);
//    }

//    public void Update()
//    {
//        if (Input.GetMouseButtonDown(0) && !System.Array.Exists(stepsWithButton, step => step == currentStep)) // 0 is left mouse button
//        {
//            HideAllTutorialSteps();
//        }
//    }

//    public void HideAllTutorialSteps()
//    {
//        foreach (GameObject tutorialStep in tutorialSteps)
//        {
//            var stepCtrl = tutorialStep.GetComponent<TutorialStepController>();
//            if (stepCtrl != null) stepCtrl.DemoteButton();
//            tutorialStep.SetActive(false);
//        }
//        if (blockerMask != null) blockerMask.SetActive(false);
//        currentStep = -1;
//    }

//    public void ShowTutorialStep(int step)
//    {
//        Debug.Log("Showing tutorial step: " + step);
//        HideAllTutorialSteps();
//        if (blockerMask != null) blockerMask.SetActive(true);
//        tutorialSteps[step].SetActive(true);
//        currentStep = step;
//        if (step == 5)
//        {
//            upgradePanel.SetActive(true);
//        }
//    }
//}

using UnityEngine;

public class TutorialController : MonoBehaviour
{
    public GameObject[] tutorialSteps;
    public GameObject upgradePanel;
    public GameObject blockerMask;
    private int currentStep = -1;

    // Add every step that requires a BUTTON click here.
    // Steps NOT in this list will advance when the player clicks ANYWHERE.
    //private int[] stepsWithButton = { 0, 5, 6, 7, 8, 9, 10 };
    private int[] stepsWithButton = { 0, 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13 };

    void Start()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);

        // Subscribe to the event manager so we know when a button was clicked
        if (EventManager.current != null)
        {
            EventManager.current.onTutorialStepCompleted += OnStepCompletedByButton;
        }

        // Start the first step after a tiny delay to ensure everything is initialized
        Invoke("StartTutorial", 0.5f);
    }

    void StartTutorial()
    {
        ShowTutorialStep(0);
    }

    public void Update()
    {
        // If the tutorial is over or not started, do nothing
        if (currentStep == -1) return;

        // CHECK: Is this a "Click Anywhere" step?
        bool isButtonStep = System.Array.Exists(stepsWithButton, step => step == currentStep);

        if (Input.GetMouseButtonDown(0) && !isButtonStep)
        {
            // Player clicked anywhere on an instructional step -> Move to next
            AdvanceToNextStep();
        }
    }

    // This is called by the EventManager when a specific UI button is clicked
    private void OnStepCompletedByButton(int stepIndex)
    {
        if (stepIndex == currentStep)
        {
            Debug.Log($"Step {stepIndex} completed via button click.");
            AdvanceToNextStep();
        }
    }

    public void AdvanceToNextStep()
    {
        int nextStep = currentStep + 1;

        if (nextStep < tutorialSteps.Length)
        {
            ShowTutorialStep(nextStep);
        }
        else
        {
            Debug.Log("Tutorial Complete!");
            HideAllTutorialSteps();
        }
    }

    public void HideAllTutorialSteps()
    {
        foreach (GameObject tutorialStep in tutorialSteps)
        {
            var stepCtrl = tutorialStep.GetComponent<TutorialStepController>();
            if (stepCtrl != null) stepCtrl.DemoteButton();
            tutorialStep.SetActive(false);
        }
        if (blockerMask != null) blockerMask.SetActive(false);
        currentStep = -1;
    }

    public void ShowTutorialStep(int step)
    {
        if (step < 0 || step >= tutorialSteps.Length) return;

        Debug.Log("Showing tutorial step: " + step);

        // Clean up previous state
        HideAllTutorialSteps();

        // Turn on the mask for the new step
        if (blockerMask != null) blockerMask.SetActive(true);

        // Show the specific step UI
        //tutorialSteps[step].SetActive(true);
        var stepCtrl = tutorialSteps[step].GetComponent<TutorialStepController>();
        if (stepCtrl != null)
        {
            stepCtrl.SetStepActive(true); // This sets the 'isShowing' flag to true
        }
        currentStep = step;

        if (step == 5 && upgradePanel != null)
        {
            upgradePanel.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks or errors when changing scenes
        if (EventManager.current != null)
        {
            EventManager.current.onTutorialStepCompleted -= OnStepCompletedByButton;
        }
    }
}
