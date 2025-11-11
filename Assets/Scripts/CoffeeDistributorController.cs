using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// Add UI namespace
using UnityEngine.UI;

public class CoffeeDistributorController : MonoBehaviour
{
    public GameObject coffeeCupPrefab;
    public GameObject spawnPoint;
    public GameObject gearObject;
    public AudioClip hydraulicSound;
    public AudioClip popSound;
    
    // Replace progress bar references with ProgressBarController
    [Header("Progress Bar")]
    public ProgressBarController progressBar;
    
    public AudioClip malfunctionSound; // Sound to play when malfunction occurs
    public GameObject explosionEffectPrefab; // Prefab for the explosion particle effect
    private bool isTesting = false;
    private bool _interruptRequested = false; // Flag to signal interruption
    private bool _isDayOver = false; // Flag to check if the day has ended
    
    // List to hold all gear objects (original + duplicates)
    private List<GameObject> activeGears = new List<GameObject>();
    private Vector3 originalCenterPosition;
    private const float GEAR_SPACING = 1f; // Spacing between gears
    
    // New simplified Speed and Accuracy
    private float currentSpeed;
    public float CurrentSpeed { get => currentSpeed; private set => currentSpeed = Mathf.Clamp(value, 0f, 100f); }
    
    private float currentAccuracy;
    public float CurrentAccuracy { get => currentAccuracy; private set => currentAccuracy = Mathf.Clamp(value, 0f, 100f); }

