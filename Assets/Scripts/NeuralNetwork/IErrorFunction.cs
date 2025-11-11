using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IErrorFunction
{
    float Error(float output, float target);
    float Derivative(float output, float target);
}

public class MeanSquaredError : IErrorFunction
{
    public float Error(float output, float target)
    {
        float diff = output - target;
        return 0.5f * diff * diff;  // 1/2 * (output - target)^2
    }

    public float Derivative(float output, float target)
    {
        return output - target;  // d/dx(1/2 * (x - t)^2) = x - t
    }
}