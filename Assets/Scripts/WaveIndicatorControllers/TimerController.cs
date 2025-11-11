using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimerController : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    private float timeRemaining;
    private bool isTimerRunning = false;
    private bool isPaused = false;

    void Awake()
    {
        EventManager.current.onDayStarted += onDayStarted;
        EventManager.current.onDayCompleted += onDayCompleted;
        EventManager.current.onTimerPaused += OnTimerPaused;
        EventManager.current.onTimerResumed += OnTimerResumed;
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        EventManager.current.onDayStarted -= onDayStarted;
        EventManager.current.onDayCompleted -= onDayCompleted;
        EventManager.current.onTimerPaused -= OnTimerPaused;
        EventManager.current.onTimerResumed -= OnTimerResumed;
    }

    void onDayStarted(int duration)
    {
        timeRemaining = duration;
        isTimerRunning = true;
        isPaused = false;
        gameObject.SetActive(true);
    }

    void onDayCompleted()
    {
        isTimerRunning = false;
        isPaused = false;
        gameObject.SetActive(false);
    }

    void OnTimerPaused()
    {
        isPaused = true;
    }

    void OnTimerResumed()
    {
        isPaused = false;
    }

    void Update()
    {
        if (!isTimerRunning || isPaused) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            isTimerRunning = false;
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
                EventManager.current.DayCompleted();
            }
        }

        UpdateTimerDisplay();
    }

    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
