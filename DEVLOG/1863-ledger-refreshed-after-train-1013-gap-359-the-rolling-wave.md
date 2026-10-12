## Entry 1863 — 2026-10-04 10:30 PDT — Ledger refreshed after train 1013 (GAP 359); the rolling wave takes a per-group model

Refreshed in the same turn as train 1013's landing report, as entry 1861 requires: `gen_ledger.py` back-filled the two missing
points itself (114 points), `ledger-in-flight.md` names trains 1011 to 1013, the dropped PB322 cluster and wave 1014, and the page
was republished (version 89, accepted on the first call). `wf_rolling_wave.js` now honours a group's own `model`
(`model: g.model || IMPL_MODEL`), so one wave can run a mechanical group on Sonnet and a design-heavy one on Opus (owner
2026-10-03: size the model to the work); `SKILL.md` documents it. Wave 1014 is the first to use it.

**Files:** `docs/rearchitecture/evidence/ledger-trend.json`, `docs/rearchitecture/evidence/ledger-in-flight.md`,
`.claude/skills/workstream/templates/wf_rolling_wave.js`, `.claude/skills/workstream/SKILL.md`. No code changed, no
diagnostic code used.