    void Start()
    {
        // Initialize new Speed and Accuracy
        CurrentAccuracy = 20f;
        CurrentSpeed = 20f; 
        // Fire initial event
        if (EventManager.current != null)
        {
            EventManager.current.CoffeeDistributorSpeedChanged(CurrentSpeed, CurrentAccuracy);
        }
        
        // Subscribe to the give_coffee event
        EventManager.current.onGiveCoffee += GiveCoffee;
        
        // Removed subscriptions to correct/incorrect coffee decision events
        
        // Progress bar is now handled by the ProgressBarController
        // No need to check it here as it starts hidden by default
        if (progressBar == null)
        {
            Debug.LogWarning("No ProgressBarController assigned to CoffeeDistributor!");
        }

        // Initialize the gear list with the original gear and store center position
        if (gearObject != null)
        {
            originalCenterPosition = gearObject.transform.position;
            activeGears.Add(gearObject);
        }
        else
        {
            Debug.LogWarning("Original gearObject is not assigned!");
        }

        // Subscribe to the upgrade event
        if (EventManager.current != null) // Check needed if Start runs before EventManager Awake
        {
            EventManager.current.onUpgradeApplied += OnUpgradeApplied;
            EventManager.current.onDayCompleted += HandleDayCompleted; // Subscribe to day end
            EventManager.current.onStartNextDay += HandleStartNextDay; // Subscribe to day start
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (EventManager.current != null)
        { 
            EventManager.current.onGiveCoffee -= GiveCoffee;
            // Removed unsubscriptions for correct/incorrect coffee decision events
            EventManager.current.onUpgradeApplied -= OnUpgradeApplied; // Unsubscribe here
            EventManager.current.onDayCompleted -= HandleDayCompleted; // Unsubscribe from day end
            EventManager.current.onStartNextDay -= HandleStartNextDay; // Unsubscribe from day start
        }
    }

    private void GiveCoffee(Coffee coffee)
    {
        // Don't start making coffee if the day is over
        if (_isDayOver)
        {
            Debug.Log("Day is over. Not starting new coffee production.");
            // Optionally, trigger an event or feedback if needed
            return;
        }

        StartCoroutine(GiveCoffeeSequence(coffee));
    }

    private IEnumerator GiveCoffeeSequence(Coffee coffee)
    {
        _interruptRequested = false; // Reset interrupt flag at the start

        EventManager.current.CoffeeOrderPressed();
        Debug.Log("Shaking...");
        // Calculate shake duration based on speed
        // Higher speed = shorter duration
        // At speed 0: duration is 10x base duration
        // At speed 100: duration is 0.5x base duration
        float baseShakeDuration = 1f; // Define a base duration
        float speedFactor = Mathf.Lerp(10f, 0.5f, CurrentSpeed / 100f); // Use CurrentSpeed
        float shakeDuration = baseShakeDuration * speedFactor;
        
        // // Start the progress bar using the existing implementation - do this once before any branches
        // if (progressBar != null)
        // {
        //     // Convert shake duration to int seconds for the progress bar, ensure at least 1 second
        //     int progressBarDuration = Mathf.Max(1, Mathf.CeilToInt(shakeDuration));
        //     StartCoroutine(progressBar.StartProgressBarAndWait(progressBarDuration));
        // }
        
        AudioSource.PlayClipAtPoint(hydraulicSound, spawnPoint.transform.position);
        
        // Start shake animation
        StartCoroutine(ShakeAnimation(shakeDuration, 1f));
        
        // Check for interrupt before gear animation
        if (_interruptRequested) { HandleInterrupt(); yield break; }

        // Handle gear animation if it exists
        if (activeGears.Count > 0)
        {
            // Adjust rotation speed based on distributor speed
            float rotationMultiplier = Mathf.Lerp(0.5f, 2f, CurrentSpeed / 100f); // Use CurrentSpeed
            float totalRotation = -360f * rotationMultiplier;

            // Start rotation for all gears simultaneously
            List<Coroutine> rotationCoroutines = new List<Coroutine>();
            foreach (GameObject gear in activeGears)
            {
                if (gear != null)
                {
                    rotationCoroutines.Add(StartCoroutine(RotateAnimation(gear, totalRotation, shakeDuration)));
                }
            }

            // Wait for all gear rotations to complete
            foreach (Coroutine routine in rotationCoroutines)
            {
                yield return routine;
                // Check for interrupt after each gear rotation finishes (optional, but allows faster response)
                if (_interruptRequested) { HandleInterrupt(); yield break; }
            }
        }
        else
        {
            // If no gears, just wait the same duration
            yield return new WaitForSeconds(shakeDuration);
            Debug.LogWarning("No active gears found to rotate!");
        }
        
        // Check for interrupt before malfunction check
        if (_interruptRequested) { HandleInterrupt(); yield break; }

        bool isInTutorialAndExplodeCoffee = TutorialHelper.IsInTutorial && TutorialHelper.explodeCoffee;
        // Check for malfunction at the end of the sequence
        // Malfunction chance now directly uses CurrentAccuracy (higher accuracy = lower failure chance)
        if ((TutorialHelper.IsInTutorial && TutorialHelper.explodeCoffee) || (!TutorialHelper.IsInTutorial && (Random.Range(0f, 100f) > CurrentAccuracy && !isTesting)))
        {
            Debug.Log("Coffee machine malfunctioned!");
            TutorialHelper.QueueTutorialStep(4);
            TutorialHelper.SetExplodeCoffee(false);
            if (malfunctionSound != null)
            {
                AudioSource.PlayClipAtPoint(malfunctionSound, spawnPoint.transform.position);
            }
            
            // Create explosion effect
            if (explosionEffectPrefab != null)
            {
                GameObject explosionEffect = Instantiate(explosionEffectPrefab, spawnPoint.transform.position, Quaternion.identity);
                
                // Configure particle system
                ParticleSystem particleSystem = explosionEffect.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                    renderer.sortingLayerName = "Default";
                    renderer.sortingOrder = 10;

                    particleSystem.Stop();
                    particleSystem.Clear();
                    particleSystem.Play();
                }

                // Clean up explosion after delay
                StartCoroutine(CleanupExplosion(explosionEffect));
            }

            // Do a violent shake with rotation for all gears
            StartCoroutine(ShakeAnimation(0.5f, 2f)); // Shake the whole distributor
            float malfunctionRotation = 720f; // Spin twice
            float malfunctionDuration = 1f;
            List<Coroutine> malfunctionRotations = new List<Coroutine>();
            foreach (GameObject gear in activeGears)
            {
                if (gear != null)
                {
                    malfunctionRotations.Add(StartCoroutine(RotateAnimation(gear, malfunctionRotation, malfunctionDuration)));
                }
            }
            // Wait for all malfunction rotations
            foreach (Coroutine routine in malfunctionRotations)
            {
                yield return routine;
                 // Check for interrupt during malfunction animation
                if (_interruptRequested) { HandleInterrupt(); yield break; }
            }
            
            // Instead of ending without coffee, give poison coffee
            Debug.Log("Giving poison coffee due to malfunction");
            AudioSource.PlayClipAtPoint(popSound, spawnPoint.transform.position);
            GameObject poisonCoffeeCup = Instantiate(coffeeCupPrefab, spawnPoint.transform.position, Quaternion.identity);
            poisonCoffeeCup.SetActive(true);
            
            // Create a poison coffee regardless of what was ordered
            Coffee poisonCoffee = new Coffee(CoffeeFlavor.Poison, coffee.temperature);
            poisonCoffeeCup.GetComponent<CoffeeCupController>().GiveCoffee(poisonCoffee);
            
            // Hide the progress bar
            if (progressBar != null)
            {
                progressBar.HideProgressBar();
            }
            EventManager.current.CoffeeProduced(poisonCoffee);
            yield break; // End the sequence after giving poison coffee
        }

        // Check for interrupt before giving normal coffee
        if (_interruptRequested) { HandleInterrupt(); yield break; }

        AudioSource.PlayClipAtPoint(popSound, spawnPoint.transform.position);
        GameObject coffeeCup = Instantiate(coffeeCupPrefab, spawnPoint.transform.position, Quaternion.identity);
        coffeeCup.SetActive(true);
        coffeeCup.GetComponent<CoffeeCupController>().GiveCoffee(coffee);
        
        // Hide the progress bar
        if (progressBar != null)
        {
            progressBar.HideProgressBar();
        }
        EventManager.current.CoffeeProduced(coffee);
    }

