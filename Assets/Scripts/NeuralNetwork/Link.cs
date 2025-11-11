using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Link
{
    public string Id { get; private set; }
    public Node Source { get; private set; }
    public Node Destination { get; private set; }
    public float Weight { get; set; }
    public bool IsDead { get; set; }

    public float ErrorDerivative { get; set; }
    public float AccumulatedErrorDerivative { get; set; }
    public int NumAccumulatedDerivatives { get; set; }

    public IRegularizationFunction Regularization { get; private set; }

    public Link(Node source, Node dest, IRegularizationFunction regularization = null, bool initZero = false)
    {
        Id = $"{source.Id}-{dest.Id}";
        Source = source;
        Destination = dest;
        this.Regularization = regularization;
        Weight = initZero ? 0 : Random.Range(-0.5f, 0.5f);
    }
}