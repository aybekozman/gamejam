# Android mobile adaptation — 2026-11-03

The authored underwater and ship scenes and gameplay balance were preserved. Mobile controls are runtime additions using cropped copies of Assets/Sprites/mobilebuttons.png; the source sheet is unchanged.

- Left joystick steers either protagonist. A second finger holds fish boost or taps ship dash. Native speed, cost and steering rules remain in use.
- Contextual speech button handles inspect, talk, continue and home interactions. Dialogue panels also accept a tap to advance.
- Top-left energy/XP selector accepts direct touches on either half. Upgrade cards accept touch selection through the existing upgrade handler.
- On-screen pause/resume; held controls clear on disable, focus loss and application suspension.
- Landscape left/right, safe-area aware overlays/HUD, phone and tablet scaling; mobile instructions replace keyboard prompts.
- P shortcut removed. Four short empty-space taps followed by a fifth hold of at least one second skip the current part through the existing journey transition. Taps must be within 0.55 seconds of each other; UI taps, multitouch, drags and a short fifth press reset the sequence. No hint appears in game.

## Verification

check-phone.txt: 24 passing checks at 1600x720.
check-tablet.txt: 24 passing checks at 1024x768.
Both runs inject actual InputSystem TouchState events into a virtual touchscreen, covering two simultaneous fingers, real movement and dash cost, dialogue, pause, direct loot selection, upgrade selection, hidden gesture positive/negative cases and simulated display insets. No runtime errors were recorded.
The tablet tutorial strip was moved below the HUD after visual inspection showed an overlap.
These are Editor integration tests, not physical Android device tests. adb reported no connected device.

## Build

Unity 6000.3.19f1; Android ARM64 / IL2CPP; OpenGLES3; minimum API25; target API35.
Package ID: com.andronovagames.remnantsofthesea
Build script: Assets/Editor/VoyageAndroidSetup.cs (command bridge Temp/mobile-command.txt).
APK: Builds/Android/RemnantsOfTheSea.apk
Only MainMenu, UnderwaterWorld and ShipScene 1 are included.
Build result and package verification will be appended after completion.

Build completed successfully with zero errors (20m03s, including first platform import/native compilation).
APK file size: 125,863,355 bytes (~120 MiB). Development/test APK, signed with the local Android debug key.
Android build-tools apksigner verify: exit 0; APK Signature Scheme v2 verified; one signer.
aapt badging: package com.andronovagames.remnantsofthesea, version1.0, minSDK25, targetSDK35, ARM64-v8a, OpenGLES3, launchable UnityPlayerGameActivity.
Only the APK is required for sideloading; Burst debug information and symbols are developer artifacts.
No physical Android device was attached, so installation, hardware video playback and device performance remain unverified.

Application display name updated to Remnants of the Sea. Rebuilt APK successfully (zero errors); aapt application-label verified; apksigner verification passed.

## Mobile feedback pass — 2026-11-03

- Expanded discovery arrival/escort completion and interaction radii; dead bodies inspect automatically on approach through the existing story handler.
- Dialogue advances with short screen taps; no continue button. Mobile dialogue no longer expires automatically; travel lines queue until read. Movement, boost, loot and pause inputs are excluded from dialogue taps.
- Floating joystick uses the left lower region as its touch area; center follows touch-down, visual diameter240 (was200), travel68, knob86. Default joystick and boost/interact positions were raised.
- Ship left HUD scaled125%, loot selector an additional135%; purification bar moved below it.
- Intro video no longer plays. First launch displays the game logo for1.8sec, then the menu.
- Harbor attack clearance increased from17 to30 world units from the harbor origin. The native producer and combat balance remain unchanged.
- Peaceful energy starts only after the same clearance and gameplay dialogue; first spawn after2sec, max6 pickups (was20), one spawn per6sec (was1.4).
- Removed fish boost lesson. Ship tutorial points to the loot selector with a green arrow, then explains carrying clean water to town and points to purification percentage.
- Shark journey objective and travel dialogue explicitly explain dodging cannonballs to reach the shark leader.

polish-phone.txt and polish-tablet.txt each contain44 passing integration checks, with actual InputSystem touch events and zero runtime errors. Includes floating-stick center, multitouch, whole-screen dialogue, automatic corpse inspections, departure/energy thresholds, arrows, selection cards, hidden gesture and shark instructions. Guide screenshots inspected in both aspect ratios. Physical Android hardware remains untested.

Feedback-pass APK rebuilt successfully: zero build errors, 6m53s. Final APK size147,487,215 bytes. aapt confirms Remnants of the Sea / ARM64; APK Signature Scheme v2 verification passed. This supersedes the earlier APK build-size entry.

Energy/joystick follow-up: first peaceful ship visit shows an energy conservation/pickup lesson after purification, pointing at the energy bar for seven seconds. Floating stick returns to its original local position on release/cancel/disable, while touch-down still relocates its center. energy-return-check.txt:46 passing integration checks, including both new behaviors; energy-guide.png visually inspected.

Energy/joystick APK build succeeded with zero errors (5m43s). APK signature v2 verified. Physical Android hardware remains untested.
