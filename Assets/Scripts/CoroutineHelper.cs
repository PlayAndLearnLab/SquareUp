using UnityEngine;
using System.Collections;

public class CoroutineHelper : MonoBehaviour
{
    private static CoroutineHelper _instance;

    public static CoroutineHelper Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("CoroutineHelper");
                _instance = go.AddComponent<CoroutineHelper>();
                DontDestroyOnLoad(go); // Keep it persistent across scenes if needed
            }
            return _instance;
        }
    }

    // Method to start a coroutine from a non-MonoBehaviour script
    public void RunCoroutine(IEnumerator coroutine)
    {
        StartCoroutine(coroutine);
    }

    // Optional: Add a way to stop coroutines if needed
    public void StopRunningCoroutine(Coroutine coroutine)
    {
         if (coroutine != null)
         {
            StopCoroutine(coroutine);
         }
    }
}
