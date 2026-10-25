# Proposal

## Why

Частицы в VR дорого стоят на GPU: крупные прозрачные спрайты перерисовывают экран слоями (overdraw), модуль Lights
добавляет источники реального времени, коллизии с миром считаются на CPU каждый кадр, тени частиц — лишние проходы.
Аудитор частицы сейчас не проверяет. Владелец, 25.10.2026: «делай частицы и шейдер и reflection probe».

## What Changes

- Новая категория проверок «Частицы» (`Particles`) после «Освещения».
- Проверки включённых систем частиц: `PART_MAX_PARTICLES` (W / C), `PART_TOTAL` (W, на сцену), `PART_LARGE_TRANSPARENT`
  (W), `PART_LIGHTS` (W, исправление), `PART_COLLISION_WORLD` (W, исправление), `PART_SHADOWS` (W, исправление),
  `PART_MESH_NO_INSTANCING` (I, исправление), `PART_NOISE_HIGH` (I).
- Частиц одновременно — оценка: Max Particles, но не больше Rate over Time × Start Lifetime (у незацикленной — не
  дольше Duration) + Bursts; у систем с Rate over Distance, выключенной Emission и у суб-эмиттеров — Max Particles.
- Пороги — константы `VRBudgets`.
- Этап плана: версия 1.6.0.

## Capabilities

### Modified Capabilities
- `scene-audit`: проверки частиц.

## Impact

- Код: `Checks/ParticleChecks.cs` (новый), `Strings.Particles.cs` (новый), `AuditIssue.cs` (категория),
  `SceneAuditor.cs` (шаг), `VRBudgets.cs` (пороги), `Strings.Ui.cs` (имя категории), `Strings.cs`.
- Исправления — по строке после просмотра, Undo; с копиями префаба работают «Копии вместе» и «Исправить в префабе».
