using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class MachineConfigController : MonoBehaviour
{
    // Create public variables for accuracy and speed ui sliders
    public Slider accuracySlider;
    public Slider speedSlider;

    // Create private variables for accuracy and speed
    private float accuracy = 1;
    private float speed = 0;

    void Start()
    {
        accuracy = accuracySlider.value;
        speed = 1 - accuracy;
    }

    void Update()
    {
        // Check if the speed slider has changed
        if (speedSlider.value != speed)
        {
            // Update the speed value
            speed = speedSlider.value;
            // Update the accuracy value
            accuracy = 1 - speed;
            accuracySlider.value = accuracy;
        }
        // Check if the accuracy slider has changed
        if (accuracySlider.value != accuracy)
        {
            // Update the accuracy value
            accuracy = accuracySlider.value;
            // Update the speed value
            speed = 1 - accuracy;
            speedSlider.value = speed;
        }
        // Debug.Log("Accuracy: " + accuracy);
        // Debug.Log("Speed: " + speed);
    }

    public float GetAccuracy()
    {
        return accuracy;
    }
    // map to 1x to 10x
    public float GetSpeed()
    {
        return speed * 9 + 1;
    }

    public void SetAccuracy(float accuracy)
    {
        this.accuracy = accuracy;
    }

    public void SetSpeed(float speed)
    {
        this.speed = speed;
    }

}
