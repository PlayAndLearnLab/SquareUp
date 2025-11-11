using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class IntroConrtoller : MonoBehaviour
{
    private int currentSlide = 0;
    [SerializeField] private IntroSlidesData slidesData;
    [SerializeField] private Image slideImage;

    public Button nextButton;
    private TextMeshProUGUI nextButtonText;
    public Button previousButton;
    
    private Action onSlideshowComplete;

    void Start()
    {
        if (slidesData == null)
        {
            Debug.LogError("Slides Data is not assigned!");
            return;
        }

        // Set the first slide
        if (slidesData.SlidesCount > 0)
        {
            slideImage.sprite = slidesData.Slides[currentSlide];
        }

        nextButton.onClick.AddListener(NextSlide);
        previousButton.onClick.AddListener(PreviousSlide);
        nextButton.interactable = false;
        previousButton.interactable = false;

        // check if there is only one slide
        if (slidesData.SlidesCount != 1)
        {
            nextButton.interactable = true;
        }

        nextButtonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();

        // Print the current width and height of the game window
        print("Current width: " + Screen.width);
        print("Current height: " + Screen.height);
    }

    public void CheckInteractable()
    {
        if (currentSlide == 0)
        {
            previousButton.interactable = false;
        }
        else
        {
            previousButton.interactable = true;
        }

        if (currentSlide == slidesData.SlidesCount - 1)
        {
            nextButtonText.text = "Start!";
        }
        else
        {
            nextButtonText.text = "Next";
        }
    }

    public void NextSlide()
    {
        if (currentSlide == slidesData.SlidesCount - 1)
        {
            if (onSlideshowComplete != null)
            {
                onSlideshowComplete.Invoke();
            }
            else
            {
                StartGame(); // Default behavior if no callback is set
            }
            return;
        }
        currentSlide++;
        slideImage.sprite = slidesData.Slides[currentSlide];
        CheckInteractable();
    }

    public void PreviousSlide()
    {
        currentSlide--;
        slideImage.sprite = slidesData.Slides[currentSlide];
        CheckInteractable();
    }

    public void StartSlideshow(IntroSlidesData newSlidesData)
    {
        // Update slides data
        slidesData = newSlidesData;
        
        // Reset to first slide
        currentSlide = 0;
        
        // Update the image
        if (slidesData.SlidesCount > 0)
        {
            slideImage.sprite = slidesData.Slides[currentSlide];
        }
        
        // Update button states
        CheckInteractable();
    }

    private void StartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainGame");
    }

    public void SetCompletionCallback(Action callback)
    {
        onSlideshowComplete = callback;
    }
}
