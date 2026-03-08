# Asset Migration Map (Python -> Godot)

## Runtime Assets

| Python asset | Python usage/context | Godot destination | Godot integration/context |
|---|---|---|---|
| `assets/icon.png` | Finestra app via `render/pygame_app_display.py::set_game_icon()` | `Assets/icons/icon.png` | `project.godot` -> `config/icon="res://Assets/icons/icon.png"` |
| `assets/startmenuwallpaper.png` | Sfondo menu iniziale via `render/pygame_app.py::load_start_menu_background()` | `Assets/ui/startmenuwallpaper.png` | `Scripts/UI/MainMenuController.cs` carica `GameAssets.LoadMainMenuWallpaper()` su `Scenes/MainMenu.tscn` |
| `assets/backgrounds/room.pdf` | Sfondo battaglia via `core/game.py::current_battle_bg` + `render/pygame_app.py::_draw_vector_room()` | `Assets/backgrounds/room.pdf` (sorgente) | Mantenuto come sorgente; conversione runtime-friendly in `Assets/backgrounds/room.png` |
| `assets/backgrounds/room.pdf` -> `room.png` | Stesso contesto battle, ma raster in Godot | `Assets/backgrounds/room.png` | `Scripts/UI/BattleController.cs` carica `GameAssets.LoadBattleBackground()` su `Scenes/Battle.tscn` |
| `assets/images/player.png` | Sprite player caricato da `core/game.py` (`AsciiSprite.preload_png_dir`) e usato in battle render | `Assets/characters/player/player.png` | `Scripts/UI/BattleController.cs` portrait player via `GameAssets.LoadCharacterSprite(..., enemy:false)` |
| `assets/enemy/goblin.png` | Sprite nemico `goblin` (`core/enemies.py` + preload PNG) | `Assets/characters/enemy/goblin.png` | Caricato da `GameAssets.LoadEnemySprite("goblin")` in Explore/Battle |
| `assets/enemy/orco.png` | Sprite nemico `orco` | `Assets/characters/enemy/orco.png` | Caricato da `GameAssets.LoadEnemySprite("orco")` in Explore/Battle |
| `assets/enemy/scheletro.png` | Sprite nemico `scheletro` | `Assets/characters/enemy/scheletro.png` | Caricato da `GameAssets.LoadEnemySprite("scheletro")` in Explore/Battle |
| `assets/enemy/slime.png` | Sprite nemico `slime` | `Assets/characters/enemy/slime.png` | Caricato da `GameAssets.LoadEnemySprite("slime")` in Explore/Battle |
| `assets/enemy/bandito.png` | Sprite nemico `bandito` | `Assets/characters/enemy/bandito.png` | Caricato da `GameAssets.LoadEnemySprite("bandito")` in Explore/Battle |
| `assets/enemy/stregone.png` | Sprite nemico `stregone` | `Assets/characters/enemy/stregone.png` | Caricato da `GameAssets.LoadEnemySprite("stregone")` in Explore/Battle |
| `assets/enemy/enemy.png` | Fallback sprite generico (preload PNG dir) | `Assets/characters/enemy/enemy.png` | Fallback in `GameAssets.EnemyFallbackSpritePath` |
| `assets/enemy/linussss.png` | Asset pre-caricato per nome file (non referenziato dal roster base) | `Assets/characters/enemy/linussss.png` | Registrato in `GameAssets` e disponibile via lookup dinamico sprite id |

## Source/Authoring Assets

| Python asset | Python usage/context | Godot destination | Godot integration/context |
|---|---|---|---|
| `assets/enemy/enemy.psd.zip` | Archivio sorgente grafico (non usato runtime) | `Assets/source/enemy.psd.zip` | Conservato come source art, non collegato al runtime |

## Code hooks added in Godot

- `Scripts/Core/GameAssets.cs`: catalogo centralizzato path/fallback degli asset migrati.
- `Scripts/Core/OverworldEnemy.cs`: aggiunto campo `Sprite` persistente nel modello nemico overworld.
- `Scripts/Autoload/GameSession.cs`: le spawn overworld ora includono `Sprite = spec.Sprite`.
- `Scripts/Explore/EnemyAgent.cs`: rendering nemici con `Sprite3D` billboard basato su asset migrati.
- `Scripts/UI/MainMenuController.cs` + `Scenes/MainMenu.tscn`: wallpaper menu applicato.
- `Scripts/UI/BattleController.cs` + `Scenes/Battle.tscn`: background battle + portrait player/nemico applicati.
