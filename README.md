# Dragon AR

A mobile augmented-reality client for the **Dragon Eden** environmental monitoring network.
Scanning a printed marker affixed to a monitoring station spawns a rigged 3D dragon that
stays world-anchored beside the station while orbiting bubbles render live sensor readings
pulled from ThingsBoard.

Built with Unity 6 and AR Foundation 6, targeting Android (ARCore) and iOS (ARKit).

---

## 1. Runtime behavior

The application starts with an empty camera feed — nothing is rendered until a marker is
recognized. The sequence is:

1. `AppBootstrapper` runs automatically via `[RuntimeInitializeOnLoadMethod]`. No scene
   wiring is required; the composition root is code-driven rather than prefab-driven.
2. `ImageTargetTrackingManager` attaches an `ARTrackedImageManager` and waits for the
   reference image to be detected.
3. On first detection, the dragon is placed **0.55 m to the viewer's right of the marker**,
   at the marker's own height. "Right" is derived from the camera-to-marker vector via
   `Vector3.Cross(Vector3.up, towardMarker)` rather than from the tracked image's local
   axes, because ARCore and ARKit do not agree on tracked-image axis conventions.
4. The spawned pivot receives an `ARAnchor` so it remains fixed to the spatial map as
   tracking refines. The dragon does not follow the camera.
5. A ring of metric bubbles is parented to the dragon pivot and begins polling telemetry.
6. Plane detection runs in parallel; once a surface is resolved, detection stops and the
   plane visualization is hidden to keep the frame clean.

When the scene contains no `ARTrackedImageManager` — for example an Editor test scene — the
dragon spawns directly in front of the camera instead, so creature behavior can be iterated
without a marker.

---

## 2. Technology stack

| Component | Version | Notes |
|---|---|---|
| Unity Editor | **6000.5.8f1** | Pinned in `ProjectSettings/ProjectVersion.txt`. Other versions trigger reimport warnings. |
| AR Foundation | 6.5.0 | Provider-agnostic AR API surface |
| ARCore XR Plugin | 6.5.0 | Android provider |
| ARKit XR Plugin | 6.5.0 | iOS provider |
| XR Plug-in Management | 4.6.0 | Provider loader configuration |
| Universal Render Pipeline | 17.5.0 | Custom shaders are URP-specific |
| XR Interaction Toolkit | 3.5.1 | Sample assets only |
| Android Logcat | 1.4.7 | On-device log capture |
| Newtonsoft JSON | via package | ThingsBoard response parsing |

---

## 3. Architecture

Code is split into six assembly definitions enforcing a strict one-way dependency graph.
Feature modules never reference one another; they communicate only through contracts
declared in `DragonAR.Core` and through Unity types such as `Pose`.

```
                    DragonAR.App          (composition root)
                          |
        +-----------------+-----------------+-----------------+
        |                 |                 |                 |
  DragonAR.AR      DragonAR.Creature  DragonAR.Telemetry  DragonAR.UI
        |                 |                 |                 |
        +-----------------+--------+--------+-----------------+
                                   |
                            DragonAR.Core     (no dependencies)
```

| Assembly | Responsibility | External references |
|---|---|---|
| `DragonAR.Core` | Shared contracts and geometry helpers: `ITelemetrySource`, `TelemetrySample`, `TelemetryHistory`, `TrackableSurfaceInfo`, `BoundsScaler` | none |
| `DragonAR.AR` | AR Foundation adapters: `ImageTargetTrackingManager`, `SurfaceTrackingManager`, `ArAnchorService` | AR Foundation, AR Subsystems, XR Core Utils |
| `DragonAR.Creature` | Dragon instantiation and ground-glow effect: `DragonSpawner`, `GroundGlowEffect` | Core only |
| `DragonAR.Telemetry` | Sensor data sources: `ThingsBoardTelemetrySource`, `SimulatedTelemetrySource`, `ThingsBoardConfig` | Newtonsoft JSON |
| `DragonAR.UI` | World-space presentation: `MetricBubbleRing`, `MetricDisplay`, `WorldPanelBillboard` | TextMeshPro, uGUI, Input System |
| `DragonAR.App` | The only assembly permitted to reference all feature modules; owns startup wiring | all of the above |

`DragonAR.App` is the single composition root. If that assembly is absent the project still
compiles cleanly but renders nothing at runtime — a failure mode worth recognizing, since an
overly broad `*.app` pattern in `.gitignore` previously excluded the entire folder from
version control.

### Component initialization convention

