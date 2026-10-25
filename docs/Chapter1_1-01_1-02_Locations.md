# Lone Truth — локации 1-01 «Станция» и 1-02 «Главная улица»

Сцены собраны и проверены в Play-режиме: переход между ними работает, все 7 заглушек отвечают «закрыто» и ничего не загружают. Графика пока временная — плоские силуэты, нарисованные билдером. Каждый файл лежит по своему финальному пути: если положить поверх него готовый PNG с тем же именем, ссылки в сценах сохранятся.

Источники: дизайн-документ (правила, сюжет), «Персонажи и локации» (внешность, анимации), «Карты и квесты» (размеры, X-координаты, переходы).

## 1. Что сделано в проекте

| Что | Где |
| --- | --- |
| Сцена 1-01, 30 м, начало игры: ночь, дождь, пролог | `Assets/Scenes/Chapter1/Station.unity` |
| Сцена 1-02, 60 м, ночь прибытия | `Assets/Scenes/Chapter1/MainStreet.unity` |
| Обе сцены добавлены в Build Settings | `ProjectSettings/EditorBuildSettings.asset` |
| Билдер сцен (перезапускаемый) | `Assets/Editor/LoneTruth/Chapter1LocationBuilder.cs` |
| Рисовальщик временных спрайтов | `Assets/Editor/LoneTruth/PlaceholderPainter.cs` |
| Заглушка перехода (новый компонент) | `Assets/Scripts/Interaction/TransitionStub.cs` |
| Переход между сценами + точка появления | `Assets/Scripts/Interaction/SceneTransitionDoor.cs`, `SpawnPoint.cs` |
| Префабы | `Assets/Prefabs/Chapter1/` — `P_Player`, `P_MainCamera`, `P_GameMaster`, `P_ScreenFader`, `P_NPC`, `P_InspectPoint`, `P_Lamppost`, `P_TransitionStub`, `P_SceneDoor`, `P_Prop`, `P_FX_Rain` |
| Анимации и контроллеры | `Assets/Art/Chapter1/Animation/` |
| Пост-обработка (виньетка, зерно, приглушённый цвет) | `Assets/Art/Chapter1/PP_Chapter1_NightRain.asset` |

**Пересборка.** Меню `Lone Truth → Build → Chapter 1 — 1-01 Station + 1-02 Main Street` или из терминала:

```bash
unity command eval --timeout 300 --code "return LoneTruth.EditorTools.Chapter1LocationBuilder.Build();"
```

Билдер пересоздаёт сцены, префабы, анимации и материалы. Существующие PNG он не трогает, поэтому готовую графику не перезапишет.

**Использованы существующие системы:** `PlayerController`, `CameraFollow`, `InteractionDetector` + `IInteractable`, `InteractableNPC` (реплики NPC и точки осмотра), `ScreenFader`, `GameMaster`, `InventoryManager`, `WeaponController`, `ItemSO`. Новые компоненты появились только там, где их не было: заглушка перехода (`InteractableDoor` телепортирует внутри сцены и не умеет «закрыто без ключа») и переход между двумя сценами.

### Устройство сцены

- 1 юнит = 1 м. X — метры от левого края комнаты, как на карте. Пол на y = 0, линия героя на z = 0, фасады на z ≈ 3, силуэты переднего плана на z ≈ −3.
- Пол — невидимый `BoxCollider` плюс видимый quad с материалом. Стандартных кубов в кадре нет.
- Края комнаты закрыты невидимыми стенами `Wall_Left_x0` и `Wall_Right_x30` / `x60`.
- Камера перспективная, FOV 35°, наклон вниз 6°, следует только по X. Границы: станция 6–24 м, улица 6–54 м.
- Свет: лунный направленный (холодный, 0,25) и тёплые точечные источники — фонари, окно телеграфа, окна салуна. Плюс экспоненциальный туман, дождь частицами и Volume.
- Порядок прорисовки: дождь — `sortingOrder 10`, силуэты переднего плана — `20`. Так фасады больше не перекрывают дождь.

### 1-01 «Станция» — что где стоит

