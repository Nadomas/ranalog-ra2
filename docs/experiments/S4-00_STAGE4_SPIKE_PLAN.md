# S4-00 — STAGE 4 Spike Plan (Construction Prototype)

> **Date:** 2026-09-21  
> **Status:** S4-01 **PASS**  
> **Depends on:** [STAGE3_EXIT.md](./STAGE3_EXIT.md) PASS (thin), STAGE 2 HARD GATE GO  
> **Principle:** thin construction validation on shared v1 blueprint — no catalog, no economy, no editor polish.

---

## 1. Goal

Prove that **player-built (or designer-authored) blueprints** can be validated with the same rules client preview and server admit use, including mass/CoM readout and RA2 attachment constraints, then spawn on the shared runtime.

## 2. Spike sequence (thin)

| ID | Hypothesis | Status | In scope | Out of scope |
|----|------------|--------|----------|--------------|
| **S4-01** | Construction validator rejects illegal builds; ≥2 valid robots spawn+drive; CoM applied | **PASS** | mass/CoM, wheel-on-axle, power/mass budgets, dual samples, Play verifier | Construction UI, catalog, snaps UX |
| **S4-02** | (optional) Save/load workshop blueprint round-trip under construction rules | deferred | covered enough by S3-02 + S4 samples validating | Editor chrome |

## 3. S4-01 success

- `RobotMassProperties` computes total mass + CoM from blueprint data.  
- `RobotBlueprintValidator` enforces chassis ≤16, wheel→motor axle, battery+electric budget, weight-class mass cap, rb/hinge budgets.  
- ≥2 distinct valid samples (different CoM).  
- Illegal: wheel-on-chassis, >16 pts, overmass, no Control Board — all rejected by admit path.  
- Spawn applies CoM to root Rigidbody; both samples drive without NaN.  
- No new packages; no Transform teleport.

## 4. Explicitly NOT in Stage 4 thin

- Full construction editor UX / polygon drawing UI  
- Component catalog / economy costs  
- Perfect geometric collision mesh validation  
- Freeze of mass class numbers

---

*Next after S4-01 PASS: Stage 5 thin Configure (rebinding wiring) or Stage 6 mode shell.*
