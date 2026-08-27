using System;
using UnityEngine;

namespace RobotControl.Core.Domain
{
    /// <summary>
    /// Orden de traccion normalizada, independiente del chasis.
    ///
    /// Se expresa en el rango [-1, 1] a proposito: la interfaz no sabe si el robot
    /// real lleva orugas, ruedas o cuanto corre. Traducir de "-1..1" a PWM de motor
    /// es responsabilidad del ultimo eslabon (la simulacion, o la Raspberry Pi).
    /// </summary>
    [Serializable]
    public struct DriveCommand
    {
        [Range(-1f, 1f)]
        [Tooltip("Avance: 1 = adelante a fondo, -1 = marcha atras a fondo.")]
        public float Forward;

        [Range(-1f, 1f)]
        [Tooltip("Giro: 1 = derecha a fondo, -1 = izquierda a fondo.")]
        public float Turn;

        /// <summary>Robot detenido.</summary>
        public static DriveCommand Stop => new DriveCommand { Forward = 0f, Turn = 0f };

        /// <summary>Crea una orden garantizando que ambos ejes caen dentro de [-1, 1].</summary>
        public static DriveCommand Clamped(float forward, float turn)
        {
            return new DriveCommand
            {
                Forward = Mathf.Clamp(forward, -1f, 1f),
                Turn = Mathf.Clamp(turn, -1f, 1f),
            };
        }

        public bool IsIdle => Mathf.Approximately(Forward, 0f) && Mathf.Approximately(Turn, 0f);

        public override string ToString() => $"Avance {Forward:F2} | Giro {Turn:F2}";
    }
}
