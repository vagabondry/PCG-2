using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Combined : MonoBehaviour
{
    [Header("Voronoi Settings")]
    public int gridWidth = 64; // Grid width
    public int gridHeight = 64; // Grid height
    public int voronoiPoints = 10; // Number of Voronoi centers
    public GameObject cellPrefab; // Prefab for Voronoi cells
    public RectTransform canvas; // The canvas to which cells are added

    [Header("DLA Settings")]
    public int dlaRadius = 20; // The radius around each Voronoi center for DLA growth
    public int maxIterations = 10000; // The maximum number of random walker iterations

    private Image[,] grid; // 2D array to store grid cells
    private List<Vector2Int> voronoiCenters = new List<Vector2Int>(); // List to store Voronoi center positions
    private List<Color> voronoiColors = new List<Color>(); // List to store Voronoi region colors

    void Start()
    {
        InitializeGrid();  // Initialize the grid of cells
        InitializeVoronoi();  // Initialize Voronoi diagram
        StartDLAInRegions();  // Start the DLA growth process in each region
    }

    // Initializes the grid and instantiates the cells on the canvas
    void InitializeGrid()
    {
        grid = new Image[gridWidth, gridHeight];  // Create 2D grid for storing Image components
        float cellSizeX = canvas.rect.width / gridWidth;  // Calculate the width of each cell
        float cellSizeY = canvas.rect.height / gridHeight;  // Calculate the height of each cell

        // Loop through the grid and instantiate cells
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GameObject cellObj = Instantiate(cellPrefab, canvas);  // Instantiate cell prefab
                RectTransform rt = cellObj.GetComponent<RectTransform>();  // Get the RectTransform for positioning
                rt.sizeDelta = new Vector2(cellSizeX, cellSizeY);  // Set size of the cell
                rt.anchorMin = Vector2.zero;  // Anchor at the bottom-left
                rt.anchorMax = Vector2.zero;
                rt.pivot = Vector2.zero;
                rt.anchoredPosition = new Vector2(x * cellSizeX, y * cellSizeY); // Position cell on canvas

                Image cellImage = cellObj.GetComponent<Image>();  // Get the Image component to apply colors
                grid[x, y] = cellImage;  // Store reference to the cell's image
            }
        }
    }

    // Initializes Voronoi diagram by randomly placing centers and colors
    void InitializeVoronoi()
    {
        voronoiCenters.Clear();  // Clear any previous Voronoi centers

        // Generate random centers and colors for Voronoi regions
        for (int i = 0; i < voronoiPoints; i++)
        {
            Vector2Int center = new Vector2Int(Random.Range(0, gridWidth), Random.Range(0, gridHeight));  // Random center position
            voronoiCenters.Add(center);  // Add center to list
            voronoiColors.Add(new Color(Random.Range(0.5f, 1f), Random.Range(0.5f, 1f), Random.Range(0.5f, 1f), 1f));  // Assign random pastel color
        }

        // Assign each grid cell to the nearest Voronoi center
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);  // Current cell position
                int closestIndex = FindClosestVoronoi(cellPos);  // Find the closest Voronoi center
                grid[x, y].color = voronoiColors[closestIndex];  // Set the cell color based on the closest Voronoi center
            }
        }
    }

    // Finds the closest Voronoi center for a given grid cell position
    int FindClosestVoronoi(Vector2Int cellPos)
    {
        float minDistance = float.MaxValue;  // Start with a large minimum distance
        int closestIndex = 0;

        // Compare distances to all Voronoi centers
        for (int i = 0; i < voronoiCenters.Count; i++)
        {
            float distance = Vector2Int.Distance(cellPos, voronoiCenters[i]);  // Calculate distance to the Voronoi center
            if (distance < minDistance)  // If the distance is smaller, update the closest center
            {
                minDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;  // Return index of the closest Voronoi center
    }

    // Starts the DLA process in each Voronoi region
    void StartDLAInRegions()
    {
        for (int i = 0; i < voronoiCenters.Count; i++)
        {
            Vector2Int center = voronoiCenters[i];
            StartCoroutine(GrowDLA(center, i));  // Start the DLA growth process for each Voronoi center
        }
    }

    // Simulates the growth of a DLA cluster within the region
    IEnumerator GrowDLA(Vector2Int seedCenter, int regionIndex)
    {
        int squareSize = dlaRadius * 2 + 5;  // The size of the square matrix for DLA
        int seedX = dlaRadius + 2;  // Initial position of the seed in the matrix
        int seedY = dlaRadius + 2;
        int[,] matrix = new int[squareSize, squareSize];  // The matrix representing the DLA grid
        List<GameObject> cells = new List<GameObject>();  // List of cells for rendering the cluster

        // Initialize matrix with seed and boundary conditions
        for (int row = 0; row < squareSize; row++)
        {
            for (int col = 0; col < squareSize; col++)
            {
                if (row == seedY && col == seedX)
                    matrix[row, col] = 1;  // Mark the seed particle
                else if (Mathf.Sqrt(Mathf.Pow(seedX - col, 2) + Mathf.Pow(seedY - row, 2)) > dlaRadius)
                    matrix[row, col] = 2;  // Mark the boundary of the cluster
            }
        }

        // Create visual cells for DLA cluster rendering
        for (int row = 0; row < squareSize; row++)
        {
            for (int col = 0; col < squareSize; col++)
            {
                GameObject cell = Instantiate(cellPrefab, canvas);
                RectTransform rt = cell.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2((seedCenter.x - dlaRadius + col) * 10, (seedCenter.y - dlaRadius + row) * 10);
                rt.sizeDelta = new Vector2(10, 10);
                cells.Add(cell);
            }
        }

        int addedCount = 0;
        int randomWalkersCount = 0;
        bool completeCluster = false;

        // Run the DLA process until the cluster is complete or max iterations are reached
        while (!completeCluster && randomWalkersCount < maxIterations)
        {
            randomWalkersCount++;
            Vector2Int location = RandomAtRadius(dlaRadius, seedX, seedY);  // Random walker starts within radius
            bool foundFriend = false;
            bool nearEdge = false;

            // Random walker moves until it finds a neighbor (the cluster) or reaches the edge
            while (!foundFriend && !nearEdge)
            {
                (location, foundFriend, nearEdge) = CheckAround(location, matrix, squareSize);  // Check surrounding cells
                if (foundFriend)
                {
                    matrix[location.y, location.x] = 1;  // Stick to the cluster
                    addedCount++;
                }
            }

            if (foundFriend && IsTouchingBoundary(location, squareSize))  // Complete the cluster if a walker reaches the boundary
                completeCluster = true;

            RenderCluster(matrix, cells, squareSize);  // Update visual representation of the cluster
            yield return null;  // Yield to simulate growth over time
        }
    }

    // Renders the cluster in the UI grid
    void RenderCluster(int[,] matrix, List<GameObject> cells, int squareSize)
    {
        for (int row = 0; row < squareSize; row++)
        {
            for (int col = 0; col < squareSize; col++)
            {
                int value = matrix[row, col];
                Color color = (value == 1) ? Color.white : Color.clear;  // White for cluster cells, clear for empty ones
                cells[row * squareSize + col].GetComponent<Image>().color = color;  // Set the color of the visual cell
            }
        }
    }

    // Checks the surrounding cells for a neighboring cluster or edge
    (Vector2Int, bool, bool) CheckAround(Vector2Int location, int[,] matrix, int squareSize)
    {
        bool foundFriend = false;
        bool nearEdge = location.x <= 1 || location.x >= squareSize - 1 || location.y <= 1 || location.y >= squareSize - 1;

        // If not near edge, check for adjacent cells in the cluster
        if (!nearEdge)
        {
            if (matrix[location.y + 1, location.x] == 1 || matrix[location.y - 1, location.x] == 1 ||
                matrix[location.y, location.x + 1] == 1 || matrix[location.y, location.x - 1] == 1)
                foundFriend = true;
        }

        // Move the walker randomly if no neighbor found
        if (!foundFriend && !nearEdge)
        {
            location += new Vector2Int(Random.Range(-1, 2), Random.Range(-1, 2));
            location.x = Mathf.Clamp(location.x, 0, squareSize - 1);
            location.y = Mathf.Clamp(location.y, 0, squareSize - 1);
        }

        return (location, foundFriend, nearEdge);  // Return updated location and status
    }

    // Generates a random position within the radius of the Voronoi center
    Vector2Int RandomAtRadius(int radius, int seedX, int seedY)
    {
        float theta = Random.Range(0f, Mathf.PI * 2);  // Random angle
        int x = Mathf.RoundToInt(radius * Mathf.Cos(theta)) + seedX;  // X position
        int y = Mathf.RoundToInt(radius * Mathf.Sin(theta)) + seedY;  // Y position
        return new Vector2Int(x, y);  // Return random position
    }

    // Checks if the walker is touching the boundary of the DLA matrix
    bool IsTouchingBoundary(Vector2Int location, int squareSize)
    {
        return location.x <= 1 || location.x >= squareSize - 1 || location.y <= 1 || location.y >= squareSize - 1;
    }
}
