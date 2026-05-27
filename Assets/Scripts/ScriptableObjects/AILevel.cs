using UnityEngine;

[CreateAssetMenu(fileName = "New AILevel", menuName = "AILevel")]
public class AILevel : ScriptableObject
{
    public int trainingTime;
    public AIWave trainingWave;
    public AIWave testWave;
    public ShopUpgrade[] levelUpgrades;
}
