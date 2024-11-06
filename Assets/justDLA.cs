using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class justDLA : MonoBehaviour
{
    public int radius = 50;
    public bool needGif = false;
    public int maxIterations = 400000;
    public RectTransform canvas; // Assign your Canvas Transform in Unity Inspector
    public GameObject cellPrefab; // Prefab with Image component to act as cells in the grid

    private int squareSize;
    private int seedX, seedY;
    private int[,] matrix;
    private List<GameObject> cells = new List<GameObject>();

    void Start()
    {
        InitializeDLA();
        RunDLA();
        RenderCluster();
    }

    void InitializeDLA()
    {
        squareSize = radius * 2 + 5;
        seedX = radius + 2;
        seedY = radius + 2;

        matrix = new int[squareSize, squareSize];

        // Initialize matrix with seed and border
        for (int row = 0; row < squareSize; row++)
        {
            for (int col = 0; col < squareSize; col++)
            {
                if (row == seedY && col == seedX)
                    matrix[row, col] = 1; // Seed particle
                else if (Mathf.Sqrt(Mathf.Pow(seedX - col, 2) + Mathf.Pow(seedY - row, 2)) > radius)
                    matrix[row, col] = 2; // Outside circle
            }
        }

        // Create cell objects
        for (int row = 0; row < squareSize; row++)
        {
            for (int col = 0; col < squareSize; col++)
            {
                GameObject cell = Instantiate(cellPrefab, canvas);
                cell.GetComponent<RectTransform>().anchoredPosition = new Vector2(col * 10, -row * 10);
                cells.Add(cell);
            }
        }
    }

    void RunDLA()
    {
        int addedCount = 0;
        int randomWalkersCount = 0;
        bool completeCluster = false;

        while (!completeCluster && randomWalkersCount < maxIterations)
        {
            randomWalkersCount++;
            Vector2Int location = RandomAtRadius(radius, seedX, seedY);
            bool foundFriend = false;
            bool nearEdge = false;

            // Run random walker until it sticks to the cluster or reaches an edge
            while (!foundFriend && !nearEdge)
            {
                (location, foundFriend, nearEdge) = CheckAround(location);
                if (foundFriend)
                {
                    matrix[location.y, location.x] = 1;
                    addedCount++;
                }
            }

            if (foundFriend && IsTouchingBoundary(location))
                completeCluster = true;
        }
    }

    void RenderCluster()
    {
        for (int row = 0; row < squareSize; row++)
        {
            for (int col = 0; col < squareSize; col++)
            {
                int value = matrix[row, col];
                Color color = (value == 1) ? Color.white : ((value == 2) ? Color.black : Color.blue);
                cells[row * squareSize + col].GetComponent<Image>().color = color;
            }
        }
    }

    (Vector2Int, bool, bool) CheckAround(Vector2Int location)
    {
        bool foundFriend = false;
        bool nearEdge = location.x <= 1 || location.x >= squareSize - 1 || location.y <= 1 || location.y >= squareSize - 1;

        if (!nearEdge)
        {
            if (matrix[location.y + 1, location.x] == 1 || matrix[location.y - 1, location.x] == 1 ||
                matrix[location.y, location.x + 1] == 1 || matrix[location.y, location.x - 1] == 1)
                foundFriend = true;
        }

        if (!foundFriend && !nearEdge)
        {
            location += new Vector2Int(Random.Range(-1, 2), Random.Range(-1, 2));
            location.x = Mathf.Clamp(location.x, 0, squareSize - 1);
            location.y = Mathf.Clamp(location.y, 0, squareSize - 1);
        }

        return (location, foundFriend, nearEdge);
    }

    Vector2Int RandomAtRadius(int radius, int seedX, int seedY)
    {
        float theta = Random.Range(0f, Mathf.PI * 2);
        int x = Mathf.RoundToInt(radius * Mathf.Cos(theta)) + seedX;
        int y = Mathf.RoundToInt(radius * Mathf.Sin(theta)) + seedY;
        return new Vector2Int(x, y);
    }

    bool IsTouchingBoundary(Vector2Int location)
    {
        return location.x <= 1 || location.x >= squareSize - 1 || location.y <= 1 || location.y >= squareSize - 1;
    }
}
