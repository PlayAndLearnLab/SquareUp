using UnityEngine;

[CreateAssetMenu(fileName = "New Order", menuName = "Order")]
public class Order : ScriptableObject
{
    public AudioClip orderSound;
    public string orderText;
    public CoffeeFlavor coffeeType;
}
