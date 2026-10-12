## Entry 295 — Full-NIST-suite audit + forward roadmap (resume-prompt rewritten)

Audited all 459 programs in `tests/nist/programs/` to scope what "complete the full NIST suite operational"
actually requires, and rewrote `resume-prompt.md` from a (now-historical) file-I/O/IX/ST narrative into a
prioritized roadmap. Survey findings (`scripts/run-suite.sh` per suite):

- **Quick baseline wins (~22, little/no code, need vacuous-verify + chain-order):** SM104A/105A/205A (CLEAN,
  un-baselined); OBSQ1A/3A/4A/5A (obsolete-sequential, all 4 CLEAN); and **IC has 20 CLEAN but only 4
  baselined** — 15 candidates (IC101A/103A/108A/112A/115A/201A/206A/207A/209A/213A/216A/222A/223A/226A/237A).
  ⚠ IC "CLEAN" is vacuous-prone: a caller that CALLs a separately-compiled callee can no-op under the guard's
  one-.cob→one-.dll build while still self-certifying PASS — which is why the earlier session deliberately
  baselined only 4. Each needs the inter-program transfer verified before baselining.
- **SQ/RL un-surveyed tail:** ~24 SQ + ~13 RL neither baselined nor excluded — re-survey + the proven
  file-I/O runtime-correctness method.
- **One known open file-I/O bug:** RL208A (variable-record REWRITE pads to max instead of actual length).
- **IC genuine work:** multi-program compile/link for caller+separate-callee (21 NO_OUTPUT callee halves +
  4 COMPILE_FAIL + 2 FAIL\*); IC233A/234A USE GLOBAL declaratives; IC235A/227A/114A EXTERNAL+file-I/O.
- **Whole unimplemented modules (all COMPILE_FAIL — large, several OBSOLETE; need a scope decision):**
  DB Debug 15, RW Report Writer 6, SG Segmentation 13 (obsolete COBOL-2002), CM Communication 9 (obsolete
  COBOL-2002), OBNC/OBIC 5, EXEC85 1. Recommended default: exclude CM/SG/OBNC/OBIC/EXEC as out-of-scope
  obsolete/non-standard; treat DB + RW as optional stretch features — confirm with the user before either
  implementing or formally excluding.

Coverage today: 459 present / **299 baselined** / 309 running. No code change this entry. The resume prompt
now carries the coverage table, the 5-phase roadmap, the process rules, and an architecture quick-reference.

