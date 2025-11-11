using UnityEngine;
using System.Collections.Generic;

public class NeuralNetworkVisualizer : MonoBehaviour
{
    [System.Serializable]
    public class LayerVisualization
    {
        public int nodeCount;
        public float spacing = 1f;
        public GameObject nodePrefab;
        public List<GameObject> nodes = new List<GameObject>();
    }

    [System.Serializable]
    public class Connection
    {
        public LineRenderer lineRenderer;
        public float weight;
        public GameObject fromNode;
        public GameObject toNode;
    }

    public List<LayerVisualization> layers = new List<LayerVisualization>();
    public Material connectionMaterial;
    public float minLineWidth = 0.02f;
    public float maxLineWidth = 0.1f;
    public Color positiveWeightColor = Color.green;
    public Color negativeWeightColor = Color.red;
    public float layerDistance = 2f;
    public Color[] colorGradient = new Color[] {
        new Color(0.8f, 0.2f, 0.2f), // Dark red
        new Color(0.95f, 0.95f, 0.95f), // White
        new Color(0.2f, 0.8f, 0.2f)  // Dark green
    };
    public float[] colorThresholds = new float[] { -0.5f, 0f, 0.7f };

    private NeuralNetwork network;
    private List<Connection> connections = new List<Connection>();
    private float minObservedWeight = float.MaxValue;
    private float maxObservedWeight = float.MinValue;

    void CreateVisualization()
    {
        Vector2 position = transform.position;
        float maxHeight = 0;

        // Calculate total height needed
        foreach (var layer in layers)
        {
            float layerHeight = layer.nodeCount * layer.spacing;
            maxHeight = Mathf.Max(maxHeight, layerHeight);
        }

        // Create nodes
        for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
        {
            var layer = layers[layerIndex];
            float layerHeight = layer.nodeCount * layer.spacing;
            Vector2 startPos = position + Vector2.up * (layerHeight / 2);

            // Center this layer vertically
            startPos.y += (maxHeight - layerHeight) / 2;

            for (int nodeIndex = 0; nodeIndex < layer.nodeCount; nodeIndex++)
            {
                Vector2 nodePos = startPos - Vector2.up * (nodeIndex * layer.spacing);
                GameObject node = Instantiate(layer.nodePrefab, nodePos, Quaternion.identity, transform);
                node.name = $"Node_{layerIndex}_{nodeIndex}";
                layer.nodes.Add(node);
            }

            position += Vector2.right * layerDistance;
        }

        // Create connections
        for (int layerIndex = 0; layerIndex < layers.Count - 1; layerIndex++)
        {
            var currentLayer = layers[layerIndex].nodeCount;
            var nextLayer = layers[layerIndex + 1].nodeCount;

            for (int fromNode = 0; fromNode < currentLayer; fromNode++)
            {
                for (int toNode = 0; toNode < nextLayer; toNode++)
                {
                    float weight = network.GetWeight(layerIndex, fromNode, layerIndex + 1, toNode);
                    CreateConnection(
                        layers[layerIndex].nodes[fromNode],
                        layers[layerIndex + 1].nodes[toNode],
                        weight
                    );
                }
            }
        }
    }

    void UpdateVisualization()
    {
        if (network == null) return;

        // Update node visuals
        for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
        {
            var layer = layers[layerIndex];
            for (int nodeIndex = 0; nodeIndex < layer.nodes.Count; nodeIndex++)
            {
                var nodeVisual = layer.nodes[nodeIndex].GetComponent<NodeVisual>();
                if (nodeVisual != null)
                {
                    // Get activation from network node
                    float activation = GetNodeActivation(layerIndex, nodeIndex);
                    nodeVisual.activation = activation;
                    nodeVisual.UpdateVisual();
                }
            }
        }

        // Update connection weights
        foreach (var connection in connections)
        {
            float weight = GetConnectionWeight(connection.fromNode, connection.toNode);
            connection.weight = weight;
            UpdateConnectionVisual(connection);
        }
    }

    private float GetNodeActivation(int layerIndex, int nodeIndex)
    {
        // Get the actual node activation from your neural network
        // This will depend on how you expose this data in your NeuralNetwork class
        return network.GetNodeOutput(layerIndex, nodeIndex);
    }

    private float GetConnectionWeight(GameObject fromNode, GameObject toNode)
    {
        // Get the actual weight from your neural network
        // This will depend on how you expose this data in your NeuralNetwork class
        int fromLayer = GetNodeLayer(fromNode);
        int toLayer = GetNodeLayer(toNode);
        int fromIndex = GetNodeIndex(fromNode);
        int toIndex = GetNodeIndex(toNode);

        return network.GetWeight(fromLayer, fromIndex, toLayer, toIndex);
    }

