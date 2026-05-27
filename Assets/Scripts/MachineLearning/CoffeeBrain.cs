using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using TMPro;

public enum CaffeineLevel { None, Low, Medium, High }
public enum TempPreference { Hot, Cold }

[Serializable]
public class KeywordData
{
    public string keyword;
    public float suggestedCaffeine; // 0=None, 1=Yes
    public float suggestedTemp;     // 0=Hot, 1=Cold
    public float suggestedProfile;  // 0=Earthy/Bold, 1=Sweet/Fruity
    public float weight = 1.0f;
    public bool onlyUpdatesTemp = false;
}

[Serializable]
public class BrainWrapper { public List<KeywordData> keywords; }





public class CoffeeBrain : MonoBehaviour
{ 

    [Header("References")]
    public ProgressBarController progressBar;
    public GameObject aiThoughts;
    public TMP_Text uiThoughtText;
    public TMP_Text uiTranscript;
    public CoffeeDistributorController distributor;

    [Header("AI Brain Settings")]
    public List<KeywordData> brainDatabase = new List<KeywordData>();
    public string testSentence;
    public bool triggerTest;
    public AICustomer testCustomer;

    private Coroutine thinkingRoutine;
    private bool isOverrideActive = false;

    private Dictionary<string, float> liveWeights = new Dictionary<string, float>();
    private const float LEARNING_RATE = 0.2f; // How much weights increase per success

    [Header("State")]
    private Coffee pendingPrediction;
    private string lastDialogue;
    private bool isProcessingAI = false;

    [Header("Analytics")]
    public NewBrainAnalytics analytics;

    [Header("Daily Performance Tracking")]
    public int dailySuccesses = 0;
    public int dailyMisses = 0;

    public bool IsBusy => isProcessingAI;

    void Awake()
    {
        Init();
    }

    private void Update()
    {
        if (triggerTest) { StartAILogic(testSentence, testCustomer); triggerTest = false; };
    }

    public void Init()
    {
        //Debug.Log("Init()");
        aiThoughts.SetActive(false);

        GM gameManager = FindObjectOfType<GM>();
        int currentDay = (gameManager != null) ? gameManager.day : 0;

        //brainDatabase.Clear();
        // Only load if the database is empty or we're forcing a refresh
        //Debug.Log(brainDatabase.Count == 0);

        LoadDatabase();

        //if (brainDatabase == null || brainDatabase.Count == 0)
        //{
        //    //Debug.Log("loading satabse from Init()");
        //    LoadDatabase();
        //}

        analytics.CaptureStartOfDay(brainDatabase);

        dailySuccesses = 0;
        dailyMisses = 0;

        if (currentDay == 0)
        {
            //Debug.Log("Day 0: Resetting AI Brain weights to 0.1");
            foreach (var data in brainDatabase)
            {
                data.weight = 0.1f; // Force start at 0.1 as requested
            }
            SaveLearnedWeights(); // Save this clean state
        }
        else if (PlayerPrefs.HasKey("HasSavedBrain"))
        {
            LoadLearnedWeights();
        }

        EventManager.current.onDayCompleted += () => {
            isProcessingAI = false;
            if (thinkingRoutine != null) StopCoroutine(thinkingRoutine);
        };
    }




    public void old_ReinforceLearning(string dialogue)
    {
        string lowerDialogue = dialogue.ToLower();
        foreach (var data in brainDatabase)
        {
            if (lowerDialogue.Contains(data.keyword.ToLower()))
            {
                // Increase the weight of the keyword used
                data.weight += LEARNING_RATE;
                //Debug.Log($"AI Learned! {data.keyword} weight is now {data.weight}");
            }
        }
        SaveLearnedWeights();
        
    }

    public void ReinforceLearning(string dialogue)
    {
        string lowerDialogue = dialogue.ToLower();
        GM gameManager = FindObjectOfType<GM>();
        int currentDay = (gameManager != null) ? gameManager.day : 1;

        foreach (var data in brainDatabase)
        {
            if (lowerDialogue.Contains(data.keyword.ToLower()))
            {
                // SCALE: Learning is faster in early days, more precise in later days
                float dynamicRate = LEARNING_RATE / (1f + (currentDay * 0.1f));
                //data.weight += dynamicRate;
                data.weight = Mathf.Min(data.weight + dynamicRate, 3.0f);
            }
        }
        SaveLearnedWeights();
    }


