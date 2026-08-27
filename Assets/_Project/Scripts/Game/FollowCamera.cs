using UnityEngine;
using UnityEngine.InputSystem;

namespace RobotControl.Game
{
    /// <summary>Como se orienta la camara respecto al robot.</summary>
    public enum CameraFraming
    {
        /// <summary>
        /// Persecucion: la camara gira con el robot y siempre lo mira desde atras.
        /// Conducir es mas intuitivo porque W siempre aleja al robot en pantalla.
        /// </summary>
        Chase = 0,

        /// <summary>
        /// Orientacion fija respecto al mundo: la camara acompana al robot pero no
        /// gira con el. Mas estable para leer el mapa, menos intuitiva para conducir.
        /// </summary>
        Fixed = 1,
    }

    /// <summary>
    /// Camara que sigue al robot con amortiguacion.
    ///
    /// Sigue al Transform y no a la telemetria a proposito: el Transform ya ES el
    /// resultado de la telemetria (lo escribe <see cref="RobotRig"/>), asi que
    /// encadenarse a el mantiene una sola fuente de verdad en lugar de crear una
    /// segunda interpretacion de los mismos datos.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        [Header("Objetivo")]
        [Tooltip("Transform al que seguir. Normalmente la raiz del robot.")]
        [SerializeField] private Transform _target;

        [SerializeField] private CameraFraming _framing = CameraFraming.Chase;

        [Tooltip("Tecla que alterna entre persecucion y camara fija.")]
        [SerializeField] private Key _toggleKey = Key.C;

        [Header("Encuadre")]
        [Tooltip("Posicion de la camara respecto al robot, en metros. Z negativo = por detras.")]
        // La altura (3.2) esta MUY por encima de los muros del laberinto (1.0) a
        // proposito: una camara de persecucion baja se metaria dentro de las paredes
        // cada vez que el robot entra en un pasillo. Mirar desde arriba tambien deja
        // ver los cruces cercanos, que es justo lo que necesita quien conduce.
        [SerializeField] private Vector3 _offset = new Vector3(0f, 3.2f, -3.0f);

        [Tooltip("Altura sobre el robot a la que apunta la camara, en metros.")]
        [SerializeField] private float _lookHeight = 0.5f;

        [Header("Suavizado")]
        [Tooltip("Mayor = la camara alcanza al robot mas rapido. 0 = sin suavizado.")]
        [Min(0f)]
        [SerializeField] private float _positionDamping = 6f;

        [Tooltip("Mayor = la camara corrige su orientacion mas rapido.")]
        [Min(0f)]
        [SerializeField] private float _rotationDamping = 5f;

        /// <summary>Encuadre actual. Lo lee el HUD para mostrarlo al operador.</summary>
        public CameraFraming Framing => _framing;

        private void Start() => SnapToTarget();

        /// <summary>
        /// Alterna entre persecucion y camara fija. No hace falta reencuadrar de
        /// golpe: la amortiguacion hace sola la transicion, y queda mejor.
        /// </summary>
        public void ToggleFraming()
        {
            _framing = _framing == CameraFraming.Chase ? CameraFraming.Fixed : CameraFraming.Chase;
        }

        /// <summary>
        /// La camara lee su propia tecla en vez de pasar por TeleopInput: cambiar el
        /// encuadre no es una orden para el robot, es una preferencia del operador.
        /// Mezclarlas ensuciaria el limite entre lo que se envia y lo que solo se ve.
        /// </summary>
        private void ReadInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard[_toggleKey].wasPressedThisFrame) ToggleFraming();
        }

        /// <summary>
        /// El seguimiento va en LateUpdate y no en Update por una razon concreta:
        /// <see cref="RobotRig"/> mueve al robot durante Update. Si la camara se
        /// moviera tambien en Update, el resultado dependeria del orden de ejecucion
        /// de los componentes y la imagen temblaria. LateUpdate corre siempre despues
        /// de todos los Update, asi que la camara ve al robot ya en su sitio final.
        /// </summary>
        private void LateUpdate()
        {
            ReadInput();
            Track(Time.deltaTime);
        }

        private void Track(float deltaTime)
        {
            if (_target == null) return;

            Quaternion frame = FramingRotation();
            Vector3 desiredPosition = _target.position + frame * _offset;
            Vector3 lookPoint = _target.position + Vector3.up * _lookHeight;

            // Suavizado exponencial en vez de un Lerp con deltaTime crudo: asi el
            // resultado no cambia con los FPS. A 30 o a 144 se ve igual de firme.
            transform.position = Vector3.Lerp(
                transform.position, desiredPosition, Damp(_positionDamping, deltaTime));

            Vector3 toTarget = lookPoint - transform.position;
            if (toTarget.sqrMagnitude < 1e-6f) return; // Camara encima del punto mirado.

            Quaternion desiredRotation = Quaternion.LookRotation(toTarget, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, desiredRotation, Damp(_rotationDamping, deltaTime));
        }

        private Quaternion FramingRotation()
        {
            if (_framing == CameraFraming.Fixed) return Quaternion.identity;

            // Se aplana el forward sobre el plano del suelo para que la camara no
            // herede cabeceos ni balanceos del robot.
            Vector3 flat = Vector3.ProjectOnPlane(_target.forward, Vector3.up);
            if (flat.sqrMagnitude < 1e-6f) return Quaternion.identity;

            return Quaternion.LookRotation(flat.normalized, Vector3.up);
        }

        /// <summary>Factor de interpolacion independiente de los FPS.</summary>
        private static float Damp(float damping, float deltaTime)
        {
            if (damping <= 0f) return 1f; // Sin suavizado: pegada al objetivo.
            return 1f - Mathf.Exp(-damping * deltaTime);
        }

        /// <summary>
        /// Coloca la camara en su sitio de golpe, sin suavizado. Se usa al arrancar
        /// para que no entre volando desde donde quedara guardada en la escena.
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null) return;

            Quaternion frame = FramingRotation();
            transform.position = _target.position + frame * _offset;

            Vector3 toTarget = (_target.position + Vector3.up * _lookHeight) - transform.position;
            if (toTarget.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(toTarget, Vector3.up);
        }

        /// <summary>Asignacion desde codigo, usada por el generador de escena.</summary>
        public void Bind(Transform target)
        {
            _target = target;
            SnapToTarget();
        }
    }
}
