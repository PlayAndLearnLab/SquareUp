using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IntroSlidesData", menuName = "Square Up/Intro Slides Data")]
public class IntroSlidesData : ScriptableObject
{
    [SerializeField] private Sprite[] slides;
    
    public Sprite[] Slides => slides;
    
    public int SlidesCount => slides.Length;
} 