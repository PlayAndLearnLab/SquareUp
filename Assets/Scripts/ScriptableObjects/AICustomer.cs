using System;
using UnityEngine;

public enum AICustomerType { Patient, Impatient, Grumpy }

[CreateAssetMenu(fileName = "New Customer", menuName = "CoffeeGame/AICustomer")]

public class AICustomer : ScriptableObject
{
    [Header("Identity")]
    public string customerName;
    public GameObject customerPrefab;
    public int moneyReward = 15;
    public AICustomerType customerType;

    public TempPreference favoriteTemp; // The AI uses this to "nudge" its prediction
    
    [Header("AI Context")]
    [TextArea(2, 5)]
    public string orderDialogue;
    public AudioClip orderAudio;
    [Range(0, 1)]
    public float aiConfidence = 0.5f; // How well the AI "knows" this person (0 = stranger, 1 = regular)

    [Header("Ground Truth (The Hidden Correct Answer)")]
    public CoffeeFlavor expectedFlavor;
    public TempPreference expectedTemp;

    public static implicit operator AICustomer(GameObject v)
    {
        throw new NotImplementedException();
    }
}
