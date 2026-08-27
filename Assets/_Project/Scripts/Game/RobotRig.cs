using System;
using RobotControl.Core.Domain;
using RobotControl.Core.Transport;
using UnityEngine;

namespace RobotControl.Game
{
    /// <summary>Origen de datos del robot.</summary>
    public enum RobotLinkMode
    {
        /// <summary>Modelo local. Unico modo disponible por ahora.</summary>
        Simulated = 0,

        /// <summary>Raspberry Pi por red. Pendiente de decidir el protocolo.</summary>
        RaspberryPi = 1,
    }

    /// <summary>
    /// Punto de union entre el enlace y la escena.
    ///
    /// Su unica responsabilidad es: crear el enlace, avanzarlo cada frame y volcar
    /// la telemetria sobre los Transforms. Nunca mueve el robot por su cuenta; si la
    /// telemetria no cambia, el robot no se mueve. Esa disciplina es lo que hace que
    /// la escena valga igual para la simulacion que para el hardware real.
    /// </summary>
    [DisallowMultipleComponent]
    public class RobotRig : MonoBehaviour
    {
        [Header("Origen de datos")]
        [SerializeField] private RobotLinkMode _mode = RobotLinkMode.Simulated;
        [SerializeField] private bool _connectOnStart = true;

        [Header("Configuracion del robot")]
        [SerializeField] private SimulationSettings _simulation = new SimulationSettings();
        [SerializeField] private ArmLimits _limits = new ArmLimits();

        [Header("Referencias de escena")]
        [Tooltip("Transform del chasis. Si se deja vacio, se usa el de este objeto.")]
        [SerializeField] private Transform _chassis;
        [SerializeField] private ArmVisual _arm;

        /// <summary>Enlace activo. Es con esto con lo que habla el resto del juego.</summary>
        public IRobotLink Link { get; private set; }

        public LinkStatus Status => Link?.Status ?? LinkStatus.Disconnected;
        public RobotTelemetry Telemetry => Link?.Telemetry ?? RobotTelemetry.Empty;

        private IObstacleProbe _obstacles;

        private void Awake()
        {
            if (_chassis == null) _chassis = transform;

            // GetComponent acepta interfaces, asi que el rig no necesita conocer la
            // clase concreta que resuelve las colisiones. Si no hay ninguna, el robot
            // simplemente atraviesa el escenario.
            _obstacles = GetComponent<IObstacleProbe>();

            Link = CreateLink();
        }

        private IRobotLink CreateLink()
        {
            switch (_mode)
            {
                case RobotLinkMode.Simulated:
                    return new SimulatedRobotLink(
                        _simulation,
                        _limits,
                        _chassis.position,
                        HeadingFromTransform(_chassis),
                        _obstacles);

                case RobotLinkMode.RaspberryPi:
                    // Falta decidir el transporte (WebSocket/JSON, MQTT o ROS2). Cuando
                    // se decida, aqui se devuelve un RaspberryPiLink y nada mas cambia.
                    Debug.LogWarning(
                        "[RobotRig] El enlace con la Raspberry Pi todavia no existe. " +
                        "Usando la simulacion.", this);
                    goto case RobotLinkMode.Simulated;

                default:
                    throw new NotSupportedException($"Modo de enlace no soportado: {_mode}");
            }
        }

        /// <summary>
        /// Rumbo del robot en grados, tomado de hacia donde mira de verdad.
        ///
        /// No se usa eulerAngles.y a proposito: si alguien rota el objeto en el editor
        /// con el gizmo libre, la rotacion acaba teniendo componentes en X y Z, y la
        /// descomposicion a angulos de Euler devuelve una Y que NO coincide con la
        /// direccion en la que apunta el robot. Proyectar el forward sobre el plano del
        /// suelo da el rumbo correcto venga como venga la rotacion.
        /// </summary>
        private static float HeadingFromTransform(Transform t)
        {
            Vector3 flat = Vector3.ProjectOnPlane(t.forward, Vector3.up);

            // El robot apunta recto hacia arriba o hacia abajo: no hay rumbo que extraer.
            if (flat.sqrMagnitude < 1e-6f) return 0f;

            return Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        }

        private void Start()
        {
            if (_connectOnStart) Link.Connect();
        }

        private void Update()
        {
            if (Link == null) return;

            Link.Tick(Time.deltaTime);

            if (Link.Status == LinkStatus.Connected) ApplyTelemetry(Link.Telemetry);
        }

        /// <summary>Vuelca la telemetria sobre la escena. La vista solo obedece.</summary>
        private void ApplyTelemetry(RobotTelemetry telemetry)
        {
            _chassis.SetPositionAndRotation(telemetry.Position, telemetry.Rotation);
            if (_arm != null) _arm.Apply(telemetry.Arm);
        }

        private void OnDestroy()
        {
            Link?.Dispose();
            Link = null;
        }

        /// <summary>Asignacion desde codigo, usada por el generador de escena.</summary>
        public void Bind(Transform chassis, ArmVisual arm)
        {
            _chassis = chassis;
            _arm = arm;
        }
    }
}
