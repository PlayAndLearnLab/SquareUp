using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRegularizationFunction
{
    float Calculate(float weight);
    float Derivative(float weight);
}

public class L1Regularization : IRegularizationFunction
{
    public float Calculate(float weight) => Mathf.Abs(weight);
    public float Derivative(float weight) => weight < 0 ? -1 : (weight > 0 ? 1 : 0);
}

public class L2Regularization : IRegularizationFunction
{
    public float Calculate(float weight) => 0.5f * weight * weight;
    public float Derivative(float weight) => weight;
}