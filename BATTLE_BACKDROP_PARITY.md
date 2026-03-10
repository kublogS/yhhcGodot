# Battle Backdrop Parity (Python -> Godot)

## Goal
Preserve player-facing continuity between explore and battle, without reproducing Python renderer internals.

## Current Strategy
- At battle trigger time in explore, Godot captures the current viewport frame.
- That frame is stored in `GameSession` as a one-shot contextual battle backdrop.
- `BattleController` consumes it on scene load and uses it as the battle background.

Files:
- `Scripts/Explore/ExploreController.cs`
- `Scripts/Autoload/GameSessionBattleBackdrop.cs`
- `Scripts/UI/BattleControllerBackdrop.cs`

## Fallback Behavior
If no contextual frame is available (null viewport, capture failure, non-explore entry):
- battle background falls back to `GameAssets.LoadBattleBackground()`
- if that asset is missing, existing `BackdropFallback` remains active.

## Accepted Divergence
- Python caches a raycast frame during explore rendering.
- Godot uses a viewport snapshot at encounter start.
- This is an intentional implementation divergence with equivalent UX intent: battle feels attached to explored space.

## Known Limitation
- Snapshot may include transient HUD/crosshair elements depending on current frame composition.
- This is accepted for now to keep implementation low-risk and minimally coupled.
