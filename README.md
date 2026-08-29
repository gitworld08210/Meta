# Meta Cricket

**AR Cricket Game for Android** - Built with Unity 3D

Meta Cricket is an augmented reality cricket batting game that uses real-time body pose detection to let players bat using natural cricket strokes. The game tracks the player's arm movements through the phone camera, maps them to 8 distinct cricket shots, and simulates realistic ball physics with full match scoring.

## Table of Contents

- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Directory Structure](#directory-structure)
- [Gameplay](#gameplay)
- [Motion Detection](#motion-detection)
- [Career Mode](#career-mode)
- [Backend Setup](#backend-setup)
- [Build Instructions](#build-instructions)
- [Unity Cloud](#unity-cloud)
- [Contributing](#contributing)

## Tech Stack

| Technology | Purpose |
|-----------|---------|
| Unity 2022.3 LTS | Game engine |
| C# | Primary language |
| ARCore (ARFoundation) | AR camera and plane detection |
| Unity Sentis | On-device ML inference (MoveNet pose model) |
| Universal Render Pipeline (URP) | Mobile-optimized rendering |
| Supabase | Backend (auth, leaderboard, cloud save) |
| DOTween | UI and gameplay animations |
| Cinemachine | Camera management and replay system |
| TextMeshPro | UI text rendering |

## Architecture

```
+-------------------+     +--------------------+     +-------------------+
|   Motion Layer    |     |   Game Logic       |     |   Presentation    |
|                   |     |                    |     |                   |
| PoseProvider      |---->| ShotDetector       |---->| UI Screens        |
| SentisPoseProvider|     | ShotClassifier     |     | VFX Manager       |
| MLKitPoseProvider |     | BallController     |     | Camera Manager    |
| JointSmoother     |     | MatchManager       |     | Audio Manager     |
| TPoseDetector     |     | CareerManager      |     | Commentary        |
+-------------------+     +--------------------+     +-------------------+
         |                         |                          |
         v                         v                          v
+-------------------+     +--------------------+     +-------------------+
|   Calibration     |     |   Core Services    |     |   Backend         |
|                   |     |                    |     |                   |
| CalibrationMgr    |     | GameManager        |     | SupabaseManager   |
| CalibrationData   |     | EventBus           |     | SupabaseAuth      |
| CalibrationUI     |     | ServiceLocator     |     | LeaderboardSvc    |
|                   |     | SaveSystem         |     | PlayerDataSvc     |
+-------------------+     +--------------------+     +-------------------+
```

**Core Design Patterns:**
- **Event-Driven:** Systems communicate through the EventBus using strongly-typed GameEvents
- **Service Locator:** Runtime service registration and retrieval via ServiceLocator
- **ScriptableObject Data:** Game configuration stored in JSON data files loaded at runtime
- **Singleton Managers:** GameManager, AudioManager, VFXManager for global access
- **Async/Await:** All backend calls use async patterns for non-blocking I/O

## Directory Structure

```
MetaCricket/
├── Assets/
│   ├── Materials/              # Shared materials (URP Lit, glass effects)
│   ├── Prefabs/                # Reusable prefab objects
│   ├── Resources/
│   │   └── Data/               # Runtime-loaded JSON data files
│   │       ├── CareerStages.json
│   │       ├── Teams.json
│   │       ├── Stadiums.json
│   │       └── ShotDefinitions.json
│   ├── Scenes/                 # Unity scenes (Splash, Menu, Match, etc.)
│   ├── Scripts/
│   │   ├── Audio/              # AudioManager, CommentarySystem, AudioData
│   │   ├── Backend/            # Supabase integration (auth, leaderboard, player data)
│   │   ├── BallPhysics/        # Ball delivery, trajectory, bat collision, fielding
│   │   ├── Calibration/        # T-pose detection and body calibration
│   │   ├── Camera/             # Cinemachine camera management and replay
│   │   ├── CareerMode/         # Career progression, stages, teams, rewards
│   │   ├── Core/               # GameManager, EventBus, ServiceLocator, SaveSystem
│   │   ├── Editor/             # Unity Editor utilities (editor-only)
│   │   ├── MatchEngine/        # Match state, scoring, innings, opponent AI
│   │   ├── MotionDetection/    # Pose providers (Sentis/MLKit), joint smoothing
│   │   ├── ShotDetection/      # Shot classification, timing, swing analysis
│   │   ├── UI/
│   │   │   ├── Components/     # Reusable UI: ScorePopup, ProgressBar, PlayerCard
│   │   │   ├── Screens/        # Full-screen UIs: Menu, Match, Career, Settings
│   │   │   └── Theme/          # Glass-morphism, gold gradients, transitions
│   │   └── VFX/                # Visual effects: boundary, bat impact
│   └── Settings/               # URP pipeline and quality settings
├── Packages/                   # Unity Package Manager manifest
└── ProjectSettings/            # Unity project configuration
```

## Gameplay

Meta Cricket simulates batting in a cricket match using your phone's camera to track body movements.

**How It Works:**
1. The player holds their phone or places it on a stand facing them
2. ARCore provides the camera feed and environmental understanding
3. The MoveNet pose model (via Unity Sentis) detects 17 body keypoints in real-time
4. The system tracks arm positions and swing trajectories
5. When the bowler delivers, the player performs a real batting motion
6. The shot type, timing, and power are determined from the swing data
7. Ball physics simulate the result with realistic trajectory and fielding

**Match Types:**
- Quick Match (single innings, configurable overs)
- Career Match (part of career progression)
- Practice Net (no scoring, unlimited balls)

## Motion Detection

### Calibration (T-Pose)

Before each session, the player performs a T-pose (arms outstretched) for calibration:
- Establishes body proportions (arm length, shoulder width, torso height)
- Sets baseline joint positions for relative movement detection
- Accounts for varying distances from the camera
- Calibration data persists across sessions unless body position changes significantly

### Pose Detection Pipeline

```
Camera Frame -> ARCore -> Sentis (MoveNet) -> Raw Keypoints
    -> JointSmoother (Kalman filter) -> PoseData
    -> CalibrationData (normalize) -> ShotDetector
```

### 8 Shot Types

| Shot | Detection | Description |
|------|-----------|-------------|
| Straight Drive | Forward push, bat vertical, low arm angle | Classical straight bat down the ground |
| Cover Drive | Forward push with lateral angle (20-45 deg) | Elegant drive through covers |
| Pull Shot | Horizontal swing, high arm position | Cross-bat shot to leg side |
| Hook Shot | High horizontal swing, ducking posture | Short ball played over shoulder |
| Cut Shot | Lateral swing, arms away from body | Square cut behind point |
| Sweep | Low body position, horizontal arc | Front foot sweep against spin |
| Defensive Block | Minimal movement, bat vertical | Soft hands, dead bat defense |
| Lofted Shot | Upward arc, full extension | Aerial shot over fielders |

Each shot has configurable detection parameters including angle ranges, velocity thresholds, and minimum confidence scores defined in `ShotDefinitions.json`.

## Career Mode

Career mode offers 8 stages of progression from street cricket to the World Cup:

| Stage | Name | Description |
|-------|------|-------------|
| 1 | Gully Cricket | Street cricket with friends |
| 2 | School Tournament | Inter-school competition |
| 3 | District Championship | Regional selection trials |
| 4 | State Trophy | State-level Ranji-style tournament |
| 5 | IPL Contract | Franchise T20 league |
| 6 | National Selection | Selection camp for national team |
| 7 | International Debut | Bilateral series and tours |
| 8 | World Cup | ICC World Cup campaign |

**Progression System:**
- Win matches to earn XP and coins
- Meet unlock requirements (wins + XP threshold) to advance
- Difficulty increases with each stage
- New match types unlock at higher stages (T20 at stage 5, ODI at stage 7)
- Achievements provide bonus rewards

## Backend Setup

Meta Cricket uses Supabase for authentication, cloud saves, and leaderboards.

### Required Tables

```sql
-- Players table
CREATE TABLE players (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    auth_id UUID REFERENCES auth.users(id),
    display_name TEXT NOT NULL,
    avatar_url TEXT,
    total_runs INTEGER DEFAULT 0,
    matches_played INTEGER DEFAULT 0,
    highest_score INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Leaderboard table
CREATE TABLE leaderboard (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    player_id UUID REFERENCES players(id),
    score INTEGER NOT NULL,
    match_type TEXT NOT NULL,
    career_stage INTEGER DEFAULT 1,
    achieved_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Career progress table
CREATE TABLE career_progress (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    player_id UUID REFERENCES players(id) UNIQUE,
    current_stage INTEGER DEFAULT 1,
    total_xp INTEGER DEFAULT 0,
    total_coins INTEGER DEFAULT 0,
    wins INTEGER DEFAULT 0,
    losses INTEGER DEFAULT 0,
    achievements JSONB DEFAULT '[]'::jsonb,
    unlocked_stages INTEGER[] DEFAULT ARRAY[1],
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Row Level Security
ALTER TABLE players ENABLE ROW LEVEL SECURITY;
ALTER TABLE leaderboard ENABLE ROW LEVEL SECURITY;
ALTER TABLE career_progress ENABLE ROW LEVEL SECURITY;

-- Policies: users can read all, write own
CREATE POLICY "Players are viewable by everyone" ON players FOR SELECT USING (true);
CREATE POLICY "Users can update own player" ON players FOR UPDATE USING (auth.uid() = auth_id);
CREATE POLICY "Users can insert own player" ON players FOR INSERT WITH CHECK (auth.uid() = auth_id);

CREATE POLICY "Leaderboard is viewable by everyone" ON leaderboard FOR SELECT USING (true);
CREATE POLICY "Users can insert own scores" ON leaderboard FOR INSERT WITH CHECK (
    player_id IN (SELECT id FROM players WHERE auth_id = auth.uid())
);

CREATE POLICY "Users can view own progress" ON career_progress FOR SELECT USING (
    player_id IN (SELECT id FROM players WHERE auth_id = auth.uid())
);
CREATE POLICY "Users can update own progress" ON career_progress FOR UPDATE USING (
    player_id IN (SELECT id FROM players WHERE auth_id = auth.uid())
);
CREATE POLICY "Users can insert own progress" ON career_progress FOR INSERT WITH CHECK (
    player_id IN (SELECT id FROM players WHERE auth_id = auth.uid())
);
```

See [SETUP.md](SETUP.md) for full backend configuration instructions.

## Build Instructions

1. Open the MetaCricket project in Unity 2022.3 LTS
2. Set build target to Android (File > Build Settings > Android > Switch Platform)
3. Ensure minimum API level is set to Android 7.0 (API 24) for ARCore
4. Connect an ARCore-compatible Android device
5. Build and Run (Ctrl+B)

For detailed setup, see [SETUP.md](SETUP.md).

## Unity Cloud

| Field | Value |
|-------|-------|
| Account | adit080210 |
| Project Name | Meta Cricket |
| Organization | Personal |

The project is linked to Unity Cloud for build automation and crash reporting.

## Contributing

1. Follow the existing namespace conventions (`MetaCricket.<SystemName>`)
2. All new systems should register with the ServiceLocator
3. Use the EventBus for cross-system communication
4. Data-driven design: configure via JSON in `Resources/Data/`
5. Keep MonoBehaviour logic thin - delegate to plain C# classes where possible
6. All public methods should have XML documentation comments
7. Test in both Editor (fast iteration) and on-device (real AR/performance)

## License

This project is proprietary. All rights reserved.