| X, м | Объект | Реализация |
| --- | --- | --- |
| 2 | Старт игрока | `Player` |
| 3 | Пролог: перечитать письмо Агнес | `Event_Prologue_Letter_x3` (`P_InspectPoint`, реплика из документа) |
| 5 | Табличка станции | спрайт, фон |
| 6 | Проводник: «Дальше не поеду.» | `P_NPC` |
| 13 | Фонарь у телеграфа | `P_Lamppost` + точечный свет |
| 16 | Будка телеграфа, стопка неотправленных телеграмм | `Clue_UnsentTelegrams_x16` + спрайт в окне + свет окна |
| 8–22 | Навес | спрайт, фон |
| 24 | Кэллахан | `P_NPC` |
| 27,5 | Точка появления при возврате с улицы | `Spawn_from_1-02` |
| 29 | **→ 1-02** (рабочий переход) | `Transition_1-01_to_1-02` (`P_SceneDoor`) |
| фон | Уходящий поезд, пути, горы, небо | спрайты и quad |
| передний план | Фонарный столб (x 4,5), штабель ящиков (x 26,5) | силуэты |

### 1-02 «Главная улица» — что где стоит

| X, м | Объект | Реализация |
| --- | --- | --- |
| 2 | **← 1-01** (рабочий переход) | `Transition_1-02_to_1-01` |
| 3,5 | Точка появления со станции | `Spawn_from_1-01` |
| 6, 31, 48 | Фонари | `P_Lamppost` |
| 6–14 | Фасад салуна, дверь **1-03 — заглушка** на x 10 | `Transition_1-02_to_1-03` |
| 16–24 | Контора компании, дверь **1-05 — заглушка** на x 20 | `Transition_1-02_to_1-05` |
| 23 | Доска объявлений компании | `P_Prop` |
| 25–31 | Контора шерифа, дверь **1-08 — заглушка** на x 28 | `Transition_1-02_to_1-08` |
| 33–39 | Вход в переулок, калитка **1-14 — заглушка** на x 36 | `Transition_1-02_to_1-14` |
| 41 | Бочки (на карте — укрытие) | `P_Prop`; системы укрытий пока нет |
| 40–49 | Забор лесопилки, ворота **1-18 — заглушка** на x 45 | `Transition_1-02_to_1-18` |
| 49–56 | Часовня, дверь **1-09 — заглушка** на x 52 | `Transition_1-02_to_1-09` |
| 56–60 | Дорога к шахте, ворота **1-21 — заглушка** на x 58 | `Transition_1-02_to_1-21` |
| 13, 15 | Прохожие (на карте — днём) | группа `State_Day`, выключена |
| 38 | Лиам (на карте — вечер дня 2) | группа `State_Day2Evening`, выключена |
| фон | Копёр шахты вдали, горы, небо | спрайты |
| передний план | Коновязь (x 4), столбы (x 18, 50), бочки (x 34) | силуэты |

Каждая заглушка хранит `transitionId` и `targetLocationId`, стоит на своей X-координате и состоит из трёх частей: спрайт закрытой двери на фасаде, триггер взаимодействия и твёрдый `Blocker` в проёме. Подсказка показывает «<название> — закрыто», после нажатия E — короткую реплику («Ворота на цепи.» и т. п.). Чтобы подключить будущую локацию, заглушку меняют на `P_SceneDoor` с тем же id.

## 2. Полный список графики для 1-01 и 1-02

Размеры указаны при 100 px на метр — так спрайты импортируются сейчас. Лучше сдавать в 2× (200 px/м) и поставить в импорте `Pixels Per Unit = 200`. Опорная точка спрайтов — низ по центру: ступни и основания стоят на нижнем крае картинки. Свет везде сверху слева.

### 2.1 Персонажи

Набор анимаций определён по тому, что реально происходит в 1-01 и 1-02.

