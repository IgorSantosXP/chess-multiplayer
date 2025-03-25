using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class PromotionWindowManager : MonoBehaviour
{
    [SerializeField] private Transform boardTransform;
    [SerializeField] private PieceData[] whitePieces;
    [SerializeField] private PieceData[] blackPieces;
    [SerializeField] private GameObject piecePrefab;
    [SerializeField] private GameObject squareBackgroundPrefab;

    private Vector2[,] gridPositions = new Vector2[1, 4];
    private PromotionPiece[,] piecesToPromove = new PromotionPiece[1, 4];
    private GameObject promotionWindowBackground;

    private float squareSize;
    private float boardSize;
    private float squareBackgroundSize;
    private int boardLength = 8;
    private int xLength = 1;
    private int yLength = 4;

    public void Initialize(PlayerType playerType, Vector2 startPosition) {
        SetGridSizes();
        CalculateGrid(playerType, startPosition);
        SpawnPieces(playerType);
    }

    private void SetGridSizes() {
        SpriteRenderer boardRenderer = boardTransform.GetComponent<SpriteRenderer>();
        boardSize = boardRenderer.bounds.size.x;
        squareSize = boardSize / boardLength;

        SpriteRenderer backgroundRenderer = squareBackgroundPrefab.GetComponent<SpriteRenderer>();
        float backgroundRendererSize = backgroundRenderer.bounds.size.x;
        squareBackgroundSize = squareSize / backgroundRendererSize;
    }

    private void CalculateGrid(PlayerType playerType, Vector2 startPosition) {

        float startX = startPosition.x;
        float startY = startPosition.y;

        for (int x = 0; x < xLength; x++) {
            for (int y = 0; y < yLength; y++) {
                float yPosition = playerType == PlayerType.White ? startY - y * squareSize : startY + y * squareSize;
                Vector2 position = new Vector2(startX + x * squareSize, yPosition);
                gridPositions[x, y] = position;
            }
        }

        Vector2 newPosition = new Vector2(startX, playerType == PlayerType.White ? startY - 1.8f : startY + 1.8f);
        CreateSquareBackground(newPosition);
    }

    void CreateSquareBackground(Vector2 position) {
        GameObject background = Instantiate(squareBackgroundPrefab, position, Quaternion.identity, transform);
        background.transform.localScale = new Vector2(squareBackgroundSize * xLength, squareBackgroundSize * yLength);
        promotionWindowBackground = background;
    }

    void SpawnPieces(PlayerType playerType) {
        if (playerType == PlayerType.White) {
            for (int i = 0; i < whitePieces.Length; i++) {
                SpawnPiece(whitePieces[i], i);
            }
        }

        if (playerType == PlayerType.Black) {
            for (int i = 0; i < blackPieces.Length; i++) {
                SpawnPiece(blackPieces[i], i);
            }
        }        
    }

    void SpawnPiece(PieceData data, int yPosition) {
        Vector2 position = gridPositions[0, yPosition];
        GameObject newPiece = Instantiate(piecePrefab, position, Quaternion.identity, promotionWindowBackground.transform);
        float scaleX = newPiece.transform.localScale.x / promotionWindowBackground.transform.localScale.x;
        float scaleY = newPiece.transform.localScale.y / promotionWindowBackground.transform.localScale.y;
        newPiece.transform.localScale = new Vector2(scaleX, scaleY);

        PromotionPiece promotionPiece = newPiece.GetComponent<PromotionPiece>();
        promotionPiece.Initialize(data.name);

        piecesToPromove[0, yPosition] = promotionPiece;
    }

    public PromotionPiece GetPromotionPiece(int x, int y) {
        int promotionWindowX = Mathf.FloorToInt((transform.position.x + (4 * squareSize)) / squareSize);
        int promotionPieceY = y;

        if (promotionWindowX == x) {
            if (GameManager.Instance.GetLocalPlayerType() == PlayerType.White) {
                promotionPieceY = 3 - (promotionPieceY - 4);
            }
            if (promotionPieceY >= 0 && promotionPieceY < 4) {
                PromotionPiece promotionPiece = piecesToPromove[0, promotionPieceY];
                if (promotionPiece != null) {
                    return promotionPiece;
                }
            }
        }
        return null;
    }
}