    private IEnumerator ShakeAnimation(float duration, float intensityMultiplier = 1f)
    {
        Vector3 startPosition = transform.position;
        float elapsed = 0f;
        float animationCycleTime = 0.82f; // Original animation duration as one cycle
        
        while (elapsed < duration)
        {
            float overallProgress = elapsed / duration;
            float cycleProgress = (elapsed % animationCycleTime) / animationCycleTime;
            float easedProgress = CubicBezier(cycleProgress, 0.36f, 0.07f, 0.19f, 0.97f);
            
            // Calculate intensity multiplier - starts very soft, ramps up exponentially, plateaus, stops abruptly
            float currentIntensity;
            if (overallProgress < 0.3f) // First 30% - exponential ramp up
            {
                float rampProgress = overallProgress / 0.3f;
                currentIntensity = Mathf.Lerp(0.05f, 1f, rampProgress * rampProgress) * intensityMultiplier; // Exponential curve
            }
            else if (overallProgress > 0.95f) // Last 5% - stop abruptly
            {
                currentIntensity = 0f;
            }
            else // Middle section - full intensity
            {
                currentIntensity = intensityMultiplier;
            }
            
            Vector3 offset = Vector3.zero;
            if (cycleProgress < 0.1f || cycleProgress > 0.9f)
                offset = new Vector3(-0.05f, 0.02f, 0);
            else if (cycleProgress < 0.2f || cycleProgress > 0.8f)
                offset = new Vector3(0.1f, -0.02f, 0);
            else if (cycleProgress < 0.3f || (cycleProgress > 0.5f && cycleProgress < 0.7f))
                offset = new Vector3(-0.15f, 0.03f, 0);
            else if (cycleProgress < 0.4f || cycleProgress < 0.6f)
                offset = new Vector3(0.15f, -0.03f, 0);
            
            transform.position = startPosition + offset * easedProgress * currentIntensity;
                
            elapsed += Time.deltaTime;
            // Check for interrupt within the shake loop
            if (_interruptRequested) yield break; // Exit shake early if interrupted
            yield return null;
        }
        
        transform.position = startPosition;
    }

    private float CubicBezier(float t, float p0, float p1, float p2, float p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;
        
        return uuu * p0 + 
               3 * uu * t * p1 + 
               3 * u * tt * p2 + 
               ttt * p3;
    }

