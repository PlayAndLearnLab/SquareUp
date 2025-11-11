using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CoffeeOrder
{
    public GameObject CoffeeObject { get; set; }
    public Coffee Coffee { get; set; }

    public CoffeeOrder(GameObject coffeeObject, Coffee coffee)
    {
        CoffeeObject = coffeeObject;
        Coffee = coffee;
    }
}

[CreateAssetMenu(fileName = "GameState", menuName = "Game/GameState")]
public class GameState : ScriptableObject
{
    public Queue<CoffeeOrder> coffeeOrderQueue = new Queue<CoffeeOrder>();

    // Reset state when game starts
    public void Reset()
    {
        coffeeOrderQueue.Clear();
    }

    private void OnEnable()
    {
        if (EventManager.current != null)
        {
            EventManager.current.onClearQueueRequested += ClearQueue;
        }
    }

    private void OnDisable()
    {
        if (EventManager.current != null)
        {
            EventManager.current.onClearQueueRequested -= ClearQueue;
        }
    }

    // Helper methods for queue operations
    public void AddCoffee(GameObject coffee, Coffee order)
    {
        // Check if queue is empty before adding (for 0->1 transition)
        bool wasEmpty = coffeeOrderQueue.Count == 0;
        
        CoffeeOrder coffeeOrder = new CoffeeOrder(coffee, order);
        coffeeOrderQueue.Enqueue(coffeeOrder);
        
        // If queue was empty and now has an item, trigger the event
        if (wasEmpty && EventManager.current != null)
        {
            EventManager.current.QueueGotFirstItem();
        }

        EventManager.current.ItemEnqueued(coffeeOrder);
    }

    public CoffeeOrder GetNextCoffee()
    {
        if (coffeeOrderQueue.Count == 0)
            return null;
            
        CoffeeOrder order = coffeeOrderQueue.Dequeue();
        
        // Check if queue just became empty
        if (coffeeOrderQueue.Count == 0 && EventManager.current != null)
        {
            EventManager.current.QueueBecameEmpty();
        }
        
        return order;
    }

    public CoffeeOrder PeekNextCoffee()
    {
        return coffeeOrderQueue.Count > 0 ? coffeeOrderQueue.Peek() : null;
    }

    public bool HasNextCoffee()
    {
        return coffeeOrderQueue.Count > 0;
    }

    public bool RemoveCoffee(CoffeeOrder coffeeToRemove)
    {
        if (coffeeOrderQueue.Count == 0 || coffeeToRemove == null)
            return false;
            
        // Since Queue doesn't support direct removal of specific items,
        // we need to recreate the queue without the target item
        List<CoffeeOrder> tempList = coffeeOrderQueue.ToList();
        bool wasRemoved = tempList.Remove(coffeeToRemove);
        
        if (wasRemoved)
        {
            // Recreate the queue with the remaining items
            coffeeOrderQueue.Clear();
            foreach (var order in tempList)
            {
                coffeeOrderQueue.Enqueue(order);
            }
            
            // Check if queue just became empty
            if (coffeeOrderQueue.Count == 0 && EventManager.current != null)
            {
                EventManager.current.QueueBecameEmpty();
            }
        }
        
        return wasRemoved;
    }

    public void ClearQueue()
    {
        bool wasNotEmpty = coffeeOrderQueue.Count > 0;

        // Explode each coffee object before clearing
        foreach (var coffeeOrder in coffeeOrderQueue)
        {
            if (coffeeOrder?.CoffeeObject != null)
            {
                CoffeeCupController controller = coffeeOrder.CoffeeObject.GetComponent<CoffeeCupController>();
                if (controller != null)
                {    
                    // Use the helper to start the Explode coroutine
                    CoroutineHelper.Instance.RunCoroutine(controller.Explode());
                }
                else
                {
                    // Optional: Destroy immediately if no controller (shouldn't happen)
                    Debug.LogWarning("CoffeeCupController not found on queued object, destroying immediately.");
                    Destroy(coffeeOrder.CoffeeObject); 
                }
            }
        }

        // Clear the queue data structure
        coffeeOrderQueue.Clear();

        // Trigger the event if the queue was not empty before clearing
        if (wasNotEmpty && EventManager.current != null)
        {
            EventManager.current.QueueBecameEmpty();
        }
    }
}
