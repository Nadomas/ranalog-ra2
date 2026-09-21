# Cursor Project setup — RA2 Analog MVP

Short checklist to wire a **Cursor Project** to this repo’s autonomous pipeline.  
Full workflow: [`AI_WORKFLOW.md`](AI_WORKFLOW.md). Queue: [`TASK_QUEUE.md`](TASK_QUEUE.md).

---

## 1. Create the Project (UI)

1. In Cursor: **Agents** → **Projects** → **New Project**
2. Name: **`RA2 Analog — MVP`**
3. Link this GitHub repo: `https://github.com/Nadomas/ranalog-ra2.git` (same as local `origin`)
4. Prefer working branch **`agent/ai-pipeline`** (or another `agent/*`) — **not** `main`

**Three clicks after opening Cursor:** Agents → Projects → New Project — then fill name + repo.

---

## 2. Coordinator system prompt (paste into Project / Agent instructions)

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

---

## 3. First message — ANALYZE ONLY (Level 1)

```
Analyze only. Read docs/PROJECT_CONTEXT.md, docs/ai/TASK_QUEUE.md, docs/ai/BLOCKERS.md, and relevant experiments.
Report: current milestone, first open task, risks, suggested plan. Do not edit files, do not commit.
```

---

## 4. Second message — PROCEED AUTONOMOUSLY (Level 2–3)

```
Proceed autonomously at Level 2 (commit on agent/*; ask before push) or Level 3 if I said PR is allowed.
Take the first open task in docs/ai/TASK_QUEUE.md. Implement the smallest change. Verify per docs/ai/DEFINITION_OF_DONE.md (Local Unity/MCP or scripts/ai when needed). Update TASK_QUEUE. Stop on blockers or after 3 failed verify attempts. Do not ask me for the next task while open work remains.
```

---

## 5. Local verify note

Unity MCP must be running **locally** for Local Agent Play Mode / Editor verify:

1. Open `UnityProject/ra2-analog` in Unity Editor  
2. **Window → Unity MCP** → Start (Connected / listening on `8080`)  
3. Keep Editor open while agents use MCP  

Cloud Agents can do code/docs/queue; they cannot honestly claim Play Mode pass without Local MCP or `scripts/ai/*.ps1` on a machine with Unity.

**Automations** (watchdog, build guardian, etc.) — create later from **Appendix B** in [`AI_WORKFLOW.md`](AI_WORKFLOW.md). Do not block Project creation on Automations.

---

## 6. Branch hygiene

| Do | Don’t |
|----|--------|
| Cut / work on `agent/*`, `feature/*`, `experiment/*` | Commit product work on `main` |
| One TASK_QUEUE id → focused commit | Mix huge Unity experiment WIP into pipeline PRs |

Pipeline commit branch for this kit: **`agent/ai-pipeline`**.

---

*Source prompts: `AI_WORKFLOW.md` Appendix A. Update this file if those prompts change.*
