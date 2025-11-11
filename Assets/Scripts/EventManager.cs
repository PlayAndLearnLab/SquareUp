using UnityEngine;
using System;
using Unity.VisualScripting;
using System.Collections.Generic;

public class EventManager : MonoBehaviour
{
    public static EventManager current;
    private GameState gameState;
    public event Action<Coffee> onGiveCoffee;
    public event Action<Coffee> onCoffeeReadyForCustomer;
    public event Action<bool> onCoffeeServed;
    public event Action<string> onFeatureSelected;
    public event Action<int> onTutorialStepCompleted;
    public event Action<int> onTutorialStepStarted;
    public event Action<int> onTutorialStepReady;
    public event Action<int> onDayStarted;
    public event Action onDayCompleted;
    public event Action onTestingStarted;
    public event Action onTestingCompleted;
    public event Action<string> onModelPrediction;
    public event Action<int> onMoneyGained;
    public event Action onTimerPaused;
    public event Action onTimerResumed;
    public event Action onCustomerSaved;

    // Shop events
    public event Action onShopOpened;
    public event Action onShopClosed;
    public event Action onStartNextDay;  // New event for starting next day

    // Slide display event
    public event Action<IntroSlidesData> onSlideDisplayRequested;
    
    // Slideshow completion event
    public event Action<IntroSlidesData> onSlideshowCompleted;
    
    // Add these new events to EventManager class
    public event Action<Quiz> onStartQuiz;
    public event Action<Quiz, bool> onQuizCompleted;
    
    // Coffee queue state events
    public event Action onQueueBecameEmpty;
    public event Action onQueueGotFirstItem;
    public event Action<CoffeeOrder> onItemEnqueued;
    
    // Add new event for clearing the queue
    public event Action onClearQueueRequested;
    
    // Coffee distributor events
    public event Action<float, float> onCoffeeDistributorSpeedChanged; // Speed, Accuracy
    
    // Coffee approval events
    public event Action onApproveCoffee;
    public event Action onDenyCoffee;
    public event Action<bool> onCoffeeApprovalUIRequested;
    public event Action onCoffeeOrderPressed;
    public event Action<Coffee> onCoffeeProduced;
    // New events for correct coffee decisions
    public event Action onCorrectlyApprovedCoffee;
    public event Action onCorrectlyDeniedCoffee;
    
    // Customer tracking events
    public event Action onCustomerServed;
    
    // Upgrade events
    public event Action<string, UpgradeCategory, int> onUpgradeApplied;  // upgradeName, category, newLevel
    
    // New event for customer leaving early
    public event Action onCustomerLeftEarly;
    
    // New event for customer dying
    public event Action onCustomerDied;

    // New event for game over
    public event Action onGameOver;

    private void Awake()
    {
        if (current == null)
        {
            current = this;
        }
        else
        {
            Destroy(gameObject);
        }
        // initialize game state
        gameState = ScriptableObject.CreateInstance<GameState>();
    }
    public void GiveCoffeeUntyped(string coffeeFlavor)
    {
        float temperature = 0.5f;
        switch (coffeeFlavor)
        {
            case "apple":
                GiveCoffee(new Coffee(CoffeeFlavor.Apple, temperature));
                break;
            case "candy":
                GiveCoffee(new Coffee(CoffeeFlavor.Candy, temperature));
                break;
            case "pumpkin":
                GiveCoffee(new Coffee(CoffeeFlavor.Pumpkin, temperature));
                break;
        }
    }

    public void GiveCoffee(Coffee coffee)
    {
        onGiveCoffee?.Invoke(coffee);
    }
    public void CoffeeProduced(Coffee coffee)
    {
        onCoffeeProduced?.Invoke(coffee);
    }

    public void CoffeeOrderPressed()
    {
        onCoffeeOrderPressed?.Invoke();
    }

    public void CoffeeReadyForCustomer(Coffee coffee, GameObject coffeeObject)
    {
        gameState.AddCoffee(coffeeObject, coffee);
        Debug.Log("Coffee ready for customer: " + coffee.flavor);
        onCoffeeReadyForCustomer?.Invoke(coffee);
    }

    public void CoffeeServed(bool correctCoffee)
    {
        onCoffeeServed?.Invoke(correctCoffee);
    }

    public void MoneyGained(int amount)
    {
        onMoneyGained?.Invoke(amount);
    }

    public void FeatureSelected(string featureId)
    {
        onFeatureSelected?.Invoke(featureId);
    }

    public void TutorialStepCompleted(int step) 
    {
        onTutorialStepCompleted?.Invoke(step);
    }
    public void TutorialStepStarted(int step)
    {
        onTutorialStepStarted?.Invoke(step);
    }
    public void TutorialStepReady(int step)
    {
        onTutorialStepReady?.Invoke(step);
    }
    public void DayStarted(int duration)
    {
        onDayStarted?.Invoke(duration);
    }

