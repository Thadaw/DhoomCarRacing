# Dhoom Car Racing (DoomCarRacing)

A 3D racing game built in **Unity** with real-time multiplayer (up to 4 players), cloud leaderboards, and Google sign-in. Race across three tracks in single-player or multiplayer, with physics-based driving, drift, nitrous, power-ups, and post-race stats.

> Academic project — Gandaki University, Faculty of Science and Technology, Bachelor of Information Technology (BIT).

---

## Features

- **Single-player & multiplayer** racing (up to 4 players per room)
- **Real-time multiplayer** via Photon PUN2 with room-code matchmaking
- **Cloud leaderboards** via Firebase Firestore (per-track and global)
- **Google OAuth** desktop sign-in for player identity
- **3 tracks** with distinct environments, selectable lap counts (1/2/3/5)
- **Physics-based driving** — WheelColliders, FWD/RWD/AWD drive modes, drift, anti-roll bars, downforce, nitrous boost
- **Power-ups** — nitrous, rocket (stub), shield (stub); cycle with `E`, use with `F`
- **Race systems** — 3-2-1-GO countdown, sequential checkpoint validation, lap tracking, anti-cheat, finish timing
- **Post-race results** — finish time, best lap, top speed, average speed
- **Audio** — engine sounds scaled to speed, crash effects, menu/race music
- **Smooth camera** — speed-dependent zoom, look-ahead, high-speed shake

---

## Tech Stack

| Component | Details |
|-----------|---------|
| Engine | Unity **6000.3.10f1** (Unity 6.3) |
| Language | C# (.NET Standard 2.1) |
| Target platform | Windows (Desktop) |
| Multiplayer | Photon PUN2 |
| Database | Firebase Firestore |
| Auth | Firebase Auth (anonymous) + Google OAuth 2.0 |
| Input | Input System 1.7.0 (+ some Legacy Input Manager) |
| UI | UGUI, TextMesh Pro, SlimUI |

Key Unity packages: `com.unity.inputsystem`, `com.unity.splines`, `com.unity.mathematics`, `com.unity.multiplayer.center`.

---

## Getting Started

### Prerequisites

