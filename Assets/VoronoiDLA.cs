using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridVoronoiDLA : MonoBehaviour
{
    public int gridWidth = 64;
    public int gridHeight = 64;
    public int voronoiPoints = 10;
    public int dlaMaxCells = 100; // Max number of DLA cells allowed at a time
    public float dlaLifetime = 5.0f; // Time before a DLA cell disappears
    public GameObject cellPrefab;
    public GameObject dlaCellPrefab;

    private Image[,] grid; // Voronoi cells
    private List<GameObject> activeDLACells = new List<GameObject>(); // Track active DLA cells
    private int[,] regionIndex; // Voronoi region index for each cell
    private List<Vector2Int> voronoiCenters = new List<Vector2Int>();
    private Color[] voronoiColors;

    void Start()
    {
        InitializeGrid();
        InitializeVoronoi();
        //StartCoroutine(DLAGrowthLoop());
    }

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

    void InitializeVoronoi()
    {
        voronoiCenters.Clear();
        voronoiColors = new Color[voronoiPoints];

        // Generate Voronoi centers and colors
        for (int i = 0; i < voronoiPoints; i++)
        {
            Vector2Int point = new Vector2Int(Random.Range(0, gridWidth), Random.Range(0, gridHeight));
            voronoiCenters.Add(point);
            voronoiColors[i] = new Color(Random.value, Random.value, Random.value);
        }

        // Assign each grid cell to the closest Voronoi point
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);
                int closestIndex = FindClosestVoronoi(cellPos);

                grid[x, y].color = voronoiColors[closestIndex];
                regionIndex[x, y] = closestIndex; // Store Voronoi region index
            }
        }
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

    IEnumerator DLAGrowthLoop()
    {
        while (true)
        {
            AddNewDLACell();
            yield return new WaitForSeconds(0.01f); // Adjust growth speed here
        }
    }

    void AddNewDLACell()
    {
        // Randomly choose a Voronoi region to grow DLA in
        int regionIndex = Random.Range(0, voronoiCenters.Count);
        Vector2Int center = voronoiCenters[regionIndex];

        // Find a nearby position for the new DLA cell
        Vector2Int newPos = new Vector2Int(
            Mathf.Clamp(center.x + Random.Range(-2, 3), 0, gridWidth - 1),
            Mathf.Clamp(center.y + Random.Range(-2, 3), 0, gridHeight - 1)
        );

        // Instantiate DLA cell on top of the Voronoi cell
        GameObject dlaCellObj = Instantiate(dlaCellPrefab, transform);
        RectTransform rt = dlaCellObj.GetComponent<RectTransform>();
        rt.sizeDelta = grid[0, 0].rectTransform.sizeDelta;
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = grid[newPos.x, newPos.y].rectTransform.anchoredPosition;

        dlaCellObj.GetComponent<Image>().color = new Color(255, 255, 255, 0.5f); // Set opacity to 0.5

        // Add the DLA cell to the list of active cells
        activeDLACells.Add(dlaCellObj);

        // Limit the number of active DLA cells
        if (activeDLACells.Count > dlaMaxCells)
        {
            Destroy(activeDLACells[0]);
            activeDLACells.RemoveAt(0);
        }

        // Start coroutine to fade out and remove DLA cell after its lifetime
        StartCoroutine(RemoveDLACellAfterLifetime(dlaCellObj, dlaLifetime));
    }

    IEnumerator RemoveDLACellAfterLifetime(GameObject dlaCell, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);

        // Gradually fade out the DLA cell
        Image dlaImage = dlaCell.GetComponent<Image>();
        float fadeDuration = 1.0f;
        float fadeStart = Time.time;

        while (Time.time < fadeStart + fadeDuration)
        {
            float t = (Time.time - fadeStart) / fadeDuration;
            dlaImage.color = new Color(255, 255, 255, 0.5f * (1 - t)); // Reduce opacity to zero
            yield return null;
        }

        // Remove the cell completely
        Destroy(dlaCell);
        activeDLACells.Remove(dlaCell);
    }
}