    public void DayCompleted()
    {
        onDayCompleted?.Invoke();
    }

    public void TestingStarted()
    {
        onTestingStarted?.Invoke();
    }

    public void TestingCompleted()
    {
        onTestingCompleted?.Invoke();
    }

    public void ModelPrediction(string prediction)
    {
        onModelPrediction?.Invoke(prediction);
    }

    public CoffeeOrder PeekNextCoffee()
    {
        return gameState.PeekNextCoffee();
    }

    public bool HasNextCoffee()
    {
        return gameState.HasNextCoffee();
    }

    public CoffeeOrder GetNextCoffee()
    {
        return gameState.GetNextCoffee();
    }

    public void RemoveCoffee(CoffeeOrder coffeeOrder)
    {
        gameState.RemoveCoffee(coffeeOrder);
    }

    // Shop-related methods
    public void ShopOpened()
    {
        Debug.Log("EventManager: Firing ShopOpened event");
        onShopOpened?.Invoke();
    }

    public void ShopClosed()
    {
        Debug.Log("EventManager: Firing ShopClosed event");
        onShopClosed?.Invoke();
    }

    public void StartNextDay()
    {
        onStartNextDay?.Invoke();
    }

    // Slide display method
    public void RequestSlideDisplay(IntroSlidesData slideData)
    {
        Debug.Log($"EventManager: Firing RequestSlideDisplay event with slide data");
        onSlideDisplayRequested?.Invoke(slideData);
    }
    
    // Slideshow completion method
    public void SlideshowCompleted(IntroSlidesData slideData)
    {
        Debug.Log($"EventManager: Firing SlideshowCompleted event with slide data");
        onSlideshowCompleted?.Invoke(slideData);
    }

    // Add these new methods to EventManager class
    public void StartQuiz(Quiz quiz)
    {
        Debug.Log($"EventManager: Firing StartQuiz event with quiz: {quiz.quizName}");
        if (onStartQuiz != null)
        {
            onStartQuiz.Invoke(quiz);
        }
        else
        {
            Debug.LogWarning("EventManager: No listeners for StartQuiz event!");
        }
    }

    public void QuizCompleted(Quiz quiz, bool passed)
    {
        onQuizCompleted?.Invoke(quiz, passed);
    }

    // Coffee distributor methods
    public void CoffeeDistributorSpeedChanged(float speed, float accuracy)
    {
        onCoffeeDistributorSpeedChanged?.Invoke(speed, accuracy);
    }
    
    // Coffee approval methods
    public void ApproveCoffee()
    {
        onApproveCoffee?.Invoke();
    }
    
    public void DenyCoffee()
    {
        onDenyCoffee?.Invoke();
    }
    
    public void RequestCoffeeApprovalUI(bool show)
    {
        onCoffeeApprovalUIRequested?.Invoke(show);
    }
    
    // New methods for correct coffee decisions
    public void CorrectlyApprovedCoffee()
    {
        Debug.Log("EventManager: Correctly approved good coffee");
        onCorrectlyApprovedCoffee?.Invoke();
    }
    
    public void CorrectlyDeniedCoffee()
    {
        Debug.Log("EventManager: Correctly denied bad coffee");
        onCorrectlyDeniedCoffee?.Invoke();
    }
    
    // Customer tracking methods
    public void IncrementCustomerCount()
    {
        Debug.Log("EventManager: Customer served");
        onCustomerServed?.Invoke();
    }
    
    // Queue state methods
    public void QueueBecameEmpty()
    {
        Debug.Log("EventManager: Queue became empty");
        onQueueBecameEmpty?.Invoke();
    }
    
    public void QueueGotFirstItem()
    {
        Debug.Log("EventManager: Queue got its first item");
        onQueueGotFirstItem?.Invoke();
    }

    public void ItemEnqueued(CoffeeOrder order)
    {
        Debug.Log("EventManager: Item enqueued to queue: " + order.Coffee.flavor + " " + order);
        onItemEnqueued?.Invoke(order);
    }

    // Add new method to trigger the clear queue event
    public void ClearQueueRequested()
    {
        onClearQueueRequested?.Invoke();
    }

    public void PauseTimer()
    {
        onTimerPaused?.Invoke();
    }

    public void ResumeTimer()
    {
        onTimerResumed?.Invoke();
    }

    // Upgrade methods
    public void ApplyUpgrade(string upgradeName, UpgradeCategory category, int newLevel)
    {
        onUpgradeApplied?.Invoke(upgradeName, category, newLevel);
    }

    public void CustomerSaved()
    {
        onCustomerSaved?.Invoke();
    }

    // Method to trigger the new event
    public void CustomerLeftEarly()
    {
        onCustomerLeftEarly?.Invoke();
    }
    
    // Method to trigger the customer died event
    public void CustomerDied()
    {
        onCustomerDied?.Invoke();
    }

    // Method to trigger the game over event
    public void GameOver()
    {
        onGameOver?.Invoke();
    }
}
