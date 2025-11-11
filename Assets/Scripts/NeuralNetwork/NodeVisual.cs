using UnityEngine;

public class NodeVisual : MonoBehaviour
{
    public float activation;
    public SpriteRenderer spriteRenderer;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void UpdateVisual()
    {
        // Change color based on activation
        Color nodeColor = Color.Lerp(Color.black, Color.white, activation);
        spriteRenderer.color = nodeColor;

        // Optionally scale the node based on activation
        float scale = Mathf.Lerp(0.8f, 1.2f, activation);
        transform.localScale = new Vector3(scale, scale, 1);
    }
}
