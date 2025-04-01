using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Piece : NetworkBehaviour
{
    private BoardManager boardManager;
    private PieceData pieceData;
    private Vector2Int boardPosition;
    private bool hasMoved = false;
    private List<Vector2Int> directions = new List<Vector2Int>();
    private List<Vector2Int> possibleMoves = new List<Vector2Int>();

    private void Start() {
        boardManager = BoardManager.Instance;
    }

    [ClientRpc]
    public void InitializeClientRpc(string dataName, Vector2Int startPos) {
        pieceData = Resources.Load<PieceData>($"PieceData/{dataName}");

        if (pieceData == null) {
            return;
        }

        if (GameManager.Instance.GetLocalPlayerType() == PlayerType.Black) {
            transform.rotation = Quaternion.Euler(0, 0, 180);
        }

        boardPosition = startPos;
        PlayerTheme playerTheme = ProfileManager.Instance.GetPlayerTheme();
        GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"Themes/{playerTheme}/Pieces/{pieceData.playerType}{pieceData.pieceType}");
        SetPossibleMoves();
    }

    public void MoveTo(Vector2Int newPos) {
        boardPosition = newPos;
        hasMoved = true;
    }

    public PieceType GetPieceType() {
        if (pieceData != null)
            return pieceData.pieceType;
        return PieceType.None;
    }

    public PlayerType GetPlayerType() {
        if (pieceData != null)
            return pieceData.playerType;
        return PlayerType.None;
    }

    public Vector2Int GetBoardPosition() {
        return boardPosition;
    }

    private void SetPossibleMoves() {

        switch (pieceData.pieceType) {
            case PieceType.Pawn:
                SetPawnDirections();
                break;
            case PieceType.Rook:
                SetHorizontalAndVerticalDirections();
                break;
            case PieceType.Knight:
                SetKnightDirections();
                break;
            case PieceType.Bishop:
                SetDiagonalDirections();
                break;
            case PieceType.Queen:
                SetHorizontalAndVerticalDirections();
                SetDiagonalDirections();
                break;
            case PieceType.King:
                SetHorizontalAndVerticalDirections();
                SetDiagonalDirections();
                break;
        }
    }

    private void SetHorizontalAndVerticalDirections() {
        directions.Add(Vector2Int.up);
        directions.Add(Vector2Int.down);
        directions.Add(Vector2Int.left);
        directions.Add(Vector2Int.right);
    }

    private void SetDiagonalDirections() {
        directions.Add(new Vector2Int(1, 1));
        directions.Add(new Vector2Int(1, -1));
        directions.Add(new Vector2Int(-1, 1));
        directions.Add(new Vector2Int(-1, -1));
    }

    private void SetKnightDirections() {
        directions.Add(new Vector2Int(2, 1));
        directions.Add(new Vector2Int(2, -1));
        directions.Add(new Vector2Int(-2, 1));
        directions.Add(new Vector2Int(-2, -1));
        directions.Add(new Vector2Int(1, 2));
        directions.Add(new Vector2Int(1, -2));
        directions.Add(new Vector2Int(-1, 2));
        directions.Add(new Vector2Int(-1, -2));
    }

    private void SetPawnDirections() {
        if (pieceData.playerType == PlayerType.White) {
            directions.Add(new Vector2Int(0, 1));
            directions.Add(new Vector2Int(0, 2));
            directions.Add(new Vector2Int(1, 1));
            directions.Add(new Vector2Int(-1, 1));
            return;
        }
        directions.Add(new Vector2Int(0, -1));
        directions.Add(new Vector2Int(0, -2));
        directions.Add(new Vector2Int(1, -1));
        directions.Add(new Vector2Int(-1, -1));
    }

    public List<Vector2Int> GetPiecePossibleMoves(Piece[,] piecesOnBoard) {
        possibleMoves.Clear();
        
        foreach (Vector2Int dir in directions) {
            CheckDirection(dir, piecesOnBoard);
        }

        return possibleMoves;
    }

    public List<Vector2Int> AddCastlingMoves(Piece[,] piecesOnBoard) {
        int y = pieceData.playerType == PlayerType.White ? 0 : 7;

        CheckKingsideCastling(piecesOnBoard, y);

        CheckQueensideCastling(piecesOnBoard, y);

        return possibleMoves;
    }

    private void CheckKingsideCastling(Piece[,] piecesOnBoard, int y) {
        Vector2Int rookPosition = new Vector2Int(7, y);
        CheckCastling(piecesOnBoard, rookPosition, new[] { 5, 6 }, 6, y);
    }

    private void CheckQueensideCastling(Piece[,] piecesOnBoard, int y) {
        Vector2Int rookPosition = new Vector2Int(0, y);
        CheckCastling(piecesOnBoard, rookPosition, new[] { 1, 2, 3 }, 2, y);
    }

    private void CheckCastling(Piece[,] piecesOnBoard, Vector2Int rookPos, int[] xPositions, int kingTargetX, int y) {
        Piece rook = piecesOnBoard[rookPos.x, rookPos.y];

        if (rook == null || rook.GetPieceType() != PieceType.Rook || rook.hasMoved) return;

        if (boardManager.IsSquareUnderAttack(new Vector2Int(4, y), pieceData.playerType)) return;

        foreach (var x in xPositions)
        {
            if (piecesOnBoard[x, y] != null) return;
            if (boardManager.IsSquareUnderAttack(new Vector2Int(x, y), pieceData.playerType)) return;
        }

        possibleMoves.Add(new Vector2Int(kingTargetX, y));
    }

    private void CheckDirection(Vector2Int direction, Piece[,] piecesOnBoard) {
        Vector2Int newPosition = boardPosition + direction;

        while (IsWithinBounds(newPosition)) {
            if (pieceData.pieceType == PieceType.Pawn) {
                CheckPawnDirection(newPosition);
                break;
            }
            Piece piece = piecesOnBoard[newPosition.x, newPosition.y];
            if (piece != null) {
                if (IsEnemy(piece)) {
                    possibleMoves.Add(newPosition);
                }
                break;
            }
            possibleMoves.Add(newPosition);

            if (!pieceData.isSlidingPiece) {
                break;
            }
            newPosition += direction;
        }
    }

    private void CheckPawnDirection(Vector2Int newPosition) {
        Vector2Int forwardPosition = boardPosition;
        Vector2Int secondPosition = boardPosition;

        if (pieceData.playerType == PlayerType.White) {
            forwardPosition += new Vector2Int(0, 1);
            secondPosition += new Vector2Int(0, 2);
        } else {
            forwardPosition += new Vector2Int(0, -1);
            secondPosition += new Vector2Int(0, -2);
        }

        List<Vector2Int> forwardPositions = new List<Vector2Int> {
            forwardPosition,
            secondPosition
        };

        Piece piece = boardManager.GetPieceAtPosition(newPosition);

        if (!forwardPositions.Contains(newPosition)) {
            if (piece != null) {
                if (forwardPositions.Contains(newPosition)) return;
                if (IsEnemy(piece)) {
                    possibleMoves.Add(newPosition);
                }
                return;
            }

            if (newPosition == boardManager.GetLastPawnDoubleStepCapturePosition()) {
                Piece targetPositionPiece = boardManager.GetLastPawnDoubleStepPiece();
                if (targetPositionPiece != null && targetPositionPiece.GetPlayerType() != GetPlayerType()) {
                    possibleMoves.Add(newPosition);
                }
            }
        }

        if (piece != null) return;
        if (forwardPositions.Contains(newPosition)) {
            if (forwardPosition == newPosition) {
                possibleMoves.Add(newPosition);
                return;
            }
            if (!hasMoved) {
                Piece forwardPiece = boardManager.GetPieceAtPosition(forwardPosition);
                if (forwardPiece != null) return;

                possibleMoves.Add(newPosition);
            }
        }
    }

    private bool IsEnemy(Piece piece) {
        return pieceData.playerType != piece.pieceData.playerType;
    }

    private bool IsWithinBounds(Vector2Int position) {
        int maxBoardSize = 8;
        int minBoardSize = 0;
        if (position.x >= minBoardSize && position.x < maxBoardSize && position.y >= minBoardSize && position.y < maxBoardSize) {
            return true;
        }
        return false;
    }

    public PieceData GetPieceData() {
        return pieceData;
    }

    public bool GetHasMoved() { 
        return hasMoved;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void UpdatePieceDataRpc(string pieceDataName) {
        pieceData = Resources.Load<PieceData>($"PieceData/{pieceDataName}");
        PlayerTheme playerTheme = ProfileManager.Instance.GetPlayerTheme();
        GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"Themes/{playerTheme}/Pieces/{pieceData.playerType}{pieceData.pieceType}");
        SetPossibleMoves();
    }

    public void UpdatePieceDataLocal(string pieceDataName) {
        pieceData = Resources.Load<PieceData>($"PieceData/{pieceDataName}");
        PlayerTheme playerTheme = ProfileManager.Instance.GetPlayerTheme();
        GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"Themes/{playerTheme}/Pieces/{pieceData.playerType}{pieceData.pieceType}");
        SetPossibleMoves();
    }
}
