using UnityEngine;

public class NNVizExample : MonoBehaviour
{
    public NeuralNetworkVisualizer visualizer;
    private NeuralNetwork network;
    private float[] testInput = new float[2];
    private float[] targets = new float[] { 1f, 0f };
    private bool flip = false;
    void Start()
    {
        visualizer.transform.position = new Vector3(-5f, 0f, 0f);

        network = NeuralNetwork.BuildNetwork(
            new int[] { 2, 3, 2 },
            new TanhActivation(),
            new TanhActivation(),
            new L2Regularization(),
            new string[] { "0", "1" }
        );

        network.LearningRate = 0.1f;
        network.RegularizationRate = 0.01f;

        visualizer.Initialize(network);


    }

    void Update()
    {

        testInput[0] = flip ? 1f : 0f;
        testInput[1] = flip ? 0f : 1f;

        targets[0] = flip ? 0f : 1f;
        targets[1] = flip ? 1f : 0f;

        // Note: takes about 580 iterations to converge
        network.ForwardProp(testInput);

        network.BackProp(targets, new MeanSquaredError());
        network.UpdateWeights();
        visualizer.Update();

        flip = !flip;
    }
}
