# Samurai vs Zombies — Unity 6 Mac port

This branch builds the recovered game as a native Apple Silicon macOS application using Unity 6000.6.8f1. It upgrades the Unity 2017 project and its serialized assets; use a separate checkout to keep working with Unity 2017.

## Build on macOS

Install Unity Editor 6000.6.8f1 for Apple Silicon with macOS build support, and activate an editor license through Unity Hub. Open this project and let Unity finish importing it.

To build from a terminal at the repository root:

```sh
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity"
"$UNITY_EDITOR" -batchmode -nographics -quit   -projectPath "$PWD" -executeMethod MacBuild.Build   -logFile "$PWD/mac-build.log"
```

The output is `Builds/Samurai vs Zombies.app`, a windowed 1280×800 ARM64 development build. The command fails if Unity reports a failed build or any build errors. It does not notarize, publish, or install the app.

Use A/D to move and the mouse for the on-screen controls. No automatic test shutdown is included in this branch.

## What changed

- Replaced removed GUIText/GUITexture components with screen-space renderers. Text uses the recovered bitmap-font atlases directly.
- Replaced 444 legacy emitter/animator/renderer sets with compatibility components backed by ParticleSystem rendering. Existing custom particle scripts use the replacement particle data.
- Updated object activation APIs while preserving recursive activation behavior, removed the UnityScript array helper, and deferred gameplay initialization until Start.
- Upgraded serialized assets through Unity 2022 before importing them into Unity 6, and exported legacy project settings through native Unity serializers.
- Removed 14 unreferenced compiled shader artifacts from the imported assets and repaired a recovered shader's invalid output fields. The original artifacts remain available in Git history.

## Validation and limits

The development port built with zero logged build errors on an Apple M1. A local opt-in test loaded gameplay and captured the tutorial scene without logged runtime errors. Captures were inspected to correct missing bitmap text and UI draw order. The local test harness is excluded here because it changes scenes, uses the active save, and closes the app automatically.

Legacy asset and missing-script warnings remain in the build log. This is not exhaustive gameplay or visual-parity validation. Particle force, damping, orbit and random-distribution behavior is reconstructed; exact agreement with the old engine is unverified. Thirty original emitter inner-radius values outside 0–1 remain recorded in the assets but are clamped during simulation. Later levels, alternate languages, Intel Macs, Windows, Linux, Android and iOS have not been validated with this migrated project.

Save files, currency edits, built applications, editor caches and local diagnostic artifacts are not included. Back up existing saves before trying a development build. Maintainer agreement on adopting the Unity 6 project migration is still needed before merging it into the original project.
