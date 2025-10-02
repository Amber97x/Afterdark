// Using directives
using UnityEngine;

// Grid highlighter that updates grid tile atlas rects depending on the current mouse position
public class GridHoverHighlighter : MonoBehaviour
{
    // Required attributes
    [SerializeField] private Camera sceneCamera; // Scene camera (Map editor camera)
    [SerializeField] private GridOverlay grid; // Grid overlay

    // Private attributes
    private Vector2Int? lastTile; // Last modified tile
    private AtlasRect lastTilePrevRect; // Atlas rect for the last modified tile

    // Resets the required attributes
    private void Reset()
    {
        if (!sceneCamera) sceneCamera = Camera.main;
        if (!grid) grid = GetComponent<GridOverlay>();
    }

    // Called once per frame
    void Update()
    {
        // Escape early if required attributes are not available
        if (!sceneCamera || !grid) return;

        // Raycast mouse → grid plane
        Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
        Plane gridPlane = grid.GetGridPlane();

        // Try to raycast hit the grid plane using the mouse position
        if (!gridPlane.Raycast(ray, out float dist))
        {
            // Didn't hit grid plane, clear the last modified tile
            ClearLastIfAny();
            return;
        }

        // Convert raycast to hit position
        Vector3 hitPosition = ray.GetPoint(dist);

        // Try to get the tile at the hit location
        if (!grid.TryWorldToTile(hitPosition, out var tile))
        {
            // Didn't hit any tile, clear the last modified tile
            ClearLastIfAny();
            return;
        }

        // If tile changed, revert the last highlighted tile and apply to new one
        if (!lastTile.HasValue || lastTile.Value != tile)
        {
            // Revert previous
            if (lastTile.HasValue)
                grid.SetTileRect(lastTile.Value, lastTilePrevRect);

            // Cache new tile’s previous rect, then set highlight
            if (!grid.TryGetTileRect(tile, out lastTilePrevRect))
            {
                // fallback
                lastTilePrevRect = Atlas.Blank; 
            }

            // Highlight tile
            grid.SetHighlighted(tile);

            // Record last modified tile
            lastTile = tile;
        }
    }

    // Clears the last modified tile if it has a value
    private void ClearLastIfAny()
    {
        // If the last tile has a value
        if (lastTile.HasValue)
        {
            // Clear the tile
            grid.SetTileRect(lastTile.Value, lastTilePrevRect);

            // Set the last tile to null (Avoid unnecessary clearing)
            lastTile = null;
        }
    }
}