    GameObject CreateConnection(GameObject fromNode, GameObject toNode, float weight)
    {
        GameObject connectionObj = new GameObject("Connection");
        connectionObj.transform.SetParent(transform);

        LineRenderer lineRenderer = connectionObj.AddComponent<LineRenderer>();
        lineRenderer.material = connectionMaterial;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        // Set sorting order to be in front of other objects
        lineRenderer.sortingOrder = 1;

        // Set positions
        lineRenderer.SetPosition(0, fromNode.transform.position);
        lineRenderer.SetPosition(1, toNode.transform.position);

        Connection connection = new Connection
        {
            lineRenderer = lineRenderer,
            weight = weight,
            fromNode = fromNode,
            toNode = toNode
        };
        connections.Add(connection);

        UpdateConnectionVisual(connection);
        return connectionObj;
    }

    void UpdateConnectionVisual(Connection connection)
    {
        UpdateWeightRange(connection.weight);

        // Use absolute weight value for line width
        float absWeight = Mathf.Abs(connection.weight);
        float lineWidth = Mathf.Lerp(minLineWidth, maxLineWidth, absWeight);
        connection.lineRenderer.startWidth = lineWidth;
        connection.lineRenderer.endWidth = lineWidth;

        // Pass the raw weight value to color interpolation
        Color connectionColor = InterpolateColors(connection.weight);
        connection.lineRenderer.startColor = connectionColor;
        connection.lineRenderer.endColor = connectionColor;

        // Debug output to help diagnose the issue
        if (Debug.isDebugBuild)
        {
            // Debug.Log($"Weight: {connection.weight}, Color: {connectionColor}");
        }
    }

    void UpdateAllConnections()
    {
        foreach (var connection in connections)
        {
            UpdateConnectionVisual(connection);
        }
    }

    public void Initialize(NeuralNetwork neuralNetwork)
    {
        network = neuralNetwork;

        // Clear existing visualization
        foreach (var layer in layers)
        {
            foreach (var node in layer.nodes)
            {
                Destroy(node);
            }
            layer.nodes.Clear();
        }

        foreach (var connection in connections)
        {
            Destroy(connection.lineRenderer.gameObject);
        }
        connections.Clear();

        // Store the node prefab before clearing layers
        var nodePrefab = layers.Count > 0 ? layers[0].nodePrefab : null;

        // Set up layers to match network architecture
        layers.Clear();
        for (int i = 0; i < network.LayerCount; i++)
        {
            var layer = new LayerVisualization
            {
                nodeCount = network.GetLayerNodes(i),
                nodePrefab = nodePrefab,
                spacing = 1f
            };
            layers.Add(layer);
        }

        // Create new visualization
        CreateVisualization();
    }

    private int GetNodeLayer(GameObject node)
    {
        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i].nodes.Contains(node))
                return i;
        }
        return -1;
    }

    private int GetNodeIndex(GameObject node)
    {
        var layer = layers.Find(l => l.nodes.Contains(node));
        if (layer != null)
            return layer.nodes.IndexOf(node);
        return -1;
    }

    public void Update()
    {
        if (network != null)
        {
            UpdateVisualization();
        }
    }

    private Color InterpolateColors(float value)
    {
        // Ensure we have at least 2 colors and matching threshold count
        if (colorGradient.Length < 2 || colorGradient.Length != colorThresholds.Length)
        {
            // Debug.LogError("Color gradient must have at least 2 colors and match threshold count");
            return Color.white;
        }

        // Handle edge cases
        if (value <= colorThresholds[0]) return colorGradient[0];
        if (value >= colorThresholds[colorThresholds.Length - 1]) return colorGradient[colorGradient.Length - 1];

        // Find the appropriate color segment
        int index = 0;
        for (int i = 0; i < colorThresholds.Length - 1; i++)
        {
            if (value >= colorThresholds[i] && value <= colorThresholds[i + 1])
            {
                index = i;
                break;
            }
        }

        // Calculate interpolation factor with optional power curve for more distinct transitions
        float t = Mathf.InverseLerp(colorThresholds[index], colorThresholds[index + 1], value);
        t = Mathf.Pow(t, 0.7f); // This makes the transitions more distinct

        // Interpolate between the two colors
        return Color.Lerp(colorGradient[index], colorGradient[index + 1], t);
    }

    private void UpdateWeightRange(float weight)
    {
        minObservedWeight = Mathf.Min(minObservedWeight, weight);
        maxObservedWeight = Mathf.Max(maxObservedWeight, weight);

        if (Debug.isDebugBuild)
        {
            // Debug.Log($"Weight Range: [{minObservedWeight}, {maxObservedWeight}]");
        }
    }
}