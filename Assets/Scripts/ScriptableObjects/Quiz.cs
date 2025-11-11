using UnityEngine;

[CreateAssetMenu(fileName="New Quiz", menuName="Quiz")]
public class Quiz : ScriptableObject
{
    public string quizName;
    public int score;
    public int currentQuestion;
    public int passMark;
    public Question[] questions;

    public string learningVideoLink;
    
    // Slideshow to display before starting this quiz
    public IntroSlidesData introSlides;
}