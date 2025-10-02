// Using directives
using System.Collections.Generic;
using UnityEngine;

public class GridOverlay : MonoBehaviour
{
    [Header("Grid Attributes")]
    [SerializeField] private int columns = 64; // number of colums in the grid (horizontal)
    [SerializeField] private int rows = 64; // number of rows in the grid (vertical)
    [SerializeField] private float tileSize = 1.0f; // scale of each grid tile
    [SerializeField] private float yOffset = 0.001f; // prevent clipping with ground
    [SerializeField] private Material material; // Material to apply to the grid

    // Grid state variables
    private Mesh mesh; // Grid mesh object
    private List<Vector3> vertices = new List<Vector3>(); // List of all the vertices for the mesh
    private List<int> triangles = new List<int>(); // List of all the triangles for the mesh (indexes of vertices to create triangles)
    private List<Vector2> uvs = new List<Vector2>(); // List of UV data for the mesh
    private float width = 0.0f; // Width of the grid
    private float height = 0.0f; // Height of the grid
    private float halfSize = 0.5f; // Half size for a tile on the grid

    // Dictionary for quick UV changes
    private Dictionary<Vector2Int, int[]> tileUvIndices = new();

    // Dictionary for tracking tile atlas rect states
    private Dictionary<Vector2Int, AtlasRect> tileCurrentRect = new();

    // Initialization function (Called automatically when script instantiates)
    private void Awake()
    {
        //Initialize
        mesh = new Mesh { name = "GridOverlay" }; // Create mesh with name GridOverlay
        mesh.MarkDynamic(); // Mark the mesh as dynamic
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>(); // Create mesh filter component on the parent game object
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>(); // Create mesh renderer component on the parent game object
        meshFilter.sharedMesh = mesh; // Configure the mesh filter
        meshRenderer.sharedMaterial = material; // Configure the mesh renderer
        Build(); // Build the grid
    }

    // Override for default build function
    private void Build() => Build(Atlas.Blank);

