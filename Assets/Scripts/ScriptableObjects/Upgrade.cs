using UnityEngine;


public enum UpgradeType
{
    OrderStation,
    VerificationStation,
}

[CreateAssetMenu(fileName = "New Upgrade", menuName = "Upgrade")]
public class Upgrade : ScriptableObject
{
    public UpgradeType upgradeType;
    public int cost;
    public int level;
    public string title;
    public string description;
    public Sprite icon;
    public Quiz quiz;
}
