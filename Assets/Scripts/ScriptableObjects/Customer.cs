using UnityEngine;

public enum CustomerType
{
    Patient,
    Normal,
    Impatient,
}

[CreateAssetMenu(fileName = "New Customer", menuName = "Customer")]
public class Customer : ScriptableObject
{
    public CustomerType customerType;
    public CoffeeFlavor expectedFlavor;
    public Sprite expectedFlavorSprite;
    public AudioClip expectedFlavorAudio;
    public GameObject customerPrefab;
    public int moneyReward = 15; // Default reward amount
}
