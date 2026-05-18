using UnityEngine;
using TMPro;

public class AnalyticsRow : MonoBehaviour
{
    public TMP_Text keywordLabel;
    public TMP_Text valuesLabel;
    public TMP_Text deltaLabel;

    public void Setup(string keyword, float oldWeight, float newWeight)
    {
        keywordLabel.text = keyword.ToUpper();
        valuesLabel.text = $"{oldWeight:F1} → {newWeight:F1}";

        float diff = newWeight - oldWeight;
        deltaLabel.text = $"+{diff:F1}";
        deltaLabel.color = diff > 0 ? Color.green : Color.white;
    }
}