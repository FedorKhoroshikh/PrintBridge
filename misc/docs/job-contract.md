# Print queue contract (the "API")

The link between the WPF application (Win11) and the watcher (XP) is files in a shared folder.
No network API: the `job.json` manifest **is** the contract, language-independent and
surviving the VM boundary.

## Location

| Side | Path |
|---|---|
| Win11 (host) | `C:\Virtualization\Shared\Queue\` |
| XP (guest) | `\\vboxsvr\Shared\Queue\` |

Subfolders (created automatically):
- `Queue\` — incoming: `<id>.pdf` + `<id>.job.json`
- `Queue\status\` — statuses: `<id>.status.json`
- `Queue\printed\` — processed PDFs are moved here

## `<id>.job.json` — job (written by WPF, read by the watcher)

```json
{
  "id": "20260630-153012-7f3a",
  "file": "20260630-153012-7f3a.pdf",
  "copies": 2,
  "paper": "A4",
  "scale": "fit",
  "pages": "",
  "duplex": "none",
  "createdAt": "2026-06-30T15:30:12"
}
```

| Field | Values | Meaning |
|---|---|---|
| `id` | `yyyymmdd-hhmmss-xxxx` | unique job id |
| `file` | file name | `<id>.pdf` in the same folder |
| `copies` | integer ≥ 1 | number of copies (`Nx` in SumatraPDF) |
| `paper` | `A4` \| `A5` \| `B5` | selects the `Canon LBP-1120 <paper>` queue in XP |
| `scale` | `fit` \| `noscale` \| `shrink` | scale (passed directly into `-print-settings`) |
| `pages` | `""` or `1-4,7` | empty = all pages |
| `duplex` | `none` \| `manual` | manual duplex (the LBP-1120 has no auto-duplex) |
| `createdAt` | ISO-8601 | creation time |

> The LBP-1120 is black-and-white, so there is no color option.

## `status\<id>.status.json` — status (written by the watcher, read by WPF)

```json
{ "id": "...", "state": "printing", "message": "even pages", "updatedAt": "..." }
```

State machine:

```
queued ─► printing ─►[ awaiting-flip ─► printing ]─► done
                                                  └─► error
```

| `state` | Meaning |
|---|---|
| `queued` | job accepted (reserved; the watcher usually goes straight to `printing`) |
| `printing` | printing in progress |
| `awaiting-flip` | odd pages printed; waiting for the stack to be flipped (manual duplex) |
| `done` | success |
| `error` | error (see `message`) |

> `done` is written only after the printer's **spooler queue drains** (real completion of the
> host-based CAPT job), not right after SumatraPDF exits — see `implementation-plan.md` §2.

## `status\bridge.health.json` — guest heartbeat (written by the watcher, read by WPF)

Rewritten by the watcher on **every** poll cycle (~2 s). The WPF app uses it to drive the three
readiness indicators (VM / OS image / Printer) and to gate the «Печать» button.

```json
{ "watcher": true, "printerPresent": true, "printerName": "Canon LBP-1120 A4", "tick": "..." }
```

| Field | Meaning |
|---|---|
| `watcher` | always `true` while the watcher runs (its presence proves liveness) |
| `printerPresent` | a `Canon LBP-1120 …` printer exists in XP and is not offline (WMI `Win32_Printer`) |
| `printerName` | the matched queue name |
| `tick` | guest-local timestamp (diagnostic only) |

**Freshness is judged by the file's host-side mtime**, not by `tick` — the guest XP clock is
unreliable. A file older than ~20 s means the guest/watcher is down.

## Continue signal (manual duplex)

After the stack is flipped, WPF creates an empty file `Queue\<id>.continue`.
The watcher sees it, deletes it, and prints the even pages.

## Atomicity

Both sides write via `*.tmp` + rename, so that the other side
never reads a half-written file. The PDF is placed **before** `job.json`,
so by the time the manifest appears the file is guaranteed to be in place.
