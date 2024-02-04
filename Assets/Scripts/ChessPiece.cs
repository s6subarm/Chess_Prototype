using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    public enum PieceType
    {
        None,
        Knight,
        Queen,
        // Add other types as needed
    }

    public PieceType type = PieceType.None;
}
