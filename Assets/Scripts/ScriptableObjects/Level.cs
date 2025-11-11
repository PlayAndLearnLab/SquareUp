using UnityEngine;

[CreateAssetMenu(fileName = "New Level", menuName = "Level")]
public class Level : ScriptableObject
{
    public int trainingTime;
    public Wave trainingWave;
    public Wave testWave;
}
