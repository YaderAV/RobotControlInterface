using RobotControl.Core.Domain;
using RobotControl.Core.Transport;
using UnityEngine;

namespace RobotControl.Game
{
    /// <summary>
    /// HUD del operador: estado del enlace, navegacion, esquema del brazo y
    /// retroalimentacion de que parte del robot se esta moviendo.
    ///
    /// Sigue siendo IMGUI y sigue siendo provisional (la Fase 3 lo sustituira por
    /// uGUI o UI Toolkit). Se mantiene asi a proposito: no necesita Canvas, prefabs
    /// ni referencias que cablear, asi que la escena se puede regenerar entera sin
    /// perder la interfaz. Como solo consume telemetria, tirarlo mas adelante no
    /// se lleva por delante ninguna logica.
    /// </summary>
    [RequireComponent(typeof(RobotRig))]
    public class TelemetryHud : MonoBehaviour
    {
        // Cuanto sigue encendido un indicador tras dejar de detectarse movimiento.
        // Sin esto los indicadores parpadean en los frames sueltos sin cambio.
        private const float HoldSeconds = 0.15f;

        // --- Paleta ---
        private static readonly Color PanelBg = new Color(0.04f, 0.05f, 0.07f, 0.86f);
        private static readonly Color RowBg = new Color(1f, 1f, 1f, 0.05f);
        private static readonly Color TextMain = new Color(0.90f, 0.93f, 0.96f);
        private static readonly Color TextDim = new Color(0.52f, 0.58f, 0.65f);
        private static readonly Color Accent = new Color(0.95f, 0.55f, 0.15f);
        private static readonly Color Active = new Color(0.25f, 0.85f, 1.00f);
        private static readonly Color Ok = new Color(0.40f, 0.90f, 0.55f);
        private static readonly Color Warn = new Color(1.00f, 0.40f, 0.40f);

        private RobotRig _rig;
        private FollowCamera _camera;
        private bool _cameraSearched;

        // Deteccion de movimiento
        private readonly float[] _hold = new float[RobotActivity.PartCount];
        private readonly float[] _dir = new float[RobotActivity.PartCount];
        private RobotTelemetry _previous;
        private bool _hasPrevious;

        // Estilos, reconstruidos si cambia la resolucion
        private GUIStyle _title, _body, _bodyRight, _big, _key, _small;
        private float _scale;
        private int _builtForHeight = -1;

        private void Awake() => _rig = GetComponent<RobotRig>();

        // ------------------------------------------------------------------
        //  Deteccion de movimiento. Va en Update y no en OnGUI porque OnGUI se
        //  llama varias veces por frame y Time.deltaTime no es fiable ahi.
        // ------------------------------------------------------------------
        private void Update()
        {
            if (_rig == null) return;

            float dt = Time.deltaTime;
            for (int i = 0; i < _hold.Length; i++) _hold[i] = Mathf.Max(0f, _hold[i] - dt);

            if (_rig.Status != LinkStatus.Connected)
            {
                _hasPrevious = false;
                return;
            }

            RobotTelemetry now = _rig.Telemetry;

            if (_hasPrevious)
            {
                RobotActivity a = RobotActivity.Between(_previous, now, dt);

                Mark(a, RobotPart.Drive, now.SpeedMetersPerSecond);
                Mark(a, RobotPart.Turn, Mathf.DeltaAngle(_previous.HeadingDegrees, now.HeadingDegrees));
                Mark(a, RobotPart.ArmBase, now.Arm.BaseYaw - _previous.Arm.BaseYaw);
                Mark(a, RobotPart.Shoulder, now.Arm.ShoulderPitch - _previous.Arm.ShoulderPitch);
                Mark(a, RobotPart.Elbow, now.Arm.ElbowPitch - _previous.Arm.ElbowPitch);
                Mark(a, RobotPart.Wrist, now.Arm.WristPitch - _previous.Arm.WristPitch);
                Mark(a, RobotPart.Gripper, now.Arm.Gripper - _previous.Arm.Gripper);
            }

            _previous = now;
            _hasPrevious = true;
        }

