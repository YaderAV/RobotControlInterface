using RobotControl.Core.Domain;
using RobotControl.Core.Transport;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace RobotControl.Game
{
    /// <summary>
    /// Teleoperacion por teclado.
    ///
    /// Solo traduce pulsaciones a ordenes del dominio y las entrega al enlace: no
    /// mueve nada por su cuenta ni consulta Transforms. Sustituir esto por un joystick
    /// tactil o un mando fisico no obliga a tocar ninguna otra clase.
    /// </summary>
    [RequireComponent(typeof(RobotRig))]
    public class TeleopInput : MonoBehaviour
    {
        [Header("Sensibilidad del brazo")]
        [Tooltip("Grados por segundo que se pide mover una articulacion mientras se mantiene la tecla.")]
        [SerializeField] private float _jointStepPerSecond = 45f;

        private RobotRig _rig;
        private ArmPose _armTarget = ArmPose.Home;
        private bool _targetSynced;

        private void Awake() => _rig = GetComponent<RobotRig>();

        private void Update()
        {
            IRobotLink link = _rig.Link;
            if (link == null || link.Status != LinkStatus.Connected)
            {
                _targetSynced = false;
                return;
            }

            // Al conectar, partimos de donde el brazo esta de verdad y no de una
            // suposicion: asi el brazo no pega un salto en el primer frame.
            if (!_targetSynced)
            {
                _armTarget = link.Telemetry.Arm;
                _targetSynced = true;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return; // Sin teclado (build headless o mando desconectado).

            link.SendDrive(ReadDrive(keyboard));

            // El objetivo local se recorta con los MISMOS topes que aplica el enlace.
            // Sin esto, mantener la tecla pasado el tope hace que _armTarget siga
            // creciendo sin limite mientras el brazo ya esta parado, y al invertir hay
            // que desenrollar todo ese exceso antes de que se mueva: parece trabado.
            _armTarget = link.Limits.Clamp(ReadArmTarget(keyboard, _armTarget, Time.deltaTime));
            link.SendArmTarget(_armTarget);
        }

        private static DriveCommand ReadDrive(Keyboard keyboard)
        {
            float forward = 0f;
            float turn = 0f;

            if (keyboard.wKey.isPressed) forward += 1f;
            if (keyboard.sKey.isPressed) forward -= 1f;
            if (keyboard.dKey.isPressed) turn += 1f;
            if (keyboard.aKey.isPressed) turn -= 1f;

            return DriveCommand.Clamped(forward, turn);
        }

        private ArmPose ReadArmTarget(Keyboard keyboard, ArmPose current, float deltaTime)
        {
            if (keyboard.hKey.wasPressedThisFrame) return ArmPose.Home;

            float step = _jointStepPerSecond * deltaTime;

            float baseYaw = Axis(keyboard.rightArrowKey, keyboard.leftArrowKey) * step;
            float shoulder = Axis(keyboard.upArrowKey, keyboard.downArrowKey) * step;
            float elbow = Axis(keyboard.eKey, keyboard.qKey) * step;
            float wrist = Axis(keyboard.rKey, keyboard.fKey) * step;

            ArmPose next = current.Offset(baseYaw, shoulder, elbow, wrist, 0f);

            // La pinza es un interruptor, no un eje: se abre o se cierra del todo.
            if (keyboard.spaceKey.wasPressedThisFrame)
                next.Gripper = current.Gripper > 0.5f ? 0f : 1f;

            return next;
        }

        private static float Axis(ButtonControl positive, ButtonControl negative)
        {
            float value = 0f;
            if (positive.isPressed) value += 1f;
            if (negative.isPressed) value -= 1f;
            return value;
        }
    }
}
