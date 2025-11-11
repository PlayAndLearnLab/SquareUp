using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Line
{
    OrderLine,
    WaitLine
}

public class LineController : MonoBehaviour
{
    private readonly object _queueLock = new object();
    // Dictionary to track the currently running move coroutine for each customer
    private Dictionary<MovementController, Coroutine> _activeMoveCoroutines = new Dictionary<MovementController, Coroutine>();

    public static float LINE_DISTANCE_INTERVAL = 3.2f;
    public Transform lineStart; // Use this to calculate the direction
    public Transform lineEnd; // Position 1 (front) is here

    private Queue<MovementController> queue = new Queue<MovementController>();

    // Helper to calculate target position based on 1-based index
    private Vector3 GetTargetPositionForIndex(int index) // index is 1-based
    {
        Vector3 start = lineStart.position; // Assuming lineStart is "behind" lineEnd
        Vector3 end = lineEnd.position;
        Vector3 directionVector = Vector3.Normalize(end - start);
        // Position 1 is at lineEnd, Position 2 is one step back, etc.
        return end - ((index - 1) * directionVector * LINE_DISTANCE_INTERVAL);
    }

    // Returns when you are at the front of the line (position 1)
    public IEnumerator WaitInLine(int speed, MovementController customerController)
    {
        if (customerController == null) yield break;

        // Store the speed on the controller instance
        customerController.speed = speed;

        Vector3 initialTargetPosition;
        int initialPosition;

        lock (_queueLock)
        {
            // Avoid adding duplicates if logic elsewhere might call this multiple times
            if (queue.Contains(customerController))
            {
                 Debug.LogWarning($"Customer {customerController.name} already in line. Ignoring WaitInLine call.");
                 yield break;
            }

            queue.Enqueue(customerController);
            initialPosition = queue.Count; // 1-based position
            initialTargetPosition = GetTargetPositionForIndex(initialPosition);

             // Clear any potentially stale move coroutine entry
            if (_activeMoveCoroutines.ContainsKey(customerController))
            {
                _activeMoveCoroutines.Remove(customerController);
            }
        }

        // Start the initial walk to the back of the line
        Coroutine initialMoveCoroutine = StartCoroutine(customerController.WalkTo(initialTargetPosition, speed));
        lock(_queueLock) // Lock briefly to safely add to dictionary
        {
             // Only add if the customer is still relevant (might have been removed immediately)
             if (queue.Contains(customerController)) {
                 _activeMoveCoroutines[customerController] = initialMoveCoroutine;
             } else {
                 // If the customer was removed before we could track the coroutine, stop it.
                 if(initialMoveCoroutine != null) StopCoroutine(initialMoveCoroutine);
                 yield break; // Exit early as the customer is no longer in line
             }
        }

        // Ensure the coroutine is valid before yielding
        if (initialMoveCoroutine == null) {
             // This might happen if WalkTo returns null immediately or if the customer was removed.
             Debug.LogWarning($"Initial move coroutine for {customerController.name} was null or stopped early.");
             // Attempt cleanup just in case
             lock(_queueLock) { _activeMoveCoroutines.Remove(customerController); }
             yield break;
        }

        yield return initialMoveCoroutine; // Wait for the initial walk to complete

        // Check if the customer is still in the line after the initial walk before waiting
        if (GetPositionInLine(customerController) == -1)
        {
            // Customer was removed during the initial walk
            lock(_queueLock) { _activeMoveCoroutines.Remove(customerController); }
            yield break;
        }

        // Now, just wait until this customer reaches the front position (index 1)
        // The actual movement will be triggered by RemoveFromLine
        // Add a timeout to avoid being stuck forever
        float waitStartTime = Time.time;
        float timeoutDuration = 60f; // 60 seconds max wait time
        yield return new WaitUntil(() => {
            int currentPos = GetPositionInLine(customerController);
            // Debug.Log($"WaitInLine Check: {customerController.name} at position {currentPos}"); // Verbose log

            // Check if we should time out
            if (Time.time - waitStartTime > timeoutDuration) {
                Debug.LogWarning($"WaitInLine TIMEOUT: Customer {customerController.name} timed out waiting to reach position 1 (Current: {currentPos})");
                return true; // Break the wait and continue
            }

            // Check if removed from line
            if (currentPos == -1) {
                Debug.LogWarning($"WaitInLine ABORT: Customer {customerController.name} is no longer in line (position -1) during WaitUntil");
                return true; // Break the wait and continue
            }

            // Normal condition - wait until reaching position 1
            return currentPos == 1;
        });

        // Debug information about the customer's position after the wait
        int finalPosition = GetPositionInLine(customerController);
        if (finalPosition == 1) {
            Debug.Log($"Customer {customerController.name} has reached the front of the line successfully");
        } else if (finalPosition == -1) {
            Debug.LogWarning($"Customer {customerController.name} is no longer in the line after waiting");
        } else {
            Debug.LogWarning($"Customer {customerController.name} waited for front position but ended up at position {finalPosition}");
        }

        // Once the WaitUntil is satisfied, this coroutine finishes, indicating the customer is at the front.
         lock(_queueLock) // Clean up the coroutine tracker
         {
            if (_activeMoveCoroutines.ContainsKey(customerController))
            {
                _activeMoveCoroutines.Remove(customerController);
            }
         }
    }

