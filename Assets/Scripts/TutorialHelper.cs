using System.Collections;
using System;
using UnityEngine;

public static class TutorialHelper
{
    private static int currentTutorialStep = -1;
    private static bool isTutorialStepComplete = false;
    private static bool isInTutorial = false;

    public static bool explodeCoffee = true;

    public static bool IsInTutorial => isInTutorial;

    private static bool isTutorialStepQueued = false;

    public static void StartTutorial()
    {
        isInTutorial = true;
        currentTutorialStep = 0;
    }

    public static void EndTutorial()
    {
        isInTutorial = false;
        currentTutorialStep = -1;
    }

    public static void QueueTutorialStep(int stepIndex)
    {
        Debug.Log("QueueTutorialStep: " + stepIndex + " currentTutorialStep: " + currentTutorialStep);
        if (stepIndex != currentTutorialStep) {
            isTutorialStepQueued = true;
            Debug.Log("Tutorial step " + stepIndex + " queued");
        } else {
            isTutorialStepQueued = false;
            TutorialHelper.WaitForTutorialStep(stepIndex);
            Debug.Log("Tutorial step " + stepIndex + " not queued");    
        }
    }

    public static void SetExplodeCoffee(bool explode)
    {
        explodeCoffee = explode;
    }

    public static void OnTutorialStepCompleted(int stepIndex)
    {
        if (stepIndex == currentTutorialStep)
        {
            // log the current tutorial step
            Debug.Log("Tutorial step completed: " + currentTutorialStep);
            Debug.Log("Tutorial step queued: " + isTutorialStepQueued);
            currentTutorialStep++;
            isTutorialStepComplete = true;
            if (!isTutorialStepQueued) {
                EventManager.current.TutorialStepReady(currentTutorialStep);
            } else {
                isTutorialStepQueued = false;
                CoroutineHelper.Instance.RunCoroutine(WaitForTutorialStep(currentTutorialStep));
            }
        }
    }

    public static IEnumerator WaitForTutorialStep(int stepIndex)
    {
        if (!isInTutorial)
        {
            yield break;
        }

        if (currentTutorialStep != stepIndex)
        {
            Debug.LogError($"Tutorial step {stepIndex} cannot be started. IsInTutorial: {isInTutorial}, CurrentStep: {currentTutorialStep}");
            yield break;
        }

        currentTutorialStep = stepIndex;
        isTutorialStepComplete = false;
        EventManager.current.TutorialStepStarted(stepIndex);
        yield return new WaitUntil(() => isTutorialStepComplete);
    }

    public static IEnumerator ShowTutorialStepUntil(int stepIndex, Func<bool> condition)
    {
        if (!isInTutorial || currentTutorialStep != stepIndex) {
            Debug.Log("Tutorial step " + stepIndex + " cannot be started. IsInTutorial: " + isInTutorial + ", CurrentStep: " + currentTutorialStep);
            yield break;
        }

        currentTutorialStep = stepIndex;
        isTutorialStepComplete = false;
        EventManager.current.TutorialStepStarted(stepIndex);

        yield return new WaitUntil(condition);
        EventManager.current.TutorialStepCompleted(stepIndex);
    }
}