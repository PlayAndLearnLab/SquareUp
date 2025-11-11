using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class CustomerController : MovementController
{
    public GameObject coffeeObject;
    private SpeechBubbleController speechBubbleController;
    private GameObject speechBubble;

    public Reaction[] reactions;

    public AudioClip processingSound;

    // private int waitTime = 120;
    private static float SPEED_NORMAL = 10f;

    private AudioSource audioSource;
    public AudioClip deathSound;
    public AudioClip happySound;

    private Animator animator;

    private ReactionType emotion;



    // Start is called before the first frame update
    new void Awake()
    {
        base.Awake();
        coffeeObject.SetActive(false);
        speechBubble = objTransform.GetChild(0).gameObject;
        speechBubble.SetActive(false);

        speechBubbleController = speechBubble.GetComponent<SpeechBubbleController>();
        audioSource = GetComponent<AudioSource>();
    }

    public void ShowCoffee()
    {
        coffeeObject.SetActive(true);
    }

    public void HideCoffee()
    {
        coffeeObject.SetActive(false);
    }

    public void SetAnimator(Animator animator)
    {
        this.animator = animator;
    }

    public IEnumerator WaitForClickWithIcon(SpeechBubbleController.BubbleIcon icon, int waitTime, System.Action<bool> clickedCallback)
    {
        bool wasClicked = false;
        yield return speechBubbleController.ShowSpeechBubbleWithIconAndWait(icon, waitTime, (bool clicked) =>
        {
            wasClicked = clicked;
        });
        clickedCallback(wasClicked);
    }

    public IEnumerator WaitForConditionWithIcon(SpeechBubbleController.BubbleIcon icon, int waitTime, System.Func<bool> predicate)
    {
        yield return speechBubbleController.ShowSpeechBubbleWithIconAndWaitForCondition(icon, waitTime, predicate);
    }
    public IEnumerator WaitForConditionWithSprite(Sprite sprite, int waitTime, System.Func<bool> predicate)
    {
        yield return speechBubbleController.ShowSpeechBubbleWithSpriteAndWaitForCondition(sprite, waitTime, predicate);
    }
    public void ShowBubbleWithSprite(Sprite sprite)
    {
        speechBubbleController.ShowSpeechBubbleWithSprite(sprite);
    }

    public void SetTimeFloor(int timeFloor)
    {
        speechBubbleController.SetTimeFloor(timeFloor);
    }

    public void ShowIcon(SpeechBubbleController.BubbleIcon icon)
    {
        speechBubbleController.ShowSpeechBubble(icon);
    }

    public void HideSpeechBubble()
    {
        speechBubbleController.HideSpeechBubble();
    }

    public bool IsHappy()
    {
        return emotion == ReactionType.Happy;
    }

    public void PlayProcessingSound()
    {
        audioSource.clip = processingSound;
        audioSource.Play();
    }

    public IEnumerator ExpressFeature(Feature feature)
    {
        // get random expression from feature
        FeatureExpression expression = feature.expressions[Random.Range(0, feature.expressions.Length)];
        audioSource.clip = expression.audioExpression;
        audioSource.Play();
        yield return new WaitForSeconds(expression.audioExpression.length);
    }

    public void DisplayOrder(CoffeeFlavor type)
    {
        HideCoffee();

    }

    private void SetWalking(bool walking, float speedMultiplier = 1f)
    {
        if (animator == null) return;

        // Debug.Log("Setting walking to " + walking + " with speed " + speedMultiplier);
        animator.SetBool("1_Move", walking);
        animator.speed = speedMultiplier;  // This controls the animation speed
    }

    private void PointToDirection(Vector3 pathVector)
    {
        if (gameObject == null) return;

        Vector3 direction = pathVector.normalized;
        gameObject.transform.localScale = new Vector3(direction.x / Mathf.Abs(direction.x), 1, 1);
    }

    public override IEnumerator Walk(Vector3 pathVector, int speed)
    {
        float animSpeedMultiplier = speed / SPEED_NORMAL;  // Use SPEED_NORMAL instead of hardcoded value
        SetWalking(true, animSpeedMultiplier);
        PointToDirection(pathVector);
        yield return base.Walk(pathVector, speed);
        SetWalking(false);
    }

    public override IEnumerator WalkToInSecs(Vector3 targetPos, float seconds)
    {
        float distance = Vector3.Distance(targetPos, gameObject.transform.position);
        float speedMultiplier = distance / (SPEED_NORMAL * seconds);  // Use SPEED_NORMAL instead of hardcoded value
        SetWalking(true, speedMultiplier);
        PointToDirection(targetPos - gameObject.transform.position);
        yield return base.WalkToInSecs(targetPos, seconds);
        SetWalking(false);
    }

    public override IEnumerator WalkTo(Vector3 targetPos, int speed)
    {
        float animSpeedMultiplier = speed / SPEED_NORMAL;  // Use SPEED_NORMAL instead of hardcoded value
        SetWalking(true, animSpeedMultiplier);
        PointToDirection(targetPos - gameObject.transform.position);
        yield return base.WalkTo(targetPos, speed);
        SetWalking(false);
    }

    public IEnumerator PlayClip(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
        yield return new WaitForSeconds(clip.length);
    }

    public IEnumerator Die()
    {
        SetWalking(false);

        if (animator != null)
        {
            animator.SetBool("4_Death", true);
            animator.SetBool("isDeath", true);
        }

        if (audioSource != null && deathSound != null)
        {
            yield return PlayClip(deathSound);
        }

        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    public IEnumerator Happy()
    {
        yield return PlayClip(happySound);
    }

}