    public void RemoveFromLine(MovementController customerController)
    {
        if (customerController == null) return;

        MovementController actualNewFrontCustomer = null;
        bool removalOccurred = false;
        int originalLineLength = 0;
        MovementController originalFrontCustomer = null;

        lock (_queueLock)
        {
            originalLineLength = GetLineLength(); // Use safe GetLineLength
            originalFrontCustomer = GetFrontCustomer();
            Debug.Log($"[RemoveFromLine-{customerController.name}] START: Original Front='{originalFrontCustomer?.name}', Length={originalLineLength}");

            // Check if the customer exists in the line
            if (queue == null || queue.Count == 0 || !queue.Contains(customerController))
            {
                 Debug.LogWarning($"[RemoveFromLine-{customerController.name}] ABORT: Customer not found in queue.");
                 // Ensure cleanup even if not found in queue (might be called during destruction)
                if (_activeMoveCoroutines.TryGetValue(customerController, out Coroutine strayCoroutine) && strayCoroutine != null)
                {
                    StopCoroutine(strayCoroutine);
                }
                _activeMoveCoroutines.Remove(customerController);
                return;
            }

            // Stop any current movement for the departing customer
            if (_activeMoveCoroutines.TryGetValue(customerController, out Coroutine departingCoroutine) && departingCoroutine != null)
            {
                StopCoroutine(departingCoroutine);
                 Debug.Log($"[RemoveFromLine-{customerController.name}] Stopped existing move coroutine.");
            }
            _activeMoveCoroutines.Remove(customerController);

            Queue<MovementController> tempQueue = new Queue<MovementController>();
            int removedIndex = -1;
            int readIndex = 1; // 1-based index for reading original queue

            // Rebuild the queue and find the index of the removed customer
            while (queue.Count > 0)
            {
                MovementController cc = queue.Dequeue();
                if (cc == customerController)
                {
                    removedIndex = readIndex;
                    removalOccurred = true; // Mark that a removal happened
                    // Don't enqueue, effectively removing them
                }
                else if (cc != null) // Check if customer was destroyed
                {
                    tempQueue.Enqueue(cc);
                }
                else
                {
                    // If a null customer is found, just skip it. The line will shorten.
                    Debug.LogWarning($"[RemoveFromLine-{customerController.name}] Null customer found at original position {readIndex} during rebuild.");
                }
                 readIndex++;
            }

            // Assign the rebuilt queue
            queue = tempQueue;

            if (!removalOccurred) {
                 Debug.LogError($"[RemoveFromLine-{customerController.name}] ERROR: Failed to find customer during queue rebuild even though Contains was true.");
                 return;
            }

            // After rebuilding, identify the actual new front customer
            actualNewFrontCustomer = GetFrontCustomer(); // GetFrontCustomer handles null checks internally
            int newLineLength = GetLineLength();
            Debug.Log($"[RemoveFromLine-{customerController.name}] REBUILT: New Front='{actualNewFrontCustomer?.name}', Length={newLineLength}, Removed Index={removedIndex}");

            // Now, trigger moves for everyone whose position potentially changed
            int writeIndex = 1; // Reset index for the *new* queue
            foreach (MovementController customerInLine in queue)
            {
                 if (writeIndex >= removedIndex && customerInLine != null)
                 {
                    Vector3 newTargetPosition = GetTargetPositionForIndex(writeIndex);
                    Debug.Log($"[RemoveFromLine-{customerController.name}] Triggering move for {customerInLine.name} (Index {writeIndex}) to {newTargetPosition}");

                    // Stop any existing move for this customer (redundant safety check)
                    if (_activeMoveCoroutines.TryGetValue(customerInLine, out Coroutine existingCoroutine) && existingCoroutine != null)
                    {
                        StopCoroutine(existingCoroutine);
                    }

                    // Start the new move and track it
                    int customerSpeed = customerInLine.speed;
                    Coroutine moveCoroutine = StartCoroutine(customerInLine.WalkTo(newTargetPosition, customerSpeed));
                    _activeMoveCoroutines[customerInLine] = moveCoroutine; // Track the new coroutine
                 } else if (customerInLine == null) {
                     Debug.LogError($"[RemoveFromLine-{customerController.name}] ERROR: Found null customer in queue during position update loop at index {writeIndex}");
                 }
                 writeIndex++;
            }
        } // End lock

        // If a removal happened and there's a new front customer, signal them outside the lock
        if (removalOccurred && actualNewFrontCustomer != null)
        {
            Debug.Log($"[RemoveFromLine-{customerController.name}] Starting SignalCustomerReachedFront for {actualNewFrontCustomer.name}");
            StartCoroutine(SignalCustomerReachedFront(actualNewFrontCustomer));
        }
        else if (removalOccurred)
        {
            Debug.Log($"[RemoveFromLine-{customerController.name}] Removal occurred but no new front customer (line empty?).");
        }
    }

