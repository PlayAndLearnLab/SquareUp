using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnGameAreaClicked : MonoBehaviour
{
    public GameMasterScript gmScript;
    public GameArea gameArea;

    private OnColliderClicked onColliderClicked;
    
    void OnClick()
    {

    }

    void Awake()
    {
        onColliderClicked = GetComponent<OnColliderClicked>();
        onColliderClicked.clickAction = OnClick;
    }
}
