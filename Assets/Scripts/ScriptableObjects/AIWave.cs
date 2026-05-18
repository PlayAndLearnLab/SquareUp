using UnityEngine;

[CreateAssetMenu(fileName = "New AIWave", menuName = "AIWave")]
public class AIWave : ScriptableObject
{
    public AICustomer[] customers;
    public int timeBetweenCustomers;
    public int intervalRandomness;
}
