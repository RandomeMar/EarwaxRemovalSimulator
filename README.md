# Earwax Removal Simulator

This capstone project was a collaboration between Mauricio Alvarado, Edgar Vidales, Andrew Rojas, and Gianpierre Terry made at the University of Nebraska Omaha.
This application's goal is to provide medical students a safe and realistic way to practice cerumen extraction without a patient necessary. This is achieved through
the use of a VR headset and the Haply Inverse 3 haptic controller. This project was developed using Unity (URP), the Haply SDK for Unity, and Unity's ShaderLab.

## My Contributions

### XPBD Solver
I wrote an XPBD-based physics solver for simulating the behavior of earwax in a realistic yet performant way. The solver supports:
- **Distance Constraints:** Distance constraints are deformable/destroyable. Distance constraints have dynamic rest lengths to simulate viscoelasticity.
- **Density Constraint:** A global density constraint is used to keep particles from intersecting.
- **Collision Constraint**: Collision handling for collider vs. particle and collider vs. collider interactions.
- **Adhesion Constraints:** Used for mimicing stickiness against colliders.

### Screen Space Earwax Render Feature
I wrote a custom render feature based on screen space techniques used to render fluids. The idea is it blends individual particles into a singular mass. The general pipeline is:
1. Instance a spherical billboard for each particle position and store it in a depth texture.
2. Sample the depth texture and approximate a surface normal for each pixel. Apply Lambertian diffuse-style lighting.
3. Composite the earwax blob image with the main scene's color target.

## Demo and Examples
[![Watch the video](https://img.youtube.com/vi/0IO1jmCm-rU/maxresdefault.jpg)](https://www.youtube.com/watch?v=0IO1jmCm-rU)
This demo showcases the softbody physics in action, along with the earwax rendering.

**Original Scene Color:**

<img src="images/earwax_frame_1.png" width="50%">

**Billboard Pass:**

<img src="images/earwax_frame_2.png" width="50%">

**Depth Sample + Diffuse Pass:**

<img src="images/earwax_frame_3.png" width="50%">

**Composite Pass:**

<img src="images/earwax_frame_4.png" width="50%">

## How to Setup
### Prerequisites
- Need: VR headset (Meta Quest), Haptic controller (Haply Inverse 3), VR-ready computer/laptop (Windows)
- Install Unity Hub and Unity version 6000.3.9f1
- Install the Meta Quest Link app and follow the Quest setup instructions
- Clone the main branch of this repository

#### Unity setup
- In the Unity Hub, press Open > From Disk
- Select the AudiologyUnityProject folder from the repository
- Open the project to load dependencies

#### Quest
- Enable Developer Mode
- Connect to the Meta quest link app on the computer

#### Haply
- Follow Haply quick start guide
- Configure Haply as needed

### Running the project
- Make sure Haply controller is connected to the computer using USB connection and the stylus is on and the USB wireless link is connected to the computer.
- Make sure VR headset is connected to the computer using a USB connection, or Wireless connection using a local network.
- In Unity, open the Assets/_Project/Scenes/StartSceneVR
- Press the play button
- If using VR Headset, use your hands to move the virtual hands and open and close either your left or right hand to select items.
- If not using the VR headset, use your mouse and keyboard.

## Release Notes
### Code Milestone 1
- The Haply Inverse 3 Haptic controller works as intended for controlling the simulated currete. However, the feedback does not currently work for the scene we intend to use.
- The earwax cube blockage works as intended in the ear canal.
- After the Unity update, the Meta Quest controllers and VR headset no longer controls the look angle and move the simulated hands.
- Previous project build has been thoroughly reviewed.

### Code Milestone 2
- Title, settings, simulation, and end scenes are now connected.
- Created new softbody earwax.
- Developing new main scene to replace the simulation scene to use new earwax and ear model.
- Meta VR headset functionality is still not functioning properly after Unity update. Troubleshooting.

### Code Milestone 3
- Added friction to earwax
- Added tearing to earwax
- Added SDF based colliders
- Added SDF collider viewers
- Added adhesion constraints (stickiness)
- Colliders are now affected by earwax
- Added Haply controller collision and feedback with ear model
- Added mouse controls to currette to aid code development when Haply controller is absent

### Code Milestone 4
- Revised Stats scene to provide Top 5 scores, Top 5 fastest time, name search features.
- Added stat data deletion and password functionality.
- Created new colliders and force feedback functionality between new earwax and curette.

### Code Milestone 5
- Completed scoring system functionality.
- Updated simulation scene to include new ear, earwax, curette collision.
- Finalized ear wax rendering and fixed rendering bugs.
- Further testing and framerate performance optimization (nominal 90 fps).
- Optimized scenes code; reducing code in some scripts and creating new script files for better logic flow and usability.
