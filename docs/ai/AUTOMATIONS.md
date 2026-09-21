# Cursor Automations — RA2 Analog pipeline

> How to wire **Cursor Automations** for the autonomous pipeline.  
> Prompts adapted from [`AI_WORKFLOW.md`](AI_WORKFLOW.md) Appendix B.  
> **Automations cannot be fully created from chat** — the agent prefills the Glass editor; **you must Save** each one in the UI.

**Repo:** `Nadomas/ranalog-ra2`  
**Preferred checkout branch:** `agent/ai-pipeline` (merge that branch first, or point each automation’s git checkout at it)

---

## Status (what agent did vs what you finish)

| Automation | Prefill via agent | You must finish in UI |
|------------|-------------------|------------------------|
| **RA2 Watchdog** | Open Automations editor with draft (schedule + prompt + repo) | Confirm trigger **Every hour**, confirm repo/branch, **Save**, enable if toggled off |
| **RA2 Build Guardian** | Draft documented below (open next after Watchdog is saved) | Set git **push** scope to `Nadomas/ranalog-ra2` + branches `agent/*`, enable tools if needed, **Save** |
| **RA2 Code Review** | Draft documented below | Set **pull request** trigger (opened + pushed), enable **Comment on PRs**, **Save** |
| **RA2 Test Coverage** | Draft documented below | Same PR scope (or schedule), enable **Comment on PRs**, **Save** |

Cursor has **no public API to persist** Automations from the agent. `open_automation` only opens the form with a draft. Unsaved drafts are lost if you close the panel or open another draft without saving.

**Recommended order:** Save Watchdog → ask agent “open Build Guardian” (or paste from this file) → Save → Code Review → Save → Test Coverage → Save.

---

## Prerequisites

1. **GitHub connected** in Cursor (Cloud Agents / Automations can access `Nadomas/ranalog-ra2`).
2. Branch **`agent/ai-pipeline`** exists on origin (or change each automation’s checkout branch to a branch that contains `docs/ai/*`).
3. Pipeline docs on that branch: `docs/ai/TASK_QUEUE.md`, `BLOCKERS.md`, `DEFINITION_OF_DONE.md`, `AI_WORKFLOW.md`.
4. Optional: Cursor Project **RA2 Analog — MVP** (see [`CURSOR_PROJECT_SETUP.md`](CURSOR_PROJECT_SETUP.md)) — separate from Automations.
5. Cloud compute configured in [Cloud Agent dashboard](https://cursor.com/dashboard?tab=cloud-agents) if scheduled/cloud runs need a model.

---

## Notification policy (all four)

Notify the human **only** when:

1. An **Active blocker** is opened in `docs/ai/BLOCKERS.md`, or  
2. **Build/verify is broken** after **3** fix attempts, or  
3. A **milestone** completes (e.g. Stage 9 thin queue cleared)

Do **not** notify on every successful micro-task or every hourly “queue is fine” tick.

---

## Automation 1 — RA2 Watchdog

| Field | Setting |
|-------|---------|
| **Name** | RA2 Watchdog |
| **Description** | Hourly queue/blocker check; nudge coordinator or notify only on Active blockers. |
| **Trigger** | Schedule — every hour (`0 * * * *`) |
| **Repo / branch** | `Nadomas/ranalog-ra2` @ `agent/ai-pipeline` |
| **Tools** | None required (agent read + optional memory) |

### Prompt (copy-paste)

```
You are the RA2 Analog autonomous Watchdog for repo Nadomas/ranalog-ra2.

Read docs/ai/TASK_QUEUE.md and docs/ai/BLOCKERS.md (and docs/ai/DEFINITION_OF_DONE.md if needed).

Rules:
- If Active blocker exists in BLOCKERS.md: notify the human with the blocker text only. Do not invent new tasks. Do not start coding.
- If an open unfinished task exists and there is no Active blocker: leave a short reminder that the coordinator should continue that first open task (id + title). Do not invent scope. Do not implement the task yourself unless this automation is explicitly granted Level 2+.
- If queue is idle (no open tasks) and no blocker: do nothing noisy — one-line note in the run log is enough. Do not notify the human.
- Never invent GDD/roadmap items. Never switch engine away from Unity. Never install new MCP servers.
- Notification policy: human only for Active blockers, broken verify after 3 attempts, or milestone complete — not every hourly tick.
```

### UI remaining clicks

1. Open Automations (agent may already open this draft).  
2. Confirm **On a schedule → Every hour**.  
3. Confirm git checkout **Nadomas/ranalog-ra2** / **agent/ai-pipeline**.  
4. Paste prompt if empty.  
5. **Save** / Enable.

---

## Automation 2 — RA2 Build Guardian

| Field | Setting |
|-------|---------|
| **Name** | RA2 Build Guardian |
| **Description** | On push to agent/*: verify build/tests from latest changes; fix or block — no new features. |
| **Trigger** | Git — new push to branch |
| **Repo / branches** | `Nadomas/ranalog-ra2`, branches matching `agent/*` (if UI has no glob, use `agent/ai-pipeline` + other agent branches you use) |
| **Tools** | Optional: Manage check runs. Prefer agent shell for `scripts/ai/*.ps1` when the runner has Unity. |

### Prompt (copy-paste)

```
You are the RA2 Analog Build Guardian for Nadomas/ranalog-ra2.

When this run is triggered by a push (especially under agent/*):
1. Inspect the latest commit(s) on the triggering branch. Focus on changes under UnityProject/**/*.cs, scripts/ai/, and docs/ai/.
2. Verify compile:
   - Prefer: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/ai/build.ps1 from repo root (logs under artifacts/ai/).
   - If Unity/batchmode is unavailable on this runner: say so honestly, run what static checks you can, and do not claim a Play Mode pass.
3. If tests are cheap and available: powershell -File scripts/ai/test.ps1 (EditMode). Do not invent a full coverage campaign.
4. On failure: fix compile errors caused by the latest changes if clearly in scope (max 3 attempts). If still red: open/update docs/ai/BLOCKERS.md with why/what human must do, and stop. Do not start unrelated features or pull the next TASK_QUEUE item.
5. On success: no human notification. Optionally update a check run if that tool is enabled.
6. Respect DEFINITION_OF_DONE.md. Respect physics/networking/architecture rules. Never develop on main. Never force-push. Never install new MCP servers.
```

### UI remaining clicks

1. Trigger: **GitHub/GitLab event → New push to branch**.  
2. Repo: **Nadomas/ranalog-ra2**. Branches: **`agent/*`** (or list `agent/ai-pipeline`).  
3. Paste prompt. Enable **Manage check runs** if you want CI-style status.  
4. **Save**.

---

## Automation 3 — RA2 Code Review

| Field | Setting |
|-------|---------|
| **Name** | RA2 Code Review |
| **Description** | PR review focused on Unity physics, networking authority, and architecture pillars. |
| **Trigger** | Git — pull request opened and/or code pushed to a PR |
| **Repo** | `Nadomas/ranalog-ra2` |
| **Tools** | **Comment on PRs** (required for useful output) |

### Prompt (copy-paste)

```
You are the RA2 Analog Code Review automation for Nadomas/ranalog-ra2.

Review the latest PR diff against:
- docs/PROJECT_CONTEXT.md pillars (multiplayer-first, physics-first, data-driven, modular, testable)
- .cursor/rules physics, networking, architecture, Unity MonoBehaviour policy
- docs/ai/DEFINITION_OF_DONE.md for Unity/physics/net tasks

Flag (block-level findings, not nitpicks):
- Setting transform.position/rotation on dynamic Rigidbodies as locomotion / “unstuck” hacks
- Client-trusted authoritative gameplay (wins, damage application, final sim transforms)
- New Netcode/Physics/extra packages without [EXPERIMENT REQUIRED] + docs/experiments justification
- Monolithic *Manager god objects; gameplay logic stuffed into MonoBehaviour Update soup
- Skipping Play Mode / verifier for physics, joints, drive, spawn, net authority, or combat tasks

Output: a concise PR comment with Critical / Should-fix / Notes. Do not approve if DEFINITION_OF_DONE Unity checks were skipped for physics/net work. Do not invent product scope. Do not merge. Notify human only if a Stage 2-class invariant regression is found.
```

### UI remaining clicks

1. Trigger: **Pull request opened** and **Code pushed to a pull request** (both recommended).  
2. Repo: **Nadomas/ranalog-ra2**. Optionally ignore draft PRs if you want less noise.  
3. Enable tool **Comment on PRs**.  
4. Paste prompt → **Save**.

---

## Automation 4 — RA2 Test Coverage

| Field | Setting |
|-------|---------|
| **Name** | RA2 Test Coverage |
| **Description** | Nudge for thin Unity verifiers / EditMode tests — not vanity coverage %. |
| **Trigger** | Prefer same PR triggers as Code Review; alternate: schedule weekly |
| **Repo** | `Nadomas/ranalog-ra2` |
| **Tools** | **Comment on PRs** (if PR-triggered) |

### Prompt (copy-paste)

```
You are the RA2 Analog Test Coverage nudge for Nadomas/ranalog-ra2 (Unity).

When reviewing the PR or scheduled diff:
- If the change adds gameplay rules, combat/win logic, assembly/validation, or net authority paths without a Play Mode *Verifier.cs, EditMode test, or scripts/ai smoke path: comment asking for a thin verifier or EditMode test before closing the matching docs/ai/TASK_QUEUE.md item.
- Prefer patterns consistent with existing *Verifier.cs under Assets/Runtime (PhysicsTest / Robot).
- Do NOT demand line-coverage percentages, UI snapshot farms, or golden image suites.
- Docs-only / rules-only / scripts-only changes: no comment needed.
- Do not implement large test frameworks. Do not notify the human unless this gap blocks a milestone or leaves verify broken after retries.
```

### UI remaining clicks

1. Trigger: PR opened/pushed (same as Code Review) **or** weekly cron if you prefer digest mode.  
2. Enable **Comment on PRs** for PR mode.  
3. Paste prompt → **Save**.

---

## Prefill JSON (agent / power users)

Canonical shapes used when opening the Automations editor from chat. Field names must match Cursor’s workflow proto (unknown trigger keys are silently dropped).

### Watchdog

- Trigger: `cron` with `0 * * * *`
- `gitConfig.repo`: `Nadomas/ranalog-ra2`
- `gitConfig.branch`: `agent/ai-pipeline`

### Build Guardian

- Trigger: `git.push` scoped to repo `Nadomas/ranalog-ra2` (set branch filter in UI if prefill omits glob)

### Code Review / Test Coverage

- Trigger: `git.pullRequest` with actions `GIT_PULL_REQUEST_ACTION_OPENED` and `GIT_PULL_REQUEST_ACTION_PUSHED`, repo `Nadomas/ranalog-ra2`
- Action: `prComment`

If a prefilled trigger shows **Configure trigger**, re-select the trigger in the UI from the tables above — do not Save a broken card.

---

## Related docs

| Doc | Role |
|-----|------|
| [`AI_WORKFLOW.md`](AI_WORKFLOW.md) | Pipeline loop, autonomy, Appendix B source prompts |
| [`CURSOR_PROJECT_SETUP.md`](CURSOR_PROJECT_SETUP.md) | Project UI (separate from Automations) |
| [`TASK_QUEUE.md`](TASK_QUEUE.md) | Executable next work |
| [`DEFINITION_OF_DONE.md`](DEFINITION_OF_DONE.md) | Verify bar |
| [`BLOCKERS.md`](BLOCKERS.md) | Active stops |

---

*Created for Cursor Automations handoff. Update when trigger UI labels or repo default branch policy change.*
