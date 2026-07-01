# Claude Design — intro / overview

A short overview for independent study. As of mid-2026.
Sources at the bottom. Throughout the text it is marked what specifically matters for our case (WPF).

---

## 1. What it is

**Claude Design** is an **Anthropic Labs** product: a mode inside Claude where, through a dialog, you
create visual artifacts — **UI prototypes, mockups, slides, one-pagers** —
from a text description ("prompt-to-prototype"). It's not a standalone app but a part of Claude.ai.

Under the hood it generates **real frontend code**: HTML/CSS/JavaScript and React. That is,
the result is not a picture but a working web page/component you can click and
export. (Positioned as a "Figma competitor" for quickly visualizing ideas,
primarily for **non-designers**: founders, product managers, marketers.)

## 2. Access

- **Research preview**, plans **Pro, Max, Team, Enterprise**. Access is **included in the plan** and
  consumes your **subscription limits** (you can continue beyond the limits by enabling *extra usage*).
- **Enterprise: off by default** — the admin enables it in **Organization settings**.
  → Check that the feature is enabled for you.
- The model — **Claude Opus 4.7** (a vision model). Launched — **April 17, 2026**;
  later — integrations (GitHub import of design systems, `/design-sync`, on-canvas editing).

## 3. Where to open it

- Direct address: **`claude.ai/design`** (inside Claude.ai; per secondary sources — also
  a palette icon in the left panel).

## 4. How to start a project (4 input methods)

1. **Text prompt** — you describe what you need, Claude assembles the first version.
2. **File upload** — images and documents (DOCX, PPTX, XLSX). ← this is where we put our
   `design-brief.md` and a screenshot of your prototype.
3. **Point at a codebase** — Claude reads the code and adapts.
4. **Web capture** — "capture" elements from an existing site so the prototype resembles
   the real product.

## 5. How to refine

- **Inline comments** on specific elements.
- **Direct text editing** on the mockup.
- **Adjustment knobs** — live editing of spacing, color, layout.
- Then ask Claude to **apply the changes across the whole mockup** at once.

## 6. Design system / onboarding

During onboarding Claude reads the team's codebase and design files and builds a **design system**
(colors, typography, components), which is then applied automatically to all projects.
For a single household project this is not required.

## 7. Export and sharing

- An internal **URL** within the organization; save as a **folder**.
- Export to **Canva, PDF, PPTX, standalone HTML**.
- **Interactive prototypes** — static mockups turn into clickable ones (you walk through
  the screens without a single line of code).
- Collaboration: private / view by link / edit access.

→ For us: you export **standalone HTML** (or PDF/screenshots) and hand it to me as a **reference**.

## 8. Integration with Claude Code

- **Handoff to Claude Code**: when the design is ready, Claude packages everything into a **handoff bundle**,
  which is passed to Claude Code in a single instruction (so as not to rebuild "from screenshots").
- Later (an update, secondary sources ~June 2026): importing design systems from **GitHub**,
  editing directly on the canvas, the **`/design-sync`** command — pulls the organization's
  component library into Claude Design.

## 9. ⚠️ Important for our case (WPF)

- Claude Design produces **web code (HTML/CSS/JS/React)** — which **does not map onto WPF/XAML
  directly**. So in our process it's needed as a **generator of visual references**
  (layout, hierarchy, spacing, look&feel, patterns), and **not** as a source of final code.
- Practical flow: `design-brief.md` + a screenshot of your prototype → into Claude Design →
  you get variants → export **HTML/screenshots** → hand them to me → I port them into
  WPF/XAML (WPF-UI/Fluent theme).
- The brief deliberately states "this is a desktop WPF application, a single window, think in WPF
  controls" — so that it doesn't produce a web landing page but proposes a desktop layout.
- The "Claude Design → Claude Code" handoff is meant for a **web frontend**; for WPF it won't give
  ready code, but the exported mockups are still useful as a reference.

## 10. Caveats

- **Research preview** — features/behavior may change; a paid plan is required.
- Oriented toward **web/non-designers**; for the desktop (WPF) — only as a visual entry point.
- Exact formats/steps may have been updated after writing — check against the `claude.ai/design` interface.

---

## Sources

- [Introducing Claude Design by Anthropic Labs — Anthropic](https://www.anthropic.com/news/claude-design-anthropic-labs)
- [How to Use Claude Design (Tosea.ai)](https://tosea.ai/blog/claude-design-complete-guide)
- [Anthropic just launched Claude Design… (VentureBeat)](https://venturebeat.com/technology/anthropic-just-launched-claude-design-an-ai-tool-that-turns-prompts-into-prototypes-and-challenges-figma)
- [Anthropic Debuts Claude Design… (MacRumors)](https://www.macrumors.com/2026/04/17/anthropic-claude-design/)
- [Anthropic Supercharges Claude Design… direct code handoff (BigGo Finance)](https://finance.biggo.com/news/4535a14d-ea20-4047-9b2d-166397850c7c)
- [What Is Claude Design? (MindStudio)](https://www.mindstudio.ai/blog/what-is-claude-design-anthropic)
- [Claude Design: Complete Guide for Non-Designers (BuildFastWithAI)](https://www.buildfastwithai.com/blogs/claude-design-anthropic-guide-2026)
