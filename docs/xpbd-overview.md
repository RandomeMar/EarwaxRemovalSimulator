# XPBD Overview
**Author:** Mauricio Alvarado (@RandomeMar)

## Purpose
This document is meant to give a high-level understanding of **Extended Position Based Dynamics (XPBD)**. This technique will be used extensively to model our soft-body earwax.

## Why XPBD?
The main reason we are going with XPBD over other physics solvers for modelling our earwax is because XPBD is very fast for the capabilities it provides. When configured properly, XPBD is often quicker than both FEM and MPM simulations. This is because unlike those other solvers, XPBD does not have a global system that needs to be solved every frame. Instead XPBD just solves local constraints iteratively. The trade off with this is that XPBD is less accurate in comparison to FEM and MPM. XPBD is mostly accurate though. This makes it a great fit for our project which requires real time speeds while being robust enough to model plastic deformation, tearing, and incompressibility.

## Basics
Extended Position Based Dynamics is a physics solver method used a lot in games and interactive sims for its robustness and quick solve times. It is based on its ancestor, **Position Based Dynamics (PBD)**. For a basic understanding, there are two major aspects of XPBD, **particles** and **constraints**.

## Particles
Particles are the things being moved in our simulation. The attributes that define a particle are:
- Current position
- Previous position
- Velocity
- Mass

A good way to think of them is as masses moving through space. If we want to model something larger than a particle like a clump of earwax, we need to stop thinking of it as a singular object, but instead as a collection of particles.


## Constraints
A constraint can be thought of as a rule that we want to enforce on particles. Examples are:
- I want these two particles to be a certain distance apart.
- I want particles to not collide with the floor.
- I want nearby particles to maintain a certain density.

We define this rule through a function $C(x)$. Here, $x$ is the set of particle positions involved in the constraint. $C(x)$ acts as an error function for our constraint. Basically, the further $C(x)$ is from 0, the higher the error is. A good example can be seen with distance constraints. With a distance constraint, we want to keep to particles, $x_i$ and $x_j$, a certain distance apart from each other. Given our rule, we can use:

$$
C(x_i, x_j) = |x_i - x_j| - restLength
$$

When $C(x_i, x_j) > 0$, the distance between $x_i$ and $x_j$ is longer than our rest length. If $C(x_i, x_j) < 0$, the distance is too short.

$C(x)$ can help us get an understanding of how much a constraint is being violated, but in order to move our particles in a way that corrects this, we need to know what direction we need to move the particles in. This is gotten by finding the **gradient of C(x)**. A gradient is shown with this notation: $\nabla C(x)$. If you are unfamiliar with gradients, the gradient of a function shows the direction of greatest change. So in our case, $\nabla C(x)$ will point in the direction where our constraint error is growing most. We want to prevent our error from growing, so we will use $-\nabla C(x)$ as our direction to apply our correction since it points opposite the direction of increasing error.


### Delta Lambda
Now that we know how much our constraint is being violated and the direction we need to move our particle to correct it. We just plug in our values to this formula:

$$
\Delta \lambda =
\frac{-C(x) - \alpha \lambda}
{\sum_i w_i \lVert \nabla_i C \rVert^2 + \alpha}
$$

This may seem like a lot but it is not too bad. $\Delta \lambda$ will be the magnitude of our constraint correction. I will explain $\lambda$ and $\Delta \lambda$ further in a later section. For now they are not super important for a basic understanding of XPBD/PBD. Alpha ($\alpha$) is a very important scalar. It is calculated through this formula:

$$
\alpha = \frac{\text{compliance}}{\Delta t^2}
$$

$compliance$ is how "strict" a constraint is. If $compliance$ is 0, the constraint is absolute and cannot be violated at all. If the $compliance$ is higher, the constraint can be violated a little. $\Delta t$ is delta time. By including it in our formulas, we make our sim frame rate independent, which is important for more realistic corrections. To conclude, $\alpha$ acts as both the strictness of our constraint and a value that scales with frame rate.

The last thing is that summation in the denominator:

$$
\sum_i w_i \lVert \nabla_i C \rVert^2
$$

Here, $i$ represents all particles involved in the constraint. For a binary constraint like distance, it will only be two. For others, it can be much larger. For each particle, we are multiplying its inverse mass ($w_i$) by the squared magnitude of its error gradient ($\lVert \nabla_i C \rVert^2$). The reason inverse mass is involved is because particles with more mass move less when the same force is applied. Usually in order to avoid unecessary calculations, we store inverse mass instead of mass (our sim stores both).

### Position Correction
Once $\Delta \lambda$ is calculated, we change our particle's position with this formula:

$$
\Delta x_i = w_i \nabla_i C \, \Delta \lambda
$$

Here, $\Delta x_i$ is the vector representing the correction movement, $w_i$ is the particles inverse mass, $\nabla_i C$ is the direction of the change, and $\Delta \lambda$ is the amount we are pushing the particle. You might be confused why we are using the positive gradient of C here instead of the negative, but remember that $\Delta \lambda$ already has a negative that flips the gradient's direction.

### Solver Iterations
In order to improve the accuracy of XPBD, we solve constraints more than once per each physics frame. We call these substeps for each frame **solver iterations.** This is where lambda and delta lambda come into play. $\lambda$ acts as a form of "memory" for the solver. Every time $\Delta \lambda$ is calculated, $\lambda$ adds $\Delta \lambda$ to itself. As $\lambda$ grows each solver iteration, following $\Delta \lambda$ values get smaller and smaller. This allows incremental changes without corrections exploding to large sizes. At the start of the next physics frame, $\lambda$ is set back to 0.

### Conclusion on Constraints
- Constraints have a $C(x)$ function that equals 0 when the constraint is not being violated.
- Constraints have a compliance value that acts as the strictness of the constraint. 0 = very strict.
- Constraints use $-\nabla C$ to find the direction of the correction.
- $\Delta \lambda$ acts as the magnitude of the correction.
- $\lambda$ acts as memory for the solver. It allows for incremental changes every solver iteration.

## Physics Frame Sequence
Every physics frame, these actions will be taken in this order:

### 1. Reset Lambdas
Set the lambda value of all constraints to 0.

### 2. Apply External Forces
Update particle velocities by any external forces. In our case, the only external force we need to worry about is gravity updating velocities downward.

### 3. Predict Positions
This step changes the position of particles based on their velocities. It is important to note at this step particles are ignoring constraints. This means particles will move through floors and walls freely.

### 4. Solve Constraints
In XPBD, this is the main step. This is where constraints detect any violations that occured as a result of predicting positions in the previous step. This is where constraints will be solved for each solver iteration. This is where position corrections will be applied.

### 5. Update Velocities
Lastly, particle velocities are updated based on the change in the particles position from the beginning of the frame to the end of it.
