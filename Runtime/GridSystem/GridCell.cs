namespace JadedBelles.Util.GridSystem
{
    /// <summary>
    /// A single cell in a <see cref="GridSystem2D{T}"/>. Holds a primary value plus an optional
    /// shape/metadata payload, and remembers its own grid coordinates so callers can swap
    /// or query cells without touching the surrounding grid.
    /// </summary>
    /// <typeparam name="TValue">
    /// The primary contents of the cell. In a match-3 game this is usually the game-piece
    /// component (e.g. a Gem MonoBehaviour). In a tactics game it might be a Unit; in an
    /// inventory grid it might be a stack.
    /// </typeparam>
    /// <typeparam name="TShape">
    /// An optional per-cell shape or metadata payload. Use this for level-shape overrides
    /// (blocked cells, edge tiles, biome tags). Pass <see cref="System.Object"/> — or any
    /// unused reference type — if you don't need it.
    /// </typeparam>
    [System.Serializable]
    public class GridCell<TValue, TShape>
        where TValue : class
        where TShape : class
    {
        private readonly GridSystem2D<GridCell<TValue, TShape>> grid;
        private int x;
        private int y;

        private TValue value;
        private TShape shape;

        public GridCell(GridSystem2D<GridCell<TValue, TShape>> grid, int x, int y)
        {
            this.grid = grid;
            this.x = x;
            this.y = y;
        }

        public void SetValue(TValue value) => this.value = value;
        public TValue GetValue() => value;

        public void SetShape(TShape shape) => this.shape = shape;
        public TShape GetShape() => shape;

        /// <summary>
        /// Update the cached grid coordinates. Call this after moving a cell inside the grid
        /// (e.g. as part of a swap) so the cell reports its new home.
        /// </summary>
        public void SetXY(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int GetX() => x;
        public int GetY() => y;

        public GridSystem2D<GridCell<TValue, TShape>> GetGrid() => grid;
    }

    /// <summary>
    /// Thin single-value cell wrapper for grids that don't need a shape payload. Provided for
    /// symmetry with the legacy <c>GridObject&lt;T&gt;</c> in older JadedBelles code — new code
    /// should prefer <see cref="GridCell{TValue, TShape}"/>.
    /// </summary>
    public class GridObject<T>
    {
        private readonly GridSystem2D<GridObject<T>> grid;
        private readonly int x;
        private readonly int y;
        private T value;

        public GridObject(GridSystem2D<GridObject<T>> grid, int x, int y)
        {
            this.grid = grid;
            this.x = x;
            this.y = y;
        }

        public void SetValue(T value) => this.value = value;
        public T GetValue() => value;
        public int GetX() => x;
        public int GetY() => y;
        public GridSystem2D<GridObject<T>> GetGrid() => grid;
    }
}
