using System;
using UnityEngine;

namespace RobotControl.Core.Transport
{
    /// <summary>
    /// Parametros del robot simulado. Ajustarlos para que se parezcan al robot real
    /// es lo que convierte la simulacion en un gemelo digital util y no en un juguete.
    /// </summary>
    [Serializable]
    public class SimulationSettings
    {
        [Header("Traccion")]
        [Tooltip("Velocidad maxima de avance, en metros por segundo.")]
        [Min(0f)]
        public float MaxSpeed = 1.2f;

        [Tooltip("Velocidad maxima de giro, en grados por segundo.")]
        [Min(0f)]
        public float MaxTurnRate = 90f;

        [Tooltip("Aceleracion, en m/s^2. Valores bajos dan una inercia mas realista.")]
        [Min(0.01f)]
        public float Acceleration = 2.5f;

        [Header("Brazo")]
        [Tooltip("Velocidad angular de los servos, en grados por segundo.")]
        [Min(1f)]
        public float JointSpeed = 60f;

        [Tooltip("Velocidad de apertura y cierre de la pinza, en unidades normalizadas por segundo.")]
        [Min(0.01f)]
        public float GripperSpeed = 1.5f;

        [Header("Bateria")]
        [Tooltip("Consumo en reposo, en puntos porcentuales por segundo.")]
        [Min(0f)]
        public float IdleDrainPerSecond = 0.05f;

        [Tooltip("Consumo adicional a plena marcha, en puntos porcentuales por segundo.")]
        [Min(0f)]
        public float MovingDrainPerSecond = 0.35f;

        [Header("Enlace")]
        [Tooltip("Retardo simulado al conectar, en segundos. Reproduce la espera de una conexion real.")]
        [Min(0f)]
        public float ConnectDelay = 0.75f;
    }
}
