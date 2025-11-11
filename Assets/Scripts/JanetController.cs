using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;

public class JanetController : MonoBehaviour
{
    public GameObject facesParent;
    public AudioClip yaySound;
    public AudioClip screamNoSound;
    private AudioSource audioSource;
    private static readonly int MAX = 10;
    private static readonly int MIN = 0;
    private int happinessScore = (MAX - MIN) / 2;

    private GameObject[] faces;
    private int currentFaceIndex;

    private int scoreToIndex(int score)
    {
        int safeScore = Mathf.Clamp(score, MIN, MAX) - MIN;
        float ratio = (float)safeScore / (MAX - MIN);
        int i = Mathf.CeilToInt(ratio * (faces.Length - 1));
        return i;
    }

    void Awake()
    {
        faces = new GameObject[facesParent.transform.childCount];
        for (int i = 0; i < facesParent.transform.childCount; i++)
        {
            faces[i] = facesParent.transform.GetChild(i).gameObject;
            faces[i].SetActive(false);
        }
        // set current face to the middle one
        currentFaceIndex = scoreToIndex(happinessScore);
        faces[currentFaceIndex].SetActive(true);
        audioSource = GetComponent<AudioSource>();
    }

    public void CoffeeSuccess()
    {
        happinessScore++;
        Yay();
        UpdateFace();
    }
    public void CoffeeFail()
    {
        happinessScore--;
        ScreamNo();
        UpdateFace();
    }
    public void Reset()
    {
        happinessScore = (MAX - MIN) / 2;
        UpdateFace();
    }

    private void UpdateFace()
    {
        faces[currentFaceIndex].SetActive(false);
        int score = Mathf.Clamp(happinessScore, MIN, MAX);
        // map score to index
        currentFaceIndex = scoreToIndex(score);
        faces[currentFaceIndex].SetActive(true);
    }
    private void Yay()
    {
        audioSource.PlayOneShot(yaySound);
    }
    private void ScreamNo()
    {
        audioSource.PlayOneShot(screamNoSound);
    }
}
