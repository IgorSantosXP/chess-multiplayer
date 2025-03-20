using UnityEngine;

public class PromotionPiece : MonoBehaviour
{
    private PieceData pieceData;

    public void Initialize(string dataName) {
        pieceData = Resources.Load<PieceData>($"PieceData/{dataName}");

        if (pieceData == null) {
            return;
        }

        if (GameManager.Instance.GetLocalPlayerType() == PlayerType.Black) {
            transform.rotation = Quaternion.Euler(0, 0, 180);
        }

        GetComponent<SpriteRenderer>().sprite = pieceData.sprite;
    }

    public PieceData GetPieceData() {
        return pieceData;
    }
}
