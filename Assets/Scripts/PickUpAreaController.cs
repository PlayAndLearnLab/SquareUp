using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PickUpAreaController : MonoBehaviour
{
    public class Spot
    {
        public Vector3 position;
        public bool isOccupied;
        public GameObject occupant;
        public Spot(Vector3 position)
        {
            this.position = position;
            this.isOccupied = false;
            this.occupant = null;
        }
    }
    public GameObject[] spotsObjects;
    public GameMasterScript gameMaster;
    private List<Spot> spots = new List<Spot>();

    void Awake()
    {
        foreach (GameObject spotObject in spotsObjects)
        {
            spots.Add(new Spot(spotObject.transform.position));
        }
    }

    public bool IsPickUpAreaFull()
    {
        foreach (Spot spot in spots)
        {
            if (!spot.isOccupied)
            {
                return false;
            }
        }
        return true;
    }
    private void SetCoffeeArea(GameObject coffee)
    {
        CoffeeController coffeeController = coffee.GetComponent<CoffeeController>();
        if (coffeeController)
        {
            coffeeController.currentArea = CoffeeArea.PickUpArea;
        }
    }

    public void FindSpotAndOccupy(GameObject occupant)
    {
        SetCoffeeArea(occupant);
        foreach (Spot spot in spots)
        {
            if (!spot.isOccupied)
            {
                spot.isOccupied = true;
                spot.occupant = occupant;
                occupant.transform.position = spot.position;
                break;
            }
        }
        gameMaster.ShowTutorialStep(4);
    }

    public void UnoccupySpot(GameObject occupant)
    {
        foreach (Spot spot in spots)
        {
            if (spot.occupant == occupant)
            {
                spot.isOccupied = false;
                spot.occupant = null;
                break;
            }
        }
    }
}
