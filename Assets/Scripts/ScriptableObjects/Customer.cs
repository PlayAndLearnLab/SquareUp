using System.Collections.Generic;
using UnityEngine;

// 1. Keep your original Gameplay Enums
public enum CustomerType { Patient, Normal, Impatient }

[CreateAssetMenu(fileName = "New Customer", menuName = "Customer")]
public class Customer : ScriptableObject
{
    [Header("Gameplay Settings")]
    public string customerName;
    public CustomerType customerType; // Patient/Impatient logic goes here
    public GameObject customerPrefab;
    public int moneyReward = 15;

    [Header("AI Goal (The 'Truth')")]
    // This is what the customer ACTUALLY wants. 
    // The AI tries to guess this, and the Player tries to ensure it's right.
    public CoffeeFlavor expectedFlavor;
    public TempPreference favoriteTemp;
    public List<CoffeeFlavor> dislikedFlavors;

    [Header("Machine Memory (The 'Learning')")]
    // This data persists and grows as you play
    public int successfulServings = 0;
    [Range(0, 1)] public float aiConfidence = 0.1f;

    [Header("Visuals/Audio")]
    public Sprite expectedFlavorSprite;
    public AudioClip expectedFlavorAudio;

    // Helper method to "Train" this specific customer's profile
    public void RecordFeedback(bool wasCorrect)
    {
        if (wasCorrect)
        {
            successfulServings++;
            aiConfidence = Mathf.Clamp01(aiConfidence + 0.1f);
        }
        else
        {
            aiConfidence = Mathf.Clamp01(aiConfidence - 0.05f);
        }
    }
}