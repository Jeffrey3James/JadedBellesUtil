using JadedBelles.Util.GridSystem;
using UnityEngine;

namespace JadedBelles.Util.Samples
{
    /// <summary>
    /// Minimal example of a GridSystem2D. Place this on any GameObject in an empty scene,
    /// press Play, and watch the Console + Scene view.
    /// </summary>
    public class GridSystemBasicsSample : MonoBehaviour
    {
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 8;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3 origin = Vector3.zero;

        private GridSystem2D<GridCell<Transform, object>> grid;

        private void Start()
        {
            grid = GridSystem2D<GridCell<Transform, object>>.VerticalGrid(width, height, cellSize, origin, debug: true);

            grid.OnValueChangeEvent += (x, y, cell) =>
            {
                Debug.Log($"[GridSystem2D] Cell ({x},{y}) set to {(cell?.GetValue() != null ? cell.GetValue().name : "null")}");
            };

            // Fill with a marker cube per cell so the sample is visible in Scene view.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    marker.name = $"Marker_{x}_{y}";
                    marker.transform.position = grid.GetWorldPositionCenter(x, y);
                    marker.transform.localScale = Vector3.one * cellSize * 0.8f;
                    marker.transform.SetParent(transform);

                    var cell = new GridCell<Transform, object>(grid, x, y);
                    cell.SetValue(marker.transform);
                    grid.SetValue(x, y, cell);
                }
            }
        }
    }
}
