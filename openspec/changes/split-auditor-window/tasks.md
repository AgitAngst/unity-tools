# Tasks

## 1. До разреза

- [ ] 1.1 Снять снимки окна `scripts\shoot-audit-window.ps1` в `unity-tools-scratch\reports\shots-before\`; проверка — папка со снимками всех вкладок RU/EN

## 2. Окно

- [ ] 2.1 Разнести `AuditorWindow` по `partial`-файлам по таблице `design.md`, `IssueGroup` — в свой файл; проверка — каждый файл до ~600 строк, `scripts\compile-check.ps1 -Source SceneOptimizationAuditor\Editor` без ошибок и предупреждений
- [ ] 2.2 Разнести `Strings` по разделам (`Strings.cs`, `Strings.Checks.cs`, `Strings.Tabs.cs`); проверка — `compile-check` чистый, число ключей таблицы до и после совпадает (тест или строка в журнале прогона тестов)
- [ ] 2.3 Импорт в проверочном проекте создаёт `.meta` новых файлов; проверка — `.meta` лежат рядом в репозитории, консоль Unity без ошибок

## 3. Проверка и документация

- [ ] 3.1 `scripts\run-audit-tests.ps1` — 25 из 25; снимки после разреза попиксельно совпадают с 1.1; различия — только строка версии
- [ ] 3.2 `docs/scene-optimization-auditor.md` (раздел «Устройство»), `docs/ui-guideline.md` (колонка «Где»), HANDOFF — новые файлы; проверка — названные файлы есть в репозитории
- [ ] 3.3 Версия 1.1.1 (`scripts\stamp-version.ps1 -Version 1.1.1`); проверка — `ToolVersion.Version` и тулбар окна