    private IEnumerator CleanupExplosion(GameObject explosionEffect)
    {
        yield return new WaitForSeconds(1f);
        if (explosionEffect != null)
        {
            Destroy(explosionEffect);
        }
    }

    private IEnumerator RotateAnimation(GameObject target, float totalRotation, float duration)
    {
        float elapsed = 0f;
        Quaternion startRotation = target.transform.rotation;
        
        while (elapsed < duration)
        {
            // Check for interrupt within the rotate loop
            if (_interruptRequested) yield break; // Exit rotation early if interrupted

            float progress = elapsed / duration;
            
            // Use smooth step for easing
            float smoothProgress = progress * progress * (3f - 2f * progress);
            
            // Calculate current rotation
            float currentRotation = smoothProgress * totalRotation;
            target.transform.rotation = startRotation * Quaternion.Euler(0f, 0f, currentRotation);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end at exactly the desired rotation
        target.transform.rotation = startRotation * Quaternion.Euler(0f, 0f, totalRotation);
    }

    private void OnUpgradeApplied(string upgradeName, UpgradeCategory category, int newLevel)
    {
        bool changed = false;
        // Check if the upgrade is for Speed
        if (category == UpgradeCategory.Speed)
        {
            CurrentSpeed += 25f;
            Debug.Log($"Speed upgrade applied! New Speed: {CurrentSpeed}. Adding a gear.");
            DuplicateGear(); 
            RepositionAllGears(); 
            changed = true;
        }
        else if (category == UpgradeCategory.Accuracy)
        {
            CurrentAccuracy += 25f;
            Debug.Log($"Accuracy upgrade applied! New Accuracy: {CurrentAccuracy}.");
            changed = true;
        }

        // Fire event if Speed or Accuracy changed
        if (changed && EventManager.current != null)
        {
            EventManager.current.CoffeeDistributorSpeedChanged(CurrentSpeed, CurrentAccuracy);
        }
    }

    private void DuplicateGear() // Removed level parameter
    {
        if (gearObject == null)
        {
            Debug.LogError("Cannot duplicate gear: Original gearObject is null!");
            return;
        }

        // Create a new gear instance, parented like the original
        GameObject newGear = Instantiate(gearObject, gearObject.transform.parent);
        
        // Just add the new gear to the list, positioning happens in RepositionAllGears
        activeGears.Add(newGear);
    }

    private void RepositionAllGears()
    {
        int gearCount = activeGears.Count;
        if (gearCount == 0) return;

        // Calculate the total width occupied by the gears
        float totalWidth = (gearCount - 1) * GEAR_SPACING;

        // Calculate the starting position (leftmost gear) relative to the original center
        Vector3 startPosition = originalCenterPosition - new Vector3(totalWidth / 2f, 0, 0);

        // Position each gear
        for (int i = 0; i < gearCount; i++)
        {
            if (activeGears[i] != null)
            {
                activeGears[i].transform.position = startPosition + new Vector3(i * GEAR_SPACING, 0, 0);
            }
        }
    }

    /// <summary>
    /// Requests the current coffee production sequence to stop.
    /// </summary>
    public void RequestInterrupt()
    {
        _interruptRequested = true;
        Debug.Log("Coffee production interrupt requested.");
    }

    private void HandleInterrupt()
    {
        Debug.Log("Coffee production interrupted.");
        // Perform cleanup needed on interrupt
        if (progressBar != null)
        {
            progressBar.HideProgressBar();
        }
        // Potentially stop animations or sounds explicitly if needed
        StopCoroutine("ShakeAnimation"); // Stop the shake if it's running
        // Stop gear rotations if necessary (though they might finish quickly or be stopped by yield break)

        // Note: This does not clear any external queue.
    }

    private void HandleDayCompleted()
    {
        Debug.Log("Coffee Distributor: Day completed signal received.");
        _isDayOver = true;
        RequestInterrupt(); // Interrupt any ongoing production
    }

    private void HandleStartNextDay()
    {
        Debug.Log("Coffee Distributor: Starting new day.");
        _isDayOver = false;
        // Reset any other day-specific state if necessary
    }
}
