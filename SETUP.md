# Meta Cricket - Setup Guide

Complete step-by-step instructions for setting up the Meta Cricket development environment.

## Prerequisites

- Windows 10/11 or macOS 12+
- 16GB RAM minimum (recommended 32GB)
- GPU with OpenGL ES 3.1+ support
- Android device with ARCore support (for testing)
- USB cable for device deployment

## Step 1: Install Unity Hub

1. Download Unity Hub from [https://unity.com/download](https://unity.com/download)
2. Install and sign in with your Unity account
3. Activate a Unity license (Personal is fine for development)

## Step 2: Install Unity 2022.3 LTS

1. Open Unity Hub
2. Go to **Installs** tab
3. Click **Install Editor**
4. Select **Unity 2022.3 LTS** (latest patch)
5. In the module selection, enable:
   - **Android Build Support**
   - **Android SDK & NDK Tools**
   - **OpenJDK**
6. Click Install and wait for completion

> **Note:** The Android SDK, NDK, and JDK are bundled with Unity when you check the Android modules. You do not need to install Android Studio separately.

## Step 3: Open the Project

1. Open Unity Hub
2. Click **Open** (or Add project from disk)
3. Navigate to and select the `MetaCricket/` folder (the one containing `Assets/`, `Packages/`, and `ProjectSettings/`)
4. Unity will import all assets on first open (this may take 5-10 minutes)
5. If prompted about "Entering Safe Mode" due to compile errors, choose **Ignore** and ensure all packages are installed (Step 4)

## Step 4: Package Manager Imports

Open **Window > Package Manager** in Unity Editor. Ensure the following packages are installed:

### Required Packages (via Unity Registry)

| Package | ID | Version |
|---------|----|---------|
| AR Foundation | com.unity.xr.arfoundation | 5.1+ |
| ARCore XR Plugin | com.unity.xr.arcore | 5.1+ |
| Unity Sentis | com.unity.sentis | 1.3+ |
| Universal RP | com.unity.render-pipelines.universal | 14.0+ |
| Cinemachine | com.unity.cinemachine | 2.9+ |
| TextMeshPro | com.unity.textmeshpro | 3.0+ |
| Input System | com.unity.inputsystem | 1.7+ |
| Addressables | com.unity.addressables | 1.21+ |

### External Packages (via OpenUPM or .unitypackage)

| Package | Source | Notes |
|---------|--------|-------|
| DOTween | Asset Store / OpenUPM | Import DOTween Pro for full features |
| Supabase C# Client | NuGet (via NuGetForUnity) | supabase-csharp package |

**To install via Package Manager:**
1. Click the **+** button in Package Manager
2. Select **Add package by name**
3. Enter the package ID (e.g., `com.unity.sentis`)
4. Click **Add**

**To install DOTween:**
1. Download from the Unity Asset Store
2. Import the .unitypackage into the project
3. Run the DOTween setup wizard when prompted

**To install Supabase:**
1. Install NuGetForUnity from OpenUPM or GitHub
2. Open **NuGet > Manage NuGet Packages**
3. Search for "supabase-csharp" and install

## Step 5: ARCore Setup

### Project Settings

1. Go to **Edit > Project Settings > XR Plug-in Management**
2. Under the Android tab, check **ARCore**
3. Go to **Edit > Project Settings > Player > Android**
4. Set **Minimum API Level** to **Android 7.0 (API Level 24)**
5. Set **Target API Level** to **Android 13 (API Level 33)** or higher
6. Under **Other Settings**, ensure:
   - **Auto Graphics API** is unchecked
   - **OpenGLES3** is in the Graphics APIs list (remove Vulkan if present for ARCore compatibility)
   - **Scripting Backend** is set to **IL2CPP**
   - **Target Architectures**: check **ARM64** (uncheck ARMv7 for modern devices)

### AR Session Setup (in Scene)

1. In your AR scene, add an **AR Session** GameObject (GameObject > XR > AR Session)
2. Add an **AR Session Origin** (or XR Origin) with an **AR Camera**
3. The `CameraManager.cs` script handles the Cinemachine virtual cameras on top of the AR camera

## Step 6: Sentis Model Import (MoveNet)

### Download the Model

1. Download the MoveNet SinglePose Lightning model in ONNX format from:
   - TensorFlow Hub: [https://tfhub.dev/google/movenet/singlepose/lightning](https://tfhub.dev/google/movenet/singlepose/lightning)
   - Convert to ONNX using tf2onnx, or use a pre-converted version
2. Place the `.onnx` file at: `Assets/Models/movenet_lightning.onnx`

### Configure in Unity

1. Select the model file in the Project window
2. Unity Sentis will automatically recognize it as a neural network asset
3. In the Inspector, verify:
   - Input shape: `[1, 192, 192, 3]` (image input)
   - Output shape: `[1, 1, 17, 3]` (17 keypoints with x, y, confidence)
4. The `SentisPoseProvider.cs` script references this model at runtime

### Model Settings

In the `SentisPoseProvider` component Inspector:
- **Model Asset**: Assign the MoveNet .onnx file
- **Backend Type**: GPUCompute (recommended) or CPU for debugging
- **Inference Interval**: 33ms (30 FPS) for real-time pose detection

## Step 7: Supabase Project Setup

### Create Supabase Project

1. Go to [https://supabase.com](https://supabase.com) and create an account
2. Create a new project (choose a region close to your users)
3. Note your **Project URL** and **Anon Key** from Settings > API

### Configure in Unity

1. Open `Assets/Scripts/Backend/SupabaseConfig.cs`
2. Create a ScriptableObject instance:
   - Right-click in Project > Create > MetaCricket > Supabase Config
   - Fill in your Project URL and Anon Key
3. Assign the config to the `SupabaseManager` GameObject in your scene

### Create Database Tables

Open the Supabase SQL Editor and run the following:

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
```

### Enable Row Level Security

```sql
ALTER TABLE players ENABLE ROW LEVEL SECURITY;
ALTER TABLE leaderboard ENABLE ROW LEVEL SECURITY;
ALTER TABLE career_progress ENABLE ROW LEVEL SECURITY;

-- Players policies
CREATE POLICY "Players are viewable by everyone" ON players FOR SELECT USING (true);
CREATE POLICY "Users can update own player" ON players FOR UPDATE USING (auth.uid() = auth_id);
CREATE POLICY "Users can insert own player" ON players FOR INSERT WITH CHECK (auth.uid() = auth_id);

-- Leaderboard policies
CREATE POLICY "Leaderboard is viewable by everyone" ON leaderboard FOR SELECT USING (true);
CREATE POLICY "Users can insert own scores" ON leaderboard FOR INSERT WITH CHECK (
    player_id IN (SELECT id FROM players WHERE auth_id = auth.uid())
);

-- Career progress policies
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

### Enable Authentication

1. In Supabase Dashboard, go to **Authentication > Providers**
2. Enable **Email** provider (enabled by default)
3. Optionally enable **Google** OAuth for social login
4. The `SupabaseAuth.cs` script handles sign-up, sign-in, and session management

## Step 8: Build for Android

### First-Time Build Setup

1. **File > Build Settings**
2. Select **Android** platform
3. Click **Switch Platform** (if not already on Android)
4. Click **Player Settings** and verify:
   - Company Name: Your name or studio
   - Product Name: Meta Cricket
   - Package Name: `com.yourname.metacricket`
   - Version: 1.0.0
5. Ensure all scenes are added to the build (click "Add Open Scenes" or drag scenes from Project)

### Scene Build Order

| Index | Scene |
|-------|-------|
| 0 | SplashScreen |
| 1 | MainMenu |
| 2 | Calibration |
| 3 | Match |
| 4 | MatchResult |

### Build and Deploy

1. Connect your Android device via USB
2. Enable **Developer Options** and **USB Debugging** on the device
3. In Build Settings, click **Build and Run**
4. Choose a location to save the APK
5. Unity will build, install, and launch on the device

## Step 9: First-Run Calibration

When launching the app for the first time on a device:

1. **Grant Camera Permission** - The app requires camera access for AR and pose detection
2. **Position the Phone** - Place the phone on a stand or have someone hold it, facing you at chest height, approximately 2-3 meters away
3. **Perform T-Pose** - Stand with arms extended horizontally to your sides
4. **Hold for 3 seconds** - The calibration UI shows a progress indicator
5. **Calibration Complete** - Your body proportions are saved for accurate shot detection
6. **Ready to Play** - The system will now accurately track your batting motions

### Calibration Tips

- Ensure good lighting (avoid backlighting)
- Wear contrasting clothing (avoid patterns that confuse pose detection)
- Stand on a flat surface with room to swing
- Keep the full body visible in frame
- Recalibrate if you change distance from the camera significantly

## Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| ARCore not available | Ensure device supports ARCore (check Google's device list) |
| Pose detection slow | Reduce camera resolution or switch Sentis backend to GPUCompute |
| Build fails on IL2CPP | Ensure Android NDK is installed (via Unity Hub modules) |
| Supabase connection fails | Check Project URL and Anon Key in SupabaseConfig asset |
| Black screen on device | Verify URP asset is assigned in Graphics settings |
| Calibration fails | Ensure full body is visible and lighting is adequate |

### Performance Targets

| Metric | Target |
|--------|--------|
| Frame Rate | 30 FPS minimum on mid-range devices |
| Pose Detection | < 33ms per frame |
| Input Latency | < 100ms from swing to ball contact |
| Memory Usage | < 1GB RAM |
| APK Size | < 150MB |

## Development Workflow

1. Make code changes in your IDE (Visual Studio, Rider, VS Code)
2. Unity auto-compiles on focus
3. Test in Editor using mouse/keyboard input simulation
4. Deploy to device for AR and motion testing
5. Use Unity Profiler for performance optimization
