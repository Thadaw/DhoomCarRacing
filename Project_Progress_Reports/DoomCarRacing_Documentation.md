# Dhoom Car Racing — Project Documentation

**Gandaki University**
Faculty of Science and Technology
Bachelor of Information Technology (BIT)

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Project Information](#project-information)
3. [Technology Stack](#technology-stack)
4. [Project Structure](#project-structure)
5. [Scene Flow](#scene-flow)
6. [Core Systems](#core-systems)
7. [Script Reference](#script-reference)
8. [Assets Reference](#assets-reference)
9. [Architecture Diagram](#architecture-diagram)
10. [Setup & Installation](#setup--installation)
11. [Known Issues](#known-issues)
12. [Future Scope](#future-scope)

---

## Project Overview

**Dhoom Car Racing** is a 3D multiplayer racing game built in Unity featuring real-time multiplayer via Photon PUN2, cloud-stored leaderboards via Firebase Firestore, and Google OAuth authentication. Players can race across multiple tracks in both single-player and multiplayer modes with up to 4 players.

### Key Features

- **Single Player & Multiplayer** racing modes (up to 4 players)
- **Real-time multiplayer** via Photon PUN2 with room-based matchmaking
- **Cloud leaderboards** via Firebase Firestore
- **Google OAuth** desktop sign-in for player identity
- **Multiple tracks** (3 playable tracks with unique environments)
- **Multiple cars** with different stats and drive modes (FWD/RWD/AWD)
- **Physics-based driving** with WheelCollider system, drift mechanics, and nitrous boost
- **Power-up system** (nitrous, rocket, shield)
- **Race countdown**, checkpoint/lap tracking, and anti-cheat
- **Post-race results** with leaderboard, best lap, top speed, average speed
- **Audio system** with engine sounds, crash effects, and background music
- **Smooth camera** with speed-dependent zoom and shake effects

---

## Project Information

| Field | Details |
|-------|---------|
| **Project Title** | Dhoom Car Racing |
| **Company** | AAR |
| **Product Name** | DoomCarRacing |
| **Engine** | Unity (2022.3+) |
| **Language** | C# |
| **Target Platform** | Windows (Desktop) |
| **Academic Year** | 2022 |
| **Semester** | 8th |

### Team Members

| Name | Role |
|------|------|
| Aakash Rana Magar | Developer |
| Ayush Tamrakar | Developer |
| Roshan Thapa | Developer |

### Supervisor

**Er. Saroj Giri**

---

## Technology Stack

### Game Engine

| Component | Version |
|-----------|---------|
| Unity | 2022.3+ |
| C# | .NET Standard 2.1 |

### Unity Packages

| Package | Version | Purpose |
|---------|---------|---------|
| Input System | 1.18.0 | Modern input handling |
| Splines | 2.6.1 | Track spline paths |
| Post Processing | 3.5.1 | Visual effects |
| Timeline | 1.8.10 | Cinematic sequences |
| Visual Scripting | 1.9.9 | Visual scripting |
| Mathematics | 1.3.2 | Math utilities |
| UGUI | 2.0.0 | UI system |

### Third-Party Services

| Service | Purpose |
|---------|---------|
| **Photon PUN2** | Real-time multiplayer networking |
| **Firebase Firestore** | Cloud database for leaderboards |
| **Firebase Auth** | Anonymous authentication |
| **Google OAuth 2.0** | Player identity (desktop sign-in) |
| **TextMesh Pro** | High-quality text rendering |
| **SlimUI** | Modern UI asset pack |

---

## Project Structure

```
DoomCarRacing/
├── Assets/
│   ├── Scenes/                    # Unity scenes (11 total)
│   │   ├── MainMenu.unity        # Main menu screen
│   │   ├── MultiplayerMenu.unity # Multiplayer options
│   │   ├── Joining.unity         # Room code entry
│   │   ├── Lobby.unity           # Pre-game lobby
│   │   ├── Garage.unity          # Car selection
│   │   ├── TrackSelection.unity  # Track picker
│   │   ├── Track1.unity          # Race track 1
│   │   ├── Track2.unity          # Race track 2
│   │   ├── Track3.unity          # Race track 3
│   │   ├── stats.unity           # Player profile/stats
│   │   └── carsmodel.unity       # Car model viewer
│   │
│   ├── Scripts/                   # Game scripts (~65 C# files)
│   │   ├── car/                   # Car physics subsystem
│   │   ├── core/                  # Core game systems
│   │   ├── engine/                # Input handling
│   │   ├── ModTools/              # NFS data import tools
│   │   ├── stand alone/           # Utility scripts
│   │   ├── temp/                  # Prototypes (AI racer)
│   │   └── Editor/               # Editor tools
│   │
│   ├── Prefab/                    # Prefab assets
│   │   ├── PlayerCar/            # Player car prefabs
│   │   └── Transition/           # Scene transition prefabs
│   │
│   ├── Resources/                 # Runtime-loaded assets
│   │   ├── NetworkCar.prefab     # Network car prefab
│   │   ├── PlayerRow.prefab     # Lobby player row
│   │   ├── ResultPanalManager.prefab
│   │   ├── singleplayerresultpanalmanager.prefab
│   │   ├── Sounds/               # Runtime audio clips
│   │   ├── Cars/                 # Car prefabs for spawning
│   │   └── EngineSounds/         # Engine audio clips
│   │
│   ├── 3D Models/                 # 3D model assets
│   │   ├── Car_1/ ... Car_55/   # Car models with LODs
│   │   ├── RealisticRaceTrack/  # Track 1 model
│   │   ├── map2/                # Track 2 model
│   │   ├── map3/                # Track 3 model
│   │   ├── Garage Assets/       # Garage environment
│   │   ├── finish line/         # Finish line model
│   │   └── starting-line/       # Starting line model
│   │
│   ├── Sound/                     # Audio clips
│   │   ├── Tokyo Drift - Six Days.mp3
│   │   ├── Sadeness I (Enigma).mp3
│   │   ├── MainMenuMusic.mp3
│   │   ├── ButtonClickSound.wav
│   │   ├── carcrash.mp3
│   │   ├── caracceleration.mp3
│   │   ├── start acceleration.mp3
│   │   ├── runacceleration.mp3
│   │   ├── RacingLevelMusic.wav
│   │   └── SkidBreakMusic.wav
│   │
│   ├── effects/                   # Visual effects
│   │   ├── Particles/            # Particle effects (sparks)
│   │   └── AllSkyFree/           # Skybox environments
│   │
│   ├── Firebase/                  # Firebase SDK
│   ├── Photon/                    # Photon PUN2 SDK
│   ├── TextMesh Pro/             # TMP assets
│   ├── SlimUI/                   # UI pack
│   └── ExternalDependencyManager/ # Dependency resolver
│
├── Packages/
│   └── manifest.json             # Package dependencies
│
├── ProjectSettings/               # Unity project settings
├── Library/                       # Unity cache
├── Logs/                         # Unity logs
├── UserSettings/                  # User preferences
└── TODO.md                        # Project TODO list
```

---

## Scene Flow

```
                    ┌─────────────┐
                    │  MainMenu   │
                    └──────┬──────┘
                           │
            ┌──────────────┼──────────────┐
            │              │              │
            ▼              ▼              ▼
     ┌──────────┐   ┌──────────┐   ┌──────────┐
     │  Single  │   │Multiplaye│   │  Garage  │
     │  Player  │   │   Menu   │   │ (View)   │
     └────┬─────┘   └────┬─────┘   └──────────┘
          │              │
          │         ┌────┴────┐
          │         │         │
          │         ▼         ▼
          │    ┌────────┐ ┌────────┐
          │    │ Create │ │  Join  │
          │    │  Room  │ │  Room  │
          │    └───┬────┘ └───┬────┘
          │        │          │
          │        ▼          ▼
          │    ┌──────────────┐
          │    │    Lobby     │
          │    │ (Ready System)│
          │    └──────┬───────┘
          │           │
          ▼           ▼
     ┌───────────────────┐
     │  Track Selection  │
     │ (3 Tracks, Laps) │
     └────────┬──────────┘
              │
     ┌────────┼────────┐
     │        │        │
     ▼        ▼        ▼
 ┌───────┐┌───────┐┌───────┐
 │Track 1││Track 2││Track 3│
 └───┬───┘└───┬───┘└───┬───┘
     │        │        │
     ▼        ▼        ▼
 ┌───────────────────────┐
 │      Race Screen      │
 │ (Countdown → Race)    │
 └───────────┬───────────┘
             │
     ┌───────┴───────┐
     │               │
     ▼               ▼
 ┌────────┐    ┌──────────┐
 │ Results│    │   Stats  │
 │ Panel  │    │ (Profile)│
 └────────┘    └──────────┘
```

---

## Core Systems

### 1. Game Session Management

**Singleton:** `GameSession.cs`

Persists across scene loads. Stores player choices (game mode, selected car, track, room code, lap count) so all scenes can read/write the same data without manual scene-to-scene passing.

**Data Stored:**
- `GameMode` (SinglePlayer / Multiplayer / GarageViewOnly)
- `SelectedCarIndex` (0-based car selection)
- `SelectedTrackIndex` (0-2)
- `RoomCode` (Photon room name)
- `IsHost` (host flag)
- `TotalLaps` (1, 2, 3, or 5)

---

### 2. Car Physics System

#### Primary (Active - Multiplayer)

**Script:** `PhotonCarController.cs`

- 4-wheel setup using Unity WheelColliders
- Motor force, steering, braking
- Speed-dependent steering reduction
- Drift control with lateral velocity damping
- Downforce calculation
- Anti-roll bar physics
- Nitrous boost with FOV camera effect
- Drive modes: FWD, RWD, AWD

#### Secondary (Legacy/Alternate)

**Scripts:** `CarStateMachine.cs`, `CarController.cs`, `EngineManager.cs`, `SteerManager.cs`, `wheelsManager.cs`

Full engine simulation with:
- RPM calculation and torque curves
- Automatic and manual gear shifting
- Rev limiter
- Speed-dependent steering with counter-steer
- Slip-based wheel friction management
- Power-up inventory system

---

### 3. Race System

**Scripts:** `RaceManager.cs`, `RaceCheckpoint.cs`, `PlayerLapTracker.cs`, `Checkpoints.cs`

#### Race Flow:
1. **3-2-1-GO Countdown** — Animated scale pop-in, hold, fade-out
2. **Checkpoint Validation** — Sequential checkpoint passing required
3. **Lap Tracking** — Configurable lap count (1, 2, 3, 5)
4. **Anti-Cheat** — Prevents early finish line crossing
5. **Race Timing** — Total finish time, per-lap times, top speed, average speed

#### Checkpoint System:
- Trigger-based detection (`RaceCheckpoint.cs`)
- Sequential validation (must pass checkpoints in order)
- Missed checkpoint warning display
- Legacy tag-based system (`Checkpoints.cs`) also exists

---

### 4. Multiplayer System

**Scripts:** `Networkmanager.cs`, `JoiningRoomManager.cs`, `LobbyManager.cs`, `NetworkCar.cs`, `NetworkCarManager.cs`

#### Features:
- **Photon PUN2** real-time networking
- **Room-based matchmaking** — Host creates `Room_XXXX`, joiner enters code
- **Up to 4 players** per room
- **Position/rotation sync** via `IPunObservable` with lerp/slerp interpolation
- **Custom properties** for syncing: CarId, IsReady, FinishTime, TopSpeed, AverageSpeed, BestLap
- **Room properties** for track selection and lap count
- **Automatic scene sync** via `PhotonNetwork.AutomaticallySyncScene`

#### Lobby:
- Live player list with ready/unready toggle
- Host-only track selection and lap count
- Start button (enabled only when all players ready)
- Leave room functionality

---

### 5. UI System

**Scripts:** `MainMenuController.cs`, `MultiplayerMenuController.cs`, `TrackSelection.cs`, `CarSelection.cs`, `UIManager.cs`, `ResultsPanel.cs`, `SinglePlayerFinishPanel.cs`, `PauseMenu.cs`, `StatsController.cs`, `TrackPanelSetup.cs`

#### Screens:
| Screen | Purpose |
|--------|---------|
| Main Menu | Single Player, Multiplayer, Garage, Profile, Google Sign In, Quit |
| Multiplayer Menu | Create Room, Join Room, Back |
| Joining Screen | Room code input, join button, error popups |
| Lobby | Player list, ready status, track/lap selection, start button |
| Garage | Car browsing (prev/next), select car, 3D preview |
| Track Selection | 3 track previews, play button, lap count dropdown |
| In-Race HUD | Speed (KM/H), lap counter, checkpoint counter |
| Pause Menu | Resume, restart, main menu, live player list |
| Results Panel | Leaderboard, finish time, best lap, top speed, avg speed |
| Stats/Profile | Player name, play time, global leaderboard, logout |

#### Transitions:
- Fade-in/fade-out via `CanvasGroup` alpha animation (`SceneSwitcher.cs`)
- Dynamic UI creation via `TrackPanelSetup.cs`

---

### 6. Audio System

**Scripts:** `AudioManager.cs`, `CarSound.cs`, `UIbuttonSound.cs`, `MainGameMusic.cs`, `MainMenuMusic.cs`

#### AudioManager (Singleton):
- 2 AudioSources: Music + UI SFX
- Menu music and in-game music switching
- Button click sound on all UI interactions
- DontDestroyOnLoad persistence

#### CarSound (Per-car):
- Start acceleration sound
- Engine sound (pitch scales with speed)
- Crash sound (volume scales with impact speed)
- Background race music

---

### 7. Camera System

**Scripts:** `CameraMovement.cs`, `FollowCar.cs`, `CamraController.cs`

#### CameraMovement (Primary):
- Smooth follow with offset
- Speed-dependent distance zoom (zooms out at high speed)
- Look-ahead rotation
- High-speed camera shake effect

#### FollowCar (Simple):
- SmoothDamp position tracking
- LookAt car

---

### 8. Leaderboard System

**Scripts:** `FirebaseManager.cs`, `LeaderboardManager.cs`, `StatsController.cs`

#### Firebase Integration:
- Anonymous authentication for user identity
- Firestore database for cloud-stored leaderboards
- Leaderboard submission: player name, track ID, finish time, user ID, timestamp
- Per-track and all-tracks querying sorted by fastest time

#### Player Stats:
- Editable player name
- Total accumulated play time (via `PlayTimeTracker.cs`)
- Global leaderboard display

---

### 9. Power-Up System

**Scripts:** `Powerup.cs`, `CarStateMachine.cs`, `CarController.cs`

#### Power-Up Types:
| Type | Effect |
|------|--------|
| Nitrous | Speed boost with FOV camera effect |
| Rocket | Projectile attack (stub implementation) |
| Shield | Invulnerability (stub implementation) |

#### Mechanics:
- 3 inventory slots max
- Cycle with E key
- Use with F key
- Random type on pickup

---

### 10. NFS ModTools System

**Scripts:** `WorldChunksStreamer.cs`, `ChunkManager.cs`, `ChunkStream.cs`, + 20+ supporting scripts

#### Purpose:
Import and decode Need for Speed game world data files for use as racing track assets.

#### Supported Games:
- Need for Speed: Most Wanted
- Need for Speed: Carbon
- Need for Speed: Undercover
- Need for Speed: ProStreet
- Need for Speed: World (World15)

#### Features:
- Binary file parsing and chunk management
- Mesh/material/texture loading from NFS data
- Async chunk loading based on camera distance
- Solid list collision data parsing

---

## Script Reference

### Core Game Flow

| Script | Purpose |
|--------|---------|
| `Gamesession.cs` | Singleton: game mode, car, track, room code, laps |
| `MainMenuController.cs` | Main menu button handlers |
| `MultiplayerMenuController.cs` | Multiplayer menu navigation |
| `SceneSwitcher.cs` | Scene transitions with fade animation |
| `CarSpawner.cs` | Spawns local car or registers network prefabs |

### Racing & Physics

| Script | Purpose |
|--------|---------|
| `PhotonCarController.cs` | Primary car physics (multiplayer) |
| `RaceManager.cs` | Race state: countdown, start/finish, timing |
| `RaceCheckpoint.cs` | Trigger-based checkpoint detection |
| `PlayerLapTracker.cs` | Lap/progress tracking, Firebase submission |
| `Checkpoints.cs` | Legacy checkpoint system (tag-based) |
| `CarStateMachine.cs` | Component hub for car sub-systems |
| `CarController.cs` | Power-ups, downforce, FOV, spline tracking |
| `EngineManager.cs` | Engine simulation: RPM, gears, torque |
| `SteerManager.cs` | Steering with drift mechanics |
| `wheelsManager.cs` | Wheel friction management |
| `CarStats.cs` | Per-car configuration |
| `AntiRollBar.cs` | Anti-roll bar physics |

### Multiplayer / Networking

| Script | Purpose |
|--------|---------|
| `Networkmanager.cs` | Photon connection, room creation |
| `JoiningRoomManager.cs` | Room code input, join attempt |
| `Lobbymanager.cs` | Pre-game lobby, ready system |
| `NetworkCar.cs` | Network car identity, position sync |
| `NetworkCarManager.cs` | Network car instantiation singleton |

### UI

| Script | Purpose |
|--------|---------|
| `UIManager.cs` | In-race HUD: speed, laps, checkpoints |
| `RaceUIBinder.cs` | Binds UI TextMeshPro to PlayerLapTracker |
| `RacePlayerList.cs` | In-race multiplayer leaderboard |
| `ResultsPanel.cs` | Post-race results (multiplayer) |
| `SinglePlayerFinishPanel.cs` | Post-race results (single player) |
| `PauseMenu.cs` | Pause panel: resume, restart, main menu |
| `TrackSelection.cs` | Track picker with previews |
| `TrackPanelSetup.cs` | Auto-creates UI panels in track scenes |
| `CarSelection.cs` | Garage car browser |
| `StatsController.cs` | Player profile/stats screen |
| `UIbuttonSound.cs` | UI button click sound |

### Backend / Data

| Script | Purpose |
|--------|---------|
| `FirebaseManager.cs` | Firebase init, anonymous auth |
| `LeaderboardManager.cs` | Firestore leaderboard CRUD |
| `PlayerNameHelper.cs` | Player name utility (PlayerPrefs + Photon) |
| `GoogleDesktopAuth.cs` | Google OAuth2 desktop flow |
| `PlayTimeTracker.cs` | Accumulates play time across sessions |

### Camera

| Script | Purpose |
|--------|---------|
| `CameraMovement.cs` | Smooth follow, zoom, shake |
| `FollowCar.cs` | Simple SmoothDamp follow |
| `CamraController.cs` | Legacy spline-aware camera |

### Audio

| Script | Purpose |
|--------|---------|
| `AudioManager.cs` | Singleton: music + SFX management |
| `CarSound.cs` | Per-car audio: start, engine, crash |
| `MainGameMusic.cs` | Triggers in-game music |
| `MainMenuMusic.cs` | Triggers menu music |

### Utility

| Script | Purpose |
|--------|---------|
| `Rotate.cs` | Simple Y-axis rotation (garage turntable) |
| `WindowHelper.cs` | Windows P/Invoke for foreground window |
| `ShuffleScrpt.cs` | Fisher-Yates shuffle extension |
| `LateExe.cs` | Delayed/conditional method execution |
| `WorldChunksStreamer.cs` | NFS world data streaming |
| `scriptableRaccer.cs` | AI racer prototype (waypoint following) |
| `trackWaypoints.cs` | Visual waypoint path for AI |
| `GameManager.cs` | AI racer spawning, race position |

---

## Assets Reference

### Scenes (11)

| Scene | Purpose |
|-------|---------|
| `MainMenu` | Main menu with navigation options |
| `MultiplayerMenu` | Create/join room options |
| `Joining` | Room code entry screen |
| `Lobby` | Pre-game lobby with ready system |
| `Garage` | Car selection/browsing |
| `TrackSelection` | Track picker (3 tracks) |
| `Track1` | Race track 1 |
| `Track2` | Race track 2 |
| `Track3` | Race track 3 |
| `stats` | Player profile/stats |
| `carsmodel` | Car model viewer |

### Car Prefabs (15+)

| Prefab | Description |
|--------|-------------|
| `Car1.prefab` | Player car 1 |
| `Car9.prefab` | Player car 2 |
| `Car_1.prefab` - `Car_55.prefab` | Additional car models |
| `Car1.prefab` - `Car8.prefab` | Car selection variants |
| `Car_20.prefab` | Car model 20 |
| `car77.prefab` | Car model 77 |
| `Car9.prefab` | Car model 9 |
| `samosaidao 1.prefab` | Track 2 model |

### Track Assets

| Asset | Description |
|-------|-------------|
| `RealisticRaceTrack.prefab` | Track 1 prefab |
| `map3.prefab` | Track 3 prefab |
| `map2/` | Track 2 with source models |
| `finish line/` | Finish line FBX model |
| `starting-line/` | Starting line model |

### Sound Files (11)

| File | Type |
|------|------|
| `Tokyo Drift - Six Days.mp3` | Race music |
| `Sadeness I (Enigma).mp3` | Race music |
| `MainMenuMusic.mp3` | Menu music |
| `ButtonClickSound.wav` | UI SFX |
| `carcrash.mp3` | Car crash |
| `caracceleration.mp3` | Car engine |
| `start acceleration.mp3` | Engine start |
| `runacceleration.mp3` | Engine running |
| `RacingLevelMusic.wav` | Race BGM |
| `SkidBreakMusic.wav` | Skid sound |
| `dragon-studio-car-engine-roaring.mp3` | Engine roar |

### 3D Models

| Model | Format | Description |
|-------|--------|-------------|
| Car_1 - Car_55 | FBX | Car models with LOD variants |
| RealisticRaceTrack | FBX | Track 1 environment |
| map2 source models | FBX | Track 2 environment |
| RaceTrackExport | FBX | Track 3 environment |
| Garage Assets | FBX | Garage environment |
| Finish Line | FBX | Race finish line |

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        GAME SESSION                             │
│               (Singleton - DontDestroyOnLoad)                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ GameMode │ SelectedCar │ SelectedTrack │ RoomCode │ Laps│   │
│  └─────────────────────────────────────────────────────────┘   │
└──────────────────────────┬──────────────────────────────────────┘
                           │
         ┌─────────────────┼─────────────────┐
         │                 │                 │
         ▼                 ▼                 ▼
┌─────────────────┐ ┌──────────────┐ ┌──────────────────┐
│   UI SYSTEM     │ │  CAR SYSTEM  │ │  RACE SYSTEM     │
│                 │ │              │ │                  │
│ MainMenuCtrl    │ │ PhotonCar    │ │ RaceManager      │
│ LobbyManager    │ │ Controller   │ │ RaceCheckpoint   │
│ TrackSelection  │ │ EngineManager│ │ PlayerLapTracker │
│ CarSelection    │ │ SteerManager │ │ Checkpoints      │
│ ResultsPanel    │ │ WheelsManager│ │                  │
│ PauseMenu       │ │ CarStateMachine│ │                │
│ UIManager       │ │ CarStats     │ │                  │
│ StatsController │ │ AntiRollBar  │ │                  │
└────────┬────────┘ └──────┬───────┘ └────────┬─────────┘
         │                 │                  │
         │                 ▼                  │
         │    ┌────────────────────────┐      │
         │    │   NETWORKING SYSTEM    │      │
         │    │                        │      │
         │    │ NetworkManager         │◄─────┘
         │    │ NetworkCar             │
         │    │ NetworkCarManager      │
         │    │ JoiningRoomManager     │
         │    │ Photon PUN2            │
         │    └───────────┬────────────┘
         │                │
         ▼                ▼
┌─────────────────────────────────────────────────┐
│              BACKEND SERVICES                    │
│                                                  │
│  ┌──────────┐  ┌──────────────┐  ┌───────────┐ │
│  │ Firebase │  │ Photon Cloud │  │ Google    │ │
│  │ Firestore│  │ (Multiplayer)│  │ OAuth     │ │
│  │ (Leader  │  │              │  │ (Auth)    │ │
│  │  boards) │  │              │  │           │ │
│  └──────────┘  └──────────────┘  └───────────┘ │
└─────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────┐
│              SUPPORTING SYSTEMS                  │
│                                                  │
│  AudioManager │ CameraMovement │ PlayTimeTracker│
│  CarSound     │ FollowCar      │ SceneSwitcher  │
│  MainMenuMusic│                │ TrackPanelSetup│
└─────────────────────────────────────────────────┘
```

---

## Setup & Installation

### Prerequisites

- **Unity 2022.3** or later
- **Windows 10/11** (for full compatibility)
- Internet connection (for Photon/Firebase)

### Steps

1. **Clone/Open Project**
   - Open Unity Hub
   - Click "Open" → Navigate to `DoomCarRacing` folder
   - Wait for Unity to import all assets

2. **Photon Configuration**
   - Ensure Photon PUN2 is configured with your App ID
   - Edit → Project Settings → PhotonServerSettings
   - Enter your Photon App ID in the Dashboard section

3. **Firebase Configuration**
   - Place `google-services.json` (Android) or equivalent in project
   - Firebase project must have Firestore enabled
   - Anonymous authentication must be enabled in Firebase Console

4. **Google OAuth Setup**
   - Create OAuth 2.0 credentials in Google Cloud Console
   - Update `GoogleDesktopAuth.cs` with your Client ID and Client Secret
   - **Note:** Credentials are currently hardcoded in source (security concern)

5. **Build Settings**
   - File → Build Settings
   - Add all scenes in correct order:
     1. MainMenu
     2. MultiplayerMenu
     3. Joining
     4. Lobby
     5. Garage
     6. TrackSelection
     7. Track1
     8. Track2
     9. Track3
     10. stats
     11. carsmodel
   - Set target platform to Windows

### Controls

| Input | Action |
|-------|--------|
| W / Up Arrow | Accelerate |
| S / Down Arrow | Brake |
| A / Left Arrow | Steer Left |
| D / Right Arrow | Steer Right |
| Space | Handbrake |
| Shift | Nitrous Boost |
| E | Cycle Power-Up |
| F | Use Power-Up |
| Escape | Pause Menu |

---

## Known Issues

### Multiplayer
- Position sync is basic (position + rotation only), causing jittery remote car movement in corners
- Remote cars are kinematic (no physics), making car-to-car collisions unreliable
- Race countdown is local only — each client starts at slightly different times
- Lap/checkpoint progress not networked during race

### Code Quality
- Two parallel car physics implementations (PhotonCarController + CarStateMachine)
- Two checkpoint systems coexist (RaceCheckpoint + Checkpoints)
- Heavy use of `FindFirstObjectByType` and `GameObject.Find` at runtime
- Dynamic UI construction is complex and hard to maintain
- Google OAuth credentials hardcoded in source code

### Performance
- `WorldChunksStreamer` loads meshes/textures synchronously in some paths
- ResultsPanel and RacePlayerList poll in Update() instead of event-driven
- Inconsistent naming conventions across scripts

### Platform
- Windows-only P/Invoke in `WindowHelper.cs` (user32.dll)
- Mixed Input System and Legacy Input Manager usage

---

## Future Scope

### Short-Term
- Networked lap/checkpoint progress sync for remote players
- Complete rocket and shield power-up implementations
- AI opponents with rubber-band catch-up mechanics
- Car-to-car collision via physics-based network sync

### Medium-Term
- More tracks (system supports adding Track4+ easily)
- Car customization/paint system
- Drift scoring system
- Speed boost pads and track hazards
- Race replay system

### Long-Term
- Ranked/seasonal leaderboard with Firebase Cloud Functions
- Friend system via Photon + Firebase
- Cross-platform support (replace P/Invoke, standardize Input)
- VR support
- Track editor for custom track creation
- Mobile touch controls
- Loot boxes and progression system

---

*Last Updated: 2026*
*Project: Dhoom Car Racing*
*Team: Aakash Rana Magar, Ayush Tamrakar, Roshan Thapa*
*Supervisor: Er. Saroj Giri*
