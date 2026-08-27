using UnityEngine;

namespace RobotControl.Core.Domain
{
    /// <summary>Partes del robot cuyo movimiento se vigila por separado.</summary>
    public enum RobotPart
    {
        Drive = 0,
        Turn = 1,
        ArmBase = 2,
        Shoulder = 3,
        Elbow = 4,
        Wrist = 5,
        Gripper = 6,
    }

    /// <summary>
    /// Que partes del robot se estan moviendo ahora mismo.
    ///
    /// Se DERIVA de comparar dos instantes de telemetria; no se pregunta a los mandos
    /// ni al teclado. Esa distincion importa: el dia que los datos vengan de la
    /// Raspberry Pi, este calculo seguira valiendo sin tocar una linea, y ademas
    /// reflejara lo que el robot hace de verdad y no lo que se le ordeno.
    /// </summary>
    public readonly struct RobotActivity
    {
        /// <summary>Numero de valores de <see cref="RobotPart"/>.</summary>
        public const int PartCount = 7;

        // Umbrales por debajo de los cuales el movimiento se considera ruido.
        private const float JointDegreesPerSecond = 1.0f;
        private const float GripperUnitsPerSecond = 0.02f;
        private const float MetersPerSecond = 0.02f;
        private const float TurnDegreesPerSecond = 1.0f;

        private readonly int _mask;

        private RobotActivity(int mask) => _mask = mask;

        /// <summary>Robot completamente quieto.</summary>
        public static RobotActivity None => default;

        public bool IsMoving(RobotPart part) => (_mask & (1 << (int)part)) != 0;

        public bool AnyArmMoving =>
            IsMoving(RobotPart.ArmBase) || IsMoving(RobotPart.Shoulder) ||
            IsMoving(RobotPart.Elbow) || IsMoving(RobotPart.Wrist) ||
            IsMoving(RobotPart.Gripper);

        public bool AnyDriveMoving => IsMoving(RobotPart.Drive) || IsMoving(RobotPart.Turn);

        /// <summary>Compara dos instantes y deduce que se movio entre ellos.</summary>
        public static RobotActivity Between(
            in RobotTelemetry previous, in RobotTelemetry current, float deltaTime)
        {
            if (deltaTime <= 0f) return None;

            int mask = 0;

            if (Mathf.Abs(current.SpeedMetersPerSecond) > MetersPerSecond)
                mask |= 1 << (int)RobotPart.Drive;

            // DeltaAngle y no una resta directa: el rumbo da la vuelta en 360 -> 0
            // y una resta cruda ahi daria un giro fantasma de 359 grados.
            float turnRate =
                Mathf.Abs(Mathf.DeltaAngle(previous.HeadingDegrees, current.HeadingDegrees)) / deltaTime;
            if (turnRate > TurnDegreesPerSecond) mask |= 1 << (int)RobotPart.Turn;

            mask |= JointBit(previous.Arm.BaseYaw, current.Arm.BaseYaw, deltaTime, RobotPart.ArmBase);
            mask |= JointBit(previous.Arm.ShoulderPitch, current.Arm.ShoulderPitch, deltaTime, RobotPart.Shoulder);
            mask |= JointBit(previous.Arm.ElbowPitch, current.Arm.ElbowPitch, deltaTime, RobotPart.Elbow);
            mask |= JointBit(previous.Arm.WristPitch, current.Arm.WristPitch, deltaTime, RobotPart.Wrist);

            float gripperRate = Mathf.Abs(current.Arm.Gripper - previous.Arm.Gripper) / deltaTime;
            if (gripperRate > GripperUnitsPerSecond) mask |= 1 << (int)RobotPart.Gripper;

            return new RobotActivity(mask);
        }

        private static int JointBit(float before, float after, float deltaTime, RobotPart part)
        {
            float rate = Mathf.Abs(after - before) / deltaTime;
            return rate > JointDegreesPerSecond ? 1 << (int)part : 0;
        }
    }
}
