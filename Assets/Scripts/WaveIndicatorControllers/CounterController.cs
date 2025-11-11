using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CounterController : MonoBehaviour
{
    public TextMeshProUGUI counterText;
    private bool isTesting = false;
    private int counter = 0;
    private int limit = 10;
    void Awake()
    {
        EventManager.current.onDayStarted += onDayStarted;
        EventManager.current.onDayCompleted += onDayCompleted;
        EventManager.current.onCoffeeServed += OnCoffeeServed;
        EventManager.current.onCustomerLeftEarly += OnCustomerLeftOrDied;
        EventManager.current.onCustomerDied += OnCustomerLeftOrDied;
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (EventManager.current != null)
        {
            EventManager.current.onDayStarted -= onDayStarted;
            EventManager.current.onDayCompleted -= onDayCompleted;
            EventManager.current.onCoffeeServed -= OnCoffeeServed;
            EventManager.current.onCustomerLeftEarly -= OnCustomerLeftOrDied;
            EventManager.current.onCustomerDied -= OnCustomerLeftOrDied;
        }
    }

    void onDayStarted(int duration)
    {
        gameObject.SetActive(true);
        UpdateCounterText();
    }

    void onDayCompleted()
    {
        gameObject.SetActive(false);
    }

    void OnCoffeeServed(bool correctCoffee)
    {
        // Remove counter increment for successful serves
        // if (correctCoffee)
        // {
        //     IncrementCounter();
        // }
    }

    void OnCustomerLeftOrDied()
    {
        IncrementCounter();
    }

    void IncrementCounter()
    {
        counter++;
        UpdateCounterText();
        
        // Check if the limit is reached
        if (counter >= limit)
        {
            // Trigger the game over event
            if (EventManager.current != null)
            {
                EventManager.current.GameOver();
            }
        }
    }

    void UpdateCounterText()
    {
        counterText.text = counter.ToString() + "/" + limit;
    }
}
