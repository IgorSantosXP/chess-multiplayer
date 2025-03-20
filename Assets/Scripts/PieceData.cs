using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewChessPiece", menuName = "Chess/Piece Data")]
public class PieceData : ScriptableObject
{
    public Sprite sprite;
    public PieceType pieceType;
    public PlayerType playerType;
    public Vector2Int[] possibleSpawns;
    public bool isSlidingPiece;
}

public enum PieceType {
    None,
    Pawn, 
    Rook, 
    Knight,
    Bishop, 
    Queen,
    King
}

public enum PlayerType {
    None,
    White,
    Black,
}
