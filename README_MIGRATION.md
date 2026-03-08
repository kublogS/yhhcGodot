# README_MIGRATION

## Avvio Progetto Godot 4.6 (.NET)
1. Apri Godot 4.6 con supporto .NET/C# installato.
2. Importa la cartella progetto: `godot/YouHaveToHaveCharacter_Godot/`.
3. Verifica in **Project Settings > Autoload**:
   - `GameSession -> res://Scripts/Autoload/GameSession.cs`
   - `SaveService -> res://Scripts/Autoload/SaveService.cs`
   - `Database -> res://Scripts/Autoload/Database.cs`
   - `SceneRouter -> res://Scripts/Autoload/SceneRouter.cs`
4. Esegui `Scenes/Boot.tscn` o premi Run Project.

## Input Principali
- Explore: `WASD` movimento, mouse look
- Overlay: `TAB` mappa, `M` manuale, `ESC` pausa
- Battle: `A` attacco, `D` difesa, `I` item, `Q` fuga
- Slot saves: frecce su/giù, invio, canc

## Flusso Implementato
1. `Boot -> MainMenu`
2. `MainMenu -> SavesMenu` (3 slot)
3. Slot vuoto -> `NameEntry` -> creazione partita -> `Explore`
4. `Explore` FPS 3D blockout + nemici overworld + encounter on contact
5. `Battle` turn-based (fino a 4 attivi + queue)
6. `Reward` (mossa/soldi/oggetti) -> ritorno a `Explore`
7. Save/Load su `user://saves/slot_1.json`, `slot_2.json`, `slot_3.json`

## Mappa Python -> C#
| Python | Godot C# |
|---|---|
| `main.py` | `Scenes/Boot.tscn`, `Scripts/UI/BootController.cs` |
| `core/game_runtime.py` | `Scripts/Autoload/GameSession.cs`, `Scripts/UI/*Controller.cs` |
| `core/state.py` | `Scripts/Core/GameState.cs`, `Scripts/Core/Progression.cs`, `Scripts/Core/EnemyCatalog.cs` |
| `core/combat.py` | `Scripts/Core/CombatService.cs`, `Scripts/Core/CombatServiceDamage.cs`, `Scripts/Core/CombatServiceUtils.cs` |
| `core/save_manager.py` | `Scripts/Autoload/SaveService.cs`, `Scripts/Autoload/SaveServiceRuntimeMapper.cs`, `Scripts/Autoload/SaveServiceStorage.cs`, `Scripts/Autoload/SaveModels.cs` |
| `core/input_actions.py` | `Scripts/Core/InputSetup.cs`, `project.godot [input]` |
| `core/moves.py` + `data/moves.json` | `Scripts/Core/MoveModel.cs`, `Scripts/Core/Moves.cs`, `Data/moves.json` |
| `core/items.py` + `core/inventory_system.py` | `Scripts/Core/Items.cs`, `Scripts/Core/Inventory.cs` |
| `core/type_system.py` + `data/type_system.json` | `Scripts/Core/TypeSystem.cs`, `Scripts/Core/TypeSystemLoader.cs`, `Scripts/Core/TypeSystemQueries.cs`, `Scripts/Core/TypeSystemConfig.cs` |
| `dungeon/*` | `Scripts/Core/DungeonGenerator.cs`, `Scripts/Core/DungeonModels.cs`, `Scripts/Explore/DungeonBuilder.cs` |
| `core/overworld_ai.py` + pathing | `Scripts/Core/OverworldAI.cs`, `Scripts/Core/OverworldAIGeometry.cs`, `Scripts/Core/OverworldAIPathing.cs`, `Scripts/Explore/EnemyAgent.cs` |
| `render/pygame_app.py` | `Scripts/UI/MainMenuController.cs`, `SavesMenuController.cs`, `NameEntryController.cs`, `ExploreController.cs`, `BattleController.cs`, `RewardController.cs` |
| `render/raycast_renderer.py` | sostituito da Explore 3D nativo Godot (`Explore.tscn` + `DungeonBuilder.cs`) |
| `data/item_desc/*` | `Data/item_desc/*` |
| `data/manuals/*` | `Data/manuals/*` + `Scripts/UI/ManualOverlayController.cs` |

## Note Su Feature Parziali
- Rendering raycaster/ModernGL: **rimosso** e sostituito con blockout 3D nativo Godot.
- Battle move-replace: se slot mosse pieni, la sostituzione usa fallback automatico su slot 1.
- Build C# verificata via CLI: compilazione riuscita.
- Le scene sono create manualmente in `.tscn`; per rifinitura UI/UX usa editor Godot.

## Verifica Rapida
- Progetto composto solo da script C#.
- Dati gameplay importati in `Data/` (moves/type/manual/item_desc).
- Combat debug seed test disponibile in `Scripts/Core/CombatDebugTest.cs` (eseguito in Boot).
