using UnityEngine;

[CreateAssetMenu(fileName = "New Question", menuName = "Question")]
public class Question : ScriptableObject
{
    public string question;
    public string[] answers;
    public string answerExplanation;
    public int correctAnswer;

    // Add validation to ensure correctAnswer is within bounds
    private void OnValidate()
    {
        if (answers != null && correctAnswer >= answers.Length)
        {
            correctAnswer = answers.Length - 1;
        }
    }
}