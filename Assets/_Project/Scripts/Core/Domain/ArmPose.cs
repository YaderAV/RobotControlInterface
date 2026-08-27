using System;
using UnityEngine;

namespace RobotControl.Core.Domain
{
    /// <summary>
    /// Estado angular completo del brazo, en grados.
    ///
    /// Esta estructura es el "idioma unico" del brazo: exactamente el mismo dato
    /// que mueve el modelo 3D en pantalla es el que mas adelante viajara a la
    /// Raspberry Pi para posicionar los servos reales. Por eso no contiene nada
    /// especifico de Unity mas alla de los atributos de inspector.
    /// </summary>
    [Serializable]
    public struct ArmPose : IEquatable<ArmPose>
    {
        [Tooltip("Giro de la base sobre el eje vertical.")]
        public float BaseYaw;

        [Tooltip("Elevacion del hombro.")]
        public float ShoulderPitch;

        [Tooltip("Flexion del codo.")]
        public float ElbowPitch;

        [Tooltip("Inclinacion de la muneca.")]
        public float WristPitch;

        [Range(0f, 1f)]
        [Tooltip("Apertura de la pinza: 0 = cerrada, 1 = abierta.")]
        public float Gripper;

        /// <summary>Postura de reposo: brazo recogido y pinza abierta.</summary>
        public static ArmPose Home => new ArmPose
        {
            BaseYaw = 0f,
            ShoulderPitch = 20f,
            ElbowPitch = -60f,
            WristPitch = 10f,
            Gripper = 1f,
        };

        /// <summary>
        /// Avanza <paramref name="current"/> hacia <paramref name="target"/> respetando
        /// velocidades maximas. Modela que un servo real no salta a su destino: tarda.
        /// </summary>
        public static ArmPose MoveTowards(
            ArmPose current,
            ArmPose target,
            float degreesPerSecond,
            float gripperPerSecond,
            float deltaTime)
        {
            float maxDegrees = degreesPerSecond * deltaTime;
            float maxGripper = gripperPerSecond * deltaTime;

            return new ArmPose
            {
                BaseYaw = Mathf.MoveTowards(current.BaseYaw, target.BaseYaw, maxDegrees),
                ShoulderPitch = Mathf.MoveTowards(current.ShoulderPitch, target.ShoulderPitch, maxDegrees),
                ElbowPitch = Mathf.MoveTowards(current.ElbowPitch, target.ElbowPitch, maxDegrees),
                WristPitch = Mathf.MoveTowards(current.WristPitch, target.WristPitch, maxDegrees),
                Gripper = Mathf.MoveTowards(current.Gripper, target.Gripper, maxGripper),
            };
        }

        /// <summary>Suma incrementos articulacion por articulacion (util para teleoperacion).</summary>
        public ArmPose Offset(float baseYaw, float shoulder, float elbow, float wrist, float gripper)
        {
            return new ArmPose
            {
                BaseYaw = BaseYaw + baseYaw,
                ShoulderPitch = ShoulderPitch + shoulder,
                ElbowPitch = ElbowPitch + elbow,
                WristPitch = WristPitch + wrist,
                Gripper = Gripper + gripper,
            };
        }

        public bool Equals(ArmPose other)
        {
            return Mathf.Approximately(BaseYaw, other.BaseYaw)
                && Mathf.Approximately(ShoulderPitch, other.ShoulderPitch)
                && Mathf.Approximately(ElbowPitch, other.ElbowPitch)
                && Mathf.Approximately(WristPitch, other.WristPitch)
                && Mathf.Approximately(Gripper, other.Gripper);
        }

        public override bool Equals(object obj) => obj is ArmPose other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = BaseYaw.GetHashCode();
                hash = (hash * 397) ^ ShoulderPitch.GetHashCode();
                hash = (hash * 397) ^ ElbowPitch.GetHashCode();
                hash = (hash * 397) ^ WristPitch.GetHashCode();
                hash = (hash * 397) ^ Gripper.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"Base {BaseYaw:F1}° | Hombro {ShoulderPitch:F1}° | Codo {ElbowPitch:F1}° | " +
                   $"Muneca {WristPitch:F1}° | Pinza {Gripper:P0}";
        }
    }
}
