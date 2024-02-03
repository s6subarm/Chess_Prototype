using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;


public class ChessBoard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material whiteTileMaterial;
    [SerializeField] private Material blackTileMaterial;
    [SerializeField] private Material highlightTileMaterial;
    [SerializeField] private GameObject prefabKnight;
    [SerializeField] private GameObject prefabQueen;

    // Logic
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover = -Vector2Int.one;
    private Plane chessboardPlane;

    private void Awake()
    {
        GenerateAllTiles(100f, TILE_COUNT_X, TILE_COUNT_Y);
        PlaceChessPiecesRandomly(); // Place the chess pieces after generating the board
        chessboardPlane = new Plane(Vector3.up, Vector3.zero); // Assuming the chessboard is at y=0
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        float enter;
        if (chessboardPlane.Raycast(ray, out enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector2Int tileIndex = CalculateTileFromHitPoint(hitPoint);
            // Check if the tile index is valid before highlighting
            if (tileIndex.x >= 0 && tileIndex.x < TILE_COUNT_X && tileIndex.y >= 0 && tileIndex.y < TILE_COUNT_Y)
            {
                if (currentHover != tileIndex)
                {
                    ResetHighlight(); // Reset the previous hover state
                    currentHover = tileIndex;
                    HighlightTile(currentHover); // Highlight the new tile
                }
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one) // If we have a tile currently highlighted
            {
                ResetHighlight(); // Reset the hover state
                currentHover = -Vector2Int.one; // No tile is currently hovered
            }
        }
    }



    private Vector2Int CalculateTileFromHitPoint(Vector3 hitPoint)
    {
        int x = Mathf.FloorToInt(hitPoint.x / 100f);
        int y = Mathf.FloorToInt(hitPoint.z / 100f); // Use z-coordinate because of Unity's 3D space conventions
        return new Vector2Int(x, y);
    }





    // Generating the chess board
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y, (x + y) % 2 == 0);
            }
        }
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y, bool isWhite)
    {
        GameObject tileObject = new GameObject($"X:{x}, Y:{y}");
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = isWhite ? whiteTileMaterial : blackTileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, 0, y * tileSize);
        vertices[1] = new Vector3(x * tileSize, 0, (y + 1) * tileSize);
        vertices[2] = new Vector3((x + 1) * tileSize, 0, y * tileSize);
        vertices[3] = new Vector3((x + 1) * tileSize, 0, (y + 1) * tileSize);

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>().size = new Vector3(1, 2f, 1); // Adjust collider height as needed

        return tileObject;
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (tiles[x, y] == hitInfo)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return -Vector2Int.one; // Invalid
    }

    private void HighlightTile(Vector2Int index)
    {
        Renderer renderer = tiles[index.x, index.y].GetComponent<Renderer>();
        renderer.material = highlightTileMaterial; // Change to highlight material
    }

    private void ResetHighlight()
    {
        if (currentHover.x < 0 || currentHover.y < 0) return; // Invalid index check
        Renderer renderer = tiles[currentHover.x, currentHover.y].GetComponent<Renderer>();
        renderer.material = ((currentHover.x + currentHover.y) % 2 == 0) ? whiteTileMaterial : blackTileMaterial; // Reset to original material
    }




    // Placing Chess Pieces Randomly
    private void PlaceChessPiecesRandomly()
    {
        // Place first chess piece randomly
        int x1 = Random.Range(0, TILE_COUNT_X);
        int y1 = Random.Range(0, TILE_COUNT_Y);
        PlaceChessPiece(prefabKnight, x1, y1);

        // Place second chess piece randomly, ensuring it's not placed on the same tile as the first
        int x2, y2;
        do
        {
            x2 = Random.Range(0, TILE_COUNT_X);
            y2 = Random.Range(0, TILE_COUNT_Y);
        } while (x2 == x1 && y2 == y1);

        PlaceChessPiece(prefabQueen, x2, y2);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        GameObject tile = tiles[x, y];
        Vector3 center = tile.GetComponent<Renderer>().bounds.center;
        return new Vector3(center.x, 0.5f, center.z); // Adjust the y-value as needed to raise the pieces off the tiles.
    }

    private void PlaceChessPiece(GameObject piecePrefab, int x, int y)
    {
        Vector3 position = GetTileCenter(x, y);
        // Create a rotation that rotates the prefab -90 degrees on the X axis.
        Quaternion rotation = Quaternion.Euler(-90, 0, 0);

        GameObject newPiece = Instantiate(piecePrefab, position, rotation, transform); // Parent to the chessboard for a cleaner hierarchy
    }


}
