using UnityEngine;

namespace RobotControl.Core.Transport
{
    /// <summary>
    /// Pregunta al mundo si el robot cabe en una posicion.
    ///
    /// Solo la usa <see cref="SimulatedRobotLink"/>, y la razon es de coherencia
    /// fisica: el robot real no puede atravesar un muro, asi que el simulado tampoco
    /// debe poder. Sin esto el laberinto seria decorado.
    ///
    /// La simulacion no sabe QUE hay al otro lado de esta interfaz: pueden ser
    /// colliders de Unity, una rejilla de ocupacion o un mapa cargado de disco. Ese
    /// desacople es el que permite que el modelo fisico siga siendo C# puro.
    /// </summary>
    public interface IObstacleProbe
    {
        /// <summary>true si el robot chocaria estando centrado en esa posicion.</summary>
        bool IsBlocked(Vector3 position);
    }
}