| Файл | Кадров | Размер, px (1×) | Где используется |
| --- | --- | --- | --- |
| `CH_Holt_Ref` | 1 | 90 × 193 | эталон, иконка |
| `CH_Holt_Idle_00…03` | 4 (4 fps, цикл) | 90 × 193 | стоит |
| `CH_Holt_Walk_00…07` | 8 (10 fps, цикл) | 90 × 193 | ходьба |
| `CH_Holt_Interact_00…03` | 4 (10 fps) | 90 × 193 | дверь, осмотр телеграмм |
| `CH_Holt_ReadLetter_00…05` | 6 (3 fps) | 90 × 193 | пролог: письмо Агнес |
| `CH_Conductor_Ref`, `_Idle_00…01` | 1 + 2 | 90 × 183 | пролог на перроне |
| `CH_Callahan_Ref`, `_Idle_00…01` | 1 + 2 | 90 × 198 | встреча на перроне |
| `CH_PasserbyMiner_Ref`, `_Idle_00…01` | 1 + 2 | 90 × 186 | улица днём (сейчас выключены) |
| `CH_PasserbyWoman_Ref`, `_Idle_00…01` | 1 + 2 | 90 × 173 | улица днём (сейчас выключены) |
| `CH_Liam_Ref` | 1 | 90 × 143 | вечер дня 2 (сейчас выключен); бег — позже |

Перекат, выстрел, перезарядка и урон на этих двух локациях не нужны: врагов здесь нет. Их стоит делать вместе с локациями, где Холт впервые дерётся.

### 2.2 Окружение «Станция»

| Файл | Размер, м | Что это |
| --- | --- | --- |
| `ENV_Station_Train` | 13 × 4,2 | уходящий пассажирский состав, горящие окна |
| `ENV_Station_Canopy` | 14,4 × 4,6 | навес на чугунных столбах (x 8–22) |
| `ENV_Station_TelegraphBooth` | 4 × 3,3 | будка телеграфа с жёлтым окном |
| `ENV_Station_StairsDown` | 4,5 × 2,6 | спуск к улице (рабочий переход) |
| `PRP_Station_NameSign` | 3 × 2,2 | табличка станции «Холлоу-Рок» |
| `PRP_TelegramStack` | 0,5 × 0,25 | стопка телеграмм в окне (улика) |
| `TEX_Station_Trackbed` | тайл 256 | пути: шпалы, рельсы, гравий |

### 2.3 Окружение «Главная улица»

| Файл | Размер, м | Что это |
| --- | --- | --- |
| `FAC_Saloon` | 8 × 7 | салун «Последняя лампа», балкон, тёплые окна |
| `FAC_CompanyOffice` | 8 × 8 | каменная контора «Уитлок Майнинг» — самое богатое здание |
| `FAC_SheriffOffice` | 6 × 6 | контора шерифа |
| `FAC_AlleyMouth` | 6 × 7 | тёмный вход в переулок между стенами, бельё |
| `FAC_SawmillFence` | 9 × 3,5 | дощатый забор лесопилки |
| `FAC_Chapel` | 7 × 9,5 | деревянная часовня Святой Варвары, колокольня |
| `FAC_MineRoadFence` | 4 × 3 | ограда у дороги к шахте |
| `ENV_Headframe_Distant` | 6 × 13 | копёр шахты вдали, силуэт |
| `ENV_Street_StationSteps` | 4,5 × 2,6 | ступени наверх к станции (рабочий переход) |
| `TEX_Street_Mud` | тайл 256 | грязная дорога с лужами |
| `DOOR_Saloon_Closed` | 1,2 × 2,3 | дверь салуна (заглушка 1-03) |
| `DOOR_CompanyOffice_Closed` | 1,3 × 2,5 | дверь конторы (заглушка 1-05) |
| `DOOR_Sheriff_Closed` | 1,1 × 2,3 | дверь шерифа (заглушка 1-08) |
| `DOOR_AlleyGate_Closed` | 2,2 × 2,2 | калитка переулка (заглушка 1-14) |
| `DOOR_SawmillGate_Closed` | 3,2 × 3 | ворота лесопилки на цепи (заглушка 1-18) |
| `DOOR_Chapel_Closed` | 1,4 × 2,8 | арочная дверь часовни (заглушка 1-09) |
| `DOOR_MineGate_Closed` | 3 × 2,8 | ворота к шахте на замке (заглушка 1-21) |
| `PRP_NoticeBoard` | 1,4 × 2 | доска объявлений компании |

### 2.4 Общее

