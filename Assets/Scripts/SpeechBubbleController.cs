using System.Collections;
using UnityEngine;

public class SpeechBubbleController : MonoBehaviour
{

    public GameObject timerIcon;
    public GameObject activeSpeakerIcon;
    public GameObject inactiveSpeakerIcon;
    public GameObject commentIcon;
    public CoffeeBrain brain = null;

    public SpriteRenderer replacementSprite;

    public ProgressBarController progressBarController;

    private Transform objTransform;
    private OnColliderClicked speechBubbleCollider;
    public OnColliderClicked customerCollider;
    private bool clicked = false;
    private float timer = 0;

    private bool waitingForCondition = false;

    private int timeFloor = 0;
    private AudioSource audioSource;

    // Create enum for bubble icon
    public enum BubbleIcon
    {
        Timer,
        ActiveSpeaker,
        InactiveSpeaker,
        Comment,
        Pumpkin,
        Apple,
        Candy
    }

    private void OnBubbleClicked()
    {
        if (brain != null && brain.IsBusy)
        {
            Debug.Log("Bubble click ignored: AI Brain is busy.");
            return;
        }

        clicked = true;
        timer = 0;
        Debug.Log("Bubble clicked");
        EventManager.current.BubbleClicked();
        if (waitingForCondition) {
            Debug.Log("Bubble clicked: Customer saved");
            EventManager.current.CustomerSaved();
            audioSource.Play();
        }
    }

    private void Awake()
    {
        timer = 0;

        //gameObject.SetActive(true);

        speechBubbleCollider = gameObject.GetComponent<OnColliderClicked>();
        speechBubbleCollider.clickAction = OnBubbleClicked;

        customerCollider.clickAction = OnBubbleClicked;

        timerIcon.SetActive(false);
        activeSpeakerIcon.SetActive(false);
        inactiveSpeakerIcon.SetActive(false);
        commentIcon.SetActive(false);

        audioSource = gameObject.GetComponent<AudioSource>();

        //gameObject.SetActive(false);
    }

    void FixedUpdate()
    {
        if (!gameObject.activeSelf) return;

        progressBarController.UpdateProgressBar(timer);
        timer += Time.deltaTime;
        if (timeFloor > 0)
        {
            timer = Mathf.Min(timer, timeFloor);
        }
    }

    private void HideAllIcons()
    {
        timerIcon.SetActive(false);
        activeSpeakerIcon.SetActive(false);
        inactiveSpeakerIcon.SetActive(false);
        commentIcon.SetActive(false);
        replacementSprite.sprite = null;
    }

    public void ShowIcon(BubbleIcon icon)
    {
        HideAllIcons();
        switch (icon)
        {
            case BubbleIcon.Timer:
                timerIcon.SetActive(true);
                break;
            case BubbleIcon.ActiveSpeaker:
                activeSpeakerIcon.SetActive(true);
                break;
            case BubbleIcon.InactiveSpeaker:
                inactiveSpeakerIcon.SetActive(true);
                break;
            case BubbleIcon.Comment:
                commentIcon.SetActive(true);
                break;
        }


    }

    public void ShowSpeechBubble(BubbleIcon icon)
    {
        gameObject.SetActive(true);
        if (progressBarController != null)
        {
            progressBarController.HideProgressBar();
        }
        ShowIcon(icon);
    }

    public void HideSpeechBubble()
    {
        timer = 0;         // reset so the next customer starts clean
        clicked = false;
        gameObject.SetActive(false);
    }

    // Waits until the speech bubble is clicked or the time runs out
    public IEnumerator ShowSpeechBubbleWithIconAndWait(BubbleIcon icon, int waitTime, System.Action<bool> clickedCallback)
    {
        timer = 0;
        clicked = false;
        ShowSpeechBubble(icon);
        progressBarController.StartProgressBar(waitTime);
        yield return new WaitUntil(() => clicked || timer >= waitTime);
        if (progressBarController != null)
        {
            progressBarController.HideProgressBar();
        }
        clickedCallback(clicked);
    }

    public IEnumerator ShowSpeechBubbleWithIconAndWaitForCondition(BubbleIcon icon, int waitTime, System.Func<bool> predicate)
    {
        waitingForCondition = true;
        timer = 0;
        ShowSpeechBubble(icon);
        progressBarController.StartProgressBar(waitTime);
        yield return new WaitUntil(() => predicate() || timer >= waitTime);
        if (progressBarController != null)
        {
            progressBarController.HideProgressBar();
        }
        waitingForCondition = false;
    }

    public void SetTimeFloor(int _timeFloor)
    {
        timeFloor = _timeFloor;
    }

    public IEnumerator ShowSpeechBubbleWithSpriteAndWaitForCondition(Sprite sprite, int waitTime, System.Func<bool> predicate)
    {   
        waitingForCondition = true;
        timer = 0;
        ShowSpeechBubbleWithSprite(sprite);
        progressBarController.StartProgressBar(waitTime);
        yield return new WaitUntil(() => predicate() || timer >= waitTime);
        if (progressBarController != null)
        {
            progressBarController.HideProgressBar();
        }
        waitingForCondition = false;
        HideSpeechBubble();
    }

    public void ShowSpeechBubbleWithSprite(Sprite sprite)
    {
        gameObject.SetActive(true);
        HideAllIcons();
        replacementSprite.sprite = sprite;
    }
}
