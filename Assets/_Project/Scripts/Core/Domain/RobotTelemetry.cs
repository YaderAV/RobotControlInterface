using System;
using UnityEngine;

namespace RobotControl.Core.Domain
{
    /// <summary>
    /// Fotografia del estado del robot en un instante.
    ///
    /// Decision de arquitectura clave: la telemetria es la UNICA fuente de verdad,
    /// tanto en simulacion como con hardware real. La escena de Unity nunca decide
    /// donde esta el robot; solo dibuja lo que la telemetria dice. Gracias a eso,
    /// el dia que se enchufe la Raspberry Pi no hay que tocar la vista: cambia el
    /// origen de los datos, no su consumidor.
    /// </summary>
    [Serializable]
    public struct RobotTelemetry
    {
        /// <summary>Posicion en el mundo, en metros.</summary>
        public Vector3 Position;

        /// <summary>Rumbo en grados respecto al norte de la escena.</summary>
        public float HeadingDegrees;

        /// <summary>Velocidad lineal actual, en metros por segundo.</summary>
        public float SpeedMetersPerSecond;

        /// <summary>Angulos reales de las articulaciones (no los solicitados).</summary>
        public ArmPose Arm;

        /// <summary>Carga restante, de 0 a 100.</summary>
        public float BatteryPercent;

        /// <summary>Segundos transcurridos desde que arranco el enlace.</summary>
        public float TimestampSeconds;

        public static RobotTelemetry Empty => new RobotTelemetry
        {
            Position = Vector3.zero,
            HeadingDegrees = 0f,
            SpeedMetersPerSecond = 0f,
            Arm = ArmPose.Home,
            BatteryPercent = 100f,
            TimestampSeconds = 0f,
        };

        /// <summary>Rotacion equivalente al rumbo, lista para aplicar a un Transform.</summary>
        public Quaternion Rotation => Quaternion.Euler(0f, HeadingDegrees, 0f);

        /// <summary>Umbral tipico para avisar al operador de que debe volver a base.</summary>
        public bool IsBatteryLow => BatteryPercent <= 20f;
    }
}
