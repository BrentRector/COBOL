## Entry 1709 — 2026-09-25 12:39 PDT — Drift rules surfaced to agents: generated index + per-file query, wired into skills and CI

Owner: "Are the rules of the various drift tests captured in the appropriate skills?" Measured: **no** — 217 `*DriftTests`
classes, 7 named in any skill or dispatch template. Each rule's single home is its test's `<summary>`, so agents met a
rule only when its test went red (the same day, `TestRepoDriftTests` caught a brand-new test that found the repo root
itself). Copying 217 rules into skills would make a second copy of each; instead ("make it so"):

- **`scripts/spec/drift_rules.py`** — derives from every drift test its rule (the class `<summary>`, references kept by
  name) and the repo paths it scans (`TestRepo.X(...)` segments + repo-relative literals). `python scripts/spec/drift_rules.py
  <path>` answers "which rules govern the file I am about to edit": SPECIFIC rules first (the test names the file, a
  directory ≥ 3 segments above it, or the file's basename in its rule), then the TREE-WIDE sweeps by name
  (`EcEmitter.cs`: 9 specific + 46 sweeps). No arguments regenerates **`docs/DRIFT_RULES.md`** (217 rows); `--check` fails
  when the index is stale OR a drift test states no rule (all 217 do).
- **Enforced where the other audits run:** the CI `audits` job, `build-local.ps1` and `build-local.sh`.
- **Wired into the skills:** `land-a-fix` §3 and `new-construct` §1 (ask before editing), `review` (check the diff against
  every specific rule), the implementer dispatch template, **MANDATORY-PRACTICES P11** (+ `check_practices.py` fails a
  spec that drops it), and `docs/DOC_INDEX.md`.

