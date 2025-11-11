using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class NeuralNetwork
{
    private List<List<Node>> network = new List<List<Node>>();
    private IErrorFunction errorFunction;

    public float LearningRate { get; set; } = 0.1f;
    public float RegularizationRate { get; set; } = 0.01f;

    public static NeuralNetwork BuildNetwork(
        int[] networkShape,
        IActivationFunction hiddenActivation,
        IActivationFunction outputActivation,
        IRegularizationFunction regularization,
        string[] inputIds,
        bool initZero = false)
    {
        if (inputIds.Length != networkShape[0])
        {
            throw new System.ArgumentException(
                $"Number of input IDs ({inputIds.Length}) must match the number of input nodes ({networkShape[0]})"
            );
        }

        var nn = new NeuralNetwork();
        int id = 1;

        // Create layers
        for (int layerIdx = 0; layerIdx < networkShape.Length; layerIdx++)
        {
            bool isOutputLayer = layerIdx == networkShape.Length - 1;
            bool isInputLayer = layerIdx == 0;
            var currentLayer = new List<Node>();
            nn.network.Add(currentLayer);

            int numNodes = networkShape[layerIdx];
            for (int i = 0; i < numNodes; i++)
            {
                string nodeId = id.ToString();
                if (isInputLayer)
                {
                    nodeId = inputIds[i];
                }
                else
                {
                    id++;
                }

                var node = new Node(
                    nodeId,
                    isOutputLayer ? outputActivation : hiddenActivation,
                    initZero
                );
                currentLayer.Add(node);

                if (layerIdx >= 1)
                {
                    // Add links from nodes in the previous layer to this node
                    foreach (var prevNode in nn.network[layerIdx - 1])
                    {
                        var link = new Link(prevNode, node, regularization, initZero);
                        prevNode.Outputs.Add(link);
                        node.InputLinks.Add(link);
                    }
                }
            }
        }

        return nn;
    }

    public float[] ForwardProp(float[] inputs)
    {
        var inputLayer = network[0];
        if (inputs.Length != inputLayer.Count)
        {
            throw new System.ArgumentException(
                "The number of inputs must match the number of nodes in the input layer"
            );
        }

        // Update input layer
        for (int i = 0; i < inputLayer.Count; i++)
        {
            inputLayer[i].Output = inputs[i];
        }

        // Forward propagation
        for (int layerIdx = 1; layerIdx < network.Count; layerIdx++)
        {
            foreach (var node in network[layerIdx])
            {
                node.UpdateOutput();
            }
        }

        return GetOutput();
    }

    public void BackProp(float[] targets, IErrorFunction errorFunc)
    {
        var outputLayer = network[network.Count - 1];
        if (targets.Length != outputLayer.Count)
        {
            throw new System.ArgumentException(
                "Number of targets must match number of output nodes"
            );
        }

        // Handle output nodes
        for (int i = 0; i < outputLayer.Count; i++)
        {
            var outputNode = outputLayer[i];
            outputNode.OutputDerivative = errorFunc.Derivative(outputNode.Output, targets[i]);
        }

        // Go through the layers backwards
        for (int layerIdx = network.Count - 1; layerIdx >= 1; layerIdx--)
        {
            var currentLayer = network[layerIdx];

            // 1. Compute derivatives for current layer
            foreach (var node in currentLayer)
            {
                node.InputDerivative = node.OutputDerivative *
                    node.Activation.Derivative(node.TotalInput);
                node.AccumulatedInputDerivative += node.InputDerivative;
                node.NumAccumulatedDerivatives++;
            }

            // 2. Compute derivatives for weights
            foreach (var node in currentLayer)
            {
                foreach (var link in node.InputLinks)
                {
                    if (link.IsDead) continue;

                    link.ErrorDerivative = node.InputDerivative * link.Source.Output;
                    link.AccumulatedErrorDerivative += link.ErrorDerivative;
                    link.NumAccumulatedDerivatives++;
                }
            }

            // 3. Compute derivatives for previous layer
            if (layerIdx == 1) continue;

            var prevLayer = network[layerIdx - 1];
            foreach (var node in prevLayer)
            {
                node.OutputDerivative = 0;
                foreach (var output in node.Outputs)
                {
                    node.OutputDerivative += output.Weight * output.Destination.InputDerivative;
                }
            }
        }
    }

    public void UpdateWeights()
    {
        for (int layerIdx = 1; layerIdx < network.Count; layerIdx++)
        {
            foreach (var node in network[layerIdx])
            {
                // Update node's bias
                if (node.NumAccumulatedDerivatives > 0)
                {
                    node.Bias -= LearningRate * node.AccumulatedInputDerivative /
                        node.NumAccumulatedDerivatives;
                    node.AccumulatedInputDerivative = 0;
                    node.NumAccumulatedDerivatives = 0;
                }

                // Update incoming weights
                foreach (var link in node.InputLinks)
                {
                    if (link.IsDead) continue;

                    float regularizationDer = link.Regularization?.Derivative(link.Weight) ?? 0;

                    if (link.NumAccumulatedDerivatives > 0)
                    {
                        // Update based on error derivative
                        link.Weight -= (LearningRate / link.NumAccumulatedDerivatives) *
                            link.AccumulatedErrorDerivative;

                        // Update based on regularization
                        float newWeight = link.Weight -
                            (LearningRate * RegularizationRate) * regularizationDer;

                        // Handle L1 regularization special case
                        if (link.Regularization is L1Regularization &&
                            link.Weight * newWeight < 0)
                        {
                            link.Weight = 0;
                            link.IsDead = true;
                        }
                        else
                        {
                            link.Weight = newWeight;
                        }

                        link.AccumulatedErrorDerivative = 0;
                        link.NumAccumulatedDerivatives = 0;
                    }
                }
            }
        }
    }

    private Node GetOutputNode() => network[network.Count - 1][0];

    public void ForEachNode(bool ignoreInputs, System.Action<Node> accessor)
    {
        for (int layerIdx = ignoreInputs ? 1 : 0; layerIdx < network.Count; layerIdx++)
        {
            foreach (var node in network[layerIdx])
            {
                accessor(node);
            }
        }
    }

    public float GetNodeOutput(int layerIndex, int nodeIndex)
    {
        if (layerIndex < 0 || layerIndex >= network.Count) return 0;
        if (nodeIndex < 0 || nodeIndex >= network[layerIndex].Count) return 0;

        return network[layerIndex][nodeIndex].Output;
    }

    public float GetWeight(int fromLayer, int fromNode, int toLayer, int toNode)
    {
        if (fromLayer < 0 || fromLayer >= network.Count - 1) return 0;
        if (toLayer != fromLayer + 1) return 0;

        var sourceNode = network[fromLayer][fromNode];
        var targetNode = network[toLayer][toNode];

        var link = sourceNode.Outputs.Find(l => l.Destination == targetNode);
        return link?.Weight ?? 0f;
    }

    public int LayerCount => network.Count;

    public int GetLayerNodes(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= network.Count)
            throw new System.ArgumentOutOfRangeException(nameof(layerIndex));

        return network[layerIndex].Count;
    }

    public float[] GetOutput()
    {
        var outputLayer = network[network.Count - 1];
        float[] outputs = new float[outputLayer.Count];
        for (int i = 0; i < outputLayer.Count; i++)
        {
            outputs[i] = outputLayer[i].Output;
        }
        return outputs;
    }

    public void SetWeight(int fromLayer, int fromNode, int toLayer, int toNode, float weight)
    {
        network[fromLayer][fromNode].Outputs[toNode].Weight = weight;
    }
}
