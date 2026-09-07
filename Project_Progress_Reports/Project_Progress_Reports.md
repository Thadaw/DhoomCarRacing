# Project Progress Interaction Log Sheet

**Gandaki University**
Faculty of Science and Technology
Bachelor of Information Technology (BIT)

---

## Visit 1

| Field | Details |
|-------|---------|
| **Academic Year** | 2022 |
| **Semester** | 8th |
| **Visit No** | 1 |
| **Date of Visit** | |
| **Project Title** | Dhoom Car Racing |
| **Student Name(s)** | Aakash Rana Magar, Ayush Tamrakar, Roshan Thapa |
| **Student ID(s)** | BIT078, BIT079, BIT080 |
| **Supervisor Name** | Er. Saroj Giri |

### Completed Tasks / Achievement

1. Set up Unity project and installed packages: Photon PUN2, Firebase SDK, TextMesh Pro, SlimUI, Input System, Splines.
2. Imported 3D car models (Car_1 through Car_55) with LOD variants and interior models from FBX files.
3. Implemented basic car physics using Unity WheelColliders with 4-wheel setup (front-left, front-right, rear-left, rear-right) in PhotonCarController.cs.
4. Created main menu UI (MainMenuController.cs) with buttons for Single Player, Multiplayer, Garage, Profile, Google Sign In, and Quit.
5. Designed scene flow: MainMenu -> MultiplayerMenu -> Joining -> Lobby -> TrackSelection -> Race Tracks (11 scenes total).
6. Set up GameSession.cs singleton (DontDestroyOnLoad) to persist game state: game mode, selected car, selected track, room code, and lap count across scenes.
7. Implemented scene transitions with fade-in/fade-out animation using CanvasGroup alpha interpolation in SceneSwitcher.cs.

### Issues / Challenges

1. Difficulty tuning WheelCollider parameters (spring, damper, force) for realistic yet fun arcade-style driving feel.
2. DontDestroyOnLoad singletons (GameSession, AudioManager, FirebaseManager) persist across scenes causing initialization order issues.
3. Unity Input System and Legacy Input Manager coexist in the project causing occasional input conflicts.

### Next Week Plan

1. Implement full engine simulation with RPM calculation, gear shifting (automatic + manual), torque curves, and rev limiter.
2. Add speed-dependent steering reduction and drift mechanics with counter-steer.
3. Create checkpoint system with sequential validation and lap tracking.
4. Build car selection (garage) UI with previous/next car cycling and 3D car preview.

---

## Visit 2

| Field | Details |
|-------|---------|
| **Academic Year** | 2022 |
| **Semester** | 8th |
| **Visit No** | 2 |
| **Date of Visit** | |
| **Project Title** | Dhoom Car Racing |
| **Student Name(s)** | Aakash Rana Magar, Ayush Tamrakar, Roshan Thapa |
| **Student ID(s)** | BIT078, BIT079, BIT080 |
| **Supervisor Name** | Er. Saroj Giri |

### Completed Tasks / Achievement

1. Implemented full engine simulation (EngineController in EngineManager.cs) with RPM calculation via wheel RPM averaging, gear shifting with up/down bounce, torque curves via AnimationCurve, and rev limiter.
2. Added drive mode support: Front-Wheel Drive, Rear-Wheel Drive, and All-Wheel Drive (CarStats.cs, EngineManager.cs).
3. Implemented steering with speed-dependent angle curve and counter-steer for drift correction (SteerManager.cs).
4. Developed wheel friction management with slip-based stiffness curves for realistic drift behavior (wheelsManager.cs).
5. Created checkpoint system (RaceCheckpoint.cs) with trigger-based detection requiring sequential checkpoint passing.
6. Implemented lap tracking (PlayerLapTracker.cs) with configurable lap count (1, 2, 3, 5 laps), anti-cheat preventing early finish, and missed checkpoint warnings.
7. Built race countdown (RaceManager.cs) with animated 3-2-1-GO sequence using scale pop-in, hold, and fade-out.
8. Added nitrous boost system: Shift key triggers 5-second boost with FOV camera lerp effect and particle exhaust (CarController.cs).
9. Implemented anti-roll bar physics (AntiRollBar.cs) to prevent car flipping during sharp turns.

### Issues / Challenges

