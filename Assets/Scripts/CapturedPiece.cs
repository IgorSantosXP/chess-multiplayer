using UnityEngine;

public class CapturedPiece : MonoBehaviour
{
    [SerializeField] private PieceType pieceType;
    [SerializeField] private int pieceCount;

    public PieceType GetPieceType() { 
        return pieceType;
    }

    public int GetPieceCount() {
        return pieceCount;
    }

    public void IncreasePieceCount() {
        pieceCount += 1;
    }

    public void ResetPieceCount() {
        pieceCount = 0;
    }
}
