using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoffeeCupController : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField] private float slideForce = 5f;
    [SerializeField] private float dragAmount = 2f;
    [SerializeField] private float maxSpeed = 5f;

    public GameObject candyIcon;
    public GameObject appleIcon;
    public GameObject pumpkinIcon;
    public GameObject skullIcon;
    private CoffeeFlavor currentCoffeeType;

    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private AudioClip explosionSound;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.drag = dragAmount;
        candyIcon.SetActive(false);
        appleIcon.SetActive(false);
        pumpkinIcon.SetActive(false);
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void RenderCoffee(Coffee coffee)
    {
        currentCoffeeType = coffee.flavor;
        
        // Update the coffee sprite
        if (spriteRenderer != null && coffee.coffeeSprite != null)
        {
            spriteRenderer.sprite = coffee.coffeeSprite;
        }
        
        // Reset icons first
        candyIcon.SetActive(false);
        appleIcon.SetActive(false);
        pumpkinIcon.SetActive(false);
        skullIcon.SetActive(false);

        // Show icon based on coffee flavor
        switch (coffee.flavor)
        {
            case CoffeeFlavor.Candy:
                candyIcon.SetActive(true);
                break;
            case CoffeeFlavor.Apple:
                appleIcon.SetActive(true);
                break;
            case CoffeeFlavor.Pumpkin:
                pumpkinIcon.SetActive(true);
                break;
            case CoffeeFlavor.Poison:
                skullIcon.SetActive(true);
                break;
        }
    }

    public void GiveCoffee(Coffee coffee)
    {
        RenderCoffee(coffee);
        StartCoroutine(SlideCoffee(coffee));
    }

    private IEnumerator SlideCoffee(Coffee coffee)
    {
        // Debug.Log("Sliding coffee");
        // Only apply force if we're below max speed
        if (rb.velocity.magnitude < maxSpeed)
        {
            rb.AddForce(Vector2.left * slideForce, ForceMode2D.Impulse);
        }
        yield return new WaitForSeconds(1.5f);
        EventManager.current.CoffeeReadyForCustomer(coffee, gameObject);
    }

    public IEnumerator Explode()
    {
        // Disable the coffee cup's renderer and collider
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;

        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        // Spawn explosion effect at the coffee cup's position
        GameObject explosionEffect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

        // Ensure the particle system is visible in 2D
        ParticleSystem particleSystem = explosionEffect.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 10;

            // Explicitly play the particle system
            particleSystem.Stop(); // Stop any existing playback
            particleSystem.Clear(); // Clear any existing particles
            particleSystem.Play(); // Start the effect
            Debug.Log("Particle system started playing");
        }

        // Wait for the particle effect to finish
        yield return new WaitForSeconds(1f);

        // Clean up
        Destroy(explosionEffect);
        Destroy(gameObject);
    }
}
