# Cerumen (Ear Wax) Cleaning Simulation

This virtual reality application is designed to provide medical professionals and trainees with a safe, realistic environment for mastering the proper procedure for cleaning cerumen (ear wax) out of patients' ears. It achieves high fidelity through a comprehensive virtual examination room and patient model, along with integration of a Haply precision haptic controller to allow for realistic textures and tactile feedback during instrument interaction.

## How to Setup
### Prerequisites
- Need: VR headset, Haply device, VR-ready computer/laptop (Windows)
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

## Changelog
### Version 0.9.1
- Updated Unity to 6000.3.9f1

### Version 0.9.0
 - Replaced the usage of an earwax sphere with an earwax cube, using said cube to create an earwax mass for the player to clear from the ear canal
 - Reworked the scripting of the earwax sphere with a new script, giving a better impression of an earwax material that can stick and does not roll or move freely within the ear canal
 - This reworked and new code for Version 0.9.0 can be found within the Brayson-Dev branch
   
### Version 0.8.1
 - Replaced previous patient model (head) with full body patient model
 - New patient model now animates (sitting) within the clinic room
 - Replaced curette model with lower triangle count curette model
 - Yet-to-be-implemented code can found on evie-dev and Brayson-dev branches. In-development features include: Re-working of physics interactions, creation of new FullScene using new patient model
   
### Version 0.8.0
- Finished up scene layout (timer, buttons, XR rig)
- Polished UI with VR capabilities, completed switch to world space UI
- Started cerumen functionality in spawn_test branch
- Fixed Haply device and started testing physics and interactibility
- Code cleanup
### Version 0.6.0
- Added settings UI and refined previous UI
- Started implementation of world space UI for VR functionality
- File I/O with user's stats being saved after completing the simulation scene
- Most updates are in the develop branch + head_models branch
- Because of Haply limitations (broken part), we are unable to work with the device to have it connected with the curette model

### Version  0.4.0
- Added VR Functionality
- Implemented blender models for head and curette
- Created and tested new Haply script for cube feedback
- Started UI work in the UI additions branch (UI for moving between scenes, timer, progression bar)

### Version 0.2.0
- Initial setup for Github (Repository, LFS)
- Implemented and toyed with Haply samples in the Haply testing branch
- Laid out assets directory and created initial scenes
- Uploaded image asset and started UI for title screen
