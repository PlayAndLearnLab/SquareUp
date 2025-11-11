using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QualityCameraController : MonoBehaviour
{
    public GM gm;
    public GameObject light;

    void Awake() {
        EventManager.current.onItemEnqueued += OnItemEnqueued;
        EventManager.current.onUpgradeApplied += OnUpgradePurchased;
    }

    void Start() {
        gameObject.SetActive(false);
    }

    void OnDestroy() {
        EventManager.current.onItemEnqueued -= OnItemEnqueued;
        EventManager.current.onUpgradeApplied -= OnUpgradePurchased;
    }

    private void OnUpgradePurchased(string upgradeName, UpgradeCategory category, int newLevel) {
        if (upgradeName == "Quality Check") {
            gameObject.SetActive(true);
        }
    }

    IEnumerator ExplodeCoffee(CoffeeOrder coffeeOrder) {
        light.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        yield return coffeeOrder.CoffeeObject.GetComponent<CoffeeCupController>().Explode();
        light.SetActive(false);
    }

    private void OnItemEnqueued(CoffeeOrder coffeeOrder) {
        if (gm.GetQualityLevel() > 0) {
            if (coffeeOrder.Coffee.flavor == CoffeeFlavor.Poison) {
                EventManager.current.CorrectlyDeniedCoffee();
                EventManager.current.RemoveCoffee(coffeeOrder);
                StartCoroutine(ExplodeCoffee(coffeeOrder));
            }
        }
    }
}
