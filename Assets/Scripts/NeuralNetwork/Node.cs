using UnityEngine;
using System.Collections.Generic;

public class Node
{
    public string Id { get; private set; }
    public List<Link> InputLinks { get; private set; } = new List<Link>();
    public List<Link> Outputs { get; private set; } = new List<Link>();
    public float Bias { get; set; } = 0.1f;

    public float TotalInput { get; private set; }
    public float Output { get; set; }
    public float OutputDerivative { get; set; }
    public float InputDerivative { get; set; }
    public float AccumulatedInputDerivative { get; set; }
    public int NumAccumulatedDerivatives { get; set; }

    public IActivationFunction Activation { get; private set; }

    public Node(string id, IActivationFunction activation, bool initZero = false)
    {
        Id = id;
        this.Activation = activation;
        if (initZero)
        {
            Bias = 0;
        }
    }

    public float UpdateOutput()
    {
        TotalInput = Bias;
        foreach (var link in InputLinks)
        {
            if (!link.IsDead)
            {
                TotalInput += link.Weight * link.Source.Output;
            }
        }
        Output = Activation.Calculate(TotalInput);
        return Output;
    }
}