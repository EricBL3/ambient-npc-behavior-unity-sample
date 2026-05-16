# Ambient NPC Behavior Framework — Unity Sample

A Unity sample project demonstrating integration of the [Ambient NPC Behavior Framework](https://github.com/EricBL3/ambient-npc-behavior-framework) — a C++ shared library for memory-driven ambient NPC behavior.

This project is intended as a concrete starting point for anyone looking to integrate the framework into a Unity project. The wrapper layer under `AmbientNpcUnitySample/Assets/Scripts/Framework/` can be used in other projects as-is. The code under `AmbientNpcUnitySample/Assets/Scripts/Demo/` is specific to this sample scene.

Full documentation, video demos, and the associated paper are available at the [project webpage](https://www.csd.uwo.ca/~ebuitron/).

## Requirements

- Unity 6.X
- Windows (x64) or macOS (Apple Silicon / arm64)
- No additional dependencies — the precompiled framework binary is included in `AmbientNpcUnitySample/Assets/Plugins/`

## Opening and Running the Project

1. Clone this repository:
```
   git clone https://github.com/EricBL3/ambient-npc-behavior-unity-sample.git
```
2. Open the project in Unity Hub by selecting the cloned folder.
3. Open the demo scene at `AmbientNpcUnitySample/Assets/Scenes/[FILL IN SCENE NAME]`.
4. Press Play.

## Repository Structure

```
AmbientNpcUnitySample/
  Assets/
    Scenes/                              # Demo scene
    Scripts/
      Framework/                         # Reusable wrapper layer. Copy this into your own project.
      Demo/                              # Sample-specific logic
    Plugins/                             # Precompiled framework binary and public header
      AmbientCoreFramework.dll           # Windows x64
      BehaviorFrameworkInterface.h       # Public C API header
      LICENSE                            # Apache License 2.0 (framework)
      NOTICE                             # Attribution notice (framework)
      Mac/
        libAmbientCoreFramework.dylib    # macOS arm64
    Configs/                               # JSON behavior configuration files loaded at runtime
LICENSE                                # Apache License 2.0 (this sample project)
NOTICE                                 # Attribution notice (this sample project)
```

## Demo

[VIDEO will be added soon]

The demo features 5 characters exhibiting ambient behavior driven by the memory-driven selection algorithm. The video also shows which files to modify when adding new actions, characters, or behavior configurations.

## Adapting This Project

To use the wrapper layer in a different Unity project:
1. Copy `AmbientNpcUnitySample/Assets/Scripts/Framework/` into your project
2. Copy the binary files from `AmbientNpcUnitySample/Assets/Plugins/` into the same folder structure in your project
3. Create an instance of the following classes: `BehaviorFrameworkManagerBase, AmbientEntityBase, BehavioralEntityBase`
4. Create a `BehaviorFrameworkConfig` ScriptableObject with the configuration for the framework and the references to the json configuration files from `AmbientNpcUnitySample/Assets/Configs/`.
5.  Setup a GameObject with the script that implements `BehaviorFrameworkManagerBase` and reference the ScriptableObject created in the previous step.
6.  Setup a GameObject for each framework or behavioral entity with the script that implements `AmbientEntityBase` (for framework entities) or `BehavioralEntityBase` (for behavioral entities). Add a reference to the appropriate entity json configuration file from `AmbientNpcUnitySample/Assets/Configs/`.

## Framework

The `AmbientNpcUnitySample/Assets/Plugins/` directory contains a precompiled binary of the [Ambient NPC Behavior Framework](https://github.com/EricBL3/ambient-npc-behavior-framework) at version 1.0.0, redistributed under the Apache License 2.0. See `AmbientNpcUnitySample/Assets/Plugins/LICENSE` and `AmbientNpcUnitySample/Assets/Plugins/NOTICE` for details.

## Citation

If you use this project in your research, please cite the associated paper:

Eric Buitron-Lopez and Roberto Solis-Oba, "A Memory-Driven Action Selection Framework for Scalable Ambient NPC Behavior," to appear in *Proceedings of the 2026 IEEE Conference on Games (CoG)*, Madrid, Spain, September 1–4, 2026.

```bibtex
@inproceedings{buitronlopez2026,
    author    = {Buitron-Lopez, Eric and Solis-Oba, Roberto},
    title     = {A Memory-Driven Action Selection Framework for Scalable Ambient {NPC} Behavior},
    booktitle = {Proceedings of the 2026 IEEE Conference on Games (CoG)},
    year      = {2026},
    note      = {To appear}
}
```

## License

This sample project is licensed under the Apache License 2.0. See [LICENSE](LICENSE) for details.

The framework binary in `AmbientNpcUnitySample/Assets/Plugins/` is a separate component licensed under the Apache License 2.0 by Eric Buitron Lopez. See `AmbientNpcUnitySample/Assets/Plugins/LICENSE` and `AmbientNpcUnitySample/Assets/Plugins/NOTICE` for details.
