# S5-02 — Binding Groups UX (thin)

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S5-02 Binding Groups)`  
> **Date:** 2026-09-21

## Goal

Named Drive/Turn binding groups over control slots — apply/cycle bindings, overlap warn, JSON round-trip. Not full composite chrome.

## Code

| Piece | Role |
|-------|------|
| `RobotControlConfigurer` BindingGroup APIs | Drive/Turn groups, apply/cycle, overlap warn |
| `RobotBindingGroupsChrome` | Local-only IMGUI |
| `RobotBindingGroupsVerifier` | Apply → cycle → conflict → JSON bind check |

## Acceptance

- Apply Drive binding sticks on `forward_back`
- Cycle Turn binding changes `left_right`
- Same string on Drive+Turn → `group_binding_overlap` warn
- Bindings survive `RobotBlueprintSerializer` JSON round-trip
- Wiring conflicts remain empty after clear

## Pass log

```
[S5-02] VERIFIER_DONE pass=True apply=True/ drive=Up/Down cycle=True/ cycled=Left/Right turn=Left/Right conflict=True bind_json=True wires_ok=True clear_conflicts=True json_len=7087 status=cycle Turn=Left/Right conflicts=0
```
