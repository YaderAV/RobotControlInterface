using System;
using System.Collections.Generic;

namespace RobotControl.Core.Environment
{
    /// <summary>
    /// Genera laberintos con el algoritmo de vuelta atras (recursive backtracker).
    ///
    /// Produce un laberinto "perfecto": entre dos celdas cualesquiera existe un
    /// camino y solo uno, sin zonas inalcanzables ni bucles. Eso importa para lo que
    /// viene despues: cuando la Fase 2 esconda victimas por el mapa, se garantiza
    /// que todas se pueden alcanzar.
    ///
    /// Usa System.Random y no UnityEngine.Random para que la misma semilla de siempre
    /// el mismo laberinto, en cualquier maquina y sin depender del estado global del
    /// motor. Un escenario reproducible se puede comparar entre alumnos y depurar.
    /// </summary>
    public static class MazeGenerator
    {
        private static readonly MazeSide[] Sides =
        {
            MazeSide.North, MazeSide.East, MazeSide.South, MazeSide.West,
        };

        public static MazeGrid Generate(int width, int height, int seed)
        {
            var grid = new MazeGrid(width, height);
            var random = new Random(seed);
            var visited = new bool[width * height];
            var stack = new Stack<int>();

            // Pila explicita en vez de recursion: un laberinto grande desbordaria
            // la pila de llamadas, y aqui la profundidad puede ser Width * Height.
            int start = 0;
            visited[start] = true;
            stack.Push(start);

            var candidates = new MazeSide[4];

            while (stack.Count > 0)
            {
                int current = stack.Peek();
                int x = current % width;
                int y = current / width;

                int count = CollectUnvisited(grid, visited, x, y, candidates);
                if (count == 0)
                {
                    stack.Pop();
                    continue;
                }

                MazeSide side = candidates[random.Next(count)];
                int nx = x + MazeGrid.DeltaX(side);
                int ny = y + MazeGrid.DeltaY(side);

                grid.Carve(x, y, side);

                int next = ny * width + nx;
                visited[next] = true;
                stack.Push(next);
            }

            return grid;
        }

        private static int CollectUnvisited(
            MazeGrid grid, bool[] visited, int x, int y, MazeSide[] buffer)
        {
            int count = 0;
            for (int i = 0; i < Sides.Length; i++)
            {
                MazeSide side = Sides[i];
                int nx = x + MazeGrid.DeltaX(side);
                int ny = y + MazeGrid.DeltaY(side);

                if (!grid.Contains(nx, ny)) continue;
                if (visited[ny * grid.Width + nx]) continue;

                buffer[count++] = side;
            }
            return count;
        }
    }
}
