# Robot SAR — Interfaz de control

Plantilla educativa de Unity que evoluciona hacia el control gamificado de un robot
de busqueda y rescate con brazo manipulador gobernado por una Raspberry Pi.

- **Unity:** 6000.3.10f1 (URP 17.3)
- **Plataforma objetivo:** PC / Windows standalone
- **Estado:** simulacion funcionando; enlace con hardware pendiente

---

## Como probarlo

1. Abre el proyecto en Unity.
2. Menu **Tools → Robot SAR → Generar escena de demo**.
3. Pulsa **Play**.

| Tecla        | Accion                       |
|--------------|------------------------------|
| `W` / `S`    | Avanzar / retroceder         |
| `A` / `D`    | Girar izquierda / derecha    |
| `←` / `→`    | Base del brazo               |
| `↑` / `↓`    | Hombro                       |
| `Q` / `E`    | Codo                         |
| `R` / `F`    | Muneca                       |
| `Espacio`    | Abrir / cerrar pinza         |
| `H`          | Brazo a posicion de reposo   |
| `C`          | Alternar camara persecucion / fija |

---

## Arquitectura

La decision de diseno que sostiene todo el proyecto es una sola:

> **Nada por encima del transporte sabe si al otro lado hay una simulacion o un robot real.**

```
Assets/_Project/Scripts/
├── Core/                        (asmdef RobotControl.Core — sin MonoBehaviour)
│   ├── Domain/
│   │   ├── ArmPose.cs           Angulos del brazo. El mismo dato que se dibuja
│   │   │                        en pantalla es el que ira a los servos.
│   │   ├── ArmLimits.cs         Topes mecanicos. Protegen al brazo fisico.
│   │   ├── DriveCommand.cs      Traccion normalizada [-1, 1].
│   │   ├── RobotTelemetry.cs    Estado del robot. UNICA fuente de verdad.
│   │   └── RobotActivity.cs     Que partes se mueven, DERIVADO de la telemetria.
│   ├── Transport/
│   │   ├── IRobotLink.cs        ◄── EL CONTRATO. La pieza central.
│   │   ├── SimulatedRobotLink.cs    Modelo local. Hace de plantilla educativa.
│   │   ├── SimulationSettings.cs    Parametros del gemelo digital.
│   │   ├── IObstacleProbe.cs    Pregunta al mundo si el robot cabe.
│   │   └── LinkStatus.cs
│   └── Environment/
│       ├── MazeGrid.cs          Rejilla de celdas con paredes.
│       └── MazeGenerator.cs     Laberinto perfecto, determinista por semilla.
│
├── Game/                        (asmdef RobotControl.Game)
│   ├── RobotRig.cs              Crea el enlace, lo avanza, vuelca telemetria.
│   ├── ArmVisual.cs             ArmPose → Transforms. Deliberadamente tonto.
│   ├── TeleopInput.cs           Teclado → ordenes del dominio.
│   ├── TelemetryHud.cs          HUD del operador (IMGUI). Provisional.
│   ├── FollowCamera.cs          Camara de persecucion amortiguada.
│   └── PhysicsObstacleProbe.cs  Resuelve las colisiones con los colliders.
│
└── Editor/                      (asmdef RobotControl.Editor)
    └── DemoSceneBuilder.cs      Genera la escena por codigo, no a mano.
```

### Por que la telemetria manda

La escena **nunca** decide donde esta el robot: solo dibuja lo que dice
`RobotTelemetry`. En simulacion esa telemetria la produce `SimulatedRobotLink`;
con hardware la producira la Raspberry Pi. Como el consumidor es el mismo, el dia
que se enchufe el robot real no hay que reescribir la vista ni la UI.

Ese es tambien el motivo de que `IRobotLink` tenga un metodo `Tick`: la simulacion
lo usa para integrar su modelo fisico y el enlace real lo usara para vaciar la cola
de mensajes de red en el hilo principal. Mismo contrato, mismo ciclo de vida,
incluidos los estados `Connecting` y `Error`.

---

## Hoja de ruta

- [x] **Fase 0 — Cimientos.** Higiene de git, capas, dominio, `IRobotLink`,
      simulacion, teleoperacion y escena generada por codigo.
- [~] **Fase 1 — Escenario SAR.** Hecho: laberinto con colisiones y camara de
      persecucion. Falta: victimas ocultas, escombros, zona de extraccion y NavMesh
      (`com.unity.ai.navigation` ya esta instalado).
- [ ] **Fase 2 — Gamificacion.** Misiones, deteccion de victimas, puntuacion,
      limite de tiempo y bateria como recurso, progresion por niveles.
- [ ] **Fase 3 — UI definitiva.** Sustituir `TelemetryHud` por uGUI o UI Toolkit.
- [ ] **Fase 4 — Cinematica del brazo.** IK para apuntar la pinza a un objetivo en
      vez de mover articulacion por articulacion.
- [ ] **Fase 5 — Enlace real.** `RaspberryPiLink : IRobotLink`. **Transporte aun
      sin decidir** (WebSocket+JSON, MQTT o ROS2/rosbridge).
- [ ] **Fase 6 — Tests.** `Core` no depende de MonoBehaviour: es comprobable con
      el Test Framework, ya instalado.

---

## Notas de repositorio

- **Git LFS**: `.gitattributes` envia modelos, texturas y audio a LFS. Ejecuta
  `git lfs install` una vez antes de anadir binarios.
- **Fusion de escenas**: `.gitattributes` declara `merge=unityyamlmerge`. Para
  activarlo hay que registrar el driver de Unity (`UnityYAMLMerge.exe`) en la
  configuracion de git.
- Los `.csproj` y `.sln` **no se versionan**: Unity los regenera.
