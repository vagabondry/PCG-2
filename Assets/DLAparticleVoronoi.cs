using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CircularDLAGrowth : MonoBehaviour
{
    [Header("Voronoi Settings")]
    public int gridWidth = 64;
    public int gridHeight = 64;
    public int voronoiPoints = 10;
    public GameObject cellPrefab; // Prefab for Voronoi cells
    public GameObject dlaCellPrefab; // Prefab for DLA cells

    [Header("DLA Settings")]
    public int dlaMaxRadius = 15; // Maximum radius for DLA growth within each region
    public int dlaMaxCellsPerRegion = 500; // Maximum number of DLA cells per region

    private Image[,] grid; // 2D array representing the grid
    private int[,] regionIndex; // Tracks which Voronoi region each cell belongs to
    private List<Vector2Int> voronoiCenters = new List<Vector2Int>(); // Centers of Voronoi regions
    private Color[] voronoiColors; // Colors for Voronoi regions

    void Start()
    {
        InitializeGrid();
        InitializeVoronoi();
        StartDLAGrowthInRegions();
    }

    // Step 1: Initialize the grid
    void InitializeGrid()
    {
        grid = new Image[gridWidth, gridHeight];
        regionIndex = new int[gridWidth, gridHeight];

        float cellSizeX = Screen.width / gridWidth;
        float cellSizeY = Screen.height / gridHeight;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GameObject cellObj = Instantiate(cellPrefab, transform);
                RectTransform rt = cellObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(cellSizeX, cellSizeY);
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0, 0);
                rt.anchoredPosition = new Vector2(x * cellSizeX, y * cellSizeY);

                Image cellImage = cellObj.GetComponent<Image>();
                grid[x, y] = cellImage;
            }
        }
    }

    // Step 2: Initialize Voronoi centers and assign each cell to the nearest center
    void InitializeVoronoi()
    {
        voronoiCenters.Clear();
        voronoiColors = new Color[voronoiPoints];

        // Generate Voronoi centers and pastel colors
        for (int i = 0; i < voronoiPoints; i++)
        {
            Vector2Int point = new Vector2Int(Random.Range(0, gridWidth), Random.Range(0, gridHeight));
            voronoiCenters.Add(point);
            voronoiColors[i] = GeneratePastelColor();
        }

        // Assign each cell to the closest Voronoi center
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);
                int closestIndex = FindClosestVoronoi(cellPos);

                grid[x, y].color = voronoiColors[closestIndex];
                regionIndex[x, y] = closestIndex;
            }
        }
    }

    Color GeneratePastelColor()
    {
        return new Color(
            Random.Range(0.5f, 1f),
            Random.Range(0.5f, 1f),
            Random.Range(0.5f, 1f),
            1f
        );
    }

    int FindClosestVoronoi(Vector2Int cellPos)
    {
        float minDistance = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < voronoiCenters.Count; i++)
        {
            float distance = Vector2Int.Distance(cellPos, voronoiCenters[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    // Step 3: Start DLA growth in each Voronoi region as a separate coroutine
    void StartDLAGrowthInRegions()
    {
        for (int i = 0; i < voronoiPoints; i++)
        {
            CreateDLACell(voronoiCenters[i]);
            StartCoroutine(GrowDLAInRegion(i));
        }
    }

    IEnumerator GrowDLAInRegion(int regionIndex)
    {
        Vector2Int center = voronoiCenters[regionIndex];
        HashSet<Vector2Int> clusterCells = new HashSet<Vector2Int>(); // Track positions in the cluster
        clusterCells.Add(center); // Initial seed cell

        int cellCount = 1;

        while (cellCount < dlaMaxCellsPerRegion && clusterCells.Count < dlaMaxCellsPerRegion)
        {
            Vector2Int startPoint = RandomPointOnCircle(dlaMaxRadius / 2, center); // Start near center
            bool foundFriend = false;
            Vector2Int position = startPoint;

            while (!foundFriend && Vector2Int.Distance(position, center) < dlaMaxRadius)
            {
                // Check if this position is near the cluster
                foreach (Vector2Int dir in GetRandomizedDirections())
                {
                    Vector2Int neighbor = position + dir;
                    if (clusterCells.Contains(neighbor))
                    {
                        foundFriend = true;
                        break;
                    }
                }

                if (foundFriend)
                {
                    // Add the cell to the cluster
                    clusterCells.Add(position);
                    CreateDLACell(position);
                    cellCount++;
                }
                else
                {
                    // Move the particle randomly
                    position += GetRandomDirection();
                }

                yield return new WaitForSeconds(0.01f); // Adjust growth speed here
            }
        }
    }

    Vector2Int RandomPointOnCircle(int radius, Vector2Int center)
    {
        float theta = Random.Range(0f, Mathf.PI * 2);
        int x = Mathf.RoundToInt(center.x + radius * Mathf.Cos(theta));
        int y = Mathf.RoundToInt(center.y + radius * Mathf.Sin(theta));
        return new Vector2Int(x, y);
    }

    // Get a random direction for particle movement
    Vector2Int GetRandomDirection()
    {
        List<Vector2Int> directions = new List<Vector2Int> {
            Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down
        };
        return directions[Random.Range(0, directions.Count)];
    }

    // Get randomized directions to prevent bias in checking order
    List<Vector2Int> GetRandomizedDirections()
    {
        List<Vector2Int> directions = new List<Vector2Int> {
            Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down
        };
        for (int i = 0; i < directions.Count; i++)
        {
            Vector2Int temp = directions[i];
            int randomIndex = Random.Range(i, directions.Count);
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }
        return directions;
    }

    // Create a new DLA cell at the given grid position
    void CreateDLACell(Vector2Int gridPosition)
    {
        GameObject dlaCellObj = Instantiate(dlaCellPrefab, transform);
        RectTransform rt = dlaCellObj.GetComponent<RectTransform>();
        rt.sizeDelta = grid[0, 0].rectTransform.sizeDelta;
        rt.anchoredPosition = grid[gridPosition.x, gridPosition.y].rectTransform.anchoredPosition;

        Image dlaImage = dlaCellObj.GetComponent<Image>();
        dlaImage.color = Color.white; // Set color for DLA cell
    }
}
