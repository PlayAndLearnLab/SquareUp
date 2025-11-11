using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// public enum CoffeeFlavor
// {
//     PumpkinSpice,
//     Candy,
//     Apple,
// }

public enum CoffeeArea
{
    CoffeeQueue,
    CoffeeMachine,
    PickUpArea,
    CheckArea,
    Trash,
}
public class CoffeeController : MovementController
{
    public CoffeeFlavor type;
    public CoffeeFlavor filledType;
    public GameObject appleCoffeeCup;
    public GameObject candyCoffeeCup;
    public GameObject pumpkinSpiceCoffeeCup;
    public GameObject emptyCoffeeCup;
    private OnColliderClicked onColliderClicked;
    public bool isFull = false;
    public CoffeeArea currentArea = CoffeeArea.CoffeeMachine;

    public CheckAreaController checkAreaController;
    public GameMasterScript GM;

    public new void Awake()
    {
        base.Awake();
        appleCoffeeCup.SetActive(false);
        candyCoffeeCup.SetActive(false);
        pumpkinSpiceCoffeeCup.SetActive(false);
        emptyCoffeeCup.SetActive(true);

        onColliderClicked = GetComponent<OnColliderClicked>();
        onColliderClicked.clickAction = CoffeeClicked;
    }

    public void FillCoffee(float accuracy)
    {
        Debug.Log("Filling coffee with accuracy: " + accuracy);
        isFull = true;
        filledType = type;
        emptyCoffeeCup.SetActive(false);

        if (Random.value > accuracy)
        {
            // Randomly select a different coffee type
            filledType = (CoffeeFlavor)Random.Range(0, 3);

            // If the random coffee type is the same as the original coffee type, try again
            while (filledType == type)
            {
                filledType = (CoffeeFlavor)Random.Range(0, 3);
            }

        }

        switch (filledType)
        {
            // case CoffeeFlavor.PumpkinSpice:
            //     pumpkinSpiceCoffeeCup.SetActive(true);
            //     break;
            // case CoffeeFlavor.Candy:
            //     candyCoffeeCup.SetActive(true);
            //     break;
            // case CoffeeFlavor.Apple:
            //     appleCoffeeCup.SetActive(true);
            //     break;
        }
    }

    public void CoffeeClicked()
    {
        Debug.Log("Coffee clicked in area: " + currentArea);
        if (currentArea == CoffeeArea.PickUpArea)
        {
            GM.SelectCoffee(gameObject);
        }
        else if (currentArea == CoffeeArea.CheckArea)
        {
            checkAreaController.OnCheckAreaClicked();
        }
    }

}
