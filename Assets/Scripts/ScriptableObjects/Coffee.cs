using UnityEngine;

public enum CoffeeFlavor
{
    CandyLatte,  // Caffeine, Sweet
    HerbalTea,   // No Caffeine, Earthy       
    PumpkinTea,  // No Caffeine, Sweet
    AppleMatcha, // Caffeine, Neutral/Earthy        
    Chocolate,   // No Caffeine, Neutral/Other
    Coffee,       // Caffeine, Earthy
    Candy,
    Apple,
    Pumpkin,
    Poison,
    HotApple,
    ColdApple,
    HotCandy,
    ColdCandy,
    HotPumpkin,
    ColdPumpkin
}

public class Coffee
{
    public CoffeeFlavor flavor;
    public float temperature;
    public float confidence;
    public Sprite coffeeSprite;
    public Coffee(CoffeeFlavor flavor, float temperature, float confidence = 0.5f)
    {
        this.flavor = flavor;
        this.temperature = temperature;
        this.confidence = confidence;
    }
}

//public class Coffee
//{
//    public CoffeeFlavor flavor;
//    [Range(0f, 100f)]
//    public float temperature;
//    public Sprite coffeeSprite;

//    public Coffee(CoffeeFlavor flavor, float temperature)
//    {
//        this.flavor = flavor;
//        this.temperature = temperature;
//    }
//} 