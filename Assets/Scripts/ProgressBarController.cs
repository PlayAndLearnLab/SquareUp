using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProgressBarController : MonoBehaviour
{
    private SpriteRenderer progressBarSprite;
    private Transform progressBarTranform;
    private float time = 0;
    public bool waitDone = false;
    public float duration = 0;
    private void Awake()
    {
        gameObject.SetActive(true);

        progressBarSprite = gameObject.GetComponent<SpriteRenderer>();
        progressBarTranform = gameObject.GetComponent<Transform>();

        progressBarSprite.color = Color.green;
    }

    // Calculate the gradient color of the progress bar between good color and bad color based on the progress
    private Color ColorProgressMap(float progress)
    {
        float r = 0;
        float g = 0;
        float b = 0;
        if (progress < 0.5)
        {
            r = 255;
            g = 255 * progress * 2;
        }
        else
        {
            r = 255 * (1 - progress) * 2;
            g = 255;
        }
        return new Color(r / 255, g / 255, b / 255);

    }

    public void UpdateProgressBar(float time)
    {
        if (duration <= 0)
        {
            duration = 1.0f; // Fallback to a tiny duration so we don't break the math
        }

        //if (gameObject.activeSelf && time > 0.001f)
        if (gameObject.activeSelf)
        {
            // 'time' is the elapsed time (e.g. 1.5 seconds)
            // 'duration' is the total time (e.g. 3.0 seconds)
            float progressFraction = Mathf.Clamp01(time / duration);

            // 0.0 to 1.0 scale
            float xScale = 1 - progressFraction;

            progressBarTranform.localScale = new Vector2(xScale, 0.74f);
            progressBarSprite.color = ColorProgressMap(xScale);


            //float progress = Mathf.Min(time, duration);

            //float xScale = progress / duration;
            //xScale = Mathf.Clamp01(xScale);

            //progressBarTranform.localScale = new Vector2(1 - xScale, 0.74f);

            //progressBarSprite.color = ColorProgressMap(1 - xScale);
        }
    }

    private void ShowProgressBar()
    {
        gameObject.SetActive(true);
    }

    public void StartProgressBar(int _duration)
    {
        duration = _duration;
        ShowProgressBar();
    }

    public void HideProgressBar()
    {
        gameObject.SetActive(false);
    }
}