- **Unity 6000.3.10f1** (via [Unity Hub](https://unity.com/download)) — match this exact version to avoid import churn
- Windows 10/11
- Internet connection (Photon + Firebase)

### 1. Open the project

1. Clone the repository:
   ```bash
   git clone <repo-url>
   ```
2. In Unity Hub choose **Open → Add project from disk** and select the repo folder.
3. Let Unity import all assets (first import can take several minutes).

### 2. Configure Photon

1. Create a project at the [Photon dashboard](https://dashboard.photonengine.com/) and copy the **App ID**.
2. In Unity: **Edit → Project Settings → PhotonServerSettings** and paste your App ID.

### 3. Configure Firebase

1. Create a Firebase project and **enable Firestore**.
2. Enable **Anonymous** authentication under Firebase Console → Authentication.
3. Add the Firebase Unity SDK config file (`google-services.json` / Firebase options) to the project.
4. Import the Firebase Unity packages if they are not already present.

### 4. Configure Google OAuth (optional)

1. Create OAuth 2.0 credentials in the [Google Cloud Console](https://console.cloud.google.com/apis/credentials) with the desktop redirect URI used in `Assets/Scripts/GoogleDesktopAuth.cs`.
2. Set your **Client ID** and **Client Secret** in `GoogleDesktopAuth.cs`.

> ⚠️ **Security note:** OAuth client credentials are currently hardcoded in source. Move them to a git-ignored config or environment variable before publishing, and rotate any credentials that have already been committed.

### 5. Build

1. **File → Build Settings**, switch platform to **Windows**.
2. Add scenes in this order:
   1. `MainMenu`
   2. `MultiplayerMenu`
   3. `Joining`
   4. `Lobby`
   5. `Garage`
   6. `TrackSelection`
   7. `Track1`
   8. `Track2`
   9. `Track3`
   10. `stats`
   11. `carsmodel`
3. **Build** (or **Play** in the Editor for local testing).

---

## Controls

| Input | Action |
|-------|--------|
| `W` / `↑` | Accelerate |
| `S` / `↓` | Brake / Reverse |
| `A` / `←` | Steer left |
| `D` / `→` | Steer right |
| `Space` | Handbrake (drift) |
| `Shift` | Nitrous boost |
| `E` | Cycle power-up |
| `F` | Use power-up |
| `Esc` | Pause menu |

---

## Project Structure

```
DoomCarRacing/
├── Assets/
│   ├── Scenes/            # 12 scenes (menus, lobby, garage, 3 tracks, stats, AI)
│   ├── Scripts/           # All gameplay code
│   │   ├── car/           # Car physics subsystem (PhotonCarController, drift, audio)
│   │   ├── core/          # Core game systems
│   │   ├── engine/        # Input handling
│   │   ├── AI/            # AI driver + track generator
│   │   ├── ModTools/      # NFS world-data import tools
│   │   ├── stand alone/   # Utilities
│   │   ├── temp/          # Prototypes (AI racer)
│   │   └── Editor/        # Editor tooling
│   ├── Prefab/            # Player car & transition prefabs
│   ├── Resources/         # Runtime-loaded assets (NetworkCar, lobby rows, sounds)
│   ├── 3D Models/         # Cars (with LODs), tracks, garage, finish line
│   ├── Sound/             # Music and SFX
│   ├── effects/           # Particles + AllSkyFree skyboxes
│   ├── Firebase/          # Firebase SDK
│   ├── Photon/            # Photon PUN2 SDK
│   ├── TextMesh Pro/      # TMP assets
│   └── SlimUI/            # UI pack
├── Packages/manifest.json # Package dependencies
├── ProjectSettings/       # Unity project settings
└── Project_Progress_Reports/  # Documentation & progress reports
```

### Key Scripts

| Script | Purpose |
|--------|---------|
| `Gamesession.cs` | Singleton persisting mode, car, track, room code, laps across scenes |
| `PhotonCarController.cs` | Primary car physics (WheelColliders, drift, nitrous) |
| `RaceManager.cs` | Race state: countdown, start/finish, timing |
| `RaceCheckpoint.cs` / `PlayerLapTracker.cs` | Sequential checkpoints, lap tracking, result submission |
| `Networkmanager.cs` / `Lobbymanager.cs` | Photon connection, room create/join, ready system |
| `NetworkCar.cs` | Networked position/rotation sync |
| `FirebaseManager.cs` / `LeaderboardManager.cs` | Firebase init, auth, Firestore leaderboard CRUD |
| `GoogleDesktopAuth.cs` | Google OAuth2 desktop flow |
| `UIManager.cs` / `ResultsPanel.cs` / `PauseMenu.cs` | In-race HUD, results, pause |
| `CarSound.cs` / `AudioManager.cs` | Engine audio and music management |

---

## Scene Flow

```
MainMenu
  ├── Single Player ─────────────┐
  ├── Multiplayer → Create/Join  │
  │        └── Lobby (ready up)  │
  │              └───────────────┤
  ├── Garage (car viewer)        │
  └── TrackSelection ────────────┤
                                 ▼
                    Track1 / Track2 / Track3
                    (countdown → race → results)
                                 │
                                 ▼
                        stats (profile + leaderboard)
```

---

## Documentation

Full documentation lives in [`Project_Progress_Reports/`](Project_Progress_Reports/):

- [`DoomCarRacing_Documentation.md`](Project_Progress_Reports/DoomCarRacing_Documentation.md) — full system & script reference
- [`Project_Progress_Reports.md`](Project_Progress_Reports/Project_Progress_Reports.md) — visit reports
- PDF/DOCX progress reports (Visits 1–5)

---

## Known Issues

- **Multiplayer sync** is position/rotation only — remote cars jitter in corners, collisions are unreliable, and lap progress isn't networked.
- **Duplicate systems** — two car-physics implementations and two checkpoint systems coexist.
- **Runtime object lookup** — heavy use of `FindFirstObjectByType` / `GameObject.Find`.
- **Platform** — `WindowHelper.cs` uses Windows-only P/Invoke; input mixes Input System and Legacy Input Manager.
- **Security** — Google OAuth credentials hardcoded in source (see [Setup](#4-configure-google-oauth-optional)).

---

## Team

| Name | Role |
|------|------|
| Aakash Rana Magar | Developer |
| Ayush Tamrakar | Developer |
| Roshan Thapa | Developer |

**Supervisor:** Er. Saroj Giri
