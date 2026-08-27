using System;
using UnityEngine;

namespace RobotControl.Core.Domain
{
    /// <summary>Rango mecanico permitido de una articulacion, en grados.</summary>
    [Serializable]
    public struct JointLimit
    {
        public float Min;
        public float Max;

        public JointLimit(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Clamp(float value) => Mathf.Clamp(value, Min, Max);
    }

    /// <summary>
    /// Topes mecanicos del brazo.
    ///
    /// Importante para el proyecto final: el brazo fisico se rompe si se le pide un
    /// angulo imposible. Validar aqui, en el dominio, significa que ni la simulacion
    /// ni el enlace real pueden emitir una orden fuera de rango.
    /// </summary>
    [Serializable]
    public class ArmLimits
    {
        public JointLimit BaseYaw = new JointLimit(-150f, 150f);
        public JointLimit ShoulderPitch = new JointLimit(-15f, 90f);
        public JointLimit ElbowPitch = new JointLimit(-135f, 0f);
        public JointLimit WristPitch = new JointLimit(-90f, 90f);

        /// <summary>Instancia por defecto, usada cuando no se configura nada explicitamente.</summary>
        public static ArmLimits Default { get; } = new ArmLimits();

        /// <summary>Recorta una postura para que ninguna articulacion supere su tope.</summary>
        public ArmPose Clamp(ArmPose pose)
        {
            return new ArmPose
            {
                BaseYaw = BaseYaw.Clamp(pose.BaseYaw),
                ShoulderPitch = ShoulderPitch.Clamp(pose.ShoulderPitch),
                ElbowPitch = ElbowPitch.Clamp(pose.ElbowPitch),
                WristPitch = WristPitch.Clamp(pose.WristPitch),
                Gripper = Mathf.Clamp01(pose.Gripper),
            };
        }
    }
}