1. Two parallel car physics implementations exist: PhotonCarController.cs (active for multiplayer) and CarStateMachine system (CarController, EngineManager, SteerManager, WheelsManager) causing maintenance confusion.
2. Checkpoint ordering validation required careful edge-case handling for out-of-sequence detection in PlayerLapTracker.cs.
3. DriftFactor in PhotonCarController uses world-space lateral velocity damping which behaves differently at varying frame rates.

### Next Week Plan

1. Integrate Photon PUN2 for real-time multiplayer: room creation (Room_XXXX), room joining by code, and up to 4 players.
2. Implement network car position/rotation synchronization using IPunObservable with lerp/slerp interpolation.
3. Build lobby system with live player list, ready/unready toggle, and host-only start button.
4. Add Google OAuth2 desktop sign-in for player identity in multiplayer.

---

## Visit 3

| Field | Details |
|-------|---------|
| **Academic Year** | 2022 |
| **Semester** | 8th |
| **Visit No** | 3 |
| **Date of Visit** | |
| **Project Title** | Dhoom Car Racing |
| **Student Name(s)** | Aakash Rana Magar, Ayush Tamrakar, Roshan Thapa |
| **Student ID(s)** | BIT078, BIT079, BIT080 |
| **Supervisor Name** | Er. Saroj Giri |

### Completed Tasks / Achievement

1. Integrated Photon PUN2 networking: host creates room (Room_XXXX format), joiner enters room code to join (Networkmanager.cs, JoiningRoomManager.cs).
2. Implemented NetworkCar.cs with IPunObservable for position/rotation synchronization using lerp/slerp interpolation on remote cars.
3. Created NetworkCarManager.cs singleton (DontDestroyOnLoad) for network car instantiation via PhotonNetwork.Instantiate with automatic scene sync.
4. Built lobby system (LobbyManager.cs) with live player list, ready/unready toggle, and host-only start button that enables when all players are ready.
5. Implemented Google OAuth2 desktop flow (GoogleDesktopAuth.cs) with browser-based authentication, local TCP listener for redirect callback, token exchange, and user info fetch.
6. Added Photon custom properties for syncing player data: CarId, IsReady, FinishTime, TopSpeed, AverageSpeed, BestLap.
7. Implemented room custom properties for track selection and lap count synced across all players.
8. Created multiplayer join screen (JoiningRoomManager.cs) with room code input, error popups via AlertPopup, and status text feedback.

### Issues / Challenges

1. Position synchronization sends only position + rotation via IPunObservable causing jittery movement for remote cars in corners.
2. Remote cars have isKinematic=true disabling physics simulation, making car-to-car collisions unreliable.
3. Race countdown is local only; each client starts countdown at different times depending on scene load speed.
4. Lap/checkpoint progress not networked during race; remote player progress only communicated via FinishTime custom property after race finishes.

### Next Week Plan

1. Develop in-race HUD displaying speed in KM/H and lap counter.
2. Build post-race results panel with multiplayer leaderboard sorted by finish time, best lap, top speed, average speed.
3. Implement pause menu with resume, restart, main menu, and live player list.
4. Add track selection UI with 3 track preview images and lap count dropdown.

---

## Visit 4

| Field | Details |
|-------|---------|
| **Academic Year** | 2022 |
| **Semester** | 8th |
| **Visit No** | 4 |
| **Date of Visit** | |
| **Project Title** | Dhoom Car Racing |
| **Student Name(s)** | Aakash Rana Magar, Ayush Tamrakar, Roshan Thapa |
| **Student ID(s)** | BIT078, BIT079, BIT080 |
| **Supervisor Name** | Er. Saroj Giri |

### Completed Tasks / Achievement

1. Implemented in-race HUD (UIManager.cs) displaying real-time speed in KM/H from PhotonCarController and lap counter.
2. Added checkpoint progress display (RaceUIBinder.cs) binding TextMeshPro elements to PlayerLapTracker for checkpoint counter.
3. Built post-race results panel (ResultsPanel.cs) with multiplayer leaderboard sorted by finish time showing position, finish time, best lap, top speed, and average speed.
4. Created single-player finish panel (SinglePlayerFinishPanel.cs) with individual race stats (finish time, best lap, top speed, average speed).
5. Implemented pause menu (PauseMenu.cs) with Resume, Restart, Main Menu buttons and live player list display.
6. Developed track selection UI (TrackSelection.cs) with 3 track previews, play button, and configurable lap count dropdown (1, 2, 3, 5 laps).
7. Added car selection/garage system (CarSelection.cs) with previous/next car cycling, 3D car preview with Y-axis rotation (Rotate.cs), and select/drive button.
8. Implemented audio system (AudioManager.cs) with dual AudioSources for music and UI SFX, menu/in-game music switching, and DontDestroyOnLoad persistence.
9. Added car-specific audio (CarSound.cs) with start acceleration sound, engine pitch-scaling with speed, crash impact-volume sound, and background race music.

