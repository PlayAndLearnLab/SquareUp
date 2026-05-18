using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BrewSelectionUI : MonoBehaviour
{
    [Header("Colors")]
    public Color activeColor = Color.yellow;
    public Color normalColor = Color.white;

    [Header("Button Groups")]
    public List<Button> beverageButtons;
    public List<Button> tempButtons;

    // Call this when a beverage is clicked
    public void HighlightBeverage(Button selectedButton)
    {
        ResetGroup(beverageButtons);
        SetButtonColor(selectedButton, activeColor);
    }

    // Call this when a temp is clicked
    public void HighlightTemp(Button selectedButton)
    {
        ResetGroup(tempButtons);
        SetButtonColor(selectedButton, activeColor);
    }

    // Reset everything after the brew starts
    public void ResetAllHighlights()
    {
        ResetGroup(beverageButtons);
        ResetGroup(tempButtons);
    }

    private void ResetGroup(List<Button> group)
    {
        foreach (Button btn in group)
        {
            SetButtonColor(btn, normalColor);
        }
    }

    private void SetButtonColor(Button btn, Color targetColor)
    {
        ColorBlock cb = btn.colors;
        cb.normalColor = targetColor;
        cb.selectedColor = targetColor; // Keeps it highlighted even if focus shifts
        btn.colors = cb;
    }
}
