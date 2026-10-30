using UnityEngine;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     3D盤面のワールド座標と、計算用の正規化座標を変換するクラス
    /// </summary>
    public class DartboardSurface : MonoBehaviour
    {
        /// <summary> 盤面の半径 </summary>
        public float Radius => _radius;

        [SerializeField, Min(0.001f), Tooltip("ローカル空間での盤面半径")]
        private float _radius = 1f;

        /// <summary>
        ///     Rayを盤面に投射し、盤面上の正規化座標を取得する
        /// </summary>
        /// <param name="ray"> 投射するRay </param>
        /// <param name="boardPosition"> 盤面上の正規化座標 </param>
        /// <returns> 交点が存在する場合はtrue、存在しない場合はfalse </returns>
        public bool TryRaytoBoard(Ray ray, out Vector2 boardPosition)
        {
            // 盤面の法線と位置から平面を作成し、レイとの交点を計算する
            var plane = new Plane(transform.forward, transform.position);

            if (!plane.Raycast(ray, out float enter))
            {
                boardPosition = default;
                return false;
            }

            // ray.GetPoint(enter)で交点のワールド座標を取得し、正規化座標に変換する
            Vector3 worldPos = ray.GetPoint(enter);
            boardPosition = WorldToBoard(worldPos);
            return true;
        }

        /// <summary>
        ///     計算用の正規化座標をワールド座標に変換する
        /// </summary>
        /// <param name="boardPosition"> 盤面中心が原点、半径1の正規化座標 </param>
        /// <returns> ワールド座標 </returns>
        public Vector3 BoardToWorld(Vector2 boardPosition)
        {
            Vector3 localPosition =
                new Vector3(boardPosition.x * _radius, boardPosition.y * _radius, 0f);
            return transform.TransformPoint(localPosition);
        }

        /// <summary>
        ///     ワールド座標を計算用の正規化座標に変換する
        /// </summary>
        /// <param name="worldPosition"> ワールド座標 </param>
        /// <returns> 盤面中心が原点、半径1の正規化座標 </returns>
        public Vector2 WorldToBoard(Vector3 worldPosition)
        {
            // ワールド座標をローカル座標に変換し、半径で割ることで正規化座標に変換する
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            return new Vector2(localPosition.x, localPosition.y) / _radius;
        }
    }
}
