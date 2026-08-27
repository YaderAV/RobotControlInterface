using System;

namespace RobotControl.Core.Environment
{
    /// <summary>Lados de una celda del laberinto.</summary>
    [Flags]
    public enum MazeSide
    {
        None = 0,
        North = 1,
        East = 2,
        South = 4,
        West = 8,
        All = North | East | South | West,
    }

    /// <summary>
    /// Rejilla de celdas con paredes. Cada celda guarda que lados suyos siguen
    /// cerrados; derribar una pared la quita de las DOS celdas que la comparten,
    /// asi que nunca puede quedar media pared colgando.
    ///
    /// Sin dependencias de Unity: es una estructura de datos, no geometria. Quien
    /// la convierte en cubos es el generador de escena.
    /// </summary>
    public sealed class MazeGrid
    {
        private readonly MazeSide[] _cells;

        public int Width { get; }
        public int Height { get; }

        public MazeGrid(int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;
            _cells = new MazeSide[width * height];

            // Se empieza con todo cerrado y el algoritmo va abriendo camino.
            for (int i = 0; i < _cells.Length; i++) _cells[i] = MazeSide.All;
        }

        public bool Contains(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public MazeSide WallsAt(int x, int y) => _cells[Index(x, y)];

        public bool HasWall(int x, int y, MazeSide side) => (_cells[Index(x, y)] & side) != 0;

        /// <summary>Derriba una pared en ambas celdas que la comparten.</summary>
        public void Carve(int x, int y, MazeSide side)
        {
            _cells[Index(x, y)] &= ~side;

            int nx = x + DeltaX(side);
            int ny = y + DeltaY(side);
            if (Contains(nx, ny)) _cells[Index(nx, ny)] &= ~Opposite(side);
        }

        public static MazeSide Opposite(MazeSide side) => side switch
        {
            MazeSide.North => MazeSide.South,
            MazeSide.South => MazeSide.North,
            MazeSide.East => MazeSide.West,
            MazeSide.West => MazeSide.East,
            _ => MazeSide.None,
        };

        public static int DeltaX(MazeSide side) => side switch
        {
            MazeSide.East => 1,
            MazeSide.West => -1,
            _ => 0,
        };

        public static int DeltaY(MazeSide side) => side switch
        {
            MazeSide.North => 1,
            MazeSide.South => -1,
            _ => 0,
        };

        private int Index(int x, int y)
        {
            if (!Contains(x, y))
                throw new ArgumentOutOfRangeException($"Celda fuera de la rejilla: ({x}, {y})");
            return y * Width + x;
        }
    }
}
