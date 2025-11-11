using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizController : MonoBehaviour
{
    [Header("Text Elements")]
    public TextMeshProUGUI titleText;      // Reference to Text (TMP) under Title
    public TextMeshProUGUI questionText;   // Reference to Text (TMP) under Question
    public TextMeshProUGUI scoreText;      // For the score display

    [Header("Button Elements")]
    public Button actionButton;            // Will serve as start/continue/submit button
    public TextMeshProUGUI actionButtonText; // Text component of the action button
    
    [Header("Answer Setup")]
    public GameObject answerButtonPrefab;   // Prefab for answer buttons
    public Transform itemPanelTransform;    // Reference to Item Panel for spawning answers
    [SerializeField] private Vector2 gridSpacing = new Vector2(250f, 110f);  // Spacing between buttons
    [SerializeField] private Vector2 gridCellSize = new Vector2(400f, 150f); // Size of each button cell
    [SerializeField] private int gridPadding = 40; // Padding around the grid
    private List<GameObject> currentAnswerButtons = new List<GameObject>();

    public GameObject itemPanel;
    private int playerAnswer = -1;
    private bool playerContinued = false;

    [Header("UI Elements")]

    public TextMeshProUGUI progressText;
    private int currentQuestionIndex = 0;

    [Header("Feedback")]
    public GameObject feedbackPanel;  // Reference to a dedicated feedback panel
    public TextMeshProUGUI feedbackText;  // Reference to feedback text component
    public TextMeshProUGUI explanationText;  // Reference to explanation text component
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public Color explanationColor = Color.white;

    [Header("Quiz Data")]
    public Quiz quiz; // Reference to the current quiz

    private void SetupGridLayout()
    {
        // Get or add GridLayoutGroup component
        GridLayoutGroup gridLayout = itemPanelTransform.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = itemPanelTransform.gameObject.AddComponent<GridLayoutGroup>();
        }

        // Configure the grid layout for 2x2
        gridLayout.padding = new RectOffset(gridPadding, gridPadding, gridPadding, gridPadding);
        gridLayout.cellSize = gridCellSize;
        gridLayout.spacing = gridSpacing;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 2; // Two columns for 2x2 layout
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        // Force layout rebuild
        LayoutRebuilder.ForceRebuildLayoutImmediate(itemPanelTransform.GetComponent<RectTransform>());
    }

    private void CreateAnswerButtons(Question question)
    {
        ClearAnswerButtons();
        
        // Ensure grid layout is set up
        SetupGridLayout();
        
        // Create buttons
        for (int i = 0; i < question.answers.Length; i++)
        {
            GameObject buttonObj = Instantiate(answerButtonPrefab, itemPanelTransform);
            currentAnswerButtons.Add(buttonObj);
            
            // Set up text
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = question.answers[i];
                buttonText.fontSize = 24;
                buttonText.alignment = TextAlignmentOptions.Center;
            }
            
            // Set up button click handler
            int index = i;
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => SetAnswerIndex(index));
            }
        }
    }

    private void Awake()
    {
        Debug.Log("QuizController: Awake called");
        // Clear any existing buttons in item panel
        ClearAnswerButtons();
        
        // Hide UI elements but keep the component enabled
        if (itemPanel != null) itemPanel.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        if (actionButton != null) actionButton.gameObject.SetActive(false);

        // Keep listening for events
        if (EventManager.current != null)
        {
            Debug.Log("QuizController: Subscribing to onStartQuiz event in Awake");
            EventManager.current.onStartQuiz += HandleStartQuiz;
        }
        gameObject.SetActive(false);
    }

    private void UpdateUIState(bool inQuiz)
    {
        // Don't manage itemPanel visibility here anymore, it's handled in specific states
        
        // Update action button for quiz start/end
        actionButtonText.text = inQuiz ? "Continue" : "Start Quiz";
        actionButton.gameObject.SetActive(!inQuiz); // Show button when not in quiz
    }

    public void OnActionButtonClicked()
    {
        if (actionButtonText.text == "Start Quiz")
        {
            StartQuiz(); // Start the quiz
        }
        else if (actionButtonText.text == "Continue" || actionButtonText.text == "End")
        {
            playerContinued = true; // Signal to continue to next question or end quiz
        }
    }

    private void incrementScore(Quiz quiz)
    {
        quiz.score++;
        scoreText.text = "Score: " + quiz.score;
    }

    private void SetAnswerIndex(int answerIndex)
    {
        playerAnswer = answerIndex;
    }

    private IEnumerator ShowQuestionFeedback(Question question, bool isCorrect)
    {
        // Update action button for "Continue"
        actionButtonText.text = "Continue";
        actionButton.gameObject.SetActive(true);
        
        // Configure feedback text
        feedbackText.text = isCorrect ? "Correct!" : "Wrong!";
        feedbackText.color = isCorrect ? correctColor : wrongColor;
        
        // Configure explanation text
        explanationText.text = question.answerExplanation;
        explanationText.color = explanationColor;
        
        // Show the feedback panel
        feedbackPanel.SetActive(true);
        
        // Wait for player to continue
        playerContinued = false;
        yield return new WaitUntil(() => playerContinued);
        
        // Hide the feedback panel
        feedbackPanel.SetActive(false);
        
        actionButton.gameObject.SetActive(false);
    }

    private IEnumerator AnimateFeedbackText(GameObject feedbackObj)
    {
        if (feedbackObj == null) yield break;

        RectTransform rectTransform = feedbackObj.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = feedbackObj.GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
            canvasGroup = feedbackObj.AddComponent<CanvasGroup>();

        // Pop in
        float startScale = 0.5f;
        float targetScale = 1f;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float currentScale = Mathf.Lerp(startScale, targetScale, progress);
            rectTransform.localScale = Vector3.one * currentScale;
            yield return null;
        }
    }

    private IEnumerator AnimateExplanationText(GameObject explanationObj)
    {
        if (explanationObj == null) yield break;

        // Wait a bit for the feedback to show first
        yield return new WaitForSeconds(0.3f);

        RectTransform rectTransform = explanationObj.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = explanationObj.GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
            canvasGroup = explanationObj.AddComponent<CanvasGroup>();

        // Fade in
        float duration = 0.3f;
        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            canvasGroup.alpha = progress;
            yield return null;
        }

        // Wait for continue button
        yield return new WaitUntil(() => playerContinued);

        // Fade out
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            canvasGroup.alpha = 1 - progress;
            yield return null;
        }
    }

    private IEnumerator ShowQuizFeedback(bool passed)
    {
        // Configure feedback text
        feedbackText.text = passed ? "You Passed!" : "You Failed!";
        feedbackText.color = passed ? correctColor : wrongColor;
        explanationText.text = ""; // Clear explanation for final feedback
        
        // Show the feedback panel
        feedbackPanel.SetActive(true);
        
        yield return new WaitForSeconds(2f);
        
        // Hide the feedback panel
        feedbackPanel.SetActive(false);
    }

    private void ClearAnswerButtons()
    {
        foreach (var button in currentAnswerButtons)
        {
            Destroy(button);
        }
        currentAnswerButtons.Clear();
    }

    private IEnumerator AskQuestion(Quiz quiz, Question question)
    {
        // Hide feedback panel at start of new question
        feedbackPanel.SetActive(false);
        
        questionText.text = question.question;
        CreateAnswerButtons(question);
        itemPanel.SetActive(true);
        
        yield return new WaitUntil(() => playerAnswer != -1);
        
        bool isCorrect = question.correctAnswer == playerAnswer;
        playerAnswer = -1;
        
        // Hide answer buttons
        foreach (var button in currentAnswerButtons)
        {
            button.SetActive(false);
        }
        
        yield return ShowQuestionFeedback(question, isCorrect);
        
        // Clear the buttons after feedback is done
        ClearAnswerButtons();
        
        if (isCorrect)
        {
            incrementScore(quiz);
        }
    }

    private void UpdateProgress(Quiz quiz)
    {
        progressText.text = $"{currentQuestionIndex + 1}/{quiz.questions.Length}";
        scoreText.text = $"{quiz.score}/{quiz.questions.Length}";
    }

    public void StartQuiz()
    {
        // Method to be called when starting the quiz
        StartCoroutine(GiveQuiz());
    }

    // Add event handling methods
    private void OnEnable()
    {
        Debug.Log("QuizController: OnEnable called");
        if (EventManager.current != null)
        {
            Debug.Log("QuizController: Subscribing to onStartQuiz event");
            EventManager.current.onStartQuiz += HandleStartQuiz;
        }
        else
        {
            Debug.LogWarning("QuizController: EventManager.current is null!");
        }
    }

    private void OnDisable()
    {
        // if (EventManager.current != null)
        // {
        //     EventManager.current.onStartQuiz -= HandleStartQuiz;
        // }
    }

    private void HandleStartQuiz(Quiz quizToStart)
    {
        Debug.Log("Starting quiz");
        // Make sure our UI elements are active when starting a quiz
        gameObject.SetActive(true);
        if (itemPanel != null) itemPanel.SetActive(true);
        quiz = quizToStart;
        StartCoroutine(GiveQuiz());
    }

    public IEnumerator GiveQuiz()
    {
        if (quiz == null) yield break;

        UpdateUIState(true);
        quiz.score = 0;
        currentQuestionIndex = 0;

        if (titleText != null)
            titleText.text = quiz.quizName;
        
        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnActionButtonClicked);
        
        // Ensure itemPanel is active at start
        itemPanel.SetActive(true);
        
        for (int i = 0; i < quiz.questions.Length; i++)
        {
            currentQuestionIndex = i;
            UpdateProgress(quiz);
            yield return AskQuestion(quiz, quiz.questions[i]);
        }
        
        // Show final results
        bool passed = quiz.score >= quiz.passMark;
        
        // Format the feedback text
        string youText = passed ? "You\nPassed!" : "You\nFailed!";
        feedbackText.text = youText;
        feedbackText.color = passed ? correctColor : wrongColor;
        explanationText.text = $"Final Score: {quiz.score}/{quiz.questions.Length}\nRequired to Pass: {quiz.passMark}"; 
        feedbackPanel.SetActive(true);
        
        // Change to End button
        actionButtonText.text = "End";
        actionButton.gameObject.SetActive(true);
        
        // Wait for player to end
        playerContinued = false;
        yield return new WaitUntil(() => playerContinued);
        Debug.Log("QuizController: Quiz completed");
        
        // Clean up
        feedbackPanel.SetActive(false);
        actionButton.gameObject.SetActive(false);
        itemPanel.SetActive(false);
        
        // UpdateUIState(false);

        // Trigger completion event
        EventManager.current.QuizCompleted(quiz, passed);
        gameObject.SetActive(false);
    }
}