| Файл | Размер | Что это |
| --- | --- | --- |
| `ENV_Sky_NightRain` | тайл 256 | ночное небо в тучах |
| `ENV_Mountains_Fog` | 20 × 6 м | горы в тумане, силуэт |
| `TEX_Planks_Wet` | тайл 256 | мокрые доски: перрон, тротуар |
| `PRP_Lamppost` | 0,7 × 4,3 м | уличный фонарь (повторяется) |
| `PRP_CrateStack` | 2,2 × 1,7 м | штабель ящиков |
| `PRP_Barrels` | 1,7 × 1,3 м | бочки |
| `PRP_HitchingPost` | 2,2 × 1,1 м | коновязь |
| `PRP_TelegraphPole` | 1,4 × 8 м | телеграфный столб |
| `FX_RainStreak` | 8 × 64 px | капля-штрих для частиц дождя |

### 2.5 3D-модели

Отдельные 3D-модели для этих двух локаций не нужны. 3D здесь — только плоскости пола, путей и неба (Unity quad) с тайловыми текстурами из списка выше. Если позже захочется настоящего объёма (перрон с торцом, ступени, навес), их делают low-poly мешами под те же размеры.

## 3. Промпты для генерации графики

### 3.1 Общий стилевой префикс

Добавлять в начало каждого промпта:

> 2D hand-painted ink illustration for a side-view 2.5D noir adventure game, 1936 Appalachian coal-mining town, night, rain. Bold ink linework, expressive silhouettes, hard contrast shadows, muted desaturated palette of blue-grey, charcoal and sepia brown; bright colour only in warm lamplight. Aged, weathered surfaces, industrial details. Flat orthographic front elevation, camera at eye level, light from upper left. Isolated on a transparent background, no ground shadow, no text, no logos, no watermark.

Для персонажей дописывать:

> Full-body character sprite, side view turned three-quarters toward the right, feet exactly on the bottom edge, same scale and lighting as the reference sheet, consistent colours across every frame.

### 3.2 Персонажи

