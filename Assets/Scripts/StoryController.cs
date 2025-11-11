using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoryController : MonoBehaviour
{
    public GameObject introControllerObject;
    private IntroConrtoller introController;
    private IntroSlidesData currentSlidesData;

    private void Start()
    {
        // Get the IntroController component from the GameObject
        if (introControllerObject != null)
        {
            introController = introControllerObject.GetComponent<IntroConrtoller>();
            if (introController == null)
            {
                Debug.LogError("IntroConrtoller component not found on introControllerObject!");
            }
        }
        
        // Subscribe to the slide display event
        EventManager.current.onSlideDisplayRequested += OnSlideDisplayRequested;
        
        // Set up the completion callback on the intro controller
        if (introController != null)
        {
            introController.SetCompletionCallback(OnSlideshowCompleted);
        }
        
        // Hide the intro controller object at start
        if (introControllerObject != null)
        {
            introControllerObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events when the object is destroyed
        if (EventManager.current != null)
        {
            EventManager.current.onSlideDisplayRequested -= OnSlideDisplayRequested;
        }
        
        // Remove the completion callback
        if (introController != null)
        {
            introController.SetCompletionCallback(null);
        }
    }

    // Event listener for slide display requests
    private void OnSlideDisplayRequested(IntroSlidesData slidesData)
    {
        if (introController != null && introControllerObject != null)
        {
            Debug.Log("StoryController received slide display request. Forwarding to IntroController.");
            currentSlidesData = slidesData;
            
            // Show the intro controller object
            introControllerObject.SetActive(true);
            
            introController.StartSlideshow(slidesData);
        }
        else
        {
            Debug.LogError("IntroController reference is missing in StoryController!");
        }
    }
    
    // Callback for when slideshow is completed
    private void OnSlideshowCompleted()
    {
        Debug.Log("StoryController: Slideshow completed");
        
        // Hide the intro controller object
        if (introControllerObject != null)
        {
            introControllerObject.SetActive(false);
        }
        
        if (EventManager.current != null && currentSlidesData != null)
        {
            EventManager.current.SlideshowCompleted(currentSlidesData);
        }
    }
}
