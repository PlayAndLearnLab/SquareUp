using UnityEngine;

[CreateAssetMenu(fileName = "New Feature", menuName = "Feature")]

// A feature is a single property of a customer that a player must measure to predict their order. E.g. these are the inputs to the prediction model that the player must train.
public class Feature : ScriptableObject
{
    public string label;
    public FeatureExpression[] expressions;
    public Sprite icon;
}
