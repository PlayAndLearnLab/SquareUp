using UnityEngine;

public enum ReactionType
{
    Happy,
    Neutral,
    Angry
}

[CreateAssetMenu(fileName = "New Reaction", menuName = "Reaction")]
public class Reaction : ScriptableObject
{
    public AudioClip reactionSound;
    public ReactionType reactionType;
}
