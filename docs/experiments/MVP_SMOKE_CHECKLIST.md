# MVP Smoke Checklist — Thin touchable loop

> Living checklist toward “пощупать” GDD MVP.  
> Updated: 2026-09-21. Critical desync class: **empty** (known thin residuals below).

## Blessed Play menus (PhysicsTest)

| Step | Menu | Expect |
|------|------|--------|
| Construction admit | `… (S4-01 Construction Validation)` | Dual sample admit + CoM |
| Configure | `… (S5-01 Configure Wiring)` | Preset rebind + JSON |
| Seamless loop | `… (S6-01 Seamless Loop)` | Design↔Configure↔Test |
| Combat win | `… (S8-01 Combat Immobility)` | Disable → Immobilized |
| UDP match | `… (S9-01 Match UDP Lobby)` | Lobby→admit→fight→MatchOutcome both sides |
| Disconnect | `… (S9-02 Disconnect Policy)` | Goodbye → DisconnectForfeit |
| Results stub | `… (S10-01 Results Stub)` | Same as S9-01 console contract |
| Results persist | `… (S10-02 Results Persist)` | JSON file + thin view |
| MVP glue | `… (S11 MVP Loop Glue)` | Workshop → local + MP admit |
| Workshop chrome | `… (S11 Workshop Chrome)` | IMGUI Design/Configure/Test |
| Workshop unified | `… (S11-06 Workshop Unified Chrome)` | Design polygon + Configure bindings + admit |
| **MVP Windows player** | `Tools/RA2/Build MVP Windows Player (S11-07)` | `Builds/Ra2MvpPlayer/Ra2MvpPlayer.exe` |
| Chassis polygon | `… (S4-02 Chassis Polygon Editor)` | ≤16 edit + admit |
| Binding groups | `… (S5-02 Binding Groups)` | Drive/Turn cycle + JSON |
| E2E loop | `… (S11 E2E Loop)` | Design→Fight→Results one scene |

## Manual touch path (local)

1. Build **MVP Windows Player (S11-07)** → run `Ra2MvpPlayer.exe`.  
2. Design → Nudge+X → Configure → Cycle → Test → WASD → Prepare Admit → **Local Fight 1v1**.  
3. Or Editor: **S11-06 Workshop Unified Chrome** → Play.  
4. Build **S9-02** → Play; confirm forfeit log (no hung fight).

## Known issues / residuals (non-blockers)

- Product UI Toolkit (still IMGUI in player)  
- UDP MP not wired into player shell (local fight only)  
- Freehand polygon gizmo / full composite binding matrix  
- Weapons beyond functional disable (Stage 7 depth)  
- Ready/lobby UX stub (S09-T03) still thin/auto  
- No reconnect; hard drop without goodbye covered by S9-04 heartbeat  
- Custom UDP not frozen; Dedicated Server Win module optional residual  
- No economy / catalog / career  
- Match history list UI

## Critical desync class

None open for thin path: host publishes outcomes; clients display only; disconnect ends match with forfeit.