    // Builds the grid (Creates vertice/triangle/uv data and updates the grid mesh)
    private void Build(AtlasRect defaultTileRect)
    {
        // Clear existing grid data
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        tileUvIndices.Clear();
        tileCurrentRect.Clear();

        // Record grid width/height & half size
        width = columns * tileSize;
        height = rows * tileSize;
        halfSize = tileSize * 0.5f;

        // Grid build loop
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                // Calculate tile center position
                Vector3 tileCenter = new Vector3(column * tileSize + halfSize, yOffset, row * tileSize + halfSize);

                // Read vertice count
                int v0 = vertices.Count;

                // Create grid tile vertices
                vertices.Add(tileCenter + new Vector3(-halfSize, 0.0f, -halfSize)); // Bottom left
                vertices.Add(tileCenter + new Vector3(halfSize, 0.0f, -halfSize)); // Bottom right
                vertices.Add(tileCenter + new Vector3(halfSize, 0.0f, halfSize)); // Top right
                vertices.Add(tileCenter + new Vector3(-halfSize, 0.0f, halfSize)); // Top left

                // Create default UV data for the grid tile
                WriteQuadUVs(v0, defaultTileRect);

                // Create triangle / indicies data for the grid tile
                // (flip winding so faces point upward)
                triangles.Add(v0);     // Bottom left
                triangles.Add(v0 + 2); // Top right
                triangles.Add(v0 + 1); // Bottom right
                triangles.Add(v0);     // Bottom left
                triangles.Add(v0 + 3); // Top left
                triangles.Add(v0 + 2); // Top right

                // Create UV indicies data for the grid tile
                tileUvIndices[new Vector2Int(column, row)] = new int[]
                {
                    v0,     // Bottom left
                    v0 + 1, // Bottom right
                    v0 + 2, // Top right
                    v0 + 3  // Top left
                };

                // Record tile atlas rect state
                tileCurrentRect[new Vector2Int(column, row)] = defaultTileRect;
            }
        }

        // Update grid mesh
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals(); // safe; helps if you ever use lit mats
    }

    // Writes UV data for a specified grid tile at base index so it displays the correct part of the atlas texture
    private void WriteQuadUVs(int baseIndex, AtlasRect atlasRect)
    {
        // Make sure there is enough space for the UV data
        EnsureUVCapacity(baseIndex + 4);

        // Map atlas rect to the quad
        uvs[baseIndex] = new Vector2(atlasRect.uvMin.x, atlasRect.uvMin.y); // Bottom left
        uvs[baseIndex + 1] = new Vector2(atlasRect.uvMax.x, atlasRect.uvMin.y); // Bottom right
        uvs[baseIndex + 2] = new Vector2(atlasRect.uvMax.x, atlasRect.uvMax.y); // Top right
        uvs[baseIndex + 3] = new Vector2(atlasRect.uvMin.x, atlasRect.uvMax.y); // Top left
    }

    // Ensures the uvs list count is at least as much as the specified required count
    private void EnsureUVCapacity(int requiredCount)
    {
        // Until the uvs list has a count more than the required count
        while (uvs.Count < requiredCount)
        {
            // Add uv to the uvs list
            uvs.Add(Vector2.zero);
        }
    }

    // Set the specified grid tile to display the specified atlas rect
    public void SetTileRect(Vector2Int tile, AtlasRect atlasRect)
    {
        // Get the tiles base index and exit early if the tile doesn't exist
        if (!tileUvIndices.TryGetValue(tile, out var idx)) return;

        // Write the UV data for the specified tile so it displays the atlas rect
        WriteQuadUVs(idx[0], atlasRect);

        // Record tile state
        tileCurrentRect[tile] = atlasRect;

        // Push UV data to the grid mesh
        mesh.SetUVs(0, uvs);
    }

    // Safe method for getting the current atlas rect for a given tile in the grid
    public bool TryGetTileRect(Vector2Int tile, out AtlasRect rect)
    {
        // Try to get the current atlas rect for the given tile and return if true/false depending on whether successful
        return tileCurrentRect.TryGetValue(tile, out rect);
    }

    // Safe method for getting a grid tile based on world position
    public bool TryWorldToTile(Vector3 worldPosition, out Vector2Int tile)
    {
        // Transform world position to local position on the grid
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

        // If the position is not within the bounds of the grid
        if (localPosition.x < 0.0f || localPosition.z < 0.0f || localPosition.x >= width || localPosition.z >= height)
        {
            // Use default tile
            tile = default;

            // Failed to get the tile at world position
            return false;
        }

        // Calculate the tile column and row
        int tileColumn = Mathf.FloorToInt(localPosition.x / tileSize);
        int tileRow = Mathf.FloorToInt(localPosition.z / tileSize);

        // Get the tile
        tile = new Vector2Int(tileColumn, tileRow);

        // Successfully retrieved the tile at world position
        return true;
    }

    // Returns the grid plane (Use for raycasts)
    public Plane GetGridPlane()
    {
        // Return the grid plane
        return new Plane(transform.up, transform.TransformPoint(new Vector3(0.0f, yOffset, 0.0f)));
    }

    // Returns the center position for the specified tile in world coordinates
    public Vector3 GetTileCenterWorld(Vector2Int tile)
    {
        // Calculate and return the center position of the specified tile in world coordinates
        return transform.TransformPoint(new Vector3(tile.x * tileSize + halfSize, yOffset, tile.y * tileSize + halfSize));
    }

    // Named shortcuts
    public void SetBlank(Vector2Int tile) => SetTileRect(tile, Atlas.Blank);
    public void SetHighlighted(Vector2Int tile) => SetTileRect(tile, Atlas.Highlighted);
    public void SetSelected(Vector2Int tile) => SetTileRect(tile, Atlas.Selected);
    public void SetOccupied(Vector2Int tile) => SetTileRect(tile, Atlas.Occupied);
    public void SetEdgeTop(Vector2Int tile) => SetTileRect(tile, Atlas.EdgeTop);
    public void SetEdgeRight(Vector2Int tile) => SetTileRect(tile, Atlas.EdgeRight);
    public void SetEdgeBottom(Vector2Int tile) => SetTileRect(tile, Atlas.EdgeBottom);
    public void SetEdgeLeft(Vector2Int tile) => SetTileRect(tile, Atlas.EdgeLeft);
    public void SetVertexTopLeft(Vector2Int tile) => SetTileRect(tile, Atlas.VertexTopLeft);
    public void SetVertexTopRight(Vector2Int tile) => SetTileRect(tile, Atlas.VertexTopRight);
    public void SetVertexBottomRight(Vector2Int tile) => SetTileRect(tile, Atlas.VertexBottomRight);
    public void SetVertexBottomLeft(Vector2Int tile) => SetTileRect(tile, Atlas.VertexBottomLeft);
    public void SetReserved1(Vector2Int tile) => SetTileRect(tile, Atlas.Reserved1);
    public void SetReserved2(Vector2Int tile) => SetTileRect(tile, Atlas.Reserved2);
    public void SetReserved3(Vector2Int tile) => SetTileRect(tile, Atlas.Reserved3);
    public void SetReserved4(Vector2Int tile) => SetTileRect(tile, Atlas.Reserved4);
}
