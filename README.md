# Powerspheres

## Features

- Noise generation
    - Perlin noise and derivatives:
    - Fractal noise and curl noise
    - Seamlessly tiling noise
- Dynamic grass generation in geometry shaders
    - Shape controllable by various parameters: no. of segments, height,
    width, curve.
    - Quasirandom placement controllable by density slider, one-channel
    mask, and subdivision into grids
    - Reacts to player movement and wind
    - Supports LOD simplification
- Leaf-like particle system
    - Can be picked up by wind
    - Rendered billboards align to trajectory
- Wind system
    - Supports arbitrary wind data fed in from a texture
    - A few controllable parameters such as wind speed and frequency
    - Future extension: write modifications to wind texture based on player
    interaction
- URP-compatible shaders
    - Distance-based shading / material swapping
    - Simple Cook-Torrance shading using Lambertian diffuse and Blinn-Phong
    NDF and combined fresnel-visibility approximation for specular
    - Default URP GI shading for indirect
    - Post-processing effects in compute shader

## Demo controls

### Mouse+keyboard
- Right/Left mouse buttons: Pick up or drop sphere for corresponding side
- WASD / Arrow keys: Movement
- Mouse movement: Look
- Space: Jump

### Controller (Xbox)
- RT/LT: Pick up / drop
- Dpad: Movement
- Right stick: Look
- Y: Jump

------

### Digital art concept

#### Title & concept
Hidden miniature worlds

#### Explanation of concept
A colorless, formless world is gradually revealed
by interacting with miniature spheres found scattered around. Each one, a
tiny world unto itself, when released, will fill the outer world with its
influence.

It is possible to control each sphere independently and observe the
combinations of effects possible under their influence. However, to suit
traditional control schemes, this demo only allows moving two each time.
