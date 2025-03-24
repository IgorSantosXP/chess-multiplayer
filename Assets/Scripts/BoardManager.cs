using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class BoardManager : NetworkBehaviour
{
    public static BoardManager Instance;
    [SerializeField] private Transform boardTransform;
    [SerializeField] private GameObject squareOverlayPrefab;
    [SerializeField] private GameObject hintOverlayPrefab;
    [SerializeField] private GameObject captureHintOverlayPrefab;
    [SerializeField] private GameObject piecePrefab;
    [SerializeField] private GameObject promotionWindowPrefab;
    [SerializeField] private PieceData[] whitePieces;
    [SerializeField] private PieceData[] blackPieces;

    private GameObject promotionWindow;
    private GameManager gameManager;
    
    private GameObject[,] squareOverlays = new GameObject[8, 8];
    private GameObject[,] hintOverlays = new GameObject[8, 8];
    private GameObject[,] captureHintOverlays = new GameObject[8, 8];
    private Piece[,] piecesOnBoard = new Piece[8, 8];
    private Vector2[,] gridPositions = new Vector2[8, 8];

    private List<GameObject> hintOverlaysArray = new List<GameObject>();
    private List<GameObject> captureHintOverlaysArray = new List<GameObject>();
    private List<Vector2Int> possibleMoves = new List<Vector2Int>();

    private float squareSize;
    private float boardSize;
    private float overlaySize;
    private int boardLength = 8;
    private bool isPromotionWindowOpen;
    private bool isGameRunning;

    private bool isDragging = false;
    private Piece currentDraggingPiece;
    private Vector2Int startDragPosition;
    private Vector3 originalPosition;

    private Vector2Int selectedPiecePosition;
    private Vector2Int pieceStartPosition;
    private Vector2Int pieceEndPosition;
    private Vector2Int lastPawnDoubleStepCapturePosition;
    private Vector2Int lastPawnDoubleStepPosition;
    
    public event EventHandler OnPieceMove;
    public event Action<string, string> OnEndGame;

    private void Awake() {
        Instance = this;
    }

    void Start() {
        SetBoardSizes();
        CalculateGrid();
        gameManager = GameManager.Instance;
        gameManager.OnGameStarted += GameManager_OnGameStarted;
        gameManager.OnEndGame += GameManager_OnEndGame;
    }

    void Update() {
        if (!isGameRunning) return;
        if (Input.GetMouseButtonDown(0)) {
            DetectSquareClick();
        }

        if (isDragging) {
            DuringDrag();
        }

        if (Input.GetMouseButtonUp(0)) {
            EndDrag();
        }
    }

    private void GameManager_OnEndGame(string title, string text) {
        isGameRunning = false;
    }

    private void GameManager_OnGameStarted(object sender, EventArgs e) {
        ResetDefaultValues();
        isGameRunning = true;

        if (NetworkManager.Singleton.IsServer) {
            StartCoroutine(ClearAndSetPiecesOnBoardCoroutine());
        }
    }

    private void ResetDefaultValues() {
        ClearHighLightedSquares();
        ClearHighLightedHint();
        ClearHighLightedCaptureHint();
        DestroyPromotionWindow();
        removeSelectedSquareOverlay();
        selectedPiecePosition = new Vector2Int();
        pieceStartPosition = new Vector2Int();
        pieceEndPosition = new Vector2Int();
        lastPawnDoubleStepCapturePosition = new Vector2Int();
        lastPawnDoubleStepPosition = new Vector2Int();
    }

    private IEnumerator ClearAndSetPiecesOnBoardCoroutine() {
        List<NetworkObject> objectsToDespawn = new List<NetworkObject>();
        foreach (Piece piece in piecesOnBoard)
        {
            if (piece != null) {
                NetworkObject pieceObj = piece.GetComponent<NetworkObject>();
                objectsToDespawn.Add(pieceObj);
            }
        }
        foreach (var obj in objectsToDespawn) {
            obj.Despawn();
        }
        yield return new WaitForEndOfFrame();
        SyncPiecesOnBoardRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SyncPiecesOnBoardRpc() {
        piecesOnBoard = new Piece[8, 8];
        SpawnPieces();
    }

    private void SetBoardSizes() {
        SpriteRenderer boardRenderer = boardTransform.GetComponent<SpriteRenderer>();
        boardSize = boardRenderer.bounds.size.x;
        squareSize = boardSize / boardLength;

        SpriteRenderer overlayRenderer = squareOverlayPrefab.GetComponent<SpriteRenderer>();
        float overlayRendererSize = overlayRenderer.bounds.size.x;
        overlaySize = squareSize / overlayRendererSize;
    }

    void CalculateGrid() {
        float startX = -boardSize / 2 + squareSize / 2;
        float startY = -boardSize / 2 + squareSize / 2;

        for (int x = 0; x < boardLength; x++) {
            for (int y = 0; y < boardLength; y++) {
                Vector2 position = new Vector2(startX + x * squareSize, startY + y * squareSize);

                gridPositions[x, y] = position;
                CreateSquareOverlays(position, x, y);
                CreateHintOverlays(position, x, y);
                CreateCaptureHintOverlays(position, x, y);
            }
        }
    }

    void CreateSquareOverlays(Vector2 position, int x, int y) {
        GameObject overlay = Instantiate(squareOverlayPrefab, position, Quaternion.identity);
        overlay.transform.localScale = new Vector2(overlaySize, overlaySize);
        overlay.SetActive(false);
        squareOverlays[x, y] = overlay;
    }

    void CreateHintOverlays(Vector2 position, int x, int y) {
        GameObject overlay = Instantiate(hintOverlayPrefab, position, Quaternion.identity);
        overlay.SetActive(false);
        hintOverlays[x, y] = overlay;
    }

    void CreateCaptureHintOverlays(Vector2 position, int x, int y) {
        GameObject overlay = Instantiate(captureHintOverlayPrefab, position, Quaternion.identity);
        overlay.SetActive(false);
        captureHintOverlays[x, y] = overlay;
    }

    private Vector2Int GetGridPosition(Vector2 worldPosition) {
        int x = Mathf.FloorToInt((worldPosition.x + (4 * squareSize)) / squareSize);
        int y = Mathf.FloorToInt((worldPosition.y + (4 * squareSize)) / squareSize);
        return new Vector2Int(x, y);
    }

    private bool IsOutOfBoard(Vector2Int gridPos) {
        if (gridPos.x < 0 || gridPos.x >= boardLength || gridPos.y < 0 || gridPos.y >= boardLength) return true;
        return false;
    }

    void DetectSquareClick() {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int gridPos = GetGridPosition(mouseWorldPos);

        if (isPromotionWindowOpen) {
            CheckPromotionClick(gridPos.x, gridPos.y);
        }

        DestroyPromotionWindow();
        ClearHighLightedHint();
        ClearHighLightedCaptureHint();
        if (!IsOutOfBoard(gridPos)) {
            Piece clickedPiece = piecesOnBoard[gridPos.x, gridPos.y];
            if (clickedPiece != null) {
                if (!possibleMoves.Contains(gridPos)) {
                    HighlightSelectedSquare(gridPos.x, gridPos.y);
                }
                if (clickedPiece.GetPieceData().playerType == gameManager.GetLocalPlayerType()) {
                    isDragging = true;
                    currentDraggingPiece = clickedPiece;
                    startDragPosition = gridPos;
                    originalPosition = currentDraggingPiece.transform.position;
                    ShowPossibleMoves(clickedPiece, gridPos);
                    currentDraggingPiece.GetComponent<SpriteRenderer>().sortingOrder = 21;
                    return;
                }
            } 

            if (gameManager.GetCurrentPlayablePlayerType() == gameManager.GetLocalPlayerType()) {
                if (possibleMoves.Contains(gridPos)) {
                    Piece selectedPiece = piecesOnBoard[selectedPiecePosition.x, selectedPiecePosition.y];
                    if (selectedPiece != null) {
                        if (selectedPiece.GetPieceType() == PieceType.Pawn &&
                            (gridPos.y == 7 || gridPos.y == 0)) {
                            ShowPromotionWindow(selectedPiece.GetPieceData().playerType, gridPos);
                        } else {
                            RequestMoveServerRpc(selectedPiecePosition, gridPos, isDragging);
                        }
                    }
                }
            }
        }
        removeSelectedSquareOverlay();
        possibleMoves.Clear();
    }

    private void DuringDrag() {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currentDraggingPiece.transform.position = mouseWorldPos;
    }

    private void EndDrag() {
        if (!isDragging) return;
        if (gameManager.GetCurrentPlayablePlayerType() == gameManager.GetLocalPlayerType()) {
            currentDraggingPiece.GetComponent<SpriteRenderer>().sortingOrder = 20;

            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int targetGridPos = GetGridPosition(mouseWorldPos);

            if (possibleMoves.Contains(targetGridPos)) {
                if (currentDraggingPiece.GetPieceType() == PieceType.Pawn &&
                    (targetGridPos.y == 7 || targetGridPos.y == 0)) {
                    ShowPromotionWindow(currentDraggingPiece.GetPieceData().playerType, targetGridPos);
                } else {
                    RequestMoveServerRpc(startDragPosition, targetGridPos, isDragging);
                }
            } else {
                if (currentDraggingPiece != null) {
                    currentDraggingPiece.transform.position = originalPosition;
                    currentDraggingPiece = null;
                }
            }
        } else {
            if (currentDraggingPiece != null) {
                currentDraggingPiece.transform.position = originalPosition;
                currentDraggingPiece = null;
            }
        }

        isDragging = false;
    }

    private void CheckPromotionClick(int x, int y) {
        PromotionWindowManager promotionWindowManager = promotionWindow.GetComponent<PromotionWindowManager>();
        PromotionPiece selectedPromotionPiece = promotionWindowManager.GetPromotionPiece(x, y);
        if (selectedPromotionPiece != null) {
            int clickedPositionX = Mathf.FloorToInt((promotionWindowManager.transform.position.x + (4 * squareSize)) / squareSize);
            int clickedPositionY = Mathf.FloorToInt((promotionWindowManager.transform.position.y + (4 * squareSize)) / squareSize);
            Vector2Int clickedPosition = new Vector2Int(clickedPositionX, clickedPositionY);
            RequestMoveServerRpc(selectedPiecePosition, clickedPosition, isDragging, selectedPromotionPiece.GetPieceData().name);
            return;
        }
        if (currentDraggingPiece != null) {
            currentDraggingPiece.transform.position = originalPosition;
            currentDraggingPiece = null;
        }
    }

    private void ShowPossibleMoves(Piece clickedPiece, Vector2Int clickedPosition) {
        possibleMoves = clickedPiece.GetPiecePossibleMoves(piecesOnBoard);
        if (clickedPiece.GetPieceData().pieceType == PieceType.King && !clickedPiece.GetHasMoved()) {
            possibleMoves = clickedPiece.AddCastlingMoves(piecesOnBoard);
        }
        PreventCheck(clickedPosition, clickedPiece.GetPieceData().playerType);
        foreach (Vector2Int possibleMove in possibleMoves) {
            if (piecesOnBoard[possibleMove.x, possibleMove.y] != null) {
                HighlightCaptureHint(possibleMove.x, possibleMove.y);
                continue;
            }
            HighlightHint(possibleMove.x, possibleMove.y);
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void RequestMoveServerRpc(Vector2Int start, Vector2Int target, bool isDragging, string pieceDataName = null) {
        if (IsValidMove(start, target)) {
            UpdateMovePositionsRpc(start, target, pieceDataName, isDragging);
            return;
        }
        if (currentDraggingPiece != null) {
            currentDraggingPiece.transform.position = originalPosition;
            currentDraggingPiece = null;
        }
    }

    private bool IsValidMove(Vector2Int start, Vector2Int target) {
        Piece piece = piecesOnBoard[start.x, start.y];
        possibleMoves = piece.GetPiecePossibleMoves(piecesOnBoard);
        if (piece.GetPieceData().pieceType == PieceType.King && !piece.GetHasMoved()) {
            possibleMoves = piece.AddCastlingMoves(piecesOnBoard);
        }
        PreventCheck(start, piece.GetPieceData().playerType);

        return possibleMoves.Contains(target);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateMovePositionsRpc(Vector2Int start, Vector2Int target, string pieceDataName, bool isDragging) {
        selectedPiecePosition = new Vector2Int();
        Piece piece = piecesOnBoard[start.x, start.y];

        if (piece.GetPieceData().pieceType == PieceType.King && Math.Abs(target.x - start.x) == 2) {
            int y = piece.GetPieceData().playerType == PlayerType.White ? 0 : 7;
            int rookStartX = target.x > 4 ? 7 : 0;
            int rookTargetX = target.x > 4 ? 5 : 3;
            StartCoroutine(MoveToPosition(new Vector2Int(rookStartX, y), new Vector2Int(rookTargetX, y), true, pieceDataName, false));
        }
        StartCoroutine(MoveToPosition(start, target, false, pieceDataName, isDragging));
    }

    private void PreventCheck(Vector2Int movingPiecePosition, PlayerType playerType) {
        Vector2Int kingPosition = FindKingPosition(playerType);
        SimulateMoves(kingPosition, movingPiecePosition, possibleMoves);
    }

    private void SimulateMoves(Vector2Int kingPosition, Vector2Int movingPiecePosition, List<Vector2Int> possibleMoves) {
        List<Vector2Int> movesToRemove = new List<Vector2Int>();
        Piece movingPiece = piecesOnBoard[movingPiecePosition.x, movingPiecePosition.y];

        if (movingPiece != null) {
            foreach (Vector2Int possibleMove in possibleMoves) {
                Piece targetPiece = piecesOnBoard[possibleMove.x, possibleMove.y];
                piecesOnBoard[movingPiecePosition.x, movingPiecePosition.y] = null;
                piecesOnBoard[possibleMove.x, possibleMove.y] = movingPiece;

                Vector2Int simulatedKingPosition = movingPiece.GetPieceData().pieceType == PieceType.King ? possibleMove : kingPosition;
                bool isKingInCheck = false;

                foreach (Piece enemyPiece in GetEnemyPieces(movingPiece.GetPieceData().playerType)) {
                    if (enemyPiece.GetPiecePossibleMoves(piecesOnBoard).Contains(simulatedKingPosition)) {
                        isKingInCheck = true;
                        break;
                    }
                }

                piecesOnBoard[movingPiecePosition.x, movingPiecePosition.y] = movingPiece;
                piecesOnBoard[possibleMove.x, possibleMove.y] = targetPiece;

                if (isKingInCheck) {
                    movesToRemove.Add(possibleMove);
                }
            }

            foreach (Vector2Int moveToRemove in movesToRemove) {
                possibleMoves.Remove(moveToRemove);
            }
        }
    }

    private List<Piece> GetEnemyPieces(PlayerType player) {
        List<Piece> enemyPieces = new List<Piece>();
        for (int x = 0; x < boardLength; x++) {
            for (int y = 0; y < boardLength; y++) {
                if (piecesOnBoard[x, y] != null && piecesOnBoard[x, y].GetPieceData().playerType != player) {
                    enemyPieces.Add(piecesOnBoard[x, y]);
                }
            }
        }
        return enemyPieces;
    }

    IEnumerator MoveToPosition(Vector2Int startPosition, Vector2Int targetPosition, bool isCastling, string pieceDataName, bool isDragging) {
        HighLightMovedPiece(startPosition, targetPosition);
        Piece piece = piecesOnBoard[startPosition.x, startPosition.y];
        piece.MoveTo(targetPosition);
        Vector2 start = gridPositions[startPosition.x, startPosition.y];
        Vector2 target = gridPositions[targetPosition.x, targetPosition.y];
        float elapsedTime = 0f;
        float duration = 0.18f;

        if (!(gameManager.GetLocalPlayerType() == gameManager.GetCurrentPlayablePlayerType() && isDragging)) {
            while (elapsedTime < duration) {
                piece.transform.position = Vector2.Lerp(start, target, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        piece.transform.position = target;

        if (IsServer) {
            ExecuteMoveServerRpc(startPosition, targetPosition, isCastling, pieceDataName);
        }
        
    }

    private void ShowPromotionWindow(PlayerType playerType, Vector2Int position) {
        Vector2 newPosition = gridPositions[position.x, position.y];
        GameObject newPromotionWindow = Instantiate(promotionWindowPrefab, newPosition, Quaternion.identity);
        newPromotionWindow.GetComponent<PromotionWindowManager>().Initialize(playerType, newPosition);
        promotionWindow = newPromotionWindow;
        isPromotionWindowOpen = true;
    }

    private void DestroyPromotionWindow() {
        if (promotionWindow != null) {
            Destroy(promotionWindow);
            isPromotionWindowOpen = false;
        }
    }

    [ServerRpc]
    private void ExecuteMoveServerRpc(Vector2Int start, Vector2Int target, bool isCastling, string pieceDataName) {
        Piece piece = piecesOnBoard[start.x, start.y];

        if (piece != null) {
            if (!string.IsNullOrEmpty(pieceDataName)) {
                piece.UpdatePieceDataRpc(pieceDataName);
            }
            Piece targetPiece = piecesOnBoard[target.x, target.y];
            
            if (target == lastPawnDoubleStepCapturePosition && piece.GetPieceType() == PieceType.Pawn) {
                targetPiece = piecesOnBoard[lastPawnDoubleStepPosition.x, lastPawnDoubleStepPosition.y];
            }

            if (targetPiece != null && piece.GetPlayerType() != targetPiece.GetPlayerType()) {
                NetworkObject targetObj = targetPiece.GetComponent<NetworkObject>();
                targetObj.Despawn();
            }

            if (!isCastling) {
                OnPieceMove?.Invoke(this, EventArgs.Empty);
            }
            
            UpdatePiecesOnBoardRpc(start, target);
        }


        CheckEndGame(piece.GetPlayerType());
    }

    private void CheckEndGame(PlayerType actualPlayerType) {
        PlayerType opponent = actualPlayerType == PlayerType.White ? PlayerType.Black : PlayerType.White;

        bool isKingInCheck = IsKingInCheck(opponent);
        bool hasLegalMove = HasLegalMoves(opponent);

        if (isKingInCheck && !hasLegalMove) {
            OnCheckmateRpc(actualPlayerType);
            return;
        }
        if (!isKingInCheck && !hasLegalMove) {
            OnStalemateRpc();
            return;
        }

        if (IsInsufficientMaterial()) {
            OnInsufficientMaterialRpc();
            return;
        }
    }

    private bool IsInsufficientMaterial() {
        List<PieceType> whitePieces = new List<PieceType>();
        List<PieceType> blackPieces = new List<PieceType>();
        int whiteBishopsOnColor = 0;
        int blackBishopsOnColor = 0;

        for (int x = 0; x < boardLength; x++) {
            for (int y = 0; y < boardLength; y++) {
                Piece piece = piecesOnBoard[x, y];
                if (piece == null || piece.GetPieceType() == PieceType.King) continue;

                if (piece.GetPlayerType() == PlayerType.White) {
                    whitePieces.Add(piece.GetPieceType());
                    if (piece.GetPieceType() == PieceType.Bishop) {
                        whiteBishopsOnColor = (x + y) % 2;
                    }
                } else {
                    blackPieces.Add(piece.GetPieceType());
                    if (piece.GetPieceType() == PieceType.Bishop) {
                        blackBishopsOnColor = (x + y) % 2;
                    }
                }
            }
        }

        if (whitePieces.Count == 0 && blackPieces.Count == 0) return true;

        if ((whitePieces.Count == 0 && IsOnlyMinorPiece(blackPieces)) ||
            (blackPieces.Count == 0 && IsOnlyMinorPiece(whitePieces))) {
            return true;
        }

        if (whitePieces.Count == 1 && blackPieces.Count == 1 &&
            whitePieces[0] == PieceType.Bishop && blackPieces[0] == PieceType.Bishop &&
            whiteBishopsOnColor == blackBishopsOnColor) {
            return true;
        }


        return false;
    }

    private bool IsOnlyMinorPiece(List<PieceType> pieces) {
        return pieces.Count == 1 &&
              (pieces[0] == PieceType.Bishop || pieces[0] == PieceType.Knight);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OnInsufficientMaterialRpc() {
        string title = "Draw!";
        string text = "draw due to insufficient material!";
        isGameRunning = false;
        OnEndGame?.Invoke(title, text);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OnStalemateRpc() {
        string title = "Draw!";
        string text = "draw by stalemate";
        isGameRunning = false;
        OnEndGame?.Invoke(title, text);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OnCheckmateRpc(PlayerType winner) {
        string title = gameManager.GetLocalPlayerType() == winner ? "Victory!" : "Defeat!";
        string text = $"{winner} won by checkmate!";
        isGameRunning = false;
        OnEndGame?.Invoke(title, text);
    }

    private bool IsKingInCheck(PlayerType player) {
        Vector2Int kingPos = FindKingPosition(player);
        return IsSquareUnderAttack(kingPos, player);
    }

    public bool IsSquareUnderAttack(Vector2Int square, PlayerType defender) {
        PlayerType attacker = defender == PlayerType.White ? PlayerType.Black : PlayerType.White;

        for (int x = 0; x < boardLength; x++) {
            for (int y = 0; y < boardLength; y++) {
                Piece piece = piecesOnBoard[x, y];
                if (piece != null && piece.GetPlayerType() == attacker) {
                    List<Vector2Int> moves = piece.GetPiecePossibleMoves(piecesOnBoard);
                    if (moves.Contains(square)) {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private bool HasLegalMoves(PlayerType player) {
        for (int x = 0; x < boardLength; x++) {
            for (int y = 0; y < boardLength; y++) {
                Piece piece = piecesOnBoard[x, y];
                if (piece != null && piece.GetPlayerType() == player) {
                    List<Vector2Int> rawMoves = piece.GetPiecePossibleMoves(piecesOnBoard);
                    List<Vector2Int> validMoves = new List<Vector2Int>(rawMoves);

                    Vector2Int kingPos = FindKingPosition(player);
                    SimulateMoves(kingPos, new Vector2Int(x, y), validMoves);

                    if (validMoves.Count > 0) return true;
                }
            }
        }
        return false;
    }

    private Vector2Int FindKingPosition(PlayerType player) {
        for (int x = 0; x < boardLength; x++) {
            for (int y = 0; y < boardLength; y++) {
                Piece piece = piecesOnBoard[x, y];
                if (piece != null &&
                    piece.GetPieceType() == PieceType.King &&
                    piece.GetPlayerType() == player) {
                    return new Vector2Int(x, y);
                }
            }
        }
        return new Vector2Int(-1, -1);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdatePiecesOnBoardRpc(Vector2Int start, Vector2Int target) {
        lastPawnDoubleStepCapturePosition = new Vector2Int(-1, -1);
        lastPawnDoubleStepPosition = new Vector2Int(-1, -1);
        Piece piece = piecesOnBoard[start.x, start.y];

        if (piece != null) {
            if (piece.GetPieceType() == PieceType.Pawn && Mathf.Abs(target.y - start.y) == 2) {
                int yPosition = piece.GetPlayerType() == PlayerType.White ? target.y - 1 : target.y + 1;
                lastPawnDoubleStepCapturePosition = new Vector2Int(target.x, yPosition);
                lastPawnDoubleStepPosition = target;
            }

            piecesOnBoard[target.x, target.y] = piece;
            piecesOnBoard[start.x, start.y] = null;
        }
    }

    private void removeSelectedSquareOverlay() {
        if (selectedPiecePosition != null) {
            squareOverlays[selectedPiecePosition.x, selectedPiecePosition.y].SetActive(false);
            selectedPiecePosition = new Vector2Int();
        }
    }

    void HighlightSelectedSquare(int x, int y) {
        removeSelectedSquareOverlay();
        if (pieceEndPosition != new Vector2Int(x, y)) {
            selectedPiecePosition = new Vector2Int(x, y);
            squareOverlays[x, y].SetActive(true);
        }
    }

    void HighlightHint(int x, int y) {
        hintOverlays[x, y].SetActive(true);
        hintOverlaysArray.Add(hintOverlays[x, y]);
    }

    void HighlightCaptureHint(int x, int y) {
        captureHintOverlays[x, y].SetActive(true);
        captureHintOverlaysArray.Add(captureHintOverlays[x, y]);
    }

    private void HighLightMovedPiece(Vector2Int startPosition, Vector2Int targetPosition) {
        ClearHighLightedSquares();
        ClearHighLightedHint();
        ClearHighLightedCaptureHint();
        pieceStartPosition = startPosition;
        pieceEndPosition = targetPosition;

        squareOverlays[pieceStartPosition.x, pieceStartPosition.y].SetActive(true);
        squareOverlays[pieceEndPosition.x, pieceEndPosition.y].SetActive(true);
    }

    void ClearHighLightedSquares() {
        squareOverlays[pieceStartPosition.x, pieceStartPosition.y].SetActive(false);
        squareOverlays[pieceEndPosition.x, pieceEndPosition.y].SetActive(false);
    }

    void ClearHighLightedHint() {
        foreach (GameObject hint in hintOverlaysArray) {
            hint.SetActive(false);
        }
        hintOverlaysArray.Clear();
    }

    void ClearHighLightedCaptureHint() {
        foreach (GameObject captureHint in captureHintOverlaysArray) {
            captureHint.SetActive(false);
        }
        captureHintOverlaysArray.Clear();
    }

    void SpawnPieces() {
        for (int i = 0; i < whitePieces.Length; i++) {
            foreach (Vector2Int possibleSpawn in whitePieces[i].possibleSpawns) {
                SpawnPiece(whitePieces[i], possibleSpawn);
            }
        }

        for (int i = 0; i < blackPieces.Length; i++) {
            foreach (Vector2Int possibleSpawn in blackPieces[i].possibleSpawns) {
                SpawnPiece(blackPieces[i], possibleSpawn);
            }
        }

        SyncPiecesClientRpc();
    }

    [ClientRpc]
    private void SyncPiecesClientRpc() {
        Piece[] allPieces = FindObjectsByType<Piece>(FindObjectsSortMode.None);
        foreach (Piece piece in allPieces) {
            Vector2Int pos = piece.GetBoardPosition();
            piecesOnBoard[pos.x, pos.y] = piece;
        }
    }

    void SpawnPiece(PieceData data, Vector2Int possibleSpawn) {
        if (!NetworkManager.Singleton.IsServer) return;

        Vector2 position = gridPositions[possibleSpawn.x, possibleSpawn.y];
        GameObject newPiece = Instantiate(piecePrefab, position, Quaternion.identity);
        Piece chessPiece = newPiece.GetComponent<Piece>();
        NetworkObject networkObject = newPiece.GetComponent<NetworkObject>();

        networkObject.DontDestroyWithOwner = true;
        networkObject.Spawn(true);
        chessPiece.InitializeClientRpc(data.name, possibleSpawn);

        piecesOnBoard[possibleSpawn.x, possibleSpawn.y] = chessPiece;
    }

    public Piece GetPieceAtPosition(Vector2Int boardPosition) {
        Piece piece = piecesOnBoard[boardPosition.x, boardPosition.y];
        return piece;
    }

    public int GetBoardLength() {
        return boardLength;
    }

    public Vector2Int GetLastPawnDoubleStepCapturePosition() {
        return lastPawnDoubleStepCapturePosition;
    }

    public Piece GetLastPawnDoubleStepPiece() {
        return piecesOnBoard[lastPawnDoubleStepPosition.x, lastPawnDoubleStepPosition.y];
    }
}
