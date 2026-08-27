using RobotControl.Core.Domain;
using UnityEngine;

namespace RobotControl.Game
{
    /// <summary>
    /// Traduce una <see cref="ArmPose"/> a rotaciones de Transforms.
    ///
    /// Es deliberadamente "tonto": no decide nada, solo dibuja la postura que le
    /// dan. Cuando se sustituyan estos cubos por el modelo 3D del brazo real, solo
    /// hay que reasignar los Transforms y ajustar los ejes; ninguna otra clase cambia.
    /// </summary>
    public class ArmVisual : MonoBehaviour
    {
        [Header("Articulaciones")]
        [SerializeField] private Transform _baseJoint;
        [SerializeField] private Transform _shoulderJoint;
        [SerializeField] private Transform _elbowJoint;
        [SerializeField] private Transform _wristJoint;

        [Header("Pinza")]
        [Tooltip("Dedo izquierdo de la pinza. Se desplaza sobre su eje local X.")]
        [SerializeField] private Transform _gripperLeft;
        [Tooltip("Dedo derecho de la pinza. Se desplaza sobre su eje local X.")]
        [SerializeField] private Transform _gripperRight;
        [Tooltip("Separacion de cada dedo respecto al centro con la pinza abierta, en metros.")]
        [SerializeField] private float _gripperOpenOffset = 0.08f;

        [Header("Ejes de giro")]
        [Tooltip("Eje local sobre el que gira la base.")]
        [SerializeField] private Vector3 _baseAxis = Vector3.up;
        [Tooltip("Eje local sobre el que giran hombro, codo y muneca.")]
        [SerializeField] private Vector3 _pitchAxis = Vector3.right;

        // Rotacion de montaje de cada articulacion. Los angulos de ArmPose son
        // relativos a esta pose de reposo, no absolutos en el mundo.
        private Quaternion _baseRest, _shoulderRest, _elbowRest, _wristRest;
        private bool _restCaptured;

        private void Awake() => CaptureRestPose();

        /// <summary>Memoriza la orientacion de montaje de cada articulacion.</summary>
        public void CaptureRestPose()
        {
            if (_baseJoint != null) _baseRest = _baseJoint.localRotation;
            if (_shoulderJoint != null) _shoulderRest = _shoulderJoint.localRotation;
            if (_elbowJoint != null) _elbowRest = _elbowJoint.localRotation;
            if (_wristJoint != null) _wristRest = _wristJoint.localRotation;
            _restCaptured = true;
        }

        /// <summary>Coloca el brazo en la postura indicada.</summary>
        public void Apply(ArmPose pose)
        {
            if (!_restCaptured) CaptureRestPose();

            SetJoint(_baseJoint, _baseRest, _baseAxis, pose.BaseYaw);
            SetJoint(_shoulderJoint, _shoulderRest, _pitchAxis, pose.ShoulderPitch);
            SetJoint(_elbowJoint, _elbowRest, _pitchAxis, pose.ElbowPitch);
            SetJoint(_wristJoint, _wristRest, _pitchAxis, pose.WristPitch);

            float offset = Mathf.Clamp01(pose.Gripper) * _gripperOpenOffset;
            SetFinger(_gripperLeft, -offset);
            SetFinger(_gripperRight, offset);
        }

        private static void SetJoint(Transform joint, Quaternion rest, Vector3 axis, float degrees)
        {
            if (joint == null) return;
            joint.localRotation = rest * Quaternion.AngleAxis(degrees, axis);
        }

        private static void SetFinger(Transform finger, float localX)
        {
            if (finger == null) return;
            Vector3 p = finger.localPosition;
            p.x = localX;
            finger.localPosition = p;
        }

        /// <summary>Asignacion de Transforms desde codigo, usada por el generador de escena.</summary>
        public void Bind(
            Transform baseJoint, Transform shoulder, Transform elbow, Transform wrist,
            Transform gripperLeft, Transform gripperRight)
        {
            _baseJoint = baseJoint;
            _shoulderJoint = shoulder;
            _elbowJoint = elbow;
            _wristJoint = wrist;
            _gripperLeft = gripperLeft;
            _gripperRight = gripperRight;
            CaptureRestPose();
        }
    }
}
