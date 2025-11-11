using UnityEngine;

[CreateAssetMenu(fileName = "New Wave", menuName = "Wave")]
public class Wave : ScriptableObject
{
    public Customer[] customers;
    public int timeBetweenCustomers;
    public int intervalRandomness;
}