        private void Mark(RobotActivity activity, RobotPart part, float delta)
        {
            if (!activity.IsMoving(part)) return;
            _hold[(int)part] = HoldSeconds;
            _dir[(int)part] = Mathf.Sign(delta);
        }

        private bool Moving(RobotPart part) => _hold[(int)part] > 0f;
        private float Dir(RobotPart part) => Moving(part) ? _dir[(int)part] : 0f;

        // ------------------------------------------------------------------
        //  Dibujo
        // ------------------------------------------------------------------
        private void OnGUI()
        {
            if (_rig == null) return;
            EnsureStyles();

            float m = 18f * _scale;
            float w = Mathf.Max(280f * _scale, Screen.width * 0.20f);

            DrawStatusPanel(new Rect(m, m, w, 132f * _scale));

            if (_rig.Status != LinkStatus.Connected)
            {
                DrawCenterNotice();
                return;
            }

            float navH = 175f * _scale;
            DrawNavPanel(new Rect(Screen.width - w - m, m, w, navH));

            float armH = 405f * _scale;
            DrawArmPanel(new Rect(m, Screen.height - armH - m, w, armH));

            float ctrlH = 355f * _scale;
            DrawControlsPanel(new Rect(Screen.width - w - m, Screen.height - ctrlH - m, w, ctrlH));
        }

        private void DrawStatusPanel(Rect rect)
        {
            float pad = 12f * _scale;
            float y = BeginPanel(rect, "ENLACE");

            LinkStatus status = _rig.Status;
            Color statusColor = status switch
            {
                LinkStatus.Connected => Ok,
                LinkStatus.Connecting => Accent,
                LinkStatus.Error => Warn,
                _ => TextDim,
            };

            Label(new Rect(rect.x + pad, y, rect.width - pad * 2f, _big.fontSize * 1.3f),
                StatusText(status), _big, statusColor);
            y += _big.fontSize * 1.45f;

            if (status != LinkStatus.Connected) return;

            float battery = _rig.Telemetry.BatteryPercent;
            Color batteryColor = _rig.Telemetry.IsBatteryLow ? Warn : Ok;

            Label(new Rect(rect.x + pad, y, rect.width * 0.5f, _small.fontSize * 1.5f),
                "BATERIA", _small, TextDim);
            Label(new Rect(rect.x + rect.width * 0.5f - pad, y, rect.width * 0.5f, _small.fontSize * 1.5f),
                battery.ToString("F0") + "%", _bodyRight, batteryColor);
            y += _small.fontSize * 1.7f;

            Bar(new Rect(rect.x + pad, y, rect.width - pad * 2f, 12f * _scale),
                battery / 100f, batteryColor);
        }

        private void DrawNavPanel(Rect rect)
        {
            float pad = 12f * _scale;
            float y = BeginPanel(rect, "NAVEGACION");
            RobotTelemetry t = _rig.Telemetry;

            float speed01 = Mathf.Clamp01(Mathf.Abs(t.SpeedMetersPerSecond) / 1.2f);

            Row(rect, ref y, "VELOCIDAD", t.SpeedMetersPerSecond.ToString("F2") + " m/s",
                Moving(RobotPart.Drive) ? Active : TextMain);
            Bar(new Rect(rect.x + pad, y, rect.width - pad * 2f, 8f * _scale), speed01,
                Moving(RobotPart.Drive) ? Active : TextDim);
            y += 18f * _scale;

            Row(rect, ref y, "RUMBO",
                t.HeadingDegrees.ToString("F0") + " grados  " + Compass(t.HeadingDegrees),
                Moving(RobotPart.Turn) ? Active : TextMain);
            Row(rect, ref y, "POSICION",
                "X " + t.Position.x.ToString("F1") + "   Z " + t.Position.z.ToString("F1"), TextMain);
        }

