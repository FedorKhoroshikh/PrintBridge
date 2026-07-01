# CLAUDE.md — Canon Print Bridge

Репозиторий одного проекта: **Canon Print Bridge** — печать на Canon LASER SHOT
LBP-1120 (host-based CAPT, нет x64-драйвера) с Windows 11 через гостевую Windows XP
в VirtualBox. Приложение на Win11 выбирает PDF + параметры, реальная печать идёт
в XP автоматически через файловую очередь в общей папке.

Корень репозитория — `T:\Dev\`. Основной код — в `CanonPrintBridge/`.

## Раскладка

| Путь | Что |
|---|---|
| `CanonPrintBridge/` | решение .NET 8 (`net8.0-windows`, WPF) |
| `CanonPrintBridge/src/CanonPrintBridge/` | WPF-приложение (Win11-сторона) |
| `CanonPrintBridge/xp-watcher/` | сторож `watcher.vbs` + разовая настройка XP |
| `CanonPrintBridge/docs/` | контракты и спецификации (см. ниже) |
| `CanonPrintBridge/README.md` | обзор и сборка |
| `CanonPrintBridge/STATUS.md` | текущий статус и фактическая среда (host+XP) |
| `printer-xp-icon.ico` | иконка приложения (embedded resource) |

Документы в `docs/`:
- `job-contract.md` — файловый контракт `job.json` / `status.json` (это и есть «API»).
- `ui-spec.md` — UI/UX-спецификация и вводные по look&feel (задача редизайна).
- `implementation-plan.md` — план имплементации редизайна.

## Сборка / публикация

```powershell
dotnet build CanonPrintBridge/CanonPrintBridge.sln -c Release
# single-file (см. implementation-plan.md, фаза «single exe»):
dotnet publish CanonPrintBridge/src/CanonPrintBridge -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Готовый exe после build: `CanonPrintBridge/src/CanonPrintBridge/bin/Release/net8.0-windows/CanonPrintBridge.exe`.

## Фактическая среда (host + XP), вне кода

Полностью — в `CanonPrintBridge/STATUS.md`. Кратко:
- VM VirtualBox: **`Microelectronics`** (XP). `VBoxManage`: `C:\Program Files\Oracle\VirtualBox\VBoxManage.exe`.
- USB-фильтр «Canon CAPT» (VendorId `04a9`, ProductId `262b`) → принтер захватывается сам при старте VM.
- XP-очередь печати: **`Canon LBP-1120 A4`** (A5/B5 пока не создавались).
- Сторож в XP: `C:\CanonBridge\watcher.vbs`, автозапуск в Автозагрузке профиля `User`, автологин.
- Мост-папка: хост `C:\Virtualization\Shared\Queue\` ↔ гость `\\vboxsvr\Shared\Queue`.
- Лаунчер VM: `Printer_Canon_lbp_1120\Print-Canon.ps1` (в корне репо; сборка копирует его рядом с exe, `LauncherPath` относительный — портируемость).

## Ограничения железа LBP-1120

Только ч/б; двусторонняя — только ручная; формат — через отдельные XP-очереди, а не флаги драйвера.

## Грабли (не повторять)

- **PowerShell 5.1**: `.ps1` читается в ANSI/1251 — кириллица без BOM ломает парсер →
  лаунчер держим **ASCII-only**. `$ErrorActionPreference='Stop'` превращает stderr от
  VBoxManage в терминирующую ошибку → в лаунчере `'Continue'` + `2>$null`.
- **`watcher.vbs`**: комментарии — только английские (cscript/XP плохо едят non-ASCII в исходнике).
- Часы гостевой XP сбиты → таймстемпы `updatedAt` в статусах врать могут; свежесть считать по
  mtime файлов на **хосте**, а не по времени внутри XP.
