using UnityEngine;

public class BrainTester : MonoBehaviour
{
    public CoffeeBrain brain;
    public AICustomer testCustomer;

    [Header("Test Input")]
    [TextArea(3, 10)]
    public string testDialogue = "I am so tired and freezing!";

    [ContextMenu("Test AI Prediction")]
    public void TestAI()
    {
        if (brain == null || testCustomer == null)
        {
            Debug.LogError("Assign Brain and Customer in the Inspector!");
            return;
        }

        // Ensure the brain is fresh for this specific test run
        brain.Init();

        if (brain.brainDatabase.Count == 0)
        {
            Debug.LogError("The Brain has no keywords! Check your JSON path and formatting.");
            return;
        }

        // Run the prediction
        Coffee result = brain.PredictCoffee(testDialogue, testCustomer);

        // We can manually calculate the logic here just for the Debug view
        // to help your lab see the "Hidden" math
        LogCognitiveBreakdown(result);
    }

    //private void LogCognitiveBreakdown(Coffee result)
    //{
    //    // This mirrors the internal logic of PredictCoffee to show you the "Why"
    //    Debug.Log($"<color=cyan><b>[AI TEST RESULT]</b></color>\n" +
    //              $"<color=white>Dialogue:</color> '{testDialogue}'\n" +
    //              $"<color=yellow>Final Choice:</color> <b>{result.flavor}</b> ({result.temperature}°C)");

    //    Debug.Log("<color=orange><b>[LOGIC BREAKDOWN]</b></color>\n" +
    //              $"> <b>Temp Context:</b> {(result.temperature > 50 ? "Hot" : "Cold")}\n" +
    //              $"> <b>Customer Bias Applied:</b> {testCustomer.favoriteTemp}\n" +
    //              $"> <b>Flavor Profile:</b> {(IsSweet(result.flavor) ? "Sweet/Fruity" : "Earthy/Bold")}");
    //}

    private void LogCognitiveBreakdown(Coffee result)
    {
        string certaintyBar = new string('█', Mathf.RoundToInt(result.confidence * 10)) +
                             new string('░', 10 - Mathf.RoundToInt(result.confidence * 10));

        Debug.Log($"<color=cyan><b>[AI TEST RESULT]</b></color>\n" +
                  $"<color=white>Dialogue:</color> '{testDialogue}'\n" +
                  $"<color=yellow>Final Choice:</color> <b>{result.flavor}</b> ({result.temperature}°C)\n" +
                  $"<color=lime><b>Certainty:</b></color> [{certaintyBar}] {Mathf.RoundToInt(result.confidence * 100)}%");

        Debug.Log("<color=orange><b>[LOGIC BREAKDOWN]</b></color>\n" +
                  $"> <b>Temp Context:</b> {(result.temperature > 50 ? "Hot" : "Cold")}\n" +
                  $"> <b>Customer Bias:</b> {testCustomer.favoriteTemp}\n" +
                  $"> <b>Ambiguity Level:</b> {(result.confidence < 0.4f ? "High (Conflicting Keywords)" : "Low")}");
    }

    private bool IsSweet(CoffeeFlavor flavor)
    {
        return flavor == CoffeeFlavor.CandyLatte ||
               flavor == CoffeeFlavor.PumpkinTea ||
               flavor == CoffeeFlavor.Chocolate; // Assuming chocolate is sweet
    }
}