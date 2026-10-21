# scene-audit-export Specification

## Purpose
Выгрузка результата аудита сцены в JSON и CSV — из окна утилиты и из командной строки Unity, чтобы
хранить, сравнивать и проверять отчёты без открытого окна.

## Requirements

### Requirement: JSON report
The JSON report SHALL contain the scene list, an ISO 8601 timestamp, the platform, the target FPS,
the XR state, the checked quality level, the severity summary, the scene budgets with an estimate flag,
a `hasProfilerSnapshot` flag with the snapshot, and the issue list. Check ids, categories, severities
and XR values SHALL be in English; messages and suggestions SHALL be in the current window language.

#### Scenario: No profiler snapshot
- **WHEN** a report is exported before any profiler capture
- **THEN** `hasProfilerSnapshot` is false

#### Scenario: Russian window language
- **WHEN** the window language is RU and the user exports JSON
- **THEN** `severity` values are English and `message` values are Russian

### Requirement: CSV report
The CSV report SHALL have the columns `CheckId, Category, Severity, Scene, ObjectPath, AssetPath,
Message, Suggestion, AutoFixable`, one row per issue, SHALL escape fields by RFC 4180 and SHALL be
written as UTF-8 with BOM.

#### Scenario: Message with a comma and quotes
- **WHEN** an issue message contains a comma and double quotes
- **THEN** the field is quoted and inner quotes are doubled

### Requirement: Command-line export
The tool SHALL provide a static method for `-executeMethod` that opens the scene given by `-auditScene`,
runs the audit and writes the report to the path given by `-auditReport` (format by extension), and
SHALL exit with code 0 on success and 1 on any error.

#### Scenario: Missing scene argument
- **WHEN** the method runs without `-auditScene`
- **THEN** an English error is logged and Unity exits with code 1
