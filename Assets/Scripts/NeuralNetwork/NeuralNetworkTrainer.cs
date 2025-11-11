using System.Collections.Generic;

[System.Serializable]
public class TrainingParameters
{
    public float learningRate = 0.1f;
    public float momentum = 0.9f;
    public int batchSize = 32;
    public float regularization = 0.0001f; // L2 regularization parameter
}
public class NeuralNetworkTrainer
{

    public TrainingParameters Parameters { get; set; }
    private NeuralNetwork network;
    private float currentError;
    private int epochCount;

    private List<float[]> batchInputs = new List<float[]>();
    private List<float[]> batchTargets = new List<float[]>();

    public NeuralNetworkTrainer(NeuralNetwork network, TrainingParameters parameters)
    {
        this.network = network;
        Parameters = parameters;
    }

    public void Train(float[] input, float[] targetOutput)
    {
        // Add to batch
        batchInputs.Add(input);
        batchTargets.Add(targetOutput);

        // Process batch when it's full
        if (batchInputs.Count >= Parameters.batchSize)
        {
            ProcessBatch();
        }
    }

    private void ProcessBatch()
    {
        currentError = 0f;

        // Process each sample in the batch
        for (int i = 0; i < batchInputs.Count; i++)
        {
            // Forward pass
            float[] output = network.ForwardProp(batchInputs[i]);

            // Calculate error
            float error = CalculateError(output, batchTargets[i]);
            currentError += error;

            // Backward pass
            network.BackProp(batchTargets[i], new MeanSquaredError());
        }

        // Average error over batch
        currentError /= batchInputs.Count;

        // Update weights (includes regularization)
        network.UpdateWeights();

        // Clear batch
        batchInputs.Clear();
        batchTargets.Clear();

        epochCount++;
    }

    private float CalculateError(float[] output, float[] target)
    {
        if (output.Length != target.Length)
        {
            throw new System.ArgumentException("Output and target arrays must have the same length");
        }
        float error = 0f;
        for (int i = 0; i < output.Length; i++)
        {
            float diff = target[i] - output[i];
            error += diff * diff;
        }
        return error * 0.5f; // MSE
    }

    public float GetCurrentError()
    {
        return currentError;
    }

    public int GetEpochCount()
    {
        return epochCount;
    }
}