using System.IO;
using RobotControl.Core.Environment;
using RobotControl.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RobotControl.EditorTools
{
    /// <summary>
    /// Genera la escena de demostracion por codigo.
    ///
    /// Se hace asi, y no guardando una escena a mano, por dos motivos: la escena se
    /// puede regenerar identica en cualquier momento, y el "esqueleto" del robot queda
    /// documentado como codigo legible en vez de como YAML opaco. Las primitivas son
    /// un marcador de posicion hasta que exista el modelo 3D del robot real.
    /// </summary>
    public static class DemoSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Demo_Teleop.unity";
        private const string MaterialsFolder = "Assets/_Project/Materials";

        // --- Laberinto ---
        // Impar a proposito: asi existe una celda central exacta y el robot puede
        // arrancar en ella, en el origen del mundo.
        private const int MazeCells = 9;
        private const float CellSize = 3f;        // Pasillos comodos: el robot mide 0.9 x 0.6
        private const float WallHeight = 1.0f;
        private const float WallThickness = 0.2f;
        private const int MazeSeed = 2026;        // Cambiar la semilla = otro laberinto

        // Medidas del robot, en metros. Cambiarlas para acercarse al robot real.
        private const float ChassisHeight = 0.25f;
        private const float UpperArmLength = 0.35f;
        private const float ForearmLength = 0.30f;
        private const float HandLength = 0.10f;

        [MenuItem("Tools/Robot SAR/Generar escena de demo")]
        public static void Generate()
        {
            // Si hay cambios sin guardar, Unity pregunta que hacer. Si el usuario
            // cancela ese dialogo, esto devuelve false y hay que abortar. Antes se
            // abortaba en silencio, y desde fuera era indistinguible de "el menu no
            // hace nada": por eso ahora lo dice.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[Robot SAR] Generacion cancelada: la escena actual " +
                                 "tenia cambios sin guardar y se cancelo el dialogo.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Material groundMat = GetOrCreateMaterial("Demo_Ground", new Color(0.32f, 0.34f, 0.36f));
            Material chassisMat = GetOrCreateMaterial("Demo_Chassis", new Color(0.18f, 0.20f, 0.24f));
            Material armMat = GetOrCreateMaterial("Demo_Arm", new Color(0.90f, 0.45f, 0.10f));

            Material wallMat = GetOrCreateMaterial("Demo_Muro", new Color(0.46f, 0.49f, 0.55f));

            BuildGround(groundMat);
            BuildMaze(wallMat);
            RobotRig rig = BuildRobot(chassisMat, armMat);
            SetUpCamera(rig.transform);

            EnsureFolder(Path.GetDirectoryName(ScenePath).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, ScenePath);

            Selection.activeGameObject = rig.gameObject;
            Debug.Log($"[Robot SAR] Escena generada en {ScenePath}. Pulsa Play y conduce con WASD.");
        }

        private static void BuildGround(Material material)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Suelo";
            ground.transform.localScale = new Vector3(5f, 1f, 5f); // 50 x 50 metros
            ground.GetComponent<Renderer>().sharedMaterial = material;
        }

        /// <summary>
        /// Levanta la geometria del laberinto a partir de la rejilla logica.
        ///
        /// Solo se construyen las paredes sur y oeste de cada celda, mas el borde
        /// norte y este del mapa. Como derribar una pared la quita de las dos celdas
        /// vecinas, ese recorrido cubre cada pared exactamente una vez: sin esta
        /// regla saldrian muros dobles superpuestos en todo el interior.
        /// </summary>
        private static void BuildMaze(Material material)
        {
            MazeGrid maze = MazeGenerator.Generate(MazeCells, MazeCells, MazeSeed);

            var root = new GameObject("Laberinto");
            float half = (MazeCells - 1) * 0.5f;
            float span = CellSize + WallThickness;

            for (int y = 0; y < MazeCells; y++)
            {
                for (int x = 0; x < MazeCells; x++)
                {
                    var center = new Vector3((x - half) * CellSize, 0f, (y - half) * CellSize);

                    if (maze.HasWall(x, y, MazeSide.South))
                        Wall(root.transform, material, $"S_{x}_{y}",
                            center + new Vector3(0f, 0f, -CellSize * 0.5f),
                            new Vector3(span, WallHeight, WallThickness));

                    if (maze.HasWall(x, y, MazeSide.West))
                        Wall(root.transform, material, $"O_{x}_{y}",
                            center + new Vector3(-CellSize * 0.5f, 0f, 0f),
                            new Vector3(WallThickness, WallHeight, span));

                    // Bordes exteriores: no los cubre el recorrido sur/oeste.
                    if (y == MazeCells - 1 && maze.HasWall(x, y, MazeSide.North))
                        Wall(root.transform, material, $"N_{x}_{y}",
                            center + new Vector3(0f, 0f, CellSize * 0.5f),
                            new Vector3(span, WallHeight, WallThickness));

                    if (x == MazeCells - 1 && maze.HasWall(x, y, MazeSide.East))
                        Wall(root.transform, material, $"E_{x}_{y}",
                            center + new Vector3(CellSize * 0.5f, 0f, 0f),
                            new Vector3(WallThickness, WallHeight, span));
                }
            }
        }

        private static void Wall(
            Transform parent, Material material, string name, Vector3 groundCenter, Vector3 size)
        {
            // El collider SI se conserva: es lo que consulta PhysicsObstacleProbe
            // para que el robot no atraviese el laberinto.
            GameObject wall = CreateBox(
                name, parent, groundCenter + Vector3.up * (size.y * 0.5f), size, material,
                keepCollider: true);
            wall.isStatic = true;
        }

        private static RobotRig BuildRobot(Material chassisMat, Material armMat)
        {
            var root = new GameObject("Robot");

            // --- Chasis ---
            GameObject chassis = CreateBox(
                "Chasis", root.transform,
                new Vector3(0f, ChassisHeight * 0.5f, 0f),
                new Vector3(0.60f, ChassisHeight, 0.90f),
                chassisMat);

            // Marca visual del frente, para que el rumbo sea legible de un vistazo.
            CreateBox("Frente", chassis.transform,
                new Vector3(0f, 0f, 0.55f), new Vector3(0.5f, 0.5f, 0.1f), armMat);

            // --- Brazo: cada articulacion es un pivote vacio con su malla como hija ---
            // Los segmentos crecen sobre +Y local, de modo que un pitch positivo sobre
            // el eje X los inclina hacia adelante.
            Transform baseJoint = CreatePivot(
                "J0_Base", root.transform, new Vector3(0f, ChassisHeight, -0.20f));
            CreateBox("Malla_Base", baseJoint, new Vector3(0f, 0.04f, 0f),
                new Vector3(0.22f, 0.08f, 0.22f), armMat);

            Transform shoulder = CreatePivot("J1_Hombro", baseJoint, new Vector3(0f, 0.08f, 0f));
            CreateBox("Malla_Brazo", shoulder, new Vector3(0f, UpperArmLength * 0.5f, 0f),
                new Vector3(0.10f, UpperArmLength, 0.10f), armMat);

            Transform elbow = CreatePivot("J2_Codo", shoulder, new Vector3(0f, UpperArmLength, 0f));
            CreateBox("Malla_Antebrazo", elbow, new Vector3(0f, ForearmLength * 0.5f, 0f),
                new Vector3(0.08f, ForearmLength, 0.08f), armMat);

            Transform wrist = CreatePivot("J3_Muneca", elbow, new Vector3(0f, ForearmLength, 0f));
            CreateBox("Malla_Mano", wrist, new Vector3(0f, HandLength * 0.5f, 0f),
                new Vector3(0.09f, HandLength, 0.06f), armMat);

            // --- Pinza: dos dedos que se separan sobre su eje local X ---
            Transform fingerLeft = CreateBox("Dedo_Izq", wrist,
                new Vector3(-0.04f, HandLength + 0.05f, 0f),
                new Vector3(0.02f, 0.10f, 0.05f), chassisMat).transform;
            Transform fingerRight = CreateBox("Dedo_Der", wrist,
                new Vector3(0.04f, HandLength + 0.05f, 0f),
                new Vector3(0.02f, 0.10f, 0.05f), chassisMat).transform;

            // --- Componentes ---
            var arm = root.AddComponent<ArmVisual>();
            arm.Bind(baseJoint, shoulder, elbow, wrist, fingerLeft, fingerRight);

            var rig = root.AddComponent<RobotRig>();
            rig.Bind(root.transform, arm);

            root.AddComponent<PhysicsObstacleProbe>();
            root.AddComponent<TeleopInput>();
            root.AddComponent<TelemetryHud>();

            return rig;
        }

        private static void SetUpCamera(Transform target)
        {
            Camera camera = Camera.main;
            if (camera == null) return;

            FollowCamera follow = camera.GetComponent<FollowCamera>();
            if (follow == null) follow = camera.gameObject.AddComponent<FollowCamera>();

            // Bind ya deja la camara encuadrada, para que la escena guardada se vea
            // bien en el editor sin necesidad de entrar en Play.
            follow.Bind(target);
        }

        private static Transform CreatePivot(string name, Transform parent, Vector3 localPosition)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = localPosition;
            return pivot.transform;
        }

        private static GameObject CreateBox(
            string name, Transform parent, Vector3 localPosition, Vector3 localScale,
            Material material, bool keepCollider = false)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = localScale;
            box.GetComponent<Renderer>().sharedMaterial = material;

            // El robot se mueve escribiendo Transforms, no con fisica: unos colliders
            // que se teletransportan cada frame solo darian trabajo al motor. Los
            // muros son la excepcion, porque el robot tiene que chocar con ellos.
            if (!keepCollider) Object.DestroyImmediate(box.GetComponent<Collider>());
            return box;
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var material = new Material(shader) { name = name, color = color };

            EnsureFolder(MaterialsFolder);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>
        /// Crea una carpeta dentro de Assets a traves de AssetDatabase.
        /// Directory.CreateDirectory tambien la crearia en disco, pero AssetDatabase
        /// no se enteraria hasta el siguiente Refresh y CreateAsset fallaria.
        /// </summary>
        private static void EnsureFolder(string assetFolderPath)
        {
            if (string.IsNullOrEmpty(assetFolderPath) || AssetDatabase.IsValidFolder(assetFolderPath))
                return;

            string parent = Path.GetDirectoryName(assetFolderPath).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetFolderPath));
        }
    }
}
