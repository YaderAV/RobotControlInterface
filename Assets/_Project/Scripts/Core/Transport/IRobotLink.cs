using System;
using RobotControl.Core.Domain;

namespace RobotControl.Core.Transport
{
    /// <summary>
    /// Contrato unico entre la interfaz de control y el robot.
    ///
    /// ESTA ES LA PIEZA CENTRAL DEL PROYECTO. Todo lo que hay "por encima"
    /// (teleoperacion, HUD, misiones, puntuacion) habla solo con esta interfaz y
    /// jamas sabe si al otro lado hay una simulacion o una Raspberry Pi.
    ///
    /// Implementaciones previstas:
    ///   - SimulatedRobotLink : modelo fisico local. Es lo que hace que el proyecto
    ///                          funcione como plantilla educativa sin hardware.
    ///   - RaspberryPiLink    : pendiente. Hablara con la Pi por el transporte que
    ///                          se decida (WebSocket/JSON, MQTT o ROS2). Al cumplir
    ///                          este mismo contrato, entra sin tocar el resto.
    /// </summary>
    public interface IRobotLink : IDisposable
    {
        /// <summary>Estado actual del enlace.</summary>
        LinkStatus Status { get; }

        /// <summary>Ultima telemetria conocida. Valida solo si <see cref="Status"/> es Connected.</summary>
        RobotTelemetry Telemetry { get; }

        /// <summary>Topes mecanicos que este enlace hace cumplir.</summary>
        ArmLimits Limits { get; }

        /// <summary>Se dispara en cada actualizacion de telemetria.</summary>
        event Action<RobotTelemetry> TelemetryUpdated;

        /// <summary>Se dispara en cada transicion de estado del enlace.</summary>
        event Action<LinkStatus> StatusChanged;

        /// <summary>Abre el enlace. Es asincrono por naturaleza: no asumas Connected al volver.</summary>
        void Connect();

        /// <summary>Cierra el enlace y detiene el robot.</summary>
        void Disconnect();

        /// <summary>
        /// Ordena traccion. Se ignora si el enlace no esta conectado.
        /// Pensado para llamarse cada frame mientras el operador mantiene el control.
        /// </summary>
        void SendDrive(DriveCommand command);

        /// <summary>
        /// Ordena una postura objetivo del brazo. El enlace la recorta a los topes
        /// mecanicos y la alcanza progresivamente, nunca de golpe.
        /// </summary>
        void SendArmTarget(ArmPose target);

        /// <summary>
        /// Avanza el enlace un paso de tiempo. Debe llamarse desde el hilo principal.
        ///
        /// En la simulacion integra el modelo fisico; en un enlace real vaciara la
        /// cola de mensajes recibidos por red. Tener este metodo en el contrato evita
        /// que la version real necesite un mecanismo distinto al de la simulada.
        /// </summary>
        void Tick(float deltaTime);
    }
}
