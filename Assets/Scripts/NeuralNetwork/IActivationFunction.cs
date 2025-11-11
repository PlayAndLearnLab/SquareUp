using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface IActivationFunction
{
    float Calculate(float input);
    float Derivative(float input);
}

public class TanhActivation : IActivationFunction
{
    public float Calculate(float x) => (float)System.Math.Tanh(x);

    public float Derivative(float x)
    {
        float output = Calculate(x);
        return 1 - output * output;
    }
}

public class ReLUActivation : IActivationFunction
{
    public float Calculate(float x) => Mathf.Max(0, x);
    public float Derivative(float x) => x <= 0 ? 0 : 1;
}

public class SigmoidActivation : IActivationFunction
{
    public float Calculate(float x) => 1f / (1f + Mathf.Exp(-x));

    public float Derivative(float x)
    {
        float output = Calculate(x);
        return output * (1 - output);
    }
}

public class LinearActivation : IActivationFunction
{
    public float Calculate(float x) => x;
    public float Derivative(float x) => 1;
}