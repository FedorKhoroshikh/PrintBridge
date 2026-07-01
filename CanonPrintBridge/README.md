# Canon Print Bridge

Мост для печати на **Canon LASER SHOT LBP-1120** с Windows 11 x64.

LBP-1120 — host-based CAPT-принтер, у которого нет 64-битного драйвера. Печать
работает только внутри гостевой Windows XP (VirtualBox, USB-проброс). Этот проект
делает печать «почти прямой»: на Win11 выбираешь PDF и параметры, а реальная
печать выполняется в XP автоматически.

```
[Win11]  WPF-приложение
   • выбрал PDF + параметры (формат, копии, масштаб, страницы, ручной дуплекс)
   • Печать ──► пишет <id>.pdf + <id>.job.json в общую папку Queue
   • показывает статус ◄── читает status\<id>.status.json
                               │
[XP VM]  watcher.vbs (в автозагрузке)
   • видит job.json ──► выбирает очередь печати по формату
   • SumatraPDF печатает на Canon (копии / масштаб / odd-even)
   • при ручном дуплексе: нечётные → «переверни стопку» → чётные
   • пишет status.json
```

## Структура

```
CanonPrintBridge.sln            ← открывать в Rider / VS
src/CanonPrintBridge/           ← WPF-приложение (.NET 8, net8.0-windows)
  appsettings.json              ← пути: QueueRoot, LauncherPath
xp-watcher/
  watcher.vbs                   ← сторож для XP
  README-XP-setup.md            ← разовая настройка стороны XP
docs/
  job-contract.md               ← контракт job.json / status.json («API»)
  ui-spec.md                    ← UI/UX-спецификация редизайна окна
  implementation-plan.md        ← план имплементации редизайна
```

Текущий статус и фактическая среда (host + XP) — в `STATUS.md`.

## Сборка

```powershell
dotnet build CanonPrintBridge.sln -c Release
# готовый exe:
# src\CanonPrintBridge\bin\Release\net8.0-windows\CanonPrintBridge.exe
```

Или просто открыть `CanonPrintBridge.sln` в Rider и запустить.

## Настройка

1. **Win11**: при необходимости поправь `src/CanonPrintBridge/appsettings.json`
   (`QueueRoot` — хостовый путь общей папки; `LauncherPath` — путь к `Print-Canon.ps1`).
2. **XP**: выполни разовую настройку по `xp-watcher/README-XP-setup.md`
   (SumatraPDF 3.1.2, очереди печати под форматы, сторож в автозагрузке, автологин).

## Использование

1. Кнопка **«Запустить принтер (VM)»** — поднимает VM и пробрасывает USB
   (вызывает существующий `Print-Canon.ps1`).
2. Перетащи PDF в окно (или **«Обзор…»**).
3. Выставь параметры → **Печать**.
4. Следи за статусом в журнале; при ручном дуплексе появится подсказка перевернуть стопку.

## Ограничения (по железу LBP-1120)

- **Только ч/б** — принтер монохромный.
- **Двусторонняя — только ручная** — нет дуплекс-модуля.
- **Формат — через очереди XP** (`Canon LBP-1120 A4/A5/B5`), а не через флаги драйвера.
- VM с XP должна быть **запущена и залогинена** (можно headless + автологин).