        private void DrawArmPanel(Rect rect)
        {
            float pad = 12f * _scale;
            float y = BeginPanel(rect, "BRAZO MANIPULADOR");
            ArmPose arm = _rig.Telemetry.Arm;

            float diagramH = 170f * _scale;
            DrawArmDiagram(new Rect(rect.x + pad, y, rect.width - pad * 2f, diagramH), arm);
            y += diagramH + 10f * _scale;

            JointRow(rect, ref y, RobotPart.ArmBase, "BASE", arm.BaseYaw.ToString("F0") + " grados");
            JointRow(rect, ref y, RobotPart.Shoulder, "HOMBRO", arm.ShoulderPitch.ToString("F0") + " grados");
            JointRow(rect, ref y, RobotPart.Elbow, "CODO", arm.ElbowPitch.ToString("F0") + " grados");
            JointRow(rect, ref y, RobotPart.Wrist, "MUNECA", arm.WristPitch.ToString("F0") + " grados");
            JointRow(rect, ref y, RobotPart.Gripper, "PINZA",
                (arm.Gripper > 0.5f ? "abierta " : "cerrada ") + arm.Gripper.ToString("P0"));
        }

        /// <summary>
        /// Esquema del brazo visto de perfil. Los angulos son acumulativos porque
        /// cada articulacion cuelga de la anterior: el codo hereda la inclinacion
        /// del hombro, igual que en la jerarquia de Transforms de la escena.
        /// </summary>
        private void DrawArmDiagram(Rect area, ArmPose arm)
        {
            Fill(area, RowBg);

            float unit = area.height * 0.30f;
            var origin = new Vector2(area.x + area.width * 0.34f, area.yMax - 26f * _scale);

            // Suelo y chasis, para dar contexto al esquema
            Fill(new Rect(area.x + 8f * _scale, origin.y + 12f * _scale,
                area.width - 16f * _scale, 2f * _scale), new Color(1f, 1f, 1f, 0.14f));
            Fill(new Rect(origin.x - 26f * _scale, origin.y, 52f * _scale, 12f * _scale),
                new Color(0.35f, 0.38f, 0.44f, 0.9f));

            float a1 = arm.ShoulderPitch;
            float a2 = a1 + arm.ElbowPitch;
            float a3 = a2 + arm.WristPitch;

            Vector2 p1 = Bone(origin, a1, unit * 1.15f, 9f * _scale, BoneColor(RobotPart.Shoulder));
            Vector2 p2 = Bone(p1, a2, unit * 1.00f, 7f * _scale, BoneColor(RobotPart.Elbow));
            Vector2 p3 = Bone(p2, a3, unit * 0.34f, 6f * _scale, BoneColor(RobotPart.Wrist));

            // Dedos de la pinza: se abren en angulo segun la apertura
            float spread = 7f + arm.Gripper * 20f;
            Color gripColor = BoneColor(RobotPart.Gripper);
            Bone(p3, a3 - spread, unit * 0.20f, 4f * _scale, gripColor);
            Bone(p3, a3 + spread, unit * 0.20f, 4f * _scale, gripColor);

            Joint(origin, 6f * _scale, Moving(RobotPart.Shoulder));
            Joint(p1, 5f * _scale, Moving(RobotPart.Elbow));
            Joint(p2, 4.5f * _scale, Moving(RobotPart.Wrist));

            // Vista en planta del giro de la base, en una esquina del esquema
            var topView = new Vector2(area.xMax - 34f * _scale, area.y + 40f * _scale);
            Label(new Rect(topView.x - 40f * _scale, area.y + 4f * _scale, 80f * _scale,
                _small.fontSize * 1.4f), "PLANTA", _small, TextDim);
            Fill(new Rect(topView.x - 22f * _scale, topView.y - 1f * _scale, 44f * _scale, 2f * _scale),
                new Color(1f, 1f, 1f, 0.12f));
            Bone(topView, arm.BaseYaw, 26f * _scale, 5f * _scale, BoneColor(RobotPart.ArmBase));
            Joint(topView, 4f * _scale, Moving(RobotPart.ArmBase));
        }

