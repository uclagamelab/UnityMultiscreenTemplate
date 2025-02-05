# UGLMultiscreen

## Overview

The following library was made to ease some of the not-fun parts of developing multi-monitor projects in Unity.

![Enter image alt description](Images/0gN_Image_1.gif)

Features include:

- Simulating various multi-monitor on a single screen, both for developing in editor, and for making single-screen builds.

- Tools for automatically positioning and setting up cameras according to a particular monitor layout.

- Scripts to query which monitor/cameras an object is visible, and to get callbacks when that visibility changes.

- Ability to rearrange screens in built games to account for OS assigned screen order changes.

## What Unity Version Should I Use?

Use Unity 6, or Unity 2022.3

## Installation

#### Option 1 (recommended):

You can download the Unity package [here](https://drive.google.com/file/d/1-_6geIsqZwJgo52MSaDoZEjJEIqEZIzb/view?usp=drive_link)

#### Option 2:

Download/clone the git repository \
[https://github.com/uclagamelab/UnityMultiscreenTemplate](https://github.com/uclagamelab/UnityMultiscreenTemplate)


## Example Scenes

#### MultiScreenExample_3x2

![Enter image alt description](Images/4oA_Image_2.gif)

Cameras arranged with the simple grid option.

Uses drag and drop events to change the appearance of a sphere when it moves onto camera 1, and to create a particle system burst each time it enters a new screen.

Uses some of the advanced coding to log specific enter and exit information to the console,and to a label above the object.

You can see the scripts and setup on ‘*EXAMPLE_OBJECT/Sphere’*

![Enter image alt description](Images/Q60_Image_3.png)

#### MultiScreenExample_6x1

![Enter image alt description](Images/Hhj_Image_4.gif)

Cameras arranged into 6x1 box, arranged as a perspective franken-camera.

- EXAMPLE_MOUSE_CURSOR uses example script MultiScreenMouse example to position a mouse cursor in the appropriate camera, and raycast into the scene.

- The boxes under ‘CLICK_CUBES’ use example script ‘MoveToCameraRandomly’ to move between cameras randomly, respond to being clicked on.

## Quick Start

1. Create a new Unity project (or make a new Scene in an existing project). Download and add the [UGL Multiscreen Unity package](https://drive.google.com/file/d/1-_6geIsqZwJgo52MSaDoZEjJEIqEZIzb/view?usp=drive_link) using the instructions found [here](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetPackagesImport.html): Assets > Import Package > Custom Package… \


2. Delete the default Main Camera.

![Enter image alt description](Images/rPa_Image_5.png)
*The children of this object are your game view cameras that output to each display in the final build.  These can be moved, have components added freely according to the needs of you project.*

4. Set your display arrangement, and position your cameras ([details](?tab=t.0#bookmark=id.j9zl0m3stz8h)) \
 \
**You can skip this at first, and use the default 3x2 display arrangement. \
 \
*You might also want to add a large model or texture that is visible across all the cameras to get a better sense of your arrangement. \
*

![Enter image alt description](Images/UpN_Image_6.gif)

![Enter image alt description](Images/c43_Image_7.png)

![Enter image alt description](Images/VlX_Image_8.gif)

## Single Screen Simulation Mode

Since it’s probably not convenient to be connected to the full multi-monitor setup at all times while working on your game, this library provides a simulation mode that will display all your game screens in a single game view.

To turn on single screen simulation of multi monitors, hit the ‘ON’ button of the ‘UGLMultiScreen’ component of the prefab. (on by default)

![Enter image alt description](Images/piD_Image_9.gif)

The simulation view might get squished, or scrambled if you resize the game view.  To fix, simply hit the ‘Refresh Simulation View’ button.

![Enter image alt description](Images/FRo_Image_10.gif)

*NOTE: Once you have settled on a screen arrangement, you can check ‘Auto Refresh…’, but it may cause issues while you are setting up.*

IMPORTANT:

Turn off simulation mode when you make a build for the multi-monitor setup!

You can leave it on to make a single screen version suitable for sharing.

## Setting A Screen Layout

With the prefab selected, tick the boxes in a pattern that best represents your intended screen layout, and hit the ‘Refresh Simulation View’ Button.

![Enter image alt description](Images/mWR_Image_11.png)

If you have an exotic idea not reflected by this grid (e.g. putting the monitors in :a outward facing circle), that’s fine, just make a 3x2 box.

![Enter image alt description](Images/Wo8_Image_12.png)

*A 3x2 box arrangement*

If your background image appears out of order, it’s because you need to update the positions of the camera objects in the unity scene, which is covered in the next section.

## Positioning the game cameras

### Position Automatically….

If you want to simulate a contiguous space across the screens, this library provides a few options for automatically positioning, and setting the camera parameters.

To use automatic positioning, open the ‘Auto Arrange Cameras’ drop down, choose your camera arrangement method, and hit the ‘Position World Cameras’ button.

![Enter image alt description](Images/Fxn_Image_13.gif)

### Camera Arrangement Styles

1. Simple Grid

This arranges the game according to the screen arrangement defined by the checkboxes.

![Enter image alt description](Images/ChJ_Image_14.gif)

This is suitable if you don’t mind some overlap/gaps in the views covered by the camera (these gaps and overlaps depend on the spacing between the cameras, the objects’ distance from the camera, and the FOV of the individual cameras).

![Enter image alt description](Images/191_Image_15.gif)

**Parameters:**

***Simple Grid Camera ******Spacing:****  \
	*How much space to put between the camera on X and Y.

2. **Seamless Orthographic (2D)**

This will arrange the cameras perfectly edge to edge in orthographic mode.  This is a good choice if you want to simulate contiguous space across the game views, particularly in 2D.

**Parameters:**

***Franken Orthographic Size******:*****  \
	**The approximate field of view of the resulting simulated camera. 

***Franken Orthographic Padding:***  \
	Add some space (with positive values), or overlap (with a negative) value to the cameras

![Enter image alt description](Images/3mK_Image_16.gif)

3. **Perspective Frankencam (3D)**

This will attempt to arrange the cameras into a single 3D frankencamera.  It’s not perfectly seamless, and suffers from some angular fisheye, but fun in it’s own way.

***Franken Perspective Fov:***** **

The approximate field of view of the resulting simulated camera.

***Franken Perspective Padding:***  \
	Add some space (with positive values), or overlap (with a negative) value to the cameras

![Enter image alt description](Images/ywk_Image_17.gif)

### Or Just Position By hand….

You are also of course free to not use, or to manually adjust ones of the automatic placements above, and position the cameras by hand according to your project needs.

![Enter image alt description](Images/3tF_Image_18.gif)

*Cameras arranged manually into a carousel*

## Running Builds

**IMPORTANT: **Before you make a build, be sure to disable simulation mode if you are making a build for the multi-monitor setup, otherwise your build will display only on a single monitor.

![Enter image alt description](Images/0d0_Image_19.png)

### Fixing mixed-up screens

Press [*CTRL+L]* to bring up the admin panel, and use the drop-down to assign the correct gameview to each screen.  

(This arrangement is saved to preferences, and shouldn’t need to be redone if the monitor arrangement stays the same) 

![Enter image alt description](Images/oMd_Image_20.gif)

## Drag And Drop Events

NOTE: this section is a work in progress, and details are subject to change.

### Respond To An Object Moving Between Screens

1. Add an **UGLMultiScreenObjectEvents** to an object.  

2. Assign the ‘Main Renderer’ for the object to the automatically added *UGLMultiScreenObject*. 

![Enter image alt description](Images/IPG_Image_21.png)
 \
*The object’s visibility will be determined by this renderer.*

3. Set up your events on the *UGLMultiScreenEvents* component.

There are events for the object entering, and exiting the view of each individual camera.

This is used to turn on the warts on the red sphere in the example scene when it enters Camera1

![Enter image alt description](Images/IMG_Image_22.png)

There are also “Any Camera” events that fire when objects enters/exits view of any camera

*This is used to play the white particles each time the sphere enters into view of a new camera.*

![Enter image alt description](Images/88N_Image_23.png)

*NOTE: depending on your camera arrangement, an object may be in view of multiple cameras, or no camera at all.*

## Advanced Scripting

### Getting Visibility Information:

** **First, attach an *UGLMultiScreenObject*[^1] to the object in question.

#### Visibility Callbacks:

With a reference to an *UGLMultiScreenObject*, you can subscribe to the event `OnEnterCameraChange`, as below.

```
 [SerializeField] UGLMultiScreenObject _mso;
 void Awake()
 {
     _mso.OnEnterCameraChange += OnCameraChange;
 }

 private void OnCameraChange(UGLMultiScreenObject.EnterChangeInfo info)
 {
     if (info.entered) 
     {
         Debug.Log("entered camera " + info.camera.cameraNumber, this);
     }
     else //exited
     {
         Debug.Log("exited camera " + info.camera.cameraNumber, this);
     }
 }
```

#### Query Visibility Info Directly:

You can also query which cameras can see the object at any time with: 

##### _mso.getAllIntersectingCameras() //iterate through with a ‘foreach’ loop

##### OR \
 _mso.isVisibleToCamera(int camNumber)

### Accessing Cameras, Arrangement Information

You can access the main script multi-screen manager script UGLMultiScreen with `UGLMultiScreen.Current`.

From this, you can access various info, and such as a list of all the UGLSubCameras (`UGLMultiScreen.Current.Cameras`), or get the camera a specific coordinate in the arrangement, etc…

In particular, if you are interested screen based raycasts, please reference *MultiScreenMouseExample.cs*[^2] 
Each UGLSubCamera exposes information, including its current output display, its position in the 2D camera arrangement (the grid of checkboxes).

*NOTE: If you want to dynamically rearrange the cameras at runtime as part of a game mechanic, this can be tentatively done as below, but this functionality is not fully implemented yet. Please let the instructor know if this is a feature you need.*

```
UGLSubCamera.SetOutputDisplay(int displayNumber);	
//followed by
UGLMultiScreen.Current.RefreshCameraSettings(true);
```
## Notes

[^1]:  An UGLMultiScreenObjectEvents is not required, if you don’t need the drag and drop unity events
[^2]:  Keep in mind that mouse movement between screens depends on Monitor layout in windows.