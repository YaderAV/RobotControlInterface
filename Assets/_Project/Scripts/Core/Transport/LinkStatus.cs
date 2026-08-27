namespace RobotControl.Core.Transport
{
    /// <summary>
    /// Estado del enlace con el robot.
    ///
    /// La simulacion pasa por los mismos estados que pasaria una conexion real.
    /// Es intencionado: si la UI se acostumbra desde el principio a manejar
    /// "Conectando" y "Error", el salto al hardware no la sorprende.
    /// </summary>
    public enum LinkStatus
    {
        /// <summary>Sin enlace. No se aceptan comandos.</summary>
        Disconnected = 0,

        /// <summary>Negociando el enlace. Todavia no se aceptan comandos.</summary>
        Connecting = 1,

        /// <summary>Enlace activo: se envian comandos y se recibe telemetria.</summary>
        Connected = 2,

        /// <summary>El enlace se cayo o fallo al establecerse.</summary>
        Error = 3,
    }
}