### Issues / Challenges

1. Heavy use of FindFirstObjectByType and GameObject.Find at runtime creates fragile dependencies and performance issues.
2. Dynamic UI construction in TrackPanelSetup.cs, StatsController.cs, and ResultsPanel.cs is complex and hard to maintain.
3. ResultsPanel.CollectPlayers() calls FindObjectsByType each time instead of caching player references.
4. Two checkpoint systems coexist: RaceCheckpoint.cs (component-based) and Checkpoints.cs (tag-based legacy).

### Next Week Plan

1. Integrate Firebase Firestore for cloud-stored global leaderboards with per-track querying.
2. Implement Firebase anonymous authentication and player profile/stats screen.
3. Add player profile screen with editable name, total play time, and global leaderboard display.
4. Implement smooth camera system with speed-dependent distance zoom and high-speed camera shake.

---

## Visit 5

| Field | Details |
|-------|---------|
| **Academic Year** | 2022 |
| **Semester** | 8th |
| **Visit No** | 5 |
| **Date of Visit** | |
| **Project Title** | Dhoom Car Racing |
| **Student Name(s)** | Aakash Rana Magar, Ayush Tamrakar, Roshan Thapa |
| **Student ID(s)** | BIT078, BIT079, BIT080 |
| **Supervisor Name** | Er. Saroj Giri |

### Completed Tasks / Achievement

1. Integrated Firebase Firestore for cloud-stored global leaderboards (LeaderboardManager.cs) with per-track and all-tracks querying sorted by fastest time.
2. Implemented Firebase anonymous authentication (FirebaseManager.cs) with DontDestroyOnLoad persistence and dependency resolution.
3. Created leaderboard submission system in PlayerLapTracker.cs that records player name, track ID, finish time, user ID, and timestamp on race completion.
4. Built player profile/stats screen (StatsController.cs) with editable player name via PlayerNameHelper.cs, total accumulated play time via PlayTimeTracker.cs, and global leaderboard display.
5. Implemented smooth camera system (CameraMovement.cs) with offset-based follow, speed-dependent distance zoom, look-ahead rotation, and high-speed camera shake.
6. Added FollowCar.cs as alternative simple camera with SmoothDamp position tracking.
7. Implemented TrackPanelSetup.cs with [RuntimeInitializeOnLoadMethod] that auto-creates results/pause/single-player panels in track scenes at runtime if missing.
8. Added power-up pickup system (Powerup.cs) with random type assignment (nitrous/rocket/shield), inventory of up to 3 slots, UI display with icons, E key to cycle, F key to use.
9. Nitrous power-up fully functional with 5-second boost duration, FOV camera effect, and particle exhaust. Rocket and shield are stub implementations (print only, no actual effect).
10. Final testing of single-player race flow (MainMenu -> Garage -> TrackSelection -> Race -> Results) and multiplayer flow (Create/Join Room -> Lobby -> Race -> Results).

### Issues / Challenges

1. Google OAuth credentials (ClientID and ClientSecret) are hardcoded in GoogleDesktopAuth.cs source code, posing a security concern.
2. Windows-only P/Invoke in WindowHelper.cs (user32.dll) prevents cross-platform compatibility.
3. WorldChunksStreamer.cs loads meshes/textures synchronously in some code paths causing potential frame drops.
4. RacePlayerList.cs and ResultsPanel.cs poll for data in Update() loops instead of using event-driven updates.
5. Inconsistent naming conventions across scripts (PascalCase RaceManager, camelCase Lobbymanager, lowercase car).

### Next Week Plan

1. Network lap/checkpoint progress sync to show real-time remote player progress during races.
2. Complete rocket power-up with projectile physics and shield power-up with invulnerability effect.
3. Add AI opponents using existing waypoint-following system (scriptableRaccer.cs) with corner braking and rubber-band force.
4. Switch remote cars from kinematic to physics-based network sync for car-to-car collisions.
5. Refactor duplicate checkpoint systems (RaceCheckpoint.cs and Checkpoints.cs) into one.
6. Cache FindObjectsByType calls and replace GameObject.Find with cached references.
