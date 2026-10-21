# unity-tools — документация

Версия программы: 0.5.0

Это документация того, что есть сейчас. Правила для сессий — [CLAUDE.md](../CLAUDE.md); замысел и
этапы — [SPEC.md](../SPEC.md); что осталось проверить — [TESTPLAN.md](../TESTPLAN.md).

## Что это и что уже работает

Репозиторий утилит для Unity (основная цель — Unity 2019.4), которые владелец подключает в свои
проекты. Каждая утилита — своя папка в корне репозитория; в проект она копируется в `Assets/`.

| Утилита | Что делает | Состояние |
|---|---|---|
| [SceneOptimizationAuditor](scene-optimization-auditor.md) | аудит загруженных сцен под PC VR (HTC Vive Pro, 90 Гц): проверки, бюджеты, отчёт, исправления, снимок профайлера | этапы 1–4 готовы: автотесты в Unity 2019.4 зелёные, живая проверка владельца — 22.10.2026 |

Этапы 1–4 из `SPEC.md` закрыты 22.10.2026 (автотесты зелёные, живую проверку владелец подтвердил) —
версия 0.5.0. Выпуска и тега нет.

## Оглавление

- [SceneOptimizationAuditor](scene-optimization-auditor.md) — подключение, окно, проверки, бюджеты,
  отчёт, исправления, снимок профайлера, устройство.
- [Гайдлайн интерфейса](ui-guideline.md) — вид редакторских окон утилит.

## Скрипты репозитория

- `scripts\compile-check.ps1 -Source <Утилита>\Editor [-UnityVersion 2019.4.41f2] [-UnityRoot …] [-LangVersion 7.3] [-Log …] [-Out …]` — компиляция исходников
  компилятором Roslyn из установленного Unity с его DLL и `-langversion:7.3`, без запуска Unity. Ловит
  возможности C# новее 7.3 и API новее версии Unity. Журнал — `unity-tools-scratch\reports\compile-check.log`;
  код выхода 0 — без ошибок, 1 — ошибки, 2 — не найден компилятор или исходники.
- `scripts\run-audit-tests.ps1 [-Only base,quality] [-UnityVersion …] [-Project …] [-Reports …]` (имена шагов — в начале
  скрипта) — автотесты SceneOptimizationAuditor (`tests/AuditTests/`) в
  проверочном проекте `unity-tools-scratch\TestProject2019` (2019.4.41f2): по запуску Unity на шаг, итог —
  таблица кодов выхода, отчёты — `unity-tools-scratch\reports\audit-tests\<шаг>.txt`; код выхода 0 — все шаги
  прошли.
- `scripts\shoot-audit-window.ps1 [-Only problems-ru-dark]` — снимки окна SceneOptimizationAuditor в проверочном
  проекте (`-Only`: `empty-ru-dark`, `problems-ru-dark`, `problems-en-dark`, `budgets-ru-dark`, `profiler-en-dark`;
  ещё `-UnityVersion`, `-Project`, `-Out`): пустое состояние, «Проблемы» RU/EN, «Бюджеты», «Профайлер» в тёмном скине (светлый в 2019.4 из кода
  не переключить). Unity открывается с окном на время снимка.
  Снимки — `unity-tools-scratch\reports\shots\`.
- `scripts\window-shot.ps1 -Title <часть заголовка> -Out <файл.png>` — снимок одного окна Windows через
  `PrintWindow`: работает, даже если окно перекрыто, и не захватывает ничего другого с экрана; код выхода 0 — сохранено, 1 — окно не найдено или снимок не удался.
