Ear-Curette Collision System
Overview

This system stops the curette from going through the ear mesh and also adds force feedback when using the Haply Inverse3 device. There are two collision methods so both can be tested and compared.

Scripts Added
ComputePenCollisionTest.cs
Goes on the simple-curette.
Uses Physics.ComputePenetration to detect when the curette is inside the ear and pushes it back out.
MouseCollisionTest.cs
Goes on the simple-curette.
Uses raycasts in multiple directions to detect the ear surface and prevent penetration.
EarCollision.cs
Goes on NewEarMesh.
Handles collision detection for the Haply device and calculates force feedback. Also optionally pushes the cursor out visually.
HapticManager.cs (modified)
Goes on HapticManager.
Updated to include EarCollision so the forces from the ear collision are applied to the Haply device.
Mouse Test Setup (CollisionTest scene)

Objects needed:

NewEarMesh
MeshCollider (Convex OFF)
EarCollision (optional for mouse only)
simple-curette
ComputePenCollisionTest (enable first)
MouseCollisionTest (disabled unless testing raycasts)

Steps:

Create a new scene.
Add NewEarMesh and make sure it has a MeshCollider with Convex OFF.
Add simple-curette.
Remove any Rigidbody or MeshCollider from the curette.
Add both collision scripts to the curette.
Drag NewEarMesh into the Ear Mesh field.
Set Collision Radius = 0.05.
Press Play and move the mouse to test.

Only enable one collision script at a time or they will conflict.

Haply Device Setup

Hierarchy:

Haptic Origin
Haptic Controller
Cursor
simple-curette (child of Cursor, with SphereCollider)
NewEarMesh
MeshCollider (Convex OFF)
EarCollision
HapticManager

Steps:

Open the device test scene.
Add EarCollision to NewEarMesh.
Add a SphereCollider to the curette (isTrigger = true).
Make curette a child of Cursor.
Set stiffness and damping in EarCollision.
Make sure useMouse = false in HapticManager.
Press Play and the device should resist when touching the ear.
Collision Methods
ComputePenCollisionTest
Adds a sphere collider to the curette
Checks if it overlaps the ear mesh
Pushes it out if inside
Uses multiple iterations for stability

Good:

Simple
Works well in most cases

Issue:

Can jitter near mesh edges due to Unity limitation
MouseCollisionTest
Sends raycasts in multiple directions
Detects if curette is too close to the surface
Pushes it out using the average normal

Good:

More stable
No jitter

Downside:

Slightly more CPU usage
EarCollision (Force Feedback)
Detects penetration using ComputePenetration
Calculates spring-like force
Sends force to Haply device
Optional visual push-out for mouse mode
Controls (Mouse Mode)
Move mouse -> move curette
Scroll wheel -> move forward/back
Right click drag -> rotate curette
Notes / Common Issues
If curette goes through ear -> increase collision radius
Make sure ear MeshCollider has Convex OFF
Only enable one collision script at a time
For device mode, remove mouse collision scripts

The curette now:

Cannot pass through the ear
Gets pushed out visually (mouse mode)
Provides physical resistance (device mode)