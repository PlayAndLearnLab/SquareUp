using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MoneyAnimation : MonoBehaviour
{
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private Color moneyColor = Color.green;
    [SerializeField] private float floatHeight = 50f;  // How high it floats up
    [SerializeField] private AudioClip chaChingSound;
    
    private float startY;
    private float elapsedTime = 0f;
    private CanvasGroup canvasGroup;
    private AudioSource audioSource;

    private void Awake()
    {
        startY = transform.position.y;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Add and set up audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        if (chaChingSound != null)
        {
            audioSource.clip = chaChingSound;
            audioSource.Play();
        }
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        
        // Float upward with easing
        float progress = elapsedTime / lifetime;
        float easedProgress = 1f - Mathf.Pow(1f - progress, 2f); // Quadratic ease-out
        float newY = startY + (floatHeight * easedProgress);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        
        // Fade out
        float alpha = 1f - progress;
        canvasGroup.alpha = alpha;
        
        // Destroy when lifetime is up
        if (elapsedTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    public void SetAmount(int amount)
    {
        if (moneyText != null)
        {
            moneyText.text = $"+${amount}";
            moneyText.color = moneyColor;
        }
    }
} 