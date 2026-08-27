using System;
using RobotControl.Core.Domain;
using UnityEngine;

namespace RobotControl.Core.Transport
{
    /// <summary>
    /// Robot simulado: modelo de traccion diferencial + servos con velocidad finita
    /// + consumo de bateria. No usa fisica de Unity ni MonoBehaviour a proposito, asi
    /// que es codigo puro y comprobable con tests.
    ///
    /// Cumple <see cref="IRobotLink"/> igual que lo hara el enlace real, incluido el
    /// retardo de conexion: el objetivo es que la UI no note la diferencia.
    /// </summary>
    public sealed class SimulatedRobotLink : IRobotLink
    {
        private readonly SimulationSettings _settings;
        private readonly IObstacleProbe _obstacles;
        private readonly Vector3 _startPosition;
        private readonly float _startHeading;

        private LinkStatus _status = LinkStatus.Disconnected;
        private RobotTelemetry _telemetry;

        private DriveCommand _drive = DriveCommand.Stop;
        private ArmPose _armTarget = ArmPose.Home;

        private float _currentSpeed;
        private float _connectCountdown;
        private float _elapsed;
        private bool _disposed;

        public SimulatedRobotLink(
            SimulationSettings settings = null,
            ArmLimits limits = null,
            Vector3 startPosition = default,
            float startHeadingDegrees = 0f,
            IObstacleProbe obstacles = null)
        {
            _settings = settings ?? new SimulationSettings();
            _obstacles = obstacles;
            Limits = limits ?? ArmLimits.Default;
            _startPosition = startPosition;
            _startHeading = startHeadingDegrees;

            _telemetry = RobotTelemetry.Empty;
            _telemetry.Position = startPosition;
            _telemetry.HeadingDegrees = startHeadingDegrees;
        }

        public LinkStatus Status => _status;
        public RobotTelemetry Telemetry => _telemetry;
        public ArmLimits Limits { get; }

        public event Action<RobotTelemetry> TelemetryUpdated;
        public event Action<LinkStatus> StatusChanged;

        public void Connect()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SimulatedRobotLink));
            if (_status == LinkStatus.Connecting || _status == LinkStatus.Connected) return;

            // Reinicia el robot a su estado de partida, como si acabara de encenderse.
            _elapsed = 0f;
            _currentSpeed = 0f;
            _drive = DriveCommand.Stop;
            _armTarget = ArmPose.Home;

            _telemetry = RobotTelemetry.Empty;
            _telemetry.Position = _startPosition;
            _telemetry.HeadingDegrees = _startHeading;

            _connectCountdown = _settings.ConnectDelay;
            SetStatus(LinkStatus.Connecting);

            // Con retardo cero la conexion es inmediata, sin esperar al primer Tick.
            if (_connectCountdown <= 0f) SetStatus(LinkStatus.Connected);
        }

        public void Disconnect()
        {
            if (_status == LinkStatus.Disconnected) return;

            _drive = DriveCommand.Stop;
            _currentSpeed = 0f;
            SetStatus(LinkStatus.Disconnected);
        }

        public void SendDrive(DriveCommand command)
        {
            if (_status != LinkStatus.Connected) return;
            _drive = DriveCommand.Clamped(command.Forward, command.Turn);
        }

        public void SendArmTarget(ArmPose target)
        {
            if (_status != LinkStatus.Connected) return;
            // Los topes se aplican aqui, en el enlace: ninguna orden imposible
            // puede llegar a los servos, ni simulados ni reales.
            _armTarget = Limits.Clamp(target);
        }

        public void Tick(float deltaTime)
        {
            if (_disposed || deltaTime <= 0f) return;

            if (_status == LinkStatus.Connecting)
            {
                _connectCountdown -= deltaTime;
                if (_connectCountdown <= 0f) SetStatus(LinkStatus.Connected);
                return;
            }

            if (_status != LinkStatus.Connected) return;

            _elapsed += deltaTime;

            StepDrive(deltaTime);
            StepArm(deltaTime);
            StepBattery(deltaTime);

            _telemetry.TimestampSeconds = _elapsed;
            TelemetryUpdated?.Invoke(_telemetry);
        }

        /// <summary>Traccion diferencial simple con inercia en el avance.</summary>
        private void StepDrive(float deltaTime)
        {
            // Sin bateria el robot no responde: es la primera regla de juego que
            // la gamificacion podra explotar (volver a base antes de quedarse tirado).
            float throttle = _telemetry.BatteryPercent > 0f ? _drive.Forward : 0f;
            float steering = _telemetry.BatteryPercent > 0f ? _drive.Turn : 0f;

            float targetSpeed = throttle * _settings.MaxSpeed;
            _currentSpeed = Mathf.MoveTowards(
                _currentSpeed, targetSpeed, _settings.Acceleration * deltaTime);

            _telemetry.HeadingDegrees = Mathf.Repeat(
                _telemetry.HeadingDegrees + steering * _settings.MaxTurnRate * deltaTime, 360f);

            Vector3 forward = _telemetry.Rotation * Vector3.forward;
            Vector3 next = _telemetry.Position + forward * (_currentSpeed * deltaTime);

            // El giro ya se aplico arriba, a proposito: asi un robot encajado contra
            // un muro todavia puede girar sobre si mismo para salir del atasco.
            if (_obstacles != null && _obstacles.IsBlocked(next))
            {
                _currentSpeed = 0f;
                _telemetry.SpeedMetersPerSecond = 0f;
                return;
            }

            _telemetry.Position = next;
            _telemetry.SpeedMetersPerSecond = _currentSpeed;
        }

        /// <summary>Los servos se acercan a su objetivo a velocidad limitada.</summary>
        private void StepArm(float deltaTime)
        {
            _telemetry.Arm = ArmPose.MoveTowards(
                _telemetry.Arm,
                _armTarget,
                _settings.JointSpeed,
                _settings.GripperSpeed,
                deltaTime);
        }

        private void StepBattery(float deltaTime)
        {
            float effort = Mathf.Clamp01(Mathf.Abs(_drive.Forward) + Mathf.Abs(_drive.Turn));
            float drain = _settings.IdleDrainPerSecond + _settings.MovingDrainPerSecond * effort;
            _telemetry.BatteryPercent = Mathf.Max(0f, _telemetry.BatteryPercent - drain * deltaTime);
        }

        private void SetStatus(LinkStatus next)
        {
            if (_status == next) return;
            _status = next;
            StatusChanged?.Invoke(next);
        }

        public void Dispose()
        {
            if (_disposed) return;
            Disconnect();
            _disposed = true;
            TelemetryUpdated = null;
            StatusChanged = null;
        }
    }
}
