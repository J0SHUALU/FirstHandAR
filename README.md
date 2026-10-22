# FirstHand AR

Mobile AR first aid trainer built with Unity 6 and AR Foundation. The trainee places a collapsed worker on a real floor and works through the response: check for danger, check response, call 112, check breathing, then chest compressions or the recovery position. Practice mode gives hints; Assessment mode is timed and scored against a pass mark of 80.

## Requirements
- Unity 6000.4.7f1 with Android Build Support
- An ARCore supported Android phone (Android 8.0 or newer)

## Getting started
1. Open the project in Unity Hub.
2. Run **FirstHand > Build Everything** to generate prefabs, data, audio, Addressables and the Bootstrap scene.
3. Open `Assets/_Project/Scenes/Bootstrap.unity` and press Play (XR Simulation) or build to Android.

## Structure
- `Assets/_Project/Scripts` runtime code (Core, Data, Steps, Scoring, Entities, AR, Simulation, Presentation)
- `Assets/_Project/Editor` one click content builders
- `Assets/_Project/Tests` EditMode tests
- `Docs` credits and design exports