    // Helper coroutine to ensure the WaitUntil condition in WaitInLine is re-evaluated
    private IEnumerator SignalCustomerReachedFront(MovementController customer)
    {
        if (customer == null) yield break;
        Debug.Log($"[Signal-{customer.name}] Coroutine START.");

        // Ensure the customer is still intended to be at the front
        lock(_queueLock)
        {
            if (GetPositionInLine(customer) != 1)
            {
                 Debug.LogWarning($"[Signal-{customer.name}] ABORT: Customer no longer at position 1.");
                 yield break;
            }
        }

        // Wait for the customer's current movement coroutine to finish
        Coroutine moveCoroutine = null;
        lock(_queueLock) // Lock briefly to access dictionary safely
        {
            _activeMoveCoroutines.TryGetValue(customer, out moveCoroutine);
        }

        if (moveCoroutine != null)
        {
            Debug.Log($"[Signal-{customer.name}] Waiting for movement coroutine...");
            yield return moveCoroutine; // Wait for the walk to complete
            Debug.Log($"[Signal-{customer.name}] Movement coroutine finished.");
        }
        else
        {
            Debug.LogWarning($"[Signal-{customer.name}] No active move coroutine found to wait for.");
        }

        // Check position again *after* movement is supposed to be done
        int finalPositionPreSignal = GetPositionInLine(customer);
        Debug.Log($"[Signal-{customer.name}] Position after movement wait: {finalPositionPreSignal}");

        if (finalPositionPreSignal == 1)
        {
            // Force Unity to update the WaitUntil condition check only if still at front
            if (customer != null && customer.gameObject != null)
            {
                Debug.Log($"[Signal-{customer.name}] Toggling SetActive at final position 1.");
                customer.gameObject.SetActive(customer.gameObject.activeSelf);
            }
            else
            {
                 Debug.LogWarning($"[Signal-{customer.name}] Customer/GameObject became null before SetActive toggle.");
            }
        }
        else
        {
            Debug.LogWarning($"[Signal-{customer.name}] Not signaling. Ended up at position {finalPositionPreSignal} after movement.");
        }
         Debug.Log($"[Signal-{customer.name}] Coroutine END.");
    }

    // This now calculates the position for the *next* spot if someone were to join
    public Vector3 GetLineStartPosition()
    {
        lock (_queueLock)
        {
            // Target position for the (Count + 1)th spot
             int nextSpotIndex = queue.Count + 1;
            return GetTargetPositionForIndex(nextSpotIndex);
        }
    }

    public MovementController GetFrontCustomer()
    {
        lock (_queueLock)
        {
            if (queue == null || queue.Count == 0)
            {
                return null;
            }
            // Clean up null refs at the front proactively
            while(queue.Count > 0 && queue.Peek() == null) {
                Debug.LogWarning("Null customer found at the front of the queue. Dequeuing.");
                queue.Dequeue();
                // Removing a null from the front doesn't require shifting others here,
                // as RemoveFromLine is the primary mechanism for position updates.
            }
             return queue.Count > 0 ? queue.Peek() : null;
        }
    }

    public int GetLineLength()
    {
        lock (_queueLock)
        {
             // Filters nulls for a more accurate "active" count
             int count = 0;
             if (queue != null) {
                 foreach(var customer in queue) {
                     if (customer != null) count++;
                 }
             }
             return count;
            // return queue?.Count ?? 0; // Previous simpler version
        }
    }

    /// <summary>
    /// Gets the position of a customer in line (1-based index)
    /// </summary>
    /// <param name="customerController">The customer to find</param>
    /// <returns>Position in line (1 = front of line), or -1 if not found or if customer is null</returns>
    public int GetPositionInLine(MovementController customerController)
    {
        if (customerController == null) return -1; // Cannot find position of null

        lock (_queueLock)
        {
            if (queue == null || queue.Count == 0)
            {
                return -1;
            }

            int position = 1; // 1-based index
            foreach (MovementController customerInQueue in queue)
            {
                 if (customerInQueue == null) { // Skip null entries in the queue
                     continue;
                 }
                if (customerInQueue == customerController)
                {
                    return position;
                }
                position++; // Only increment position for non-null customers found
            }
        }
        return -1; // Not found
    }

     // Optional: Cleanup coroutine dictionary if a customer is destroyed externally
     // Note: This only cleans up if the LineController itself is destroyed.
     // Individual customer destruction needs handling within RemoveFromLine or similar logic.
     void OnDestroy()
     {
         // Stop all active movement coroutines managed by this controller when the controller itself is destroyed
         lock(_queueLock)
         {
             foreach(var kvp in _activeMoveCoroutines)
             {
                 if (kvp.Key != null && kvp.Value != null) // Check if coroutine is still valid
                 {
                    StopCoroutine(kvp.Value);
                 }
             }
             _activeMoveCoroutines.Clear();
             if (queue != null) queue.Clear(); // Clear the queue as well
         }
     }
}
