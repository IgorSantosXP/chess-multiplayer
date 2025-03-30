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
        PlayerTheme playerTheme = ProfileManager.Instance.GetPlayerTheme();
        GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"Themes/{playerTheme}/Pieces/{pieceData.playerType}{pieceData.pieceType}");
    }

    public PieceData GetPieceData() {
        return pieceData;
    }
}
