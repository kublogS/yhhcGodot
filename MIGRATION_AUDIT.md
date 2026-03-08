# MIGRATION_AUDIT

Data audit: 2026-02-14

## 1) Scope
Audit di parità Python -> C# (Godot 4.6) sui moduli richiesti:
- `main.py`
- `core/game_runtime.py`, `core/game.py`
- `core/state.py`
- `core/combat.py`
- `core/save_manager.py`
- `core/input_actions.py`
- `core/moves.py` + `data/moves.json`
- `core/items.py` + `core/inventory_system.py` + `data/item_desc/*`
- `core/type_system.py` + `data/type_system.json`
- `dungeon/*`
- `render/pygame_app.py`
- `render/pygame_app_world.py`
- `render/room_enemy_spawner.py`
- `core/overworld_ai.py` + `core/overworld_pathing.py`

Target C# analizzato:
- `Scripts/Core/*`
- `Scripts/Autoload/*`
- `Scripts/Explore/*`
- `Scripts/UI/*`
- `Scenes/*`

## 2) Metodo
- Audit statico simboli/funzioni (Python vs C#).
- Verifica presenza/assenza feature per flusso e semantica.
- Nessuna esecuzione runtime automatica in questa macchina (mancano `godot` e `dotnet` nel PATH).

## 3) Executive Summary
Verdetto: **PARITA' NON 1:1**.

- Migrazione C# presente e avviabile a livello architetturale scene/autoload.
- Flusso minimo richiesto (menu -> saves -> nome -> explore -> battle -> reward -> save/load) è coperto.
- Non è una trascrizione riga-per-riga del Python: diverse aree sono semplificate o mancanti.

Stima copertura:
- Core models (moves/items/type/combat/state): **70-80%**
- Runtime mode machine (`game_runtime.update`): **45-55%**
- Save semantics completa Python: **55-65%**
- Dungeon pipeline (`graph/embed/tilemap`): **30-40%**
- Overworld AI + reinforcement logic: **40-50%**

## 4) Checklist Accettazione Richiesta

1. Main Menu -> Partite (3 slot) -> Nuova partita -> Nome -> Explore
- Stato: **PASS**
- Implementazione: `Scenes/MainMenu.tscn`, `Scenes/SavesMenu.tscn`, `Scenes/NameEntry.tscn`, `Scripts/UI/*Controller.cs`

2. Explore FPS + minimappa/overlay + incontro -> Battle
- Stato: **PASS (parziale)**
- Note: FPS e overlay presenti; trigger encounter su contatto nemico presente in `Scripts/Explore/ExploreController.cs`.

3. Battle con max 4 nemici + queue + Attacco/Difenditi/Items/Fuga
- Stato: **PASS (parziale)**
- Note: max 4 + queue e azioni principali presenti in `Scripts/Core/CombatService*.cs`, `Scripts/UI/BattleController.cs`.
- Gap: reinforcements dinamici durante battle da overworld (timer/radius) non equivalenti a `render/pygame_app_world.py::update_battle_reinforcements`.

4. Reward: soldi + drop item + acquisizione mossa
- Stato: **PASS (parziale)**
- Note: claim soldi/oggetti/mossa presenti in `Scripts/UI/RewardController.cs` + `Scripts/Core/Inventory.cs` + `Scripts/Autoload/GameSessionRewards.cs`.
- Gap: flow `reward_multi` e `move_replace` Python non replicati in modo equivalente.

5. Save/Load slot
- Stato: **PASS (parziale)**
- Note: slot 1..3 presenti in `Scripts/Autoload/SaveService.cs`.
- Gap: formato non compatibile 1:1 con schema Python (`meta/hud_preview/savestate/custom/random_state tuple-jsonable`).

## 5) Gap Critici (Priorità Alta)

### [A] State machine runtime non equivalente a Python
Python gestisce molti mode in `core/game_runtime.py::update`: `menu`, `saves`, `name`, `explore`, `battle`, `reward`, `reward_multi`, `levelup`, `settings`, `pause`, `camp`, `move_replace`, `load`.

In C# sono presenti principalmente scene/menu/battle/reward/explore.

Mancano o sono incompleti:
- `reward_multi`
- `camp`
- `move_replace` reale
- `settings` runtime equivalente
- `load` mode dedicato
- levelup flow interattivo equivalente

### [B] Save format non stabile rispetto al Python
`core/save_manager.py` salva una struttura estesa con `savestate.*`, conversioni `_state_to_jsonable/_state_from_jsonable`, e random state Python.

C# (`Scripts/Autoload/SaveService.cs`) usa un formato diverso (`SaveFileData`) e non mantiene compatibilità strutturale col payload Python.

Impatto:
- impossibile garantire load cross-version Python<->C#
- parità semantica su campi custom incompleta

### [C] Overworld reinforcement/AI behavior incompleto
Mancano equivalenti diretti di:
- `render/pygame_app_world.py::update_battle_reinforcements`
- `render/pygame_app_world.py::spawn_flee_queue_overworld`
- `render/pygame_app_world.py::try_preemptive_attack_on_scanned_enemy`
- `core/overworld_ai.py::maybe_wake_stuck_ally`

### [D] Dungeon pipeline procedurale semplificata
Python usa pipeline:
- `dungeon/graph.py::build_graph`
- `dungeon/embed.py::place_graph_on_grid`
- `dungeon/tilemap.py::stitch_tilemap`
- `dungeon/tilemap.py::to_world_map_grid`

C# usa generatore custom semplificato (`Scripts/Core/DungeonGenerator.cs`) senza grafo/embedding/template equivalenti.

## 6) Parity Matrix Per File

| Python | C# | Stato | Note |
|---|---|---|---|
| `main.py` | `Scripts/UI/BootController.cs`, `Scenes/Boot.tscn` | OK | Entrypoint equivalente |
| `core/input_actions.py` | `Scripts/Core/InputSetup.cs` + `project.godot [input]` | PARZIALE | Azioni principali presenti, non tutto il set runtime Python |
| `core/moves.py` | `Scripts/Core/Moves.cs` | OK- | Parsing/costi/base attack presenti |
| `core/items.py` | `Scripts/Core/Items.cs` | OK- | Defs/drop/descrizioni presenti |
| `core/inventory_system.py` | `Scripts/Core/Inventory.cs` | OK- | Loot/claim/use/equip presenti; UI equip non equivalente |
| `core/type_system.py` | `Scripts/Core/TypeSystem.cs`, `TypeSystemQueries.cs` | OK- | Debolezze/famiglie/modifiers presenti |
| `core/state.py` | `Scripts/Core/GameState.cs`, `Progression.cs`, `EnemyCatalog.cs` | PARZIALE | Spawn/progressione presenti; serializzazione Python `to_dict/from_dict` non 1:1 |
| `core/combat.py` | `Scripts/Core/CombatService*.cs` | OK- | roll/combat_turn/compute damage/steal chance presenti |
| `core/save_manager.py` | `Scripts/Autoload/SaveService.cs`, `SaveModels.cs` | PARZIALE | Slot e atomic write sì; schema/semantica non identici |
| `dungeon/*` | `Scripts/Core/DungeonGenerator.cs`, `DungeonModels.cs`, `Scripts/Explore/DungeonBuilder.cs` | PARZIALE | Generazione giocabile sì, algoritmo diverso |
| `core/overworld_ai.py` + `core/overworld_pathing.py` | `Scripts/Core/OverworldAI.cs`, `Scripts/Explore/EnemyAgent.cs` | PARZIALE | BFS/LOS/chase sì; wake ally e alcune transizioni no |
| `render/pygame_app_world.py` | `Scripts/Explore/ExploreController.cs`, `Scripts/Autoload/GameSession.cs` | PARZIALE | Contact encounter sì; reinforcements/preemptive/flee queue no |
| `core/game_runtime.py` + `core/game.py` | `Scripts/Autoload/GameSession*.cs`, `Scripts/UI/*Controller.cs` | PARZIALE | Flusso base sì; modes avanzati mancanti |
| `render/pygame_app.py` | `Scenes/*` + controller UI | PARZIALE | Loop/overlay principali sì; molte feature non portate 1:1 |

## 7) Funzioni Python Senza Equivalente Diretto (estratto)

- `core/game_runtime.py::handle_reward_multi_choice`
- `core/game_runtime.py::_finish_reward_multi`
- `core/game_runtime.py::handle_camp_choice`
- `core/game_runtime.py::apply_move_replacement` (flow completo)
- `core/game_runtime.py::toggle_setting`
- `core/game_runtime.py::display_label`
- `core/game_runtime.py::set_custom_resolution`
- `render/pygame_app_world.py::update_battle_reinforcements`
- `render/pygame_app_world.py::spawn_flee_queue_overworld`
- `render/pygame_app_world.py::try_preemptive_attack_on_scanned_enemy`
- `core/overworld_ai.py::maybe_wake_stuck_ally`
- pipeline dungeon `build_graph/place_graph_on_grid/stitch_tilemap/to_world_map_grid` (equivalenza algoritmo)

## 8) Rischi Regressione
- Bilanciamento combattimento: medio
- Compatibilità save legacy Python: alto
- Progressione run (camp/reward_multi/move_replace): alto
- AI overworld in scenari complessi: medio-alto

## 9) Piano di Chiusura Gap (ordine consigliato)
1. Portare mode machine completa (`reward_multi`, `camp`, `levelup`, `move_replace`, `settings`, `load`).
2. Rendere SaveService compatibile allo schema Python (o definire migration tool esplicito).
3. Portare reinforcements dinamici/preemptive/flee queue/wake ally.
4. Portare pipeline dungeon graph/embed/tilemap o implementare equivalenza comportamentale testata con seed.
5. Aggiungere test characterization C# (combat/save/runtime flow) con seed fissi.

## 10) Conclusione
La migrazione corrente è una **base funzionante** ma **non equivalente riga-per-riga** al Python.
Per arrivare a parità alta servono i fix del punto 9.
