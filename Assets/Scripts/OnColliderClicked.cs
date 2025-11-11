using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public delegate void ClickAction();

public class OnColliderClicked : MonoBehaviour
{
    // Create a public delegate field
    public ClickAction clickAction;
    void OnMouseDown()
    {
        clickAction?.Invoke();
    }
}