| Ассет | Промпт (после префикса) |
| --- | --- |
| `CH_Holt_Ref` | Sam Holt, 38-year-old private detective, tall (185 cm), thin, slightly stooped. Charcoal-grey trench coat to the knees with raised collar and belt (#2E3033), wet-asphalt fedora with dark band (#3A3431), dark three-piece suit, loosened tie, muted oxblood scarf (#5E2A2C), worn oxfords. Stubble, tired eyes in the shadow of the brim, stubborn jaw. Shoulder holster with a .38 revolver under the coat, pocket-watch chain, a lit cigarette. Standing neutral pose. |
| `CH_Holt_Idle_00…03` | Same character as the reference, idle loop of 4 frames: slow breathing, on frame 2 he draws on the cigarette, the ember brightens. Sprite sheet in one row, identical scale and foot position. |
| `CH_Holt_Walk_00…07` | Same character, walk cycle of 8 frames, heavy unhurried noir gait, coat hem swinging, hands in coat pockets. Sprite sheet in one row, feet on the same baseline. |
| `CH_Holt_Interact_00…03` | Same character, interaction of 4 frames: reaches forward at chest height to try a door handle or pick up papers, then returns to neutral. |
| `CH_Holt_ReadLetter_00…05` | Same character, 6 frames: takes a folded letter from the inner coat pocket, unfolds it at chest height, reads with head lowered, folds and puts it back. The letter is pale paper (#D8CFBE), the only light-coloured element. |
| `CH_Conductor_Ref` + `_Idle_00…01` | Railway conductor of the 1930s, about 50, stocky. Dark navy uniform jacket with brass buttons (#2B3240, #9C8650), peaked conductor's cap, pocket watch, lantern in his hand. Idle: 2 frames, shifts his weight, impatient. **Внешность проводника не описана в документах — нужно утвердить.** |
| `CH_Callahan_Ref` + `_Idle_00…01` | Duke Callahan, 40, company security chief. A mirror of the detective's silhouette but broader and heavier: long clay-coloured duster (#6E5B45), leather vest, wide-brimmed flat-crown hat (#3B2F27), boots, Colt .45 on the hip (#8A8F95), scar across the upper lip, toothpick. Idle: 2 frames, chewing the toothpick, hand near the holster. |
| `CH_PasserbyMiner_Ref` + idle | Off-shift coal miner, about 40, flat cap, canvas jacket (#4A4642) and soot-stained trousers, face streaked with coal dust, lunch tin. Idle: 2 frames. |
| `CH_PasserbyWoman_Ref` + idle | Miner's wife, about 35, faded dark coat over a print dress (#5B5450), woollen shawl (#7A5E48), basket. Idle: 2 frames. |
| `CH_Liam_Ref` | Liam, 10-year-old newspaper boy: oversized eight-panel cap, a jacket too big for him (#5C5F63), short trousers, huge boots, canvas bag of newspapers (#8E7A5A, #D6CDB9). Mid-stride, always running. |

### 3.3 Окружение

| Ассет | Промпт (после префикса) |
| --- | --- |
| `ENV_Station_Train` | Long 1930s steam passenger car and tender seen side-on, dark wet steel, rivets, row of warm amber lit windows, steam drifting from under the wheels, the train just starting to pull away to the left. Wide 3:1 image. |
| `ENV_Station_Canopy` | Wooden railway platform canopy on slim cast-iron columns spaced every 2.3 m, corrugated roof with dripping eaves, 14 m long, side elevation, no platform floor. |
| `ENV_Station_TelegraphBooth` | Small clapboard telegraph booth on a rural platform, pitched tin roof, one warm yellow window with a counter and a stack of telegrams behind the glass, narrow door on the right, peeling paint. |
| `ENV_Station_StairsDown` | Worn stone steps with an iron handrail leading down from a railway platform to a muddy street, side elevation, wet stone. |
| `PRP_Station_NameSign` | Weathered wooden station name board on two posts, blank sign plate for text overlay. |
| `PRP_TelegramStack` | Small stack of unsent Western-Union-style telegram forms tied with string, pale paper, close-up prop. |
| `FAC_Saloon` | Two-storey false-front wooden saloon "The Last Lamp" with a balcony railing, three upper windows (two warmly lit), swinging-door entrance recess in the centre, sign band above the porch left blank, 8 m wide. |
| `FAC_CompanyOffice` | The only stone building on the street: solid grey ashlar company office of Whitlock Mining, tall windows with green lamplight, heavy recessed entrance in the centre, brass plaque area left blank, 8 m wide, looks rich and cold. |
| `FAC_SheriffOffice` | Small single-storey wooden sheriff's office, barred window, wanted-poster board by the door, sagging porch roof, 6 m wide. |
| `FAC_AlleyMouth` | Narrow dark alley between two back walls, washing lines across, rubbish and puddles, deep black passage in the middle, 6 m wide. |
| `FAC_SawmillFence` | Long rough plank fence of a company sawmill with stacked logs visible behind, 9 m wide, 3.5 m tall, with a gap for a gate. |
| `FAC_Chapel` | Wooden miners' chapel of Saint Barbara with a small belfry and cross, dark stained-glass rose window, arched entrance in the centre, 7 m wide, 9.5 m tall. |
| `FAC_MineRoadFence` | Barbed-wire and plank fence at the end of the street where the road climbs to the mine, 4 m wide. |
| `ENV_Headframe_Distant` | Distant steel mine headframe with a sheave wheel, black silhouette against fog, faint work lamps. |
| `ENV_Street_StationSteps` | Stone steps going up from the street to the railway platform, iron handrail, side elevation. |
| `DOOR_*_Closed` (7 шт.) | Closed door matching its facade: [saloon double plank door / heavy oak office door with frosted glass / plain sheriff's door / wooden alley gate / sawmill double gate with chain and padlock / arched chapel door / mine-road gate with chain and padlock], visibly locked, same scale and light as the facade. |
| `PRP_NoticeBoard` | Company notice board on two posts covered with wet pinned notices, unreadable text. |
| `PRP_Lamppost` | Cast-iron 1930s street lamp, 4.3 m tall, glowing warm glass lantern head. |
| `PRP_CrateStack`, `PRP_Barrels`, `PRP_HitchingPost`, `PRP_TelegraphPole` | [Stack of wooden freight crates / pair of wet oak barrels / wooden hitching rail / tall telegraph pole with crossbars]. Will be used both lit and as near-black foreground silhouettes. |
| `ENV_Mountains_Fog` | Layered Appalachian ridges in fog at night, wide seamless panorama strip, very low contrast. |
| `ENV_Sky_NightRain`, `TEX_*` | Seamless tileable texture: [night sky with heavy rain clouds / wet weathered planks / muddy street with puddles / railway trackbed with sleepers and rails], top-down for the floors, 1024 px, no visible seams. |

### 3.4 Промпт для сборки локации из готовых ассетов

Использовать, когда финальная графика будет лежать в проекте (через Unity Pipeline: `unity command …`):

> Работаем в проекте LoneTruthPrototype через Unity Pipeline (`unity status` → ready). Финальная графика для локаций 1-01 и 1-02 лежит в `Assets/Art/Chapter1/` и заменяет временные PNG **с теми же именами файлов** — GUID и ссылки сохраняются, переименовывать нельзя.
> 1. Проверь импорт новых PNG: Sprite (Single), опорная точка Bottom Center, Pixels Per Unit = ширина файла в px ÷ ширина ассета в метрах из `Docs/Chapter1_1-01_1-02_Locations.md`. Файлы `TEX_*` и `ENV_Sky_*` импортируй как Default с Wrap = Repeat.
> 2. Запусти `LoneTruth.EditorTools.Chapter1LocationBuilder.Build()` — он пересоберёт сцены `Station` и `MainStreet` и анимации, существующие PNG не тронет.
> 3. Если у персонажа поменялось число кадров, обнови вызовы `SpriteClip(...)` в билдере и пересобери.
> 4. Сними `capture game_view` в Play-режиме на x = 3, 16, 27 (станция) и x = 4, 20, 36, 52 (улица). Проверь, что ступни стоят на линии пола, фасады не перекрывают героя и дождь, двери-заглушки совпадают с проёмами фасадов. Сдвигай только спрайты и не меняй X-координаты из карты.
> 5. Проверь переходы: станция x 29 → улица (появление x 3,5), улица x 2 → станция (появление x 27,5). Все 7 заглушек должны отвечать «закрыто» и ничего не загружать.
> 6. Ничего не создавай поверх карты: новые объекты, NPC и события — только по документам.

## 4. Что нужно предоставить позже

У меня нет инструмента генерации изображений: модуль Unity с генерацией сейчас не подключён. Поэтому вся графика выше — временные силуэты. От художника или генератора нужны:

1. **Все PNG из раздела 2** — по промптам раздела 3, с теми же именами.
2. **Утверждение внешности проводника.** Его нет в «Персонажах и локациях», палитра и вид предложены мной.
3. **Тексты табличек:** название станции, вывески салуна и конторы, табличка у ворот шахты. Сейчас пустые плашки.
4. **Звук:** дождь по навесу и по улице, гудок уходящего поезда (упомянуты в документе), шаги по доскам и по грязи, звук запертой двери. Для него в `TransitionStub` есть поле `lockedSound`.
5. **Реплика Кэллахана.** В документе нет прямой речи, сейчас стоит временная: «Человек компании? Я провожу до салуна.» Реплика проводника и текст письма взяты из документа.

## 5. Что осталось вне задачи

- **Конфликт клавиши E.** `InteractionDetector` и `InventoryManager` оба реагируют на E: взаимодействие одновременно переключает слот. Это было в MVP, не исправлял.
- **Нет интерфейса инвентаря.** В новых сценах его нет: в MVP он собран вручную на Canvas и не оформлен префабом.
- **Анимации Interact и ReadLetter никто не запускает.** Триггеры есть в `AC_Holt`, но код их не вызывает. Нужен вызов `animator.SetTrigger("Interact")` из `InteractionDetector` или из событий письма.
- **Систем укрытий и флагов сюжета пока нет.** Бочки на x 41 — только декор. Группы `State_Day` и `State_Day2Evening` выключены вручную; когда появятся `StoryFlags`, их будет включать флаг.
- **Спрайты не освещаются.** Они рисуются стандартным unlit-материалом, поэтому фонари освещают только пол. Для настоящего нуарного света нужен lit-шейдер для спрайтов (Shader Graph).
