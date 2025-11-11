using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.AI;

public class TutorialController : MonoBehaviour
{
    public GameObject[] tutorialSteps;
    public GameObject upgradePanel;
    private int currentStep = -1;
    private int[] stepsWithButton = { 0, 5, 6, 7, 8, 9, 10 };

    void Start()
    {
        // hide upgrade panel
        upgradePanel.SetActive(false);
    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(0) && !System.Array.Exists(stepsWithButton, step => step == currentStep)) // 0 is left mouse button
        {
            HideAllTutorialSteps();
        }
    }

    public void HideAllTutorialSteps()
    {
        foreach (GameObject tutorialStep in tutorialSteps)
        {
            tutorialStep.SetActive(false);
        }
        currentStep = -1;
    }

    public void ShowTutorialStep(int step)
    {
        Debug.Log("Showing tutorial step: " + step);
        HideAllTutorialSteps();
        tutorialSteps[step].SetActive(true);
        currentStep = step;
        if (step == 5)
        {
            upgradePanel.SetActive(true);
        }
    }
}
