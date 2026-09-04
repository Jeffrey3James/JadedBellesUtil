using System;
using TMPro;
using UnityEngine;

namespace JadedBelles.Util.GridSystem
{
    /// <summary>
    /// A generic 2D grid usable in both 2D and 3D scenes. The grid stores values of any type
    /// and provides accessors keyed by grid coordinates or by world position. A pluggable
    /// <see cref="CoordinateConverter"/> lets the same grid render on the X-Y plane
    /// (<see cref="VerticalGrid"/>) or the X-Z plane (<see cref="HorizontalGrid"/>).
    /// </summary>
    /// <remarks>
    /// Extracted from Xandria Gem Jam's <c>Assets/_Scripts/GridSystem/</c> into
    /// <c>com.jadedbelles.util</c> so it can be reused across JadedBelles titles without dragging
    /// gameplay dependencies with it.
    /// </remarks>
    [System.Serializable]
    public class GridSystem2D<T>
    {
        private readonly int width;
        private readonly int height;
        private readonly float cellSize;
        private readonly Vector3 origin;
        private readonly T[,] gridArray;

        public T[,] GridArray => gridArray;
        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public Vector3 Origin => origin;

        private readonly CoordinateConverter coordinateConverter;

        /// <summary>Fires when a cell is set to a new value. Args are (x, y, newValue).</summary>
        public event Action<int, int, T> OnValueChangeEvent;

        public static GridSystem2D<T> VerticalGrid(int width, int height, float cellSize, Vector3 origin, bool debug = false)
        {
            return new GridSystem2D<T>(width, height, cellSize, origin, new VerticalConverter(), debug);
        }

        public static GridSystem2D<T> HorizontalGrid(int width, int height, float cellSize, Vector3 origin, bool debug = false)
        {
            return new GridSystem2D<T>(width, height, cellSize, origin, new HorizontalConverter(), debug);
        }

        public GridSystem2D(int width, int height, float cellSize, Vector3 origin, CoordinateConverter coordinateConverter, bool debug)
        {
            this.width = width;
            this.height = height;
            this.cellSize = cellSize;
            this.origin = origin;
            this.coordinateConverter = coordinateConverter ?? new VerticalConverter();

            gridArray = new T[width, height];

            if (debug)
            {
                DrawDebugLines();
            }
        }

        // -- Set --

        public void SetValue(Vector3 worldPosition, T value)
        {
            Vector2Int pos = coordinateConverter.WorldToGrid(worldPosition, cellSize, origin);
            SetValue(pos.x, pos.y, value);
        }

        public void SetValue(int x, int y, T value)
        {
            if (IsValid(x, y))
            {
                gridArray[x, y] = value;
                OnValueChangeEvent?.Invoke(x, y, value);
            }
        }

        // -- Get --

        public T GetValue(Vector3 worldPosition)
        {
            Vector2Int pos = GetXY(worldPosition);
            return GetValue(pos.x, pos.y);
        }

        public T GetValue(int x, int y)
        {
            return IsValid(x, y) ? gridArray[x, y] : default;
        }

        // -- Coordinates --

        public bool IsValid(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

        public Vector2Int GetXY(Vector3 worldPosition) => coordinateConverter.WorldToGrid(worldPosition, cellSize, origin);

        public Vector3 GetWorldPositionCenter(int x, int y) => coordinateConverter.GridToWorldCenter(x, y, cellSize, origin);

        public Vector3 GetWorldPosition(int x, int y) => coordinateConverter.GridToWorld(x, y, cellSize, origin);

        // -- Debug rendering --

        private void DrawDebugLines()
        {
            const float duration = 100f;
            var parent = new GameObject("GridSystem2D_Debug");

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CreateWorldText(parent, x + "," + y, GetWorldPositionCenter(x, y), coordinateConverter.Forward);
                    Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x, y + 1), Color.white, duration);
                    Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x + 1, y), Color.white, duration);
                }
            }

            Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, duration);
            Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, duration);
        }

        private TextMeshPro CreateWorldText(GameObject parent, string text, Vector3 position, Vector3 dir,
            int fontSize = 2, Color color = default, TextAlignmentOptions textAnchor = TextAlignmentOptions.Center, int sortingOrder = 0)
        {
            var go = new GameObject("DebugText_" + text, typeof(TextMeshPro));
            go.transform.SetParent(parent.transform);
            go.transform.position = position;
            go.transform.forward = dir;

            var tmp = go.GetComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color == default ? Color.white : color;
            tmp.alignment = textAnchor;
            tmp.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;

            return tmp;
        }

        // -- Coordinate converters --

        public abstract class CoordinateConverter
        {
            public abstract Vector3 GridToWorld(int x, int y, float cellSize, Vector3 origin);
            public abstract Vector3 GridToWorldCenter(int x, int y, float cellSize, Vector3 origin);
            public abstract Vector2Int WorldToGrid(Vector3 worldPosition, float cellSize, Vector3 origin);
            public abstract Vector3 Forward { get; }
        }

        /// <summary>
        /// Grid lies on the X-Y plane (standard 2D games). Grid Y grows upward on-screen.
        /// </summary>
        public class VerticalConverter : CoordinateConverter
        {
            public override Vector3 GridToWorld(int x, int y, float cellSize, Vector3 origin)
                => new Vector3(x, y, 0) * cellSize + origin;

            public override Vector3 GridToWorldCenter(int x, int y, float cellSize, Vector3 origin)
                => new Vector3(x * cellSize + cellSize * 0.5f, y * cellSize + cellSize * 0.5f, 0) + origin;

            public override Vector2Int WorldToGrid(Vector3 worldPosition, float cellSize, Vector3 origin)
            {
                Vector3 gridPosition = (worldPosition - origin) / cellSize;
                return new Vector2Int(Mathf.FloorToInt(gridPosition.x), Mathf.FloorToInt(gridPosition.y));
            }

            public override Vector3 Forward => Vector3.forward;
        }

        /// <summary>
        /// Grid lies on the X-Z plane (top-down 3D, tactics, board games).
        /// </summary>
        public class HorizontalConverter : CoordinateConverter
        {
            public override Vector3 GridToWorld(int x, int y, float cellSize, Vector3 origin)
                => new Vector3(x, 0, y) * cellSize + origin;

            public override Vector3 GridToWorldCenter(int x, int y, float cellSize, Vector3 origin)
                => new Vector3(x * cellSize + cellSize * 0.5f, 0, y * cellSize + cellSize * 0.5f) + origin;

            public override Vector2Int WorldToGrid(Vector3 worldPosition, float cellSize, Vector3 origin)
            {
                Vector3 gridPosition = (worldPosition - origin) / cellSize;
                return new Vector2Int(Mathf.FloorToInt(gridPosition.x), Mathf.FloorToInt(gridPosition.z));
            }

            public override Vector3 Forward => -Vector3.up;
        }
    }
}
