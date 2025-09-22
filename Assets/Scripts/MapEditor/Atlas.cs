// Using directives
using UnityEngine;

// Rect object for storing uv data for a texture atlas
public struct AtlasRect
{
    public Vector2 uvMin;
    public Vector2 uvMax;
}

// Helper class for simplifying use of texture atlases
public class Atlas
{
    // Creates a AtlasRect containing uv data for the specified index in a texture atlas
    public static AtlasRect Cell(int index, int cols = 4, int rows = 4)
    {
        int x = index % cols;
        int y = index / cols;
        float width = 1.0f / cols;
        float height = 1.0f / rows;
        var min = new Vector2(x * width, 1.0f - (y + 1) * height);
        var max = new Vector2((x + 1) * width, 1.0f - y * height);
        return new AtlasRect { uvMin = min, uvMax = max };
    }

    // Named shortcuts for grid overlay
    public static readonly AtlasRect Blank = Cell(0);
    public static readonly AtlasRect Highlighted = Cell(1);
    public static readonly AtlasRect Selected = Cell(2);
    public static readonly AtlasRect Occupied = Cell(3);
    public static readonly AtlasRect EdgeTop = Cell(4);
    public static readonly AtlasRect EdgeRight = Cell(5);
    public static readonly AtlasRect EdgeBottom = Cell(6);
    public static readonly AtlasRect EdgeLeft = Cell(7);
    public static readonly AtlasRect VertexTopLeft = Cell(8);
    public static readonly AtlasRect VertexTopRight = Cell(9);
    public static readonly AtlasRect VertexBottomRight = Cell(10);
    public static readonly AtlasRect VertexBottomLeft = Cell(11);
    public static readonly AtlasRect Reserved1 = Cell(12);
    public static readonly AtlasRect Reserved2 = Cell(13);
    public static readonly AtlasRect Reserved3 = Cell(14);
    public static readonly AtlasRect Reserved4 = Cell(15);
}
