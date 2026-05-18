using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BrainAnalytics : MonoBehaviour
{
    [Header("General Setup")]
    public GameObject analyticsPanel;
    public Transform listParent;
    public GameObject growthBarPrefab;

    [Header("Daily Performance UI (Today Only)")]
    public Image successBar;
    public Image missBar;
    public TMP_Text performanceText;

    [Header("Chart Axis Labels")]
    public TMP_Text yAxisMax;
    public TMP_Text yAxisMin;

    // Data storage for the 'Big Picture'
    [System.Serializable]
    public class BrainHistoryPoint
    {
        public int day;
        public float averageIntelligence;
    }
    private List<BrainHistoryPoint> overallHistory = new List<BrainHistoryPoint>();

    // Satisfies the call from CoffeeBrain.cs
    public void CaptureStartOfDay(List<KeywordData> currentDatabase)
    {
        // Placeholder for future logic
    }

    public void DisplayEndOfDay(List<KeywordData> currentDatabase, int successes, int misses)
    {
        // 1. Update the Success/Miss Bar (Current Day only)
        UpdatePerformanceBar(successes, misses);

        // 2. Calculate average intelligence for the big history chart
        float totalWeight = 0;
        foreach (var data in currentDatabase) totalWeight += data.weight;
        float average = totalWeight / currentDatabase.Count;

        // 3. Record today's data point
        BrainHistoryPoint point = new BrainHistoryPoint();
        point.day = FindObjectOfType<GM>().day;
        point.averageIntelligence = average;
        overallHistory.Add(point);

        // 4. Refresh UI
        UpdateOverallGraphic();
        
        analyticsPanel.SetActive(true);
    }

    private void UpdatePerformanceBar(int s, int m)
    {
        int total = s + m;
        performanceText.text = $"TODAY: {s} Correct | {m} Misses";

        // Get the LayoutElement components
        LayoutElement sLayout = successBar.GetComponent<LayoutElement>();
        LayoutElement mLayout = missBar.GetComponent<LayoutElement>();

        if (total == 0)
        {
            // Default to 50/50 if no customers served
            sLayout.flexibleWidth = 1f;
            mLayout.flexibleWidth = 1f;
            return;
        }

        // Set flexibleWidth based on the ratio
        // If s=8 and m=2, s gets 0.8 and m gets 0.2
        sLayout.flexibleWidth = (float)s / total;
        mLayout.flexibleWidth = (float)m / total;

        // Optional: If a bar is 0, you might want to hide it entirely
        successBar.gameObject.SetActive(s > 0);
        missBar.gameObject.SetActive(m > 0);

        Debug.Log($"Performance: {s}/{total} = {(float)s / total:P1} Success Ratio");
    }

    public void UpdateOverallGraphic()
    {
        // Clear previous bars
        foreach (Transform child in listParent) Destroy(child.gameObject);

        foreach (var point in overallHistory)
        {
            GameObject bar = Instantiate(growthBarPrefab, listParent);
            RectTransform rt = bar.GetComponent<RectTransform>();

            // Scaling: 1.0 weight = 500 pixels tall
            float displayHeight = point.averageIntelligence * 500f;
            rt.sizeDelta = new Vector2(40, displayHeight);

            // Update the % label on top of the bar
            // Note: This requires a child object named "ValueText" in your prefab
            Transform vText = bar.transform.Find("ValueText");
            if (vText != null)
            {
                vText.GetComponent<TMP_Text>().text = $"{(point.averageIntelligence * 100):F0}%";
            }

            // Update Day label at the bottom
            bar.GetComponentInChildren<TMP_Text>().text = "Day " + point.day;
        }
    }

    public void CloseAnalytics()
    {
        analyticsPanel.SetActive(false);
        if (EventManager.current != null)
        {
            EventManager.current.AnalyticsClosed();
        }
    }
}