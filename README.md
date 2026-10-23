# FirstHand AR

Mobile AR first aid trainer built with Unity 6 and AR Foundation. The trainee places a collapsed worker on a real floor and works through the response: check for danger, check response, call 112, check breathing, then chest compressions or the recovery position. Practice mode gives hints; Assessment mode is timed and scored against a pass mark of 80.

## Requirements
- Unity 6000.4.7f1 with Android Build Support
- An ARCore supported Android phone (Android 8.0 or newer)

## Getting started
1. Open the project in Unity Hub.
2. Run **FirstHand > Build Everything** to generate prefabs, data, audio, Addressables and the Bootstrap scene.
3. Open `Assets/_Project/Scenes/Bootstrap.unity` and press Play (XR Simulation).
4. Run **FirstHand > 4. Build Android APK** to build `Builds/FirstHandAR.apk` (IL2CPP, ARM64).

## Target OS
Android 8.0 (API 26) or newer with ARCore.

## Structure
- `Assets/_Project/Scripts` runtime code (Core, Data, Steps, Scoring, Entities, AR, Simulation, Presentation)
- `Assets/_Project/Editor` one click content builders
- `Assets/_Project/Tests` EditMode tests
- `Docs` asset credits and the UI designs from Affinity

## Credits

### 3D models

All models are from Poly Pizza (poly.pizza).

| Model | Author | License | Used as |
|---|---|---|---|
| Worker | Quaternius | CC0 | Casualty (collapsed security guard) |
| Wet Floor Sign | JToastie | CC-BY 3.0 | Wet floor hazard, safe state |
| Cable Long | Quaternius | CC0 | Loose cable hazard |
| Smashed Glass | AlexCrean | CC-BY 3.0 | Broken glass hazard |
| Gas Tank | Quaternius | CC0 | Gas cylinder hazard |
| Trashcan Small | Quaternius | CC0 | Smoking bin hazard |
| Shopping cart | Poly by Google | CC-BY 3.0 | Moving trolley hazard |
| Backpack | Quaternius | CC0 | Decoy |
| Office Chair | CMHT Oculus | CC-BY 3.0 | Decoy |
| Water bottle | jeremy | CC-BY 3.0 | Decoy |
| Houseplant | Quaternius | CC0 | Decoy |
| Smartphone | smallbigsquare | CC0 | Phone prop |

### Fonts

All fonts are under the SIL Open Font License 1.1.

| Font | Author | Used for |
|---|---|---|
| Bricolage Grotesque | Mathieu Triay | Headings |
| Instrument Sans | Instrument | Body text |
| Geist Mono | Vercel | Numbers, labels, timers |

### Icons, artwork and audio

The UI icons, ECG line, app icon and screen concepts were drawn for this project in Affinity and are original. Sound effects are synthesised in the editor. Narration clips are placeholders made with the macOS text to speech voice.
