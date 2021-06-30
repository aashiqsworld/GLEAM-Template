# GLEAM-Template
 ### Generate Light Estimation for AR on Mobile systems in real time

GLEAM is an augmented reality framework that provides accurate and real-time illumination on mobile devices. It currently supports the ARFoundation plugin for Unity.

# Installation

To use GLEAM in your own Unity project, clone this repository and export to a Unity package in the editor. You can then drag this package into your own project.

## ARFoundation

The Unity scene must contain the following objects:

* a reflection probe
* a mesh with a reflective material
* an ARCamera

The real world scene must contain the following objects:

* visible image target
* reflective sphere

1. Attach the GLEAMBehaviour script to the reflective mesh.
2. Position the reflection probe at the same spot as the reflective mesh.
3. In the real world, measure the distance between the center of the image target and the reflection probe on each axis. Set the "probeOffset" parameter in GLEAMBehaviour to this Vector3. (1 Unity unit = 1 meter).
4. Drag a reference to the reflective mesh to the "probe" variable.
5. Set "intensity" to 1.
6. Drag the scene's skybox into the "skyMaterial" reference.

Note that the current version of GLEAM is only supported on Unity 2018.4.8 and up.

