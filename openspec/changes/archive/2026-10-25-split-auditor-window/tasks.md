# Tasks

## 1. До разреза

- [x] 1.1 Снять снимки окна `scripts\shoot-audit-window.ps1` в `unity-tools-scratch\reports\shots-before\`; проверка — папка со снимками всех вкладок RU/EN

## 2. Окно

- [x] 2.1 Разнести `AuditorWindow` по `partial`-файлам по таблице `design.md`, `IssueGroup` — в свой файл; проверка — каждый файл до ~600 строк, `scripts\compile-check.ps1 -Source SceneOptimizationAuditor\Editor` без ошибок и предупреждений
- [x] 2.2 Разнести `Strings` по разделам (`Strings.cs`, `Strings.Ui.cs`, `Strings.Checks.cs`); проверка — `compile-check` чистый, каждая строка исходного файла — ровно в одном новом (построчная сверка при разрезе)
- [x] 2.3 Импорт в проверочном проекте создаёт `.meta` новых файлов; проверка — `.meta` лежат рядом в репозитории, консоль Unity без ошибок

## 3. Проверка и документация

- [x] 3.1 `scripts\run-audit-tests.ps1` — 29 из 29 (шагов стало 29 с 1.1.0); снимки после разреза попиксельно совпадают с 1.1; различия — только время прогона в строке контекста и в «Истории»
- [x] 3.1a Тестовый сервер (владелец 24.10.2026: «Один раз открой и тестируй там постоянно»): `scripts\audit-server.ps1`, `tests/AuditTests/Editor/AuditTestServer.cs`, тесты и снимки — командами в одном открытом редакторе; `ReportExporter.RunWithArgs` для шагов `cli-*`; проверка — те же 29 из 29 и 11 снимков за один запуск видимого редактора
- [x] 3.2 `docs/scene-optimization-auditor.md` (раздел «Устройство»), `docs/ui-guideline.md` (колонка «Где»), HANDOFF — новые файлы; проверка — названные файлы есть в репозитории
- [x] 3.3 Версия 1.1.1 (`scripts\stamp-version.ps1 -Version 1.1.1`); проверка — `ToolVersion.Version` и тулбар окна