        private void DrawControlsPanel(Rect rect)
        {
            float pad = 12f * _scale;
            float y = BeginPanel(rect, "CONTROLES");

            // Cruceta de conduccion. Se enciende la tecla que corresponde al
            // movimiento REAL del robot, no a la tecla que se esta pulsando.
            float cap = 34f * _scale;
            float cx = rect.x + rect.width * 0.5f;
            float speed = _rig.Telemetry.SpeedMetersPerSecond;

            KeyCap(new Rect(cx - cap * 0.5f, y, cap, cap), "W", Moving(RobotPart.Drive) && speed > 0f);
            y += cap + 4f * _scale;
            KeyCap(new Rect(cx - cap * 1.6f, y, cap, cap), "A",
                Moving(RobotPart.Turn) && Dir(RobotPart.Turn) < 0f);
            KeyCap(new Rect(cx - cap * 0.5f, y, cap, cap), "S", Moving(RobotPart.Drive) && speed < 0f);
            KeyCap(new Rect(cx + cap * 0.6f, y, cap, cap), "D",
                Moving(RobotPart.Turn) && Dir(RobotPart.Turn) > 0f);
            y += cap + 14f * _scale;

            ControlRow(rect, ref y, RobotPart.ArmBase, "< >", "Base");
            ControlRow(rect, ref y, RobotPart.Shoulder, "^ v", "Hombro");
            ControlRow(rect, ref y, RobotPart.Elbow, "Q E", "Codo");
            ControlRow(rect, ref y, RobotPart.Wrist, "R F", "Muneca");
            ControlRow(rect, ref y, RobotPart.Gripper, "ESP", "Pinza");

            Label(new Rect(rect.x + pad, y + 4f * _scale, rect.width - pad * 2f, _small.fontSize * 1.5f),
                "H  -  brazo a reposo", _small, TextDim);
            y += _small.fontSize * 1.6f;

            Label(new Rect(rect.x + pad, y + 4f * _scale, rect.width - pad * 2f, _small.fontSize * 1.5f),
                "C  -  camara: " + CameraModeText(), _small, TextDim);
        }

        /// <summary>
        /// Busca la camara una sola vez y se queda con el resultado, incluido el
        /// fallo: si no hay FollowCamera en la escena no tiene sentido preguntar
        /// por ella en cada frame.
        /// </summary>
        private string CameraModeText()
        {
            if (!_cameraSearched)
            {
                _cameraSearched = true;
                if (Camera.main != null) _camera = Camera.main.GetComponent<FollowCamera>();
            }

            if (_camera == null) return "-";
            return _camera.Framing == CameraFraming.Chase ? "PERSECUCION" : "FIJA";
        }

        // ------------------------------------------------------------------
        //  Piezas reutilizables
        // ------------------------------------------------------------------
        private float BeginPanel(Rect rect, string title)
        {
            float pad = 12f * _scale;
            Fill(rect, PanelBg);
            Fill(new Rect(rect.x, rect.y, rect.width, 2f * _scale), Accent);
            Label(new Rect(rect.x + pad, rect.y + pad * 0.7f, rect.width - pad * 2f,
                _title.fontSize * 1.5f), title, _title, Accent);
            return rect.y + pad * 0.7f + _title.fontSize * 1.9f;
        }

        private void Row(Rect panel, ref float y, string label, string value, Color valueColor)
        {
            float pad = 12f * _scale;
            float h = _body.fontSize * 1.6f;
            Label(new Rect(panel.x + pad, y, panel.width * 0.45f, h), label, _small, TextDim);
            Label(new Rect(panel.x + panel.width * 0.4f - pad, y, panel.width * 0.6f, h),
                value, _bodyRight, valueColor);
            y += h + 3f * _scale;
        }

        private void JointRow(Rect panel, ref float y, RobotPart part, string label, string value)
        {
            float pad = 12f * _scale;
            float h = _body.fontSize * 1.7f;
            bool on = Moving(part);
            var row = new Rect(panel.x + pad, y, panel.width - pad * 2f, h);

            if (on) Fill(row, new Color(Active.r, Active.g, Active.b, 0.16f));

            // Barra lateral: la senal mas rapida de leer de un vistazo
            Fill(new Rect(row.x, row.y, 3f * _scale, row.height),
                on ? Active : new Color(1f, 1f, 1f, 0.10f));

            Label(new Rect(row.x + 9f * _scale, row.y, row.width * 0.5f, h), label, _body,
                on ? Active : TextMain);
            Label(new Rect(row.x, row.y, row.width - 6f * _scale, h), value, _bodyRight,
                on ? Active : TextDim);
            y += h + 3f * _scale;
        }

