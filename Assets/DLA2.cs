using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DLA2 : MonoBehaviour
{
    [Header("DLA Settings")]
    public int radius = 50; // Radius for the circular boundary
    public int maxParticles = 1000; // Maximum number of particles to add
    public GameObject dlaCellPrefab; // Prefab for each particle

    private int[,] grid;
    private Vector2Int seedPosition;
    private int gridSize;
    private List<Vector2Int> directions = new List<Vector2Int> {
        Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down,
        new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(1, 1)
    };
    private int particleCount = 0;

    void Start()
    {
        InitializeDLA();
        StartCoroutine(GrowDLA());
    }

    void InitializeDLA()
    {
        // Set up the grid based on radius
        gridSize = radius * 2 + 5;
        grid = new int[gridSize, gridSize];

        // Set the seed position at the center
        seedPosition = new Vector2Int(radius + 2, radius + 2);
        grid[seedPosition.x, seedPosition.y] = 1; // Mark the seed in the grid
        CreateDLACell(seedPosition); // Create the initial seed particle

        // Set boundary cells as "out of bounds" (optional for visualization)
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (Vector2.Distance(new Vector2(seedPosition.x, seedPosition.y), new Vector2(x, y)) > radius)
                {
                    grid[x, y] = 2; // Mark as boundary (for reference)
                }
            }
        }
    }

    IEnumerator GrowDLA()
    {
        while (particleCount < maxParticles)
        {
            Vector2Int startPoint = RandomPointOnCircle(radius);
            bool foundFriend = false;
            Vector2Int position = startPoint;

            while (!foundFriend && WithinBounds(position))
            {
                // Check for neighbors and "stick" if close to a cluster particle
                if (IsNearCluster(position))
                {
                    grid[position.x, position.y] = 1;
                    CreateDLACell(position);
                    foundFriend = true;
                    particleCount++;

                    // Check if the particle reaches the circular boundary
                    if (Vector2.Distance(new Vector2(seedPosition.x, seedPosition.y), new Vector2(position.x, position.y)) >= radius - 1)
                    {
                        yield break; // Stop growth if cluster reaches the boundary
                    }
                }
                else
                {
                    // Random walk
                    position += directions[Random.Range(0, directions.Count)];
                }
                yield return null;
            }
        }
    }

    Vector2Int RandomPointOnCircle(int radius)
    {
        float theta = Random.Range(0f, Mathf.PI * 2);
        int x = Mathf.RoundToInt(seedPosition.x + radius * Mathf.Cos(theta));
        int y = Mathf.RoundToInt(seedPosition.y + radius * Mathf.Sin(theta));
        return new Vector2Int(x, y);
    }

    bool WithinBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridSize && pos.y >= 0 && pos.y < gridSize;
    }

    bool IsNearCluster(Vector2Int pos)
    {
        foreach (var dir in directions)
        {
            Vector2Int neighbor = pos + dir;
            if (WithinBounds(neighbor) && grid[neighbor.x, neighbor.y] == 1)
            {
                return true; // Found an adjacent cluster cell
            }
        }
        return false;
    }

    void CreateDLACell(Vector2Int gridPosition)
    {
        GameObject dlaCell = Instantiate(dlaCellPrefab, transform);
        RectTransform rt = dlaCell.GetComponent<RectTransform>();

        // Position DLA cell based on grid position
        float cellSize = Screen.width / gridSize;
        rt.sizeDelta = new Vector2(cellSize, cellSize);
        rt.anchoredPosition = new Vector2(gridPosition.x * cellSize, gridPosition.y * cellSize);

        // Set color to white for visibility
        Image cellImage = dlaCell.GetComponent<Image>();
        cellImage.color = new Color(1, 1, 1, 0.5f);
    }
}
