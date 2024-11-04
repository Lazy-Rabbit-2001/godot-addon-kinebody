# <center> Godot Addon - KineBody

<center>![logo](./addons/kinebody/kinebody2d/KineBody2D.svg)</center>

This is a Godot addon that provides `KineBody2D` and `KineBody3D`(WIP) nodes, which may improve your development of platform games.

## Supported targets

### Programming languages

* GDScript (Using `KineBody2D` and `KineBody3D`)
* C# (Using `KineBody2DCs` and `KineBody3DCs`)

### Versions

* Godot: >= 4.3
* Redot: >= 4.3
* Wedot: Waiting for release
* Blazium: Waiting for release

### Platforms

* Windows
* Linux
* macOS
* Android
* iOS
* Web(GDScript only)

## Preuse

### Installation

* Download the release you want from `releases` and extract the addon folder to your Godot project's `addons` folder and cover the latter.
* (For C# versions) After installing the addon, create a C# solution from `Project -> Tools -> C# -> Create C# Solution`.

Since this addon is one that barely provides extra nodes, there is no `plugin.cfg` file, which means that you don't need to enable it in the project settings. It is enabled once you add it to your project.

### Language

It is recommended to use GDScript version, as it is more efficient and easier to use, and is coherent with Godot's low-level implementation. If you find performance bottlenecks, or you are more comfortable with C#, you can use the C# version.  

Due to some technical limitations, you cannot inherit a C# class from a GDScript class, vice versa. Therefore, make sure which kind of version you want to use before you start coding. Once you have selected one, keep on it as possible as you can.

## What is `KineBody*D`?

A `KineBody*D` is an derived object from `CharacterBody*D`, which is derived with extra features that help your development of platform games, especially for platform-game characters and enemies.

### Gravity

Gravity is a common and important technique in most platform games. However, with `CharacterBody*D`, you cannot yet deploy the gravity system in fingers. Now, `KineBody*D` helps you solve this problem: By introducing `gravity_scale` property, just like one in `RigidBody*D`, it is allowed to realize the gravity system in one minute.  

Many platform games have their players falling with maximum falling speed, and in `KineBody*D`, you can set the maximum falling speed (`max_falling_speed`) with literally a fingertip.  

To activate these features, you should call `move_kinebody()` to make it move with this technique. In this wrapper method of `move_and_slide()`, the gravity is applied automatically to the current `KineBody*D` instance.

### Rotation snapping

For some multi-gravity games, players needs to rotate to fit its apperance to how they should be like in a multi-gravity space. For example, a character in a gravity space whose direction is up should be upside down.  

`KineBody` help you with this problem by introducing a system called `rotation snap`, meaning that when the character is in a special gravity space, its rotation will be snapped to a certain with a tween effect. A new property,`rotation_synchronizing_duration`, is introduced to control the duration of the tween effect. If you need it faster just tweak this value lower.  

The rotation is snapped to the `up_direction` rotating by 90° to make sure the rotation (`global_rotation`) of body fit with its rendering and appearence. To make this work, you need to call `move_kinebody()` as well when the body is going to move, or `synchronize_global_rotation_to_up_direction()` if you want to snap the rotation immediately or manually.

### Collision signals

In original `CharacterBody*D`, there is no collision signal. `KineBody*D` provides collision signals that you can use to detect collisions between `KineBody*D` instances and other physics bodies. There are three signals available:

* `collided_wall()`
* `collided_ceiling()`
* `collided_floor()`

They are emitted respectively when the corresponding collision event is invoked.

### General physics methods

#### WIP
