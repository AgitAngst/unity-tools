# Tasks

## 1. Проверки

- [x] 1.1 Снимок уровня качества: Soft Particles, Realtime Reflection Probes, VSync Count; проверка — тест
- [x] 1.2 `LIGHT_GAMMA_SPACE`, `LIGHT_SOFT_SHADOWS` (с исправлением), `REFL_QUALITY_REALTIME`, `PART_SOFT_PARTICLES`, `VR_VSYNC`; проверка — тест `quality-checks`
- [x] 1.3 `VR_FRAME_RATE_SCRIPT`: разбор скриптов `Assets/` без комментариев, кэш по времени файла; проверка — тест на строках и файлах и запись по настоящему скрипту
- [x] 1.4 Пороги `VR_MSAA` (только без MSAA) и `VR_MSAA_HIGH` (8x), дальность теней 40 м, Skin Weights больше Two Bones; карточка `VR_MSAA`; проверка — тест

## 2. Документация и итог

- [x] 2.1 Страница утилиты (таблица проверок, пороги), TESTPLAN; проверка — сверка с кодом судьёй
- [x] 2.2 `scripts\run-audit-tests.ps1` зелёный; HANDOFF; версия 2.2.0 (`stamp-version.ps1 -Version 2.2.0`)
