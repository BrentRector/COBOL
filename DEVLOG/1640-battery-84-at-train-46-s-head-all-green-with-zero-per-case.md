## Entry 1640 — 2026-09-22 13:30 PDT — Battery #84 at train 46's head: ALL GREEN with zero per-case flips — the first battery in five to meet the bar without an attributed miss

**The run.** One `bash scripts/battery.sh` invocation, 1,777 s wall, in an isolated worktree pinned at exactly `ec095d120` (train 46, DEVLOG 1639). It never ran on main or in the shared checkout. The gitignored GPL GnuCOBOL corpus (38 `.at` files, plus the tarball) was copied in FIRST, so the external-population tests measured 1,323 cases rather than going red by absence. The script's own verdict line is `=== BATTERY: ALL GREEN ===`, exit 0.

| Leg | Result | vs #83 |
|---|---|---|
| `audit_code_citations --check` | 0 findings, 4,710 files scanned for phantoms | 4,656 |
| `audit_doc_citations --check` | 0 MISFILED, 0 ELIDED, 537 checked / 486 correct | 529 / 479 |
| `audit_evidence_supersession --check` | 0 UNMARKED (11 line-coordinate drifts, non-gating) | = |
| `audit_witness_loss --check` | 0 unexcused, 0 retired, 0 re-sited | new leg (PB959) |
| `CobolSharp.sln` Debug build | 0 warnings, 0 errors | = |
| Conformance (full, unfiltered) | **7823 / 7823** | +47 |
| Unit (full) | **28688 / 28688** | +45 |
| Characterization | 33 / 33 | = |
| guard witnesses + compiler watchdog | ALL GREEN | = |
| guard-fast NIST (shipped `cobol`) | **364 MATCH, 0 REGRESSION**, audit CLEAN over 376 | = |
| guard-fast legacy legs | 1203 / 1203 · 503 / 504 (1 skipped) | = |
| GnuCOBOL differential | 1,323 cases, **`=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===`** | #83 had 2 |

Differential totals are unchanged at 580 / 466 / 237 / 40. Equal totals are consistent with offsetting flips, so the evidence that nothing moved is the per-case line, not the totals. There was no NEW or REMOVED case, no case without a compiler verdict and no harness failure. **No baseline row was rewritten.**

**Both populations reproduce train 46's own whole-assembly gate exactly**: 7,823 and 28,688, on a different worktree and a different build.

**The bar.** #83's record required #84 to be ALL GREEN with 0 flips. It is, and it is the first battery in five to manage that: #80 had 2 flips, #81 had 3, #82 had 1 and #83 had 2, all attributed and licensed.

**The complement.** A zero-flip result is evidence only about what the corpus exercises, so I measured the fan-out rather than leaving a clean-looking silence:
- Train 46's two new codes, COBOLNET2215 and COBOLNET2216 (cluster A), fire on **zero** of the 1,323 corpus cases. So do COBOLNET2031 and COBOLNET0828, which cluster B newly reaches. The corpus is not evidence about them in either direction. Their evidence is their own goldens.
- COBOLNET0819, which B now also asks of a binary-family sender moved into a group, appears on 16 corpus cases. Every one holds its #83 verdict.
- E's widening of `evaluateSubject` to condition-1 turned no corpus reject into an accept.

**One observation, not an attribution.** The Conformance leg took 18 m 29 s against #83's 9 m 39 s on only +47 cases, and the whole run took 1,777 s against 893 s. The suite's growth does not explain that. Host contention from other workflow agents on the same 32 cores is the likely cause, but I did not measure it. It cost no verdict.

**Batch covered:** DEVLOG 1633–1639, meaning train 46 (seven clusters, twenty notes, GAP 2276 → 2266), registrars #9 and #10, the Opus 5.5 pin and the two orchestration rules. `git diff --stat c982251df ec095d120 -- src/ tests/` is 143 files, +4,135 / −984.

**The record.** Plan §0's BATTERY REFERENCE is rotated: CURRENT is #84, PREVIOUS is #83 (compressed), and #82 is dropped. One sentence is appended to `docs/rearchitecture/evidence/ledger-in-flight.md`. Nothing was filed in `kb/Work/`, because there was no red to attribute. Battery #85 at the next train's head carries the same bar: ALL GREEN with 0 flips.

⚠ **Owed and not done here:** the Conformance Ledger artifact refresh (`gen_ledger.py`, the trend point and the publish) for train 46's landing and this battery close.

