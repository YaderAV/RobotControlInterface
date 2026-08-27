using RobotControl.Core.Transport;
using UnityEngine;

namespace RobotControl.Game
{
    /// <summary>
    /// Resuelve <see cref="IObstacleProbe"/> consultando los colliders de la escena.
    ///
    /// Es la traduccion entre el mundo de Unity y el modelo fisico puro: la
    /// simulacion pregunta "cabe el robot aqui" y este componente lo responde con
    /// una consulta de fisica. Ponlo en el mismo GameObject que <see cref="RobotRig"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class PhysicsObstacleProbe : MonoBehaviour, IObstacleProbe
    {
        [Tooltip("Radio de colision del robot, en metros. Algo menor que su ancho real " +
                 "para que no se enganche en las esquinas.")]
        [Min(0.01f)]
        [SerializeField] private float _radius = 0.45f;

        [Tooltip("Altura a la que se sondea, en metros. Debe quedar por encima del suelo " +
                 "para no chocar contra el propio plano del escenario.")]
        [Min(0f)]
        [SerializeField] private float _probeHeight = 0.5f;

        [Tooltip("Capas que cuentan como obstaculo.")]
        [SerializeField] private LayerMask _obstacleLayers = ~0;

        public bool IsBlocked(Vector3 position)
        {
            // Basta comprobar el destino en vez de barrer el trayecto: a 1.2 m/s y
            // 60 fps el robot avanza 2 cm por frame, muy lejos de poder atravesar un
            // muro de 20 cm. Si algun dia sube mucho la velocidad, esto pasaria a
            // ser un Physics.SphereCast entre origen y destino.
            return Physics.CheckSphere(
                position + Vector3.up * _probeHeight,
                _radius,
                _obstacleLayers,
                QueryTriggerInteraction.Ignore);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * _probeHeight, _radius);
        }
    }
}