        private void ControlRow(Rect panel, ref float y, RobotPart part, string keys, string label)
        {
            float pad = 12f * _scale;
            float h = 30f * _scale;
            bool on = Moving(part);
            var row = new Rect(panel.x + pad, y, panel.width - pad * 2f, h);

            KeyCap(new Rect(row.x, row.y, 58f * _scale, h - 4f * _scale), keys, on);
            Label(new Rect(row.x + 68f * _scale, row.y, row.width - 68f * _scale, h - 4f * _scale),
                label, _body, on ? Active : TextDim);
            y += h + 2f * _scale;
        }

        private void KeyCap(Rect rect, string key, bool lit)
        {
            Fill(rect, lit ? Active : new Color(1f, 1f, 1f, 0.08f));
            Label(rect, key, _key, lit ? new Color(0.03f, 0.05f, 0.08f) : TextDim);
        }

        private void Bar(Rect rect, float t01, Color fill)
        {
            Fill(rect, new Color(1f, 1f, 1f, 0.10f));
            Fill(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(t01), rect.height), fill);
        }

        private void Joint(Vector2 center, float radius, bool active)
        {
            float d = radius * 2f;
            Fill(new Rect(center.x - radius, center.y - radius, d, d),
                active ? Active : new Color(0.75f, 0.78f, 0.82f, 0.95f));
        }

        private Color BoneColor(RobotPart part) => Moving(part) ? Active : Accent;

        /// <summary>
        /// Dibuja un segmento desde <paramref name="origin"/> con un angulo medido
        /// desde la vertical (positivo = hacia la derecha) y devuelve su extremo.
        /// </summary>
        private static Vector2 Bone(Vector2 origin, float angleDegrees, float length,
            float thickness, Color color)
        {
            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(angleDegrees, origin);
            Fill(new Rect(origin.x - thickness * 0.5f, origin.y - length, thickness, length), color);
            GUI.matrix = saved;

            float rad = angleDegrees * Mathf.Deg2Rad;
            return origin + new Vector2(Mathf.Sin(rad) * length, -Mathf.Cos(rad) * length);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void Label(Rect rect, string text, GUIStyle style, Color color)
        {
            Color previous = style.normal.textColor;
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
            style.normal.textColor = previous;
        }

        private void DrawCenterNotice()
        {
            var rect = new Rect(0f, Screen.height * 0.45f, Screen.width, _big.fontSize * 2f);
            Label(rect, StatusText(_rig.Status), _big, Accent);
        }

        private static string StatusText(LinkStatus status) => status switch
        {
            LinkStatus.Disconnected => "SIN ENLACE",
            LinkStatus.Connecting => "CONECTANDO...",
            LinkStatus.Connected => "CONECTADO",
            LinkStatus.Error => "ERROR DE ENLACE",
            _ => status.ToString().ToUpperInvariant(),
        };

        private static string Compass(float heading)
        {
            string[] points = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };
            int index = Mathf.RoundToInt(Mathf.Repeat(heading, 360f) / 45f) % 8;
            return points[index];
        }

        // ------------------------------------------------------------------
        //  Estilos: se reconstruyen solo si cambia la altura de pantalla, para
        //  que el HUD escale igual en 1080p que en 4K.
        // ------------------------------------------------------------------
        private void EnsureStyles()
        {
            if (_builtForHeight == Screen.height && _body != null) return;

            _builtForHeight = Screen.height;
            _scale = Mathf.Clamp(Screen.height / 900f, 0.9f, 2.4f);

            _body = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(19f * _scale),
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 0, 0),
                wordWrap = false,
            };
            _bodyRight = new GUIStyle(_body) { alignment = TextAnchor.MiddleRight };
            _small = new GUIStyle(_body) { fontSize = Mathf.RoundToInt(14f * _scale) };
            _title = new GUIStyle(_body)
            {
                fontSize = Mathf.RoundToInt(15f * _scale),
                fontStyle = FontStyle.Bold,
            };
            _big = new GUIStyle(_body)
            {
                fontSize = Mathf.RoundToInt(30f * _scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _key = new GUIStyle(_body)
            {
                fontSize = Mathf.RoundToInt(15f * _scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
        }
    }
}
