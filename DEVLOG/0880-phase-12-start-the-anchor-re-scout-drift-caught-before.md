## Entry 880 — 2026-07-17 15:40 PDT — PHASE-12 START — the anchor re-scout (drift caught before coding) + wave 1: the live FLOAT trio matrix-locked

PHASE-12 (M3 / COBOL-2014 deltas) opens on branch `phase-12-m3-2014`. Baseline green at HEAD `91ff7301`:
**3521 conformance · 301 unit** (both re-run on the fresh build).

**The re-scout (methodology first, per the P10/P11 lesson `feedback_persist_anchor_rescout`).** The PHASE-12 plan
was authored 2026-07-07 — BEFORE P8/P9/P10/P11 landed — so its code-state and several spec anchors had drifted
badly. Ran a 6-scout + adversarial-verify workflow re-verifying every plan anchor against `specs/ISO_COBOL.md` and
the current tree; persisted the corrected reference as `docs/rearchitecture/PHASE-12-scout-notes.md`. **Every one of
the 13 completed verify agents returned UPHELD or MODIFIED — not one scout claim was refuted; the plan drifted in
every checked case.** The load-bearing catches (this is the P11-CONCATENATE class of finding — a plan premise that
contradicts the spec):
1. **IEEE-754 fidelity INVERTED.** The plan called backing FLOAT-BINARY-128 by `double` "a conforming implementor
   choice per §13.18.60.4 GR13". Re-checked directly against the spec: GR13/GR21 make ONLY the FLOAT-SHORT/LONG/
   EXTENDED trio implementor-defined; **GR14-18 (spec lines 22826-22867) PIN** FLOAT-BINARY-32/64/128 to ISO/IEC
   60559:2020 binary32/64/128 and FLOAT-DECIMAL-16/34 to decimal64/decimal128. Decision for Step 7:
   FLOAT-BINARY-32→`float`, FLOAT-BINARY-64→`double` (exact/conforming); FLOAT-BINARY-128 + FLOAT-DECIMAL-16/34 →
   **processor-dependent non-support** (Annex A.3 items 17/19), loud — never `double`-backed.
2. **PROGRAM-POINTER is DONE as a 2002 feature** (P10 Step 7) — the plan's "2014" + `{is2014()}?` Step-8 gate would
   REGRESS it. FUNCTION-POINTER surface also done (staged loud 0899); only its runtime semantics remain.
3. **Diagnostic band 1540-1559 COLLIDES** — 17/20 codes live (high-water 1559, not the plan's 1538). Keep the
   1550/1551/1552 pointer earmark; DYNAMIC LENGTH + float take fresh codes from 1561+.
4. **`>>PROPAGATE` is LIVE in 2023**, not removed → introduction gate (~2002), no top-end span. Ownership conflict
   P9/P13/P12 flagged for resolution.
5. **TYPE TO is NOT a pointer form** — it is the TYPE clause's optional word (§13.18.57.2); the §7 table swaps the
   TYPE/TYPEDEF/SAME-AS citations. Registry is `Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs`, not the plan's
   path. `scripts/guard.ps1` does not exist. Battery baseline 3521/301, not the plan's stale 3166/281.

Direct CLI probes confirmed the scout: the FLOAT trio compiles+RUNS at 2002 (`S=0021` from 10.5×2), FLOAT-BINARY-32
fails to parse ("no viable alternative"), and the E-picture is staged loud at COBOLNET0899.

**Wave 1 (Steps 1-3, no grammar — this commit).** OCCURS DYNAMIC's `occurs-dynamic-2014` row was already active with
all 10 `dyn_*` enabled (Step 1 = verify-and-lock, no change). Flipped the three live FLOAT-trio rows
`usage-float-{short,long,extended}-2002` `pending`→`active`, corrected their descriptions (drop the stale
"silent-misbind"/"PENDING") and the §13.18.59→§13.18.60 citation (§13.18.59 is UNDERLINE; USAGE is §13.18.60). Step 3
needs no new program — `tests/conformance/2002/float_usage.cob` already exercises the whole trio. Reconciled the plan
doc: STATUS → `IN PROGRESS @ step 3` + a prominent RE-SCOUT CORRECTIONS banner pointing at the scout notes.
Verified green: **1975/1975** across `VersionMatrixTests | CorpusRunnerTests | Occurs*Tests` (the three trio rows now
assert compile at 2002/2014/2023 and COBOLNET0900 at 1985). Next: wave 2 = DYNAMIC LENGTH (§8.5.1.10 / §13.18.19).

