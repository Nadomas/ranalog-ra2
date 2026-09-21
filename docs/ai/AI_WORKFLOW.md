# AI Workflow — Autonomous Cursor Pipeline (ra2-analog)

> How Cloud/Local Cursor agents develop this Unity repo without inventing product scope.  
> **Stage covered here:** docs + rules + verification scripts (pipeline Stages 1–5 of the setup plan).  
> **Not done by agents:** creating a Cursor Project or Automations in the UI — see § Manual steps (human later).

**Related:** [`TASK_QUEUE.md`](TASK_QUEUE.md) · [`DEFINITION_OF_DONE.md`](DEFINITION_OF_DONE.md) · [`BLOCKERS.md`](BLOCKERS.md) · [`../PROJECT_CONTEXT.md`](../PROJECT_CONTEXT.md) · [`../TECHNICAL_ROADMAP.md`](../TECHNICAL_ROADMAP.md)

---

## 1. Source-of-truth hierarchy

Agents resolve conflicts **top-down**. Lower docs may not silently override higher ones.

```
VISION.md
  → GDD.md / GAMEPLAY.md
    → TECHNICAL_PRINCIPLES.md / UNITY_ENGINE.md / SDS.md
      → TECHNICAL_ROADMAP.md          (proof order + gates)
        → docs/experiments/           (validated / rejected approaches)
          → docs/ai/TASK_QUEUE.md     (executable next actions)
            → code in UnityProject/ra2-analog/
```

Stable shared context (always read first): **`docs/PROJECT_CONTEXT.md`**.

Cursor always-applied rules under `.cursor/rules/` enforce pillars (physics, networking, architecture, Unity). Autonomous rules (`autonomous-development`, `ai-testing`, `ai-git`) add workflow — they must **not** contradict physics/net/MB policy.

---

## 2. Pipeline loop

```
Coordinator (Cloud or Local)
  → pick first open task from TASK_QUEUE
  → Coding Agent(s): implement smallest change on agent/* | feature/* | experiment/*
  → Local Unity Agent (when Editor/Play/MCP required)
       → Unity MCP (localhost:8080) and/or scripts/ai/*.ps1
  → verify per DEFINITION_OF_DONE
  → update TASK_QUEUE (+ experiment report if spike)
  → commit on allowed branch
  → next open task
```

Stop if [`BLOCKERS.md`](BLOCKERS.md) gains an Active entry or a stop condition fires (§7).

---

## 3. Cloud vs Local split

| Concern | Cloud agent | Local agent (this machine) |
|---------|-------------|----------------------------|
| Edit C#, docs, rules, scripts | Yes | Yes |
| Static review, queue updates, PR text | Yes | Yes |
| Unity Editor open / domain reload | No | Yes |
| Play Mode / physics feel | No | Yes |
| Unity MCP (`read_console`, `manage_playmode`, …) | Only if tunnelled (not default) | Yes — Editor + MCP server running |
| Batchmode compile/tests via `scripts/ai` | If CI runner has Unity | Yes on Windows with Editor installed |
| Commit / push | Per autonomy level + git rules | Same |

**Rule:** anything that needs Play Mode, Console after script reload, scene building menus (`Tools/RA2/…`), or MCP is a **Local** step. Cloud implements code; Local verifies Unity.

---

## 4. Experiments rule

Before inventing a new physics/net/package approach:

1. Search `docs/experiments/` (and Decision Log in `UNITY_ENGINE.md`).
2. Reuse PASS conclusions; do **not** re-run rejected approaches without a new hypothesis.
3. Mark unproven tech `[EXPERIMENT REQUIRED]`; write a short report when a spike finishes.
4. Stage 2 HARD GATE is **GO** on provisional custom UDP — do not silently switch to NGO/NFE “because tutorials”.

---

## 5. Branch policy

| Branch | Use |
|--------|-----|
| `main` | Integration only; **never** develop directly on `main` |
| `agent/<short-topic>` | Autonomous agent task branches |
| `feature/<short-topic>` | Human or planned features |
| `experiment/<id-slug>` | Explicit spikes / alternate approaches |

Details: `.cursor/rules/ai-git.mdc`. Commits: one logical TASK_QUEUE item (or small focused stack). No force-push to `main`.

Remote today: `https://github.com/Nadomas/ranalog-ra2.git` (already configured).

---

## 6. Autonomy levels

| Level | Behavior |
|-------|----------|
| **1** Analyze only | Read queue/docs/code; propose plan; **no** edits/commits |
| **2** Implement + verify, stop before push | Code, local verify, commit on `agent/*`, update queue; ask before push/PR |
| **3** Implement + PR | Level 2 + push branch + open PR; stop on blockers / DoD fail after retries |
| **4** Merge-capable | Only with explicit human grant; still never force-push `main`; respect HARD GATE |

**Start recommendation for this repo: Level 2–3.**  
Stage 2 physics+MP is proven thin; remaining work is session/results/MVP glue — still high coupling to Unity Play Mode, so keep Local verification mandatory.

---

## 7. Stop conditions

Stop autonomous “proceed” and notify (§8) when:

- Active blocker written to `BLOCKERS.md`
- Compile still red after **3** fix attempts
- Play Mode / verifier fails after **3** attempts with no new hypothesis
- Task requires a product decision not covered by GDD/roadmap/context
- Stage 2-class invariants regress (authority leak, transform-drive locomotion, desync class bug)
- Unity Editor/MCP required and unavailable, and batchmode scripts also cannot run
- Human says stop

Do **not** stop merely to ask “what’s next?” if `TASK_QUEUE` has an unfinished open task.

---

## 8. Notification policy

Notify the human **only** when:

1. A real **blocker** is opened, or  
2. **Build/verify broken** after N=3 attempts, or  
3. A **milestone** completes (e.g. all Stage 9 thin tasks done)

Do not spam on every successful micro-commit.

---

## 9. Unity MCP (this repo)

### Config (intentional local / “shadow” vs org Runlayer)

| Item | Value |
|------|--------|
| Cursor config | `.cursor/mcp.json` → server id `unity-mcp` |
| URL | `http://localhost:8080/` |
| Package | `com.emeryporter.unitymcp` in `UnityProject/ra2-analog/Packages/manifest.json` |
| Namespace in Cursor | often `project-0-RA2 - analog-unity-mcp` |

This is the **project Unity Editor MCP** required by workspace rules — **not** a Runlayer-managed cloud MCP. Do **not** install additional Unity MCP packages or change Runlayer configs. Do **not** replace this with ad-hoc `npx` MCP servers.

### Start sequence

1. Open `UnityProject/ra2-analog` in Unity **6000.5.9f1**.
2. **Window → Unity MCP → Start** (listening on 8080).
3. In Cursor, enable/reload `unity-mcp` if prompted.
4. Keep Editor open while agents call MCP tools.

### Verification ops (prefer this order)

| Step | MCP tool | Notes |
|------|----------|--------|
| After script edits | `refresh_unity` | Then check compile |
| Compile / Console | `read_console` action `get`, types `error` or `error,warning` | Fail DoD on new errors |
| Automated tests | `run_tests` action `run` (EditMode/PlayMode) → `get_job` | Poll until completed/failed |
| Runtime spikes | `manage_playmode` `enter` → observe/verifiers → `exit` | Don’t leave Play stuck |
| Scene sanity | `describe_scene` / `diagnose_scene` / `get_scene_hierarchy` | As needed |
| Safety | `manage_checkpoint` save before destructive edits | Restore on failure |

Batchmode fallback when Editor MCP is down: §10 scripts.

---

## 10. Scripts (`scripts/ai/`)

Windows (this machine). Prefer `pwsh` if installed; otherwise Windows PowerShell:

```powershell
# From repo root
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ai/status.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ai/build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ai/test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ai/smoke-test.ps1
# Or: scripts\ai\build.cmd
```

- Set `UNITY_EDITOR_PATH` to `Unity.exe` if Hub auto-detect fails.
- Project path default: `UnityProject/ra2-analog`.
- Logs: `artifacts/ai/` (gitignored).
- Exit code ≠ 0 on failure.

Honest limitation: full CI Test Runner filters / cover may need iteration; scripts attempt standard `-batchmode -runTests` and print clear errors if Unity is missing.

---

## 11. Manual steps (human — create later, not by agent)

### A. Cursor Project (UI)

1. In Cursor, create/link a Project for this repo folder.  
2. Point agents at `docs/PROJECT_CONTEXT.md` + `docs/ai/AI_WORKFLOW.md`.  
3. Ensure `.cursor/mcp.json` Unity MCP is enabled when Editor is up.

### B. Automations (UI) — **create later, not now**

Ready-to-paste prompts are in **Appendix B**. Do not expect agents to create Automations via API in this stage.

### C. Git hygiene before heavy autonomy

1. Cut `agent/mvp-session` (or similar) from current work — large uncommitted Unity/experiment tree may exist.  
2. Do not commit on `main` directly.  
3. Remote already exists; push with `-u` when ready.

---

## Appendix A — Ready-to-paste prompts

### A.1 Coordinator system prompt

```
You are the Coordinator for ra2-analog (Unity physics combat game).

Source of truth order:
docs/PROJECT_CONTEXT.md → GDD/GAMEPLAY → TECHNICAL_ROADMAP → docs/experiments/ → docs/ai/TASK_QUEUE.md.

Rules:
- Never invent GDD/roadmap. Never switch production engine away from Unity.
- Respect .cursor/rules (physics, networking, architecture, MonoBehaviour policy).
- Complete TASK_QUEUE in order. Do not ask the human for the next task while an open unfinished item exists.
- Autonomy: operate at Level 2–3 unless told otherwise. Cloud = code/static; Local Unity Agent = Editor/Play/MCP.
- Before new physics/net approaches: search docs/experiments/; reuse conclusions; don't repeat rejected paths.
- Branch only on agent/*, feature/*, experiment/* — never develop on main.
- Definition of Done: docs/ai/DEFINITION_OF_DONE.md. Stop/blockers: docs/ai/BLOCKERS.md + AI_WORKFLOW stop conditions.
- Notify human only on blockers, broken verify after 3 attempts, or milestone complete.
- Unity MCP is localhost:8080 (com.emeryporter.unitymcp). Do not install new MCP servers.
- Smallest spike that answers one question. No content catalog/economy before Stage 9–11 thin path.
```

### A.2 Analyze-only (Level 1)

```
Analyze only. Read docs/PROJECT_CONTEXT.md, docs/ai/TASK_QUEUE.md, docs/ai/BLOCKERS.md, and relevant experiments.
Report: current milestone, first open task, risks, suggested plan. Do not edit files, do not commit.
```

### A.3 Proceed autonomously (Level 2–3)

```
Proceed autonomously at Level 2 (commit on agent/*; ask before push) or Level 3 if I said PR is allowed.
Take the first open task in docs/ai/TASK_QUEUE.md. Implement the smallest change. Verify per docs/ai/DEFINITION_OF_DONE.md (Local Unity/MCP or scripts/ai when needed). Update TASK_QUEUE. Stop on blockers or after 3 failed verify attempts. Do not ask me for the next task while open work remains.
```

---

## Appendix B — Automation prompts (**create later, not now**)

Paste into Cursor Automations UI when you are ready. Do **not** treat these as already configured.

### B.1 Watchdog (queue stall)

```
On schedule or when chat goes idle: read docs/ai/TASK_QUEUE.md and docs/ai/BLOCKERS.md.
If an open task exists and no blocker: remind coordinator to continue that task.
If Active blocker: notify human with blocker text only. Do not invent new tasks.
```

### B.2 Build guardian

```
After agent commits touching UnityProject/**/*.cs: run powershell -File scripts/ai/build.ps1 (or MCP read_console for errors).
If fail: open/update docs/ai/BLOCKERS.md or comment on the task; do not start unrelated features.
```

### B.3 Code review

```
Review the latest agent PR/diff against PROJECT_CONTEXT pillars, physics.mdc, networking.mdc, architecture.mdc.
Flag transform teleports on dynamics, client authority leaks, new packages without experiment, monolithic managers.
Do not approve if DEFINITION_OF_DONE Unity checks were skipped for physics/net tasks.
```

### B.4 Test coverage nudge

```
When a spike adds gameplay rules without a verifier or Test Runner test, comment asking for a Play Mode verifier or EditMode test before closing the TASK_QUEUE item.
Prefer thin verifiers consistent with existing *Verifier.cs patterns.
```

---

## Appendix C — Role mapping (existing team doc)

Aligns with [`docs/TEAM_WORKFLOW.md`](../TEAM_WORKFLOW.md):

| Role | In this pipeline |
|------|------------------|
| Human / owner | Product decisions, Automations/Project UI, Level 4 merge grant |
| Coordinator | Codex-like: queue, boundaries, reviews, gates |
| Coding + Local Unity | Cursor agents implementing and verifying in Editor |

---

*Update this file when MCP tool names, autonomy defaults, or queue milestones change.*
