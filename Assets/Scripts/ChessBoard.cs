using System.Collections.Generic;
using UnityEngine;



public class ChessBoard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material whiteTileMaterial;
    [SerializeField] private Material blackTileMaterial;
    [SerializeField] private Material highlightTileMaterial;
    [SerializeField] private GameObject prefabKnight;
    [SerializeField] private GameObject prefabQueen;
    //[SerializeField] private LayerMask pieceLayerMask;
    [SerializeField] private float tileSize = 100f;


    // Initialize all necessary variables
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover = -Vector2Int.one;
    private Plane chessboardPlane;
    private GameObject selectedPiece = null;
    private GameObject[,] chessPiecesLocations = new GameObject[TILE_COUNT_X, TILE_COUNT_Y];
    private List<Vector2Int> validMoves = new List<Vector2Int>();


    private void Awake()
    {
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
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

        // Update logic for highlighting tiles while hovering cursor
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
                    // highlighting hovering only when a piece is not selected
                    if (selectedPiece == null)
                    {
                        ResetHighlight(); // Reset the previous hover state only if no piece is selected
                        currentHover = tileIndex;
                        HighlightTile(currentHover); // Highlight the new tile only if no piece is selected

                    }
                }
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one && selectedPiece == null) // If we have a tile currently highlighted and no piece is selected
            {
                ResetHighlight(); // Reset the hover state
                currentHover = -Vector2Int.one; // No tile is currently hovered
            }
        }

        // Update logic for selecting pieces and moving them
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity))
            {
                //UnityEngine.Debug.Log("Hit: " + hitInfo.collider.gameObject.name);
                GameObject clickedObject = hitInfo.collider.gameObject;
                if (clickedObject.layer == LayerMask.NameToLayer("ChessPiece"))
                {
                    ResetHighlight();
                    ResetValidMovesHighlights();
                    if (selectedPiece != clickedObject)
                    {
                        selectedPiece = clickedObject; // Select the piece
                        //UnityEngine.Debug.Log("Selected " + selectedPiece.name);
                        ShowValidMoves(); // This will be a method you implement to show valid moves for the selected piece
                    }
                    else
                    {
                        selectedPiece = null; // Deselect if the same piece is clicked again
                        //UnityEngine.Debug.Log("Deselected ");
                    }
                }
                
            }
            else
            {
                // Attempt to move the selected piece to the valid selected tile
                if (selectedPiece != null)
                {
                    Vector3 hitPoint = ray.GetPoint(enter); // Re-calculate the hitPoint for this context
                    Vector2Int clickedTileIndex = CalculateTileFromHitPoint(hitPoint);
                    //UnityEngine.Debug.Log("Attempted Move To: " + clickedTileIndex);

                    // Check if the clicked tile is one of the highlighted valid moves.
                    if (validMoves.Contains(clickedTileIndex))
                    {
                        MoveChessPiece(selectedPiece, clickedTileIndex);
                        ResetHighlight();
                        ResetValidMovesHighlights();
                        selectedPiece = null; // Clear the selected piece after moving
                    }
                    else
                    {
                        // Clicked outside a valid move, just deselect
                        ResetHighlight();
                        ResetValidMovesHighlights();
                        selectedPiece = null;
                    }
                }
            }
        }
    }




    /*
    // Generating the chess board
    */

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




    /*
    // Basic multi-purpose fuctions 
    */

    private Vector2Int CalculateTileFromHitPoint(Vector3 hitPoint)
    {
        int x = Mathf.FloorToInt(hitPoint.x / tileSize);
        int y = Mathf.FloorToInt(hitPoint.z / tileSize); // Use z-coordinate because of Unity's 3D space conventions
        return new Vector2Int(x, y);
    }

    private bool IsTileOccupied(Vector2Int position)
    {
        return chessPiecesLocations[position.x, position.y] != null;
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
        chessPiecesLocations[x, y] = newPiece; // Track the piece's position on the board
    }

    private Vector2Int GetPiecePosition(GameObject piece)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPiecesLocations[x, y] == piece)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return -Vector2Int.one; // Return an invalid position if not found
    }

    private void RemoveChessPiece(Vector2Int position)
    {
        if (position.x >= 0 && position.x < TILE_COUNT_X && position.y >= 0 && position.y < TILE_COUNT_Y)
        {
            GameObject pieceToRemove = chessPiecesLocations[position.x, position.y];
            if (pieceToRemove != null)
            {
                Destroy(pieceToRemove); // Destroy the GameObject
                chessPiecesLocations[position.x, position.y] = null; // Clear the reference from the array
            }
        }
    }

    private GameObject DeterminePiecePrefab(GameObject piece)
    {
        // Check the tag of the piece to determine its type
        switch (piece.tag)
        {
            case "Knight":
                return prefabKnight;
            case "Queen":
                return prefabQueen;
            // Add more cases as needed for other piece types
            default:
                Debug.LogError("Unknown piece type: " + piece.tag);
                return null; // Return null if the piece type is unrecognized
        }
    }


    private void MoveChessPiece(GameObject piece, Vector2Int newPosition)
    {
        Vector2Int oldPosition = GetPiecePosition(piece);

        // Calculate new position in world space and move the piece
        Vector3 newPositionWorld = GetTileCenter(newPosition.x, newPosition.y);
        piece.transform.position = newPositionWorld;

        // Update the board state
        chessPiecesLocations[oldPosition.x, oldPosition.y] = null;
        chessPiecesLocations[newPosition.x, newPosition.y] = piece;
    }




    /*
    // Highlighting tiles of the chess board
    */

    private void HighlightTile(Vector2Int index)
    {

        // Change the material to the highlight material
        Renderer renderer = tiles[index.x, index.y].GetComponent<Renderer>();
        renderer.material = highlightTileMaterial;
        
    }

    private void ResetHighlight()
    {
        if (currentHover.x < 0 || currentHover.y < 0) return; // Invalid index check
        Renderer renderer = tiles[currentHover.x, currentHover.y].GetComponent<Renderer>();
        renderer.material = ((currentHover.x + currentHover.y) % 2 == 0) ? whiteTileMaterial : blackTileMaterial; // Reset to original material
    }

    private void ResetValidMovesHighlights()
    {
        foreach (var move in validMoves)
        {
            if (move.x >= 0 && move.x < TILE_COUNT_X && move.y >= 0 && move.y < TILE_COUNT_Y)
            {
                Renderer renderer = tiles[move.x, move.y].GetComponent<Renderer>();
                renderer.material = ((move.x + move.y) % 2 == 0) ? whiteTileMaterial : blackTileMaterial;
            }
        }
        validMoves.Clear(); // Clear the list after resetting the highlights
    }




    /*
    // Placing Chess Pieces Randomly
    */

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




    /*
    // Valid Moves
    */

    private List<Vector2Int> GetValidMovesForKnight(Vector2Int position)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        //UnityEngine.Debug.Log("Knight position: " + position);

        // Knight's potential moves in "L" shapes
        int[,] offsets = new int[,] { { 1, 2 }, { 2, 1 }, { -1, 2 }, { -2, 1 }, { -1, -2 }, { -2, -1 }, { 1, -2 }, { 2, -1 } };

        for (int i = 0; i < 8; i++)
        {
            int newX = position.x + offsets[i, 0];
            int newY = position.y + offsets[i, 1];

            if (newX >= 0 && newX < TILE_COUNT_X && newY >= 0 && newY < TILE_COUNT_Y)
            {
                if (!IsTileOccupied(new Vector2Int(newX, newY)))
                {
                    // TODO: Add additional logic to differentiate between ally and enemy pieces
                    //UnityEngine.Debug.Log("Valid move for knight: (" + newX + ", " + newY + ")");
                    moves.Add(new Vector2Int(newX, newY));
                }
            }
        }

        if (moves.Count == 0)
        {
            UnityEngine.Debug.LogError("No valid moves found for knight at position: " + position);
        }

        return moves;
    }

    private List<Vector2Int> GetValidMovesForQueen(Vector2Int position)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        //UnityEngine.Debug.Log("Queen position:" + position);

        Vector2Int[] directions = new Vector2Int[] {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), // Horizontal
        new Vector2Int(0, 1), new Vector2Int(0, -1), // Vertical
        new Vector2Int(1, 1), new Vector2Int(-1, -1), // Diagonal
        new Vector2Int(-1, 1), new Vector2Int(1, -1)  // Diagonal
        };

        foreach (var dir in directions)
        {
            Vector2Int nextPosition = position + dir;

            while (nextPosition.x >= 0 && nextPosition.x < TILE_COUNT_X && nextPosition.y >= 0 && nextPosition.y < TILE_COUNT_Y)
            {
                if (!IsTileOccupied(nextPosition))
                {
                    // TODO: Add additional logic to differentiate between ally and enemy pieces
                    moves.Add(nextPosition);     
                }
                else 
                    break; // Stop at the first occupied tile

                moves.Add(nextPosition);

                nextPosition += dir;
            }
        }

        if (moves.Count == 0)
        {
            UnityEngine.Debug.LogError("No valid moves found for Queen at position: " + position);
        }
        else
        {
            foreach (var move in moves)
            {
                //UnityEngine.Debug.Log("Valid move for Queen: (" + move.x + ", " + move.y + ")");
            }
        }


        return moves;
    }

    private void ShowValidMoves()
    {
        validMoves.Clear(); // Clear previous valid moves

        if (!selectedPiece) return; // No piece is selected

        // Assuming each piece has a tag that is either "Knight" or "Queen"
        Vector2Int selectedPiecePosition = GetPiecePosition(selectedPiece);
        //UnityEngine.Debug.Log("Selected piece position after sending it to LookupTileIndex:" + selectedPiecePosition);

        if (selectedPiece.CompareTag("Knight"))
        {
            validMoves = GetValidMovesForKnight(selectedPiecePosition);
        }
        else if (selectedPiece.CompareTag("Queen"))
        {
            validMoves = GetValidMovesForQueen(selectedPiecePosition);
        }

        // Highlight valid moves
        foreach (var move in validMoves)
        {
            HighlightTile(move);
        }
    }



}
