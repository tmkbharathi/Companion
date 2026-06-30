# Godot 3D Pet Companion Project

This is a Godot 4.x (C#/.NET) project for rendering a 3D interactive desktop pet. It features transparency, a borderless window, click-and-drag movement, breathing idle animation, and global keyboard listening to animate the paws typing on a 3D laptop.

## Setup Instructions

1. **Download Godot:**
   - Download **Godot Engine 4.2 or 4.3 (Mono/C# version)** from the official website: [godotengine.org](https://godotengine.org/download).
   - *Note:* Make sure you download the .NET / Mono version so it can run the C# codebase.

2. **Open the Project:**
   - Open the Godot Editor.
   - Click **Import** and select the `project.godot` file in this directory.

3. **Build / Export the Executable:**
   - In Godot, click on the **.NET build icon** in the top-right to restore NuGet packages and build the assembly.
   - Go to **Project -> Export**.
   - Add a preset for **Windows Desktop**.
   - Set the export path to `build/godot_pet.exe`.
   - Click **Export Project** to compile `godot_pet.exe`.

4. **Integration with the main Companion App:**
   - Once compiled, the main WPF application will detect `GodotPet/build/godot_pet.exe` and automatically launch and manage the 3D pet companion when you select the "3D" option under Pet Companion settings!
