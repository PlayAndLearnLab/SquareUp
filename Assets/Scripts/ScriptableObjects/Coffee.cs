using UnityEngine;

public enum CoffeeFlavor
{
    Candy,
    Apple,
    Pumpkin,
    Poison
}

public class Coffee
{
    public CoffeeFlavor flavor;
    [Range(0f, 100f)]
    public float temperature;
    public Sprite coffeeSprite;

    public Coffee(CoffeeFlavor flavor, float temperature)
    {
        this.flavor = flavor;
        this.temperature = temperature;
    }
} 