    private void SaveLearnedWeights()
    {
        // For a research prototype, we can serialize the current database back to string
        string json = JsonUtility.ToJson(new BrainWrapper { keywords = brainDatabase });
        PlayerPrefs.SetString("SavedBrainData", json);
        PlayerPrefs.SetInt("HasSavedBrain", 1);
        PlayerPrefs.Save();
    }

    private void LoadLearnedWeights()
    {
        string json = PlayerPrefs.GetString("SavedBrainData");
        BrainWrapper wrapper = JsonUtility.FromJson<BrainWrapper>(json);
        brainDatabase = wrapper.keywords;
    }

    public void ResetBrainForNewSession()
    {
        PlayerPrefs.DeleteKey("HasSavedBrain");
        PlayerPrefs.DeleteKey("SavedBrainData");
        Init(); // Reverts to base JSON
    }

    void LoadDatabase()
    {
        //Debug.Log("loading database start");
        string path = Path.Combine(Application.streamingAssetsPath, "BrainData.json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            BrainWrapper wrapper = JsonUtility.FromJson<BrainWrapper>(json);
            brainDatabase = wrapper.keywords;
            //Debug.Log($"Database Loaded. Entries found: {brainDatabase.Count}");
            if (brainDatabase.Count > 0)
                Debug.Log($"First Keyword: {brainDatabase[0].keyword}, Caffeine: {brainDatabase[0].suggestedCaffeine}");
        }
        else
        {
            Debug.LogWarning("BrainData.json not found at " + path);
        }
    }

    private IEnumerator WaitThenStart(string dialogue, AICustomer customer)
    {
        // If the AI is busy with the previous person, wait here!
        while (isProcessingAI)
        {
            yield return null; // Wait one frame and check again
        }

        // Now that it's false, we claim the lock and start
        isProcessingAI = true;
        isOverrideActive = false;
        DisplayAIThoughts(dialogue, customer);
        thinkingRoutine = StartCoroutine(ThinkAndPredict(dialogue, customer));
    }

    public void StartAILogic(string dialogue, AICustomer customer)
    {
        //if (isProcessingAI) return;

        //isProcessingAI = true;
        //isOverrideActive = false;
        //DisplayAIThoughts(dialogue, customer);
        //thinkingRoutine = StartCoroutine(ThinkAndPredict(dialogue, customer));
        StartCoroutine(WaitThenStart(dialogue, customer));
    }

    public void RequestOverride()
    {
        if (thinkingRoutine != null)
        {
            StopCoroutine(thinkingRoutine);
            isOverrideActive = true;

            // RESEARCH LOG: Track how much of the "thinking" time passed before override
            //Debug.Log("PLAYER_INTERVENTION: AI was interrupted.");

            if (progressBar != null)
                progressBar.UpdateProgressBar(0);
        }
    }

    public void ManualSelectFlavor(CoffeeFlavor manualFlavor, float manualTemp)
    {
        if (!isOverrideActive) return;

        Coffee playerChoice = new Coffee(manualFlavor, manualTemp);

        UpdateBrainFromResult(lastDialogue, playerChoice, true);

        //ExecuteBrewing(playerChoice);
        isOverrideActive = false;

        // Hide the thinking thoughts since the human decided
        uiThoughtText.text = "MANUAL OVERRIDE COMPLETE. SYSTEM STANDING BY.";
    }

    public Coffee PredictCoffee(string dialogue, AICustomer profile)
    {
        float caffeineScore = 0;
        float tempScore = 0;
        float flavorScore = 0;
        float totalWeightFound = 0;
        string lowerDialogue = dialogue.ToLower();

        foreach (var data in brainDatabase)
        {
            if (lowerDialogue.Contains(data.keyword.ToLower()))
            {
                float w = data.weight;
                totalWeightFound += w;

                // 1. Caffeine: Sum the raw values (-1 to 1)
                caffeineScore += (data.suggestedCaffeine * w);

                // 2. Temperature: Sum the raw values (0 to 1)
                tempScore += (data.suggestedTemp * w);

                // 3. Profile: Sum the raw values (0 to 1)
                flavorScore += (data.suggestedProfile * w);
                //totalWeightFound += data.weight;

            }
        }

        if (totalWeightFound > 0)
        {
            caffeineScore /= totalWeightFound;
            tempScore /= totalWeightFound;
            flavorScore /= totalWeightFound;
        }

        // Apply Customer Bias (Nudging the average)
        if (profile.favoriteTemp == TempPreference.Hot) tempScore -= 0.1f; // Nudge toward 0
        else tempScore += 0.1f; // Nudge toward 1


        float masteryThreshold = 5.0f;
        float certaintyThreshold = 2.0f;
        float baseConfidence = Mathf.Clamp01(totalWeightFound / masteryThreshold);
        float tempCertainty = Mathf.Clamp01(Mathf.Abs(tempScore) / certaintyThreshold);
        float finalConfidence = Mathf.Clamp(baseConfidence + (profile.aiConfidence * 0.2f), 0.05f, 1f);

        bool wantsCaffeine = caffeineScore >= 0; // Above 0 is caffeine
        bool isHot = tempScore <= 0.5f;          // Below 0.5 is Hot
        bool isSweet = flavorScore > 0.5f;

        CoffeeFlavor finalFlavor;

        // Binary Decision Matrix
        if (wantsCaffeine)
        {
            if (flavorScore > 0.6f) finalFlavor = CoffeeFlavor.CandyLatte;      // Clearly Sweet
            else if (flavorScore < 0.4f) finalFlavor = CoffeeFlavor.Coffee;     // Clearly Earthy
            else finalFlavor = CoffeeFlavor.AppleMatcha;                        // Middle
        }
        else
        {
            if (flavorScore > 0.6f) finalFlavor = CoffeeFlavor.PumpkinTea;     // Clearly Sweet
            else if (flavorScore < 0.4f) finalFlavor = CoffeeFlavor.HerbalTea;  // Clearly Earthy
            else finalFlavor = CoffeeFlavor.Chocolate;                          // Middle
        }

        //Debug.Log($"isHot = {isHot} and (isHot ? 0f : 1f) = {(isHot ? 0f : 1f)}");

        return new Coffee(finalFlavor, isHot ? 0f : 1f, finalConfidence);
    }

    

    public IEnumerator ThinkAndPredict(string dialogue, AICustomer customer)
    {
        lastDialogue = dialogue;
        pendingPrediction = PredictCoffee(dialogue, customer);

        Coffee prediction = PredictCoffee(dialogue, customer);

        float baseThinkTime = customer.customerType == AICustomerType.Impatient ? 1.5f : 3.0f;
        float minThinkTime = 1.2f;
        float actualThinkTime = baseThinkTime * (1.0f - customer.aiConfidence);

        //if (actualThinkTime <= 0) actualThinkTime = 0.1f;

        actualThinkTime = Mathf.Max(actualThinkTime, minThinkTime);

        if (progressBar != null)
        {
            float elapsed = 0;
            progressBar.duration = actualThinkTime;
            progressBar.gameObject.SetActive(true);
            while (elapsed < actualThinkTime)
            {
                elapsed += Time.deltaTime;
                //progressBar.UpdateProgressBar(elapsed / actualThinkTime);
                progressBar.UpdateProgressBar(elapsed);
                yield return null;
            }
            progressBar.HideProgressBar();
            aiThoughts.SetActive(true);
        }

        
    }

    public void AcceptAIPrediction()
    {
        aiThoughts.SetActive(false);

        dailySuccesses++;


        if (pendingPrediction != null)
        {
            ExecuteBrewing(pendingPrediction);
            //EventManager.current.RequestCoffeeApprovalUI(false);

            UpdateBrainFromResult(lastDialogue, pendingPrediction, false);
;
            // Optional: Reinforce learning on acceptance
            //ReinforceLearning(lastDialogue);
        }
    }

    public void DenyAIPrediction()
    {
        aiThoughts.SetActive(false);
        dailyMisses++;
        isOverrideActive = true;

        //if (pendingPrediction != null)
        //{
        //    //ExecuteBrewing(pendingPrediction);
        //    //EventManager.current.RequestCoffeeApprovalUI(false);

        //    UpdateBrainFromResult(lastDialogue, pendingPrediction, true);
            
        //    // Optional: Reinforce learning on acceptance
        //    //ReinforceLearning(lastDialogue);
        //}
        //EventManager.current.RequestCoffeeApprovalUI(false);
        uiThoughtText.text = "<color=orange>SYSTEM: WAITING FOR MANUAL OVERRIDE...</color>";
        isProcessingAI = false;
        //ReinforceLearning(lastDialogue);
    }

    private void ExecuteBrewing(Coffee coffee)
    {
        //Debug.Log($"AI Brewing: {coffee.flavor}");
        distributor.StartManualBrewNoPoison(coffee);
        isProcessingAI = false;
    }

    public void DisplayAIThoughts(string dialogue, AICustomer profile)
    {
        

        // Get the prediction first to see the confidence
        Coffee prediction = PredictCoffee(dialogue, profile);
        int confidencePercent = Mathf.RoundToInt(prediction.confidence * 100);

        
        //string tempStatus = prediction.temperature > 50 ? "HOT" : "COLD";
        string tempStatus = prediction.temperature < 0.5f ? "HOT" : "COLD";

        string displayDialogue = "";
        string displayString = "";
        //displayString += "<size=120%><color=#50C878><b>[SYSTEM ANALYSIS]</b></color></size>\n";
        displayDialogue += $"<b>INPUT:</b> \"{dialogue}\"\n";
        //displayString += "----------------------------\n";
        displayString += $"<b>AI PREDICTION:</b> {prediction.flavor} ({tempStatus})\n";
        //displayString += $"<b>CONFIDENCE:</b> {confidencePercent}%\n";



        //if (prediction.confidence < 0.4f)
        //displayString += "<color=red><b>! LOW CONFIDENCE DETECTED</b></color>";

        uiTranscript.text = displayDialogue;
        uiThoughtText.text = displayString;

        
    }

    public void ShowEndOfDayStats()
    {
        Debug.Log("Show Analytics");
        analytics.DisplayEndOfDay(brainDatabase, dailySuccesses, dailyMisses);
        dailySuccesses = 0;
        dailyMisses = 0;
    }

    // Call this after the player finishes a Manual Brew
    public void LearnFromCorrection(string dialogue, CoffeeFlavor actualFlavor, float actualTemp)
    {
        string lowerDialogue = dialogue.ToLower();
        foreach (var data in brainDatabase)
        {
            if (lowerDialogue.Contains(data.keyword.ToLower()))
            {
                // Adjust weights based on what the HUMAN chose
                // If the human chose a Sweet drink, increase the suggestedProfile weight for these keywords
                // This is where your research "weights" actually evolve!
                data.weight += LEARNING_RATE;
            }
        }
        SaveLearnedWeights();
    }

    public void UpdateBrainFromResult(string dialogue, Coffee servedCoffee, bool isCorrection = false)
    {
        string lowerDialogue = dialogue.ToLower();

        float effectiveLearningRate = isCorrection ? 0.8f : 0.2f;

        bool servedIsHot = servedCoffee.temperature < 0.5f;
        float servedProfile = GetFlavorProfileValue(servedCoffee.flavor);
        int servedCaffeine = IsFlavorCaffeinated(servedCoffee.flavor) ? 1 : -1;

        

        foreach (var data in brainDatabase)
        {
            if (lowerDialogue.Contains(data.keyword.ToLower()))
            {
                //data.weight = Mathf.Min(data.weight + 0.1f, 3.0f);

                if (isCorrection)
                {
                    data.weight = Mathf.Max(data.weight - 0.1f, 0.1f);
                }
                else
                {
                    // If right, we increase weight slowly to build confidence
                    data.weight = Mathf.Min(data.weight + 0.1f, 3.0f);
                }

                float targetTemp = servedIsHot ? 0f : 1f;
                data.suggestedTemp = Mathf.Lerp(data.suggestedTemp, targetTemp, effectiveLearningRate);

                if (!data.onlyUpdatesTemp)
                {
                    data.suggestedProfile = Mathf.Lerp(data.suggestedProfile, servedProfile, effectiveLearningRate);

                    //data.suggestedCaffeine = servedCaffeine;
                    data.suggestedCaffeine = Mathf.Lerp(data.suggestedCaffeine, servedCaffeine, effectiveLearningRate);
                }
                
              
            }
        }
        SaveLearnedWeights();
    }

    /// <summary>
    /// Nudges the player's currently active brain parameters closer to the perfect JSON values.
    /// </summary>
    /// <param name="boostPercentage">The percentage closer to perfect (e.g., 0.3f for 30%)</param>
    public void ApplyLearningBoost(float boostPercentage)
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Ideal_BrainData.json");

        if (!File.Exists(path))
        {
            Debug.LogError("Learning Boost failed: BrainData.json master file not found!");
            return;
        }

        // Load the clean, ideal configuration you just saved
        string json = File.ReadAllText(path);
        BrainWrapper masterWrapper = JsonUtility.FromJson<BrainWrapper>(json);
        List<KeywordData> idealDatabase = masterWrapper.keywords;

        // Loop through your live runtime database and nudge them toward the ideal baseline
        foreach (var liveData in brainDatabase)
        {
            // Find the matching keyword from the ideal dataset
            KeywordData idealData = idealDatabase.Find(x => x.keyword.Equals(liveData.keyword, StringComparison.OrdinalIgnoreCase));

            if (idealData != null)
            {
                // 1. Pull the prediction metrics closer to ideal targets
                liveData.suggestedCaffeine = Mathf.Lerp(liveData.suggestedCaffeine, idealData.suggestedCaffeine, boostPercentage);
                liveData.suggestedTemp = Mathf.Lerp(liveData.suggestedTemp, idealData.suggestedTemp, boostPercentage);
                liveData.suggestedProfile = Mathf.Lerp(liveData.suggestedProfile, idealData.suggestedProfile, boostPercentage);

                // 2. Safely boost the confidence weight closer to the master baseline setting
                liveData.weight = Mathf.Lerp(liveData.weight, idealData.weight, boostPercentage);
            }
        }

        // Save these newly upgraded parameters to PlayerPrefs so they persist
        SaveLearnedWeights();
        Debug.Log($"<color=#50C878><b>[AI LEARNING BOOST]</b> Model parameters shifted {boostPercentage * 100}% closer to the ideal configuration!</color>");
    }



    private float GetFlavorProfileValue(CoffeeFlavor flavor)
    {
        if (flavor == CoffeeFlavor.CandyLatte || flavor == CoffeeFlavor.PumpkinTea) return 1f; // SWEET
        if (flavor == CoffeeFlavor.Coffee || flavor == CoffeeFlavor.HerbalTea) return 0f;    // EARTHY
        return 0.5f; // Chocolate and AppleMatcha are the NEUTRAL midpoints
    }

    

    private bool IsFlavorSweet(CoffeeFlavor flavor)
    {
        return flavor == CoffeeFlavor.CandyLatte || flavor == CoffeeFlavor.PumpkinTea || flavor == CoffeeFlavor.Chocolate;
    }

    private bool IsFlavorCaffeinated(CoffeeFlavor flavor)
    {
        Debug.Log($"corrected to have caffeine = {flavor == CoffeeFlavor.Coffee || flavor == CoffeeFlavor.AppleMatcha || flavor == CoffeeFlavor.CandyLatte}");
        return flavor == CoffeeFlavor.Coffee || flavor == CoffeeFlavor.AppleMatcha || flavor == CoffeeFlavor.CandyLatte;
    }
}