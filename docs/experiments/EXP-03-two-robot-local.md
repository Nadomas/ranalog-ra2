# EXP-03 — Two-Robot Interaction (local)

> **Status:** local thin PASS  
> **Date:** 2026-08-21  
> **Net / detach:** out of scope

## Method

PhysicsTest: driven `Robot_A` (S1-02/S1-03) vs passive `Robot_B` obstacle.

Evidence:

- S1-01 collision smoke (head-on AutoDrive) — prior PASS
- S1-02 drive verifier `push_ok` — prior PASS
- S1-03 joint verifier: wall + push B, **10/10** runs with `push_ok=True`, `B_delta≈10`, no NaN/teleport/explosion; hinge stayed attached

## Qualitative

Contacts are readable: A can displace B; impacts do not fling jointed wheel through geometry. Sliding friction is high (known), which softens “grip” feel but does not block contact readability for STAGE 1.

## Verdict

Local EXP-03 **PASS** for demo robots. Clips / fairness scoring optional later; not required for STAGE 1 exit.
