# BLOCKERS — ra2-analog autonomous pipeline

> Active blockers that stop the coordinator from continuing the next unfinished task in [`TASK_QUEUE.md`](TASK_QUEUE.md).  
> Keep this file short and honest.

---

## Active

**None.**

---

## What IS a blocker

Log an entry here (and stop autonomous proceed) when:

| Category | Examples |
|----------|----------|
| Product decision required | Win-condition change, scope cut, engine/package freeze that needs human sign-off |
| Missing credentials / access | Git remote auth failure, secret the agent must not invent |
| Hardware / tooling gap that cannot be worked around | Unity Editor missing and cannot be located; Dedicated Server module required and not installable without user |
| Unity MCP / Editor unavailable for a task that **requires** Play Mode or MCP verification, and batchmode scripts also fail |
| Conflicting source-of-truth | GDD vs roadmap contradiction that changes what “done” means |
| Hard gate FAIL | STAGE 2-class physics+MP proof regresses to NO-GO — do not continue Stages 3–14 content/polish |
| Repeated verify failure | Same task fails Definition of Done after **3** fix attempts with no new hypothesis |

Each active entry must include: **id**, **date**, **task id**, **why blocked**, **what human must do**, **unblocks when**.

---

## What is NOT a blocker

Do **not** open a blocker for:

- Preference questions (“which name looks nicer?”) when project docs already decide
- Asking the user for the next task while unfinished work remains in `TASK_QUEUE.md`
- Missing optional polish / UI chrome marked out of scope for the current spike
- Unity Dedicated Server Win module missing when headless Standalone player smoke is an accepted substitute (see Stage 2 residual notes)
- Cloud agent inability to open the Editor — hand Local Unity verification to the local agent / scripts instead
- Flaky one-off console noise that is unrelated to the change and already known

---

## Lifecycle

1. Coordinator detects stop condition → write Active entry → notify human (see [`AI_WORKFLOW.md`](AI_WORKFLOW.md)).  
2. Human resolves or re-scopes → mark entry **Resolved** with date + note (move under Resolved or delete if trivial).  
3. Resume from the same TASK_QUEUE item unless the blocker changes the queue order.

---

## Resolved (recent)

| Id | Resolved | Note |
|----|----------|------|
| — | — | (none yet for this pipeline file) |

---

*Update this file whenever autonomy stops for a real blocker. Do not invent blockers to ask for work.*