Components added via `AddComponent` are configured through an explicit `Init()` or
`Configure()` call rather than relying on serialized fields. Unity invokes `OnEnable`
synchronously inside `AddComponent`, before the caller can assign any field, so
initialization logic placed in `OnEnable` observes null dependencies. This pattern is
applied consistently across `ImageTargetTrackingManager`, `ThingsBoardTelemetrySource`, and
`SimulatedTelemetrySource`.

---

## 4. Repository layout

```
Assets/
  DragonAR.Core/        shared contracts, no dependencies
  DragonAR.AR/          AR Foundation adapters
  DragonAR.Creature/    dragon spawning and effects
  DragonAR.Telemetry/   ThingsBoard and simulated sources
  DragonAR.UI/          world-space metric bubbles
  DragonAR.App/         composition root
  Art/
    Creatures/Dragon/   FBX meshes, textures, material, animator, PF_Dragon prefab
    Markers/            marker PNGs and DragonEdenImageLibrary.asset
    Shaders/            S_BubbleWater.shader, S_GroundGlow.shader
  Resources/            M_Bubble.mat, M_GroundGlow.mat
  Scenes/SampleScene.unity
3D-Model/Dragon-AI/     Blender sources (rig and animation authoring)
tools/ar-marker-generator/   Python marker generator and quality scorer
```

`.claude/`, `.mcp.json`, `context/`, and `docs/` are intentionally excluded from version
control. They contain local development tooling and working notes rather than application
sources, and each machine maintains its own copy.

### Shader note

Both custom shaders are hand-written URP HLSL rather than Shader Graph assets, and their
materials are committed under `Assets/Resources/`. Materials must exist as assets: a shader
referenced only through `Shader.Find()` is removed by shader stripping in IL2CPP builds and
renders magenta on device.

---

## 5. Telemetry configuration

`AppBootstrapper` loads `Resources/ThingsBoardConfig`. When the asset is present and
complete, readings come from the live server; otherwise the application falls back to
`SimulatedTelemetrySource` and remains fully functional offline.

Create the asset via **Assets → Create → DragonAR → ThingsBoard Config** at
`Assets/Resources/ThingsBoardConfig.asset`. It is gitignored because it holds credentials.
`.env.example` at the repository root documents the expected fields.

| Field | Purpose |
|---|---|
| `Host` | ThingsBoard base URL |
| `DeviceId` | Target device UUID |
| `Username` / `Password` | Account used to obtain and refresh a JWT automatically |
| `JwtOverride` | Pre-issued JWT for short-lived testing only; expires within hours |
| `PollSeconds` | Poll interval, clamped to a 5 s minimum (default 10 s) |

### Metrics displayed

| Key | Unit | Key | Unit |
|---|---|---|---|
| `temperature` | °C | `pm25` | µg/m³ |
| `humidity` | % | `pm10` | µg/m³ |
| `co2` | ppm | `noise` | dB |

Keys must match the ThingsBoard timeseries keys exactly. The set is declared once in
`AppBootstrapper.BubbleMetrics`; display labels are Vietnamese.

### Credential handling

A device access token authorizes telemetry **upload** only — `GET .../values/timeseries`
returns 401 with that credential. Reading timeseries requires a user JWT.

Anything compiled into the APK is recoverable; IL2CPP does not protect embedded strings.
Configure a Customer user scoped to read-only access on the single target device rather than
a tenant administrator account. For stronger isolation, place a minimal proxy between the
application and ThingsBoard so credentials never leave the server.

---

## 6. Image target

The reference image library `Assets/Art/Markers/DragonEdenImageLibrary.asset` declares one
entry, `DragonEden_Station`, at a physical size of **200 × 200 mm**. The declared size does
not affect detection, but an incorrect value distorts the derived scale and distance of the
anchored dragon.

Print `T_Marker_DragonEden_200mm_300dpi.png` at 200 mm without scaling, on a matte surface.
`T_Marker_DragonEden.png` is the 1024 px copy consumed by Unity.

### Regenerating the marker

```bash
cd tools/ar-marker-generator
pip install Pillow numpy
python generate_marker.py
```

Both PNGs are overwritten in place. Filenames are stable so the reference image library
never needs to be repointed.

`ARTrackedImageManager` performs classical feature matching — corner detection and
descriptor comparison — with no machine learning involved. Marker quality therefore depends
on measurable image properties, which the generator scores automatically against thresholds
in `marker_lib.py` (≥ 400 keypoints, ≥ 90 % grid coverage, ≥ 0.20 contrast standard
deviation) and reports as PASS or FAIL:

