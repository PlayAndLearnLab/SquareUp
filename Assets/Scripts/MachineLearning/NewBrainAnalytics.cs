using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NewBrainAnalytics : MonoBehaviour
{
    public enum ChartMode { MasteryMode, ErrorLossModeEnum }

    [Header("Design Testing Settings")]
    public ChartMode activeDisplayMode = ChartMode.MasteryMode;

    [Header("General Setup")]
    public GameObject analyticsPanel;
    public Transform listParent;          

    [Header("Line Chart Setup")]
    public GameObject chartDotPrefab;     // A prefab of a small circular UI image/dot
    public Sprite lineSprite;             // A simple square/white UI sprite to stretch into lines
    public Color lineColor = Color.green;  // Color of the trend line
    public float lineWidth = 4f;          // Thickness of the trend line

    [Header("Word Delta Log Setup")]
    public Transform wordLogParent;
    public GameObject wordLogEntryPrefab;

    [Header("Daily Performance UI (Today Only)")]
    public Image successBar;
    public Image missBar;
    public TMP_Text performanceText;

    [Header("Chart Sizing Setup")]
    public RectTransform yAxisLine;       
    public TMP_Text yAxisMax;
    public TMP_Text yAxisMin;
    public TMP_Text chartTitleText;

    [System.Serializable]
    public class BrainHistoryPoint
    {
        public int day;
        public float averageIntelligence;
    }
    private List<BrainHistoryPoint> overallHistory = new List<BrainHistoryPoint>();
    private Dictionary<string, float> startOfDayWeights = new Dictionary<string, float>();

    private void OnValidate()
    {
        if (analyticsPanel != null && analyticsPanel.activeSelf)
        {
            UpdateOverallGraphic();
        }
    }

    public void CaptureStartOfDay(List<KeywordData> currentDatabase)
    {
        startOfDayWeights.Clear();
        foreach (var data in currentDatabase)
        {
            if (!startOfDayWeights.ContainsKey(data.keyword))
            {
                startOfDayWeights.Add(data.keyword, data.weight);
            }
        }
    }

    public void DisplayEndOfDay(List<KeywordData> currentDatabase, int successes, int misses)
    {
        UpdatePerformanceBar(successes, misses);

        float totalWeight = 0;
        foreach (var data in currentDatabase) totalWeight += data.weight;
        float average = totalWeight / currentDatabase.Count;

        BrainHistoryPoint point = new BrainHistoryPoint();
        point.day = FindObjectOfType<GM>().day;
        point.averageIntelligence = average;
        overallHistory.Add(point);

        UpdateOverallGraphic();
        PopulateWordUpdatesLog(currentDatabase);

        analyticsPanel.SetActive(true);
    }

    private void UpdatePerformanceBar(int s, int m)
    {
        int total = s + m;
        performanceText.text = $"TODAY: {s} Correct | {m} Misses";

        LayoutElement sLayout = successBar.GetComponent<LayoutElement>();
        LayoutElement mLayout = missBar.GetComponent<LayoutElement>();

        if (total == 0)
        {
            sLayout.flexibleWidth = 1f;
            mLayout.flexibleWidth = 1f;
            return;
        }

        sLayout.flexibleWidth = (float)s / total;
        mLayout.flexibleWidth = (float)m / total;

        successBar.gameObject.SetActive(s > 0);
        missBar.gameObject.SetActive(m > 0);
    }

    public void UpdateOverallGraphic()
    {
        // 1. Update Axis Labels
        if (activeDisplayMode == ChartMode.MasteryMode)
        {
            if (yAxisMax != null) yAxisMax.text = "100% Mastery";
            if (yAxisMin != null) yAxisMin.text = "0% Base";
            if (chartTitleText != null) chartTitleText.text = "Model Knowledge Growth";
        }
        else
        {
            if (yAxisMax != null) yAxisMax.text = "1.00 Max Loss (Untrained)";
            if (yAxisMin != null) yAxisMin.text = "0.00 Converged (Perfect)";
            if (chartTitleText != null) chartTitleText.text = "System Training Loss Curve";
        }

        // 2. Clear previous nodes/lines
        foreach (Transform child in listParent) Destroy(child.gameObject);

        if (overallHistory.Count == 0) return;

        // Get chart rendering boundaries
        RectTransform parentRect = listParent.GetComponent<RectTransform>();
        float chartWidth = parentRect.rect.width;
        float maxHeight = 500f;
        if (yAxisLine != null) maxHeight = yAxisLine.rect.height;

        List<Vector2> dotPositions = new List<Vector2>();

        // 3. Spawn Data Point Dots and manually calculate positions
        for (int i = 0; i < overallHistory.Count; i++)
        {
            var point = overallHistory[i];
            GameObject dotGo = Instantiate(chartDotPrefab, listParent);
            RectTransform rt = dotGo.GetComponent<RectTransform>();
            Transform vText = dotGo.transform.Find("ValueText");

            // Calculate Y (Height)
            float calculatedHeight = 0f;
            if (activeDisplayMode == ChartMode.MasteryMode)
            {
                calculatedHeight = point.averageIntelligence * maxHeight;
                if (vText != null) vText.GetComponent<TMP_Text>().text = $"{(point.averageIntelligence * 100):F0}%";
            }
            else
            {
                float lossValue = 1.0f - point.averageIntelligence;
                calculatedHeight = lossValue * maxHeight;
                if (vText != null) vText.GetComponent<TMP_Text>().text = $"{lossValue:F2} Loss";
            }

            // Calculate X (Even spacing across the width of the chart panel)
            
            float calculatedX = 0f;
            if (overallHistory.Count > 1)
            {
                calculatedX = ((float)i / (overallHistory.Count - 1)) * chartWidth;
            }
            else
            {
                //calculatedX = chartWidth / 2f; // Center it if it's only Day 0
                calculatedX = 20f;
            }

            // Apply clean Anchors & Position directly
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(calculatedX, calculatedHeight);

            dotPositions.Add(rt.anchoredPosition);

            // Update Day label text
            TMP_Text dayLabel = dotGo.GetComponentInChildren<TMP_Text>();
            if (dayLabel != null && dayLabel != vText?.GetComponent<TMP_Text>())
            {
                dayLabel.text = "Day " + point.day;
            }
        }

        // 4. Draw Connecting Lines directly using the absolute recorded coordinates
        for (int i = 0; i < dotPositions.Count - 1; i++)
        {
            CreateLineConnection(dotPositions[i], dotPositions[i + 1], listParent);
        }
    }


    private void CreateLineConnection(Vector2 dotPositionA, Vector2 dotPositionB, Transform parentFolder)
    {
        GameObject lineGo = new GameObject("LineConnection", typeof(Image));
        lineGo.transform.SetParent(parentFolder, false);
        lineGo.transform.SetAsFirstSibling(); // Draw behind the dot nodes safely

        Image image = lineGo.GetComponent<Image>();
        image.sprite = lineSprite;
        image.color = lineColor;

        RectTransform rectTransform = lineGo.GetComponent<RectTransform>();
        Vector2 dir = (dotPositionB - dotPositionA).normalized;
        float distance = Vector2.Distance(dotPositionA, dotPositionB);

        // Position & Rotate UI element to look precisely along path vector
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.sizeDelta = new Vector2(distance, lineWidth);
        rectTransform.anchoredPosition = dotPositionA + dir * distance * 0.5f;
        rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    private void PopulateWordUpdatesLog(List<KeywordData> currentDatabase)
    {
        foreach (Transform child in wordLogParent) Destroy(child.gameObject);

        foreach (var data in currentDatabase)
        {
            float startingWeight = 0f;
            if (startOfDayWeights.ContainsKey(data.keyword)) startingWeight = startOfDayWeights[data.keyword];

            float delta = data.weight - startingWeight;
            if (Mathf.Approximately(delta, 0f)) continue;

            GameObject logEntry = Instantiate(wordLogEntryPrefab, wordLogParent);
            WordLogEntryUI rowUI = logEntry.GetComponent<WordLogEntryUI>();

            if (rowUI != null)
            {
                rowUI.wordText.text = data.keyword;
                bool isPositiveUpdate = delta > 0;

                if (isPositiveUpdate)
                {
                    rowUI.deltaText.text = $"+{delta:F4}";
                    rowUI.deltaText.color = Color.green;
                }
                else
                {
                    rowUI.deltaText.text = $"{delta:F4}";
                    rowUI.deltaText.color = Color.red;
                }

                if (data.onlyUpdatesTemp)
                {
                    SetIndicatorText(rowUI.caffeineText, false, isPositiveUpdate);
                    SetIndicatorText(rowUI.tempText, true, isPositiveUpdate);
                    SetIndicatorText(rowUI.sweetnessText, false, isPositiveUpdate);
                }
                else
                {
                    SetIndicatorText(rowUI.caffeineText, data.suggestedCaffeine > 0.01f, isPositiveUpdate);
                    SetIndicatorText(rowUI.tempText, data.suggestedTemp > 0.01f, isPositiveUpdate);
                    SetIndicatorText(rowUI.sweetnessText, data.suggestedProfile > 0.01f, isPositiveUpdate);
                }
            }
        }
    }

    public void ToggleChartMode()
    {
        if (activeDisplayMode == ChartMode.MasteryMode)
            activeDisplayMode = ChartMode.ErrorLossModeEnum;
        else
            activeDisplayMode = ChartMode.MasteryMode;

        UpdateOverallGraphic();
    }

    private void SetIndicatorText(TMP_Text targetText, bool isFeatureActive, bool increased)
    {
        if (targetText == null) return;
        if (!isFeatureActive) { targetText.text = "•"; targetText.color = Color.yellow; }
        else if (increased) { targetText.text = "+"; targetText.color = Color.green; }
        else { targetText.text = "-"; targetText.color = Color.red; }
    }

    public void CloseAnalytics()
    {
        analyticsPanel.SetActive(false);
        if (EventManager.current != null) EventManager.current.AnalyticsClosed();
    }
}






//using System.Collections.Generic;
//using UnityEngine;
//using TMPro;
//using UnityEngine.UI;

//public class NewBrainAnalytics : MonoBehaviour
//{
//    public enum ChartMode { MasteryMode, ErrorLossModeEnum }

//    [Header("Design Testing Settings")]
//    [Tooltip("Toggle this to switch your entire graphic presentation view between Mastery metrics and Loss/Error metrics!")]
//    public ChartMode activeDisplayMode = ChartMode.MasteryMode;

//    [Header("General Setup")]
//    public GameObject analyticsPanel;
//    public Transform listParent;
//    public GameObject growthBarPrefab;

//    [Header("Word Delta Log Setup")]
//    public Transform wordLogParent;
//    public GameObject wordLogEntryPrefab;

//    [Header("Daily Performance UI (Today Only)")]
//    public Image successBar;
//    public Image missBar;
//    public TMP_Text performanceText;

//    [Header("Chart Sizing Setup")]
//    public RectTransform yAxisLine;

//    [Header("Chart Axis Labels")]
//    public TMP_Text yAxisMax;
//    public TMP_Text yAxisMin;
//    public TMP_Text chartTitleText; // Optional: Link your "100% Mastery" text label transform here if you want it to change dynamically

//    [System.Serializable]
//    public class BrainHistoryPoint
//    {
//        public int day;
//        public float averageIntelligence;
//    }
//    private List<BrainHistoryPoint> overallHistory = new List<BrainHistoryPoint>();
//    private Dictionary<string, float> startOfDayWeights = new Dictionary<string, float>();

//    // Automated checking when testing in the Unity editor window
//    private void OnValidate()
//    {
//        if (analyticsPanel != null && analyticsPanel.activeSelf)
//        {
//            UpdateOverallGraphic();
//        }
//    }

//    public void CaptureStartOfDay(List<KeywordData> currentDatabase)
//    {
//        startOfDayWeights.Clear();
//        foreach (var data in currentDatabase)
//        {
//            if (!startOfDayWeights.ContainsKey(data.keyword))
//            {
//                startOfDayWeights.Add(data.keyword, data.weight);
//            }
//        }
//    }

//    public void DisplayEndOfDay(List<KeywordData> currentDatabase, int successes, int misses)
//    {
//        UpdatePerformanceBar(successes, misses);

//        float totalWeight = 0;
//        foreach (var data in currentDatabase) totalWeight += data.weight;
//        float average = totalWeight / currentDatabase.Count;

//        BrainHistoryPoint point = new BrainHistoryPoint();
//        point.day = FindObjectOfType<GM>().day;
//        point.averageIntelligence = average;
//        overallHistory.Add(point);

//        UpdateOverallGraphic();
//        PopulateWordUpdatesLog(currentDatabase);

//        analyticsPanel.SetActive(true);
//    }

//    private void UpdatePerformanceBar(int s, int m)
//    {
//        int total = s + m;
//        performanceText.text = $"TODAY: {s} Correct | {m} Misses";

//        LayoutElement sLayout = successBar.GetComponent<LayoutElement>();
//        LayoutElement mLayout = missBar.GetComponent<LayoutElement>();

//        if (total == 0)
//        {
//            sLayout.flexibleWidth = 1f;
//            mLayout.flexibleWidth = 1f;
//            return;
//        }

//        sLayout.flexibleWidth = (float)s / total;
//        mLayout.flexibleWidth = (float)m / total;

//        successBar.gameObject.SetActive(s > 0);
//        missBar.gameObject.SetActive(m > 0);
//    }

//    public void UpdateOverallGraphic()
//    {

//        float maxHeight = 500f;
//        if (yAxisLine != null)
//        {
//            maxHeight = yAxisLine.rect.height;
//        }

//        // Update Axis Text Labels contextually based on chosen mode
//        if (activeDisplayMode == ChartMode.MasteryMode)
//        {
//            if (yAxisMax != null) yAxisMax.text = "100% Mastery";
//            if (yAxisMin != null) yAxisMin.text = "0% Base";
//            if (chartTitleText != null) chartTitleText.text = "Model Knowledge Growth";
//        }
//        else
//        {
//            if (yAxisMax != null) yAxisMax.text = "1.00 Max Loss (Untrained)";
//            if (yAxisMin != null) yAxisMin.text = "0.00 Converged (Perfect)";
//            if (chartTitleText != null) chartTitleText.text = "System Training Loss Curve";
//        }

//        // Clear existing bars
//        foreach (Transform child in listParent) Destroy(child.gameObject);

//        foreach (var point in overallHistory)
//        {
//            GameObject bar = Instantiate(growthBarPrefab, listParent);
//            RectTransform rt = bar.GetComponent<RectTransform>();
//            Transform vText = bar.transform.Find("ValueText");

//            if (activeDisplayMode == ChartMode.MasteryMode)
//            {
//                // Standard: Value grows up as average intelligence increases
//                float displayHeight = point.averageIntelligence * maxHeight;
//                rt.sizeDelta = new Vector2(40, displayHeight);

//                if (vText != null)
//                {
//                    vText.GetComponent<TMP_Text>().text = $"{(point.averageIntelligence * 100):F0}%";
//                }
//            }
//            else
//            {
//                // Error Loss Variant: Loss = 1.0 - Intelligence. 
//                // As intelligence increases, the error bar shrinks closer to zero!
//                float lossValue = 1.0f - point.averageIntelligence;
//                float displayHeight = lossValue * maxHeight;
//                rt.sizeDelta = new Vector2(40, displayHeight);

//                if (vText != null)
//                {
//                    vText.GetComponent<TMP_Text>().text = $"{lossValue:F2} Loss";
//                }
//            }

//            bar.GetComponentInChildren<TMP_Text>().text = "Day " + point.day;
//        }
//    }

//    private void PopulateWordUpdatesLog(List<KeywordData> currentDatabase)
//    {
//        foreach (Transform child in wordLogParent)
//        {
//            Destroy(child.gameObject);
//        }

//        foreach (var data in currentDatabase)
//        {
//            float startingWeight = 0f;

//            if (startOfDayWeights.ContainsKey(data.keyword))
//            {
//                startingWeight = startOfDayWeights[data.keyword];
//            }

//            float delta = data.weight - startingWeight;

//            if (Mathf.Approximately(delta, 0f)) continue;

//            GameObject logEntry = Instantiate(wordLogEntryPrefab, wordLogParent);
//            WordLogEntryUI rowUI = logEntry.GetComponent<WordLogEntryUI>();

//            if (rowUI != null)
//            {
//                rowUI.wordText.text = data.keyword;
//                bool isPositiveUpdate = delta > 0;

//                // Adjust numeric sign formatting to scientific notation style for precision feel
//                if (isPositiveUpdate)
//                {
//                    rowUI.deltaText.text = $"+{delta:F4}"; // Padding to four decimal spaces
//                    rowUI.deltaText.color = Color.green;
//                }
//                else
//                {
//                    rowUI.deltaText.text = $"{delta:F4}";
//                    rowUI.deltaText.color = Color.red;
//                }

//                if (data.onlyUpdatesTemp)
//                {
//                    SetIndicatorText(rowUI.caffeineText, false, isPositiveUpdate);
//                    SetIndicatorText(rowUI.tempText, true, isPositiveUpdate);
//                    SetIndicatorText(rowUI.sweetnessText, false, isPositiveUpdate);
//                }
//                else
//                {
//                    SetIndicatorText(rowUI.caffeineText, data.suggestedCaffeine > 0.01f, isPositiveUpdate);
//                    SetIndicatorText(rowUI.tempText, data.suggestedTemp > 0.01f, isPositiveUpdate);
//                    SetIndicatorText(rowUI.sweetnessText, data.suggestedProfile > 0.01f, isPositiveUpdate);
//                }
//            }
//        }
//    }

//    private void SetIndicatorText(TMP_Text targetText, bool isFeatureActive, bool increased)
//    {
//        if (targetText == null) return;

//        if (!isFeatureActive)
//        {
//            targetText.text = "•";
//            targetText.color = Color.yellow;
//        }
//        else if (increased)
//        {
//            targetText.text = "+";
//            targetText.color = Color.green;
//        }
//        else
//        {
//            targetText.text = "-";
//            targetText.color = Color.red;
//        }
//    }

//    public void ToggleChartMode()
//    {
//        if (activeDisplayMode == ChartMode.MasteryMode)
//            activeDisplayMode = ChartMode.ErrorLossModeEnum;
//        else
//            activeDisplayMode = ChartMode.MasteryMode;

//        UpdateOverallGraphic();
//    }

//    public void CloseAnalytics()
//    {
//        analyticsPanel.SetActive(false);
//        if (EventManager.current != null)
//        {
//            EventManager.current.AnalyticsClosed();
//        }
//    }
//}

