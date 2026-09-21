# Definition of Done — ra2-analog agent tasks

A TASK_QUEUE item is **done** only when all applicable checks below pass.  
Thin spikes may skip Play Mode or tests when the task text says so — say so explicitly in the task notes when skipping.

---

## 1. Compile

- Project compiles with **no** C# / Unity compile errors.
- Prefer: Unity Editor open → script reload → `read_console` (types `error`) empty of new errors.
- Fallback (headless): `powershell -File scripts/ai/build.ps1` exits 0 (see logs under `artifacts/ai/`).

## 2. Unity Console

- After the change: no new **errors** attributable to the work.
- Warnings: fix or justify in the task note / experiment report if they are new and related.
- MCP: `read_console` action `get`, types `error,warning`.
- Clear only when intentional; prefer filtering over blind clear.

## 3. Tests

- If the repo has Unity Test Runner coverage for the touched area: run EditMode (and PlayMode when relevant).
- MCP: `run_tests` → poll `get_job` until completed.
- Fallback: `powershell -File scripts/ai/test.ps1` (EditMode default; `-PlayMode` when needed).
- If **no** automated tests exist yet for the spike: Play Mode verifier / smoke for that spike counts as the test substitute — document which verifier menu/scene was used.

## 4. Play Mode (when applicable)

Required when the task touches physics, drive, joints, spawn, net authority, combat, or mode switching.

- Enter Play Mode (MCP `manage_playmode` action `enter`, or Editor).
- Run the spike verifier or manual acceptance from the task.
- Exit Play Mode cleanly (`manage_playmode` action `exit`).
- Do not leave the Editor stuck in Play Mode for the next agent.

Not required for pure docs, `.cursor` rules, or scripts-only tasks.

## 5. Project constraints (always)

- No contradiction of `docs/PROJECT_CONTEXT.md` Fixed Decisions / pillars.
- Physics: no dynamic-body transform teleports as locomotion; prefer Rigidbody / joints / forces.
- Networking: no client-authoritative gameplay results; MP-aware data contracts preserved.
- Architecture: no new monolithic managers; MonoBehaviour stays glue / view / input adapters.
- Do not add Netcode/Physics packages without `[EXPERIMENT REQUIRED]` + justification.
- Search `docs/experiments/` before retrying a rejected approach.

## 6. Documentation

- Update or add experiment / decision notes when the task is a spike or gate.
- Update [`TASK_QUEUE.md`](TASK_QUEUE.md): mark the task completed; set Status / next open item.
- Update [`BLOCKERS.md`](BLOCKERS.md) if a real blocker appeared or cleared.
- Do **not** invent GDD/roadmap content that contradicts existing docs.

## 7. Git

- Work on `agent/*`, `feature/*`, or `experiment/*` — never commit product work directly on `main`.
- One logical task → one commit (or a small stack of focused commits if the task is large).
- Commit message: conventional style preferred (`feat`, `fix`, `chore`, `docs`, `refactor`, `test`); explain **why**.
- Do not force-push `main`/`master`. Do not commit `Library/`, secrets, or `artifacts/ai/` logs.
- Push / open PR only when the workflow / human asks (Level 3–4 may open PRs per branch policy).

## 8. Smoke (optional but recommended for Unity tasks)

```powershell
powershell -File scripts/ai/smoke-test.ps1
```

Fails if Unity path missing or batch compile fails. MCP Play verification still required when the task needs runtime proof.

---

## Quick checklist (copy into PR / task close note)

- [ ] Compiles (Editor or `build.ps1`)
- [ ] Console clean of new errors
- [ ] Tests or spike verifier passed
- [ ] Play Mode verified if physics/net/combat/modes touched
- [ ] Experiments consulted / report updated if spike
- [ ] `TASK_QUEUE.md` updated
- [ ] Commit on allowed branch

---

*Adapt tool names if Unity MCP package updates; keep the same intent: compile → console → tests → play → docs → queue → commit.*