- **Dense corner features** — smooth gradients yield nothing to detect.
- **Even coverage** — a marker rich on one half loses tracking when the sparse half fills
  the frame.
- **High contrast** — adjacent tonal values match poorly.
- **Rotational asymmetry** — repeated or symmetric patterns admit orientation ambiguity.

QR codes score poorly despite appearing detailed: uniform module sizing produces repetitive
texture and the three finder patterns introduce local symmetry.

---

## 7. Development workflow

### Editor iteration with XR Simulation

AR Foundation 6 ships an in-Editor simulation of camera and tracking, removing the need to
deploy on every change.

1. Open `Assets/Scenes/SampleScene.unity`.
2. Open **Window → XR → AR Foundation → XR Environment View** and select a simulation
   environment.
3. Enter Play mode. Navigate with right mouse + WASD, Q/E for vertical movement, Shift to
   accelerate.
4. To exercise image tracking, place a copy of the marker texture inside the simulation
   environment; it is detected as it would be on device.

Tracking accuracy under real-world lighting, occlusion, and surface conditions cannot be
validated in simulation and requires a device build.

### Automated tests

Run via **Window → General → Test Runner**. EditMode tests cover deterministic logic —
geometry, telemetry parsing, state transitions. PlayMode tests cover behavior requiring
frame ticks.

Command line:

```
Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml -quit
```

---

## 8. Building

### Android

Current player settings:

| Setting | Value | Rationale |
|---|---|---|
| Minimum API level | **29** (Android 10) | ARCore on Unity 6000.5 requires API 26 with OpenGLES3; 29 also satisfies the Vulkan threshold |
| Target API level | Automatic | Highest installed SDK |
| Scripting backend | IL2CPP | Mono provides no ARM64 support |
| Target architecture | ARM64 | Required by Google Play; ARCore native libraries are 64-bit |
| Graphics API | OpenGLES3 | ARCore's baseline API |

`ARCoreBuildProcessor` enforces the minimum SDK at build time and fails the build rather
than producing a broken APK.

Deployment:

1. Enable Developer Options and USB debugging on the device, then authorize the host when
   prompted.
2. **File → Build Settings → Android → Switch Platform** (once per clone).
3. **Build And Run** installs and launches directly, or **Build** produces an APK for
   `adb install -r <file>.apk`.
4. Grant the camera permission at first launch. A denied permission produces a black
   camera feed; re-enable it under **Settings → Apps → Permissions**.
5. Inspect runtime logs through **Window → Analysis → Android Logcat**, or
   `adb logcat -s Unity ARCore` for a build not attached to the Editor.

> **Before distribution:** `applicationIdentifier` is still the Unity template default
> (`com.unity.template.ar_mobile`) and must be replaced with a project-owned package name.
> Release builds additionally require an Android App Bundle and a signing keystore
> (**Publishing Settings → Keystore Manager**).

Installability does not imply AR capability — the dragon appears only on devices present in
[Google's ARCore supported device list](https://developers.google.com/ar/devices).

### iOS

Requires macOS with Xcode; iOS builds cannot be produced from Windows.

1. **File → Build Settings → iOS → Switch Platform → Build** to emit an Xcode project.
2. Open the generated project, set a signing team under **Signing & Capabilities**, select
   the connected device, and run. Free provisioning profiles expire after seven days.
3. TestFlight distribution requires a paid Apple Developer Program membership
   (**Product → Archive → Distribute App**).

---

## 9. Troubleshooting

| Symptom | Check |
|---|---|
| Black screen on launch | Camera permission state under **Settings → Apps → Permissions** |
| Nothing spawns when scanning the marker | Confirm the printed size matches the 200 mm declared in the image library; verify detection reaches `TargetAcquiredOnce` in Logcat |
| Prompt to install Google Play Services for AR | Expected on first run; otherwise the device may not support ARCore |
| Magenta materials on device | A material asset is missing from `Resources/`, so the shader was stripped from the build |
| Dragon scaled incorrectly | `BoundsScaler` reads `sharedMesh.bounds` rather than `SkinnedMeshRenderer.bounds`, which is expanded to cover animation extents |
| Gradle or AGP errors after a Unity upgrade | Audit custom Gradle templates and third-party `.aar` compatibility |
| Empty XR Environment View in Play mode | Select a simulation environment in `Assets/XR/UserSimulationSettings/Resources/XRSimulationPreferences.asset` |
