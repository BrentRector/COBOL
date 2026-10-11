## Entry 301 — Phase 5 scope decision: exclude obsolete modules; DB/RW optional → "operational" at 350

With every self-contained CLEAN test across SM/OBSQ/IC/SQ/RL now baselined (DEVLOG 296–300), the only NIST
programs left are (a) a small in-scope runtime tail and (b) whole modules with no current support. The latter
is a scope question the roadmap reserved for the user; asked, and the decision is **"exclude obsolete; defer
DB/RW":**

- **Excluded as out-of-scope (obsolete / non-standard)** — formally not part of the "operational" target for
  modern-COBOL conformance, documented like the flagging-`M` exclusion class:
  - **CM** (Communication, 9) — `CD`/`SEND`/`RECEIVE`; OBSOLETE, removed in COBOL-2002.
  - **SG** (Segmentation, 13) — `SECTION` segment-numbers / overlay; OBSOLETE, removed in COBOL-2002.
  - **OBNC / OBIC** (5) — obsolete-feature NC/IC variants.
  - **EXEC85** (1) — the EXEC85 driver (non-standard).
- **Deferred optional stretch** — in-scope only as future enhancements, not required for "operational":
  - **DB** (Debug, 15) — `USE FOR DEBUGGING`, `DEBUG-ITEM`, debugging lines (optional COBOL-85 module).
  - **RW** (Report Writer, 6) — `RD`, `GENERATE`/`INITIATE`/`TERMINATE` (sizable optional module).

**"Operational" is therefore reached now:** 350 baselined + the NO_OUTPUT producers/builders (in chains) +
the flagging-`M` modules (no CCVS report) + this obsolete-exclusion class accounts for every NIST program
except the genuine **in-scope remaining work**, which is the next-session target:
- **RL runtime bugs** — RL106A/119A/205A/213A (FAIL\*), RL111A (real FAIL\* + a `D-CLOSE-FILES` infinite-
  recursion stack overflow), RL208A (the known `RelativeFileHandler.Rewrite` pads-varying-to-max bug).
- **IC inter-program features** — IC233A/234A (`USE GLOBAL AFTER ERROR` + **GLOBAL FILE inheritance** into a
  contained program — extend the DEVLOG-236 GLOBAL-data mechanism to FILE SECTION), IC227A/235A (EXTERNAL
  file/data), IC114A (file-chain + binary-report).

No code change this entry — a scope/finish-line decision recorded in resume-prompt, memory, and the guard
comment. Guard unchanged: 1000 unit / 347 integration / 350 NIST, ALL GREEN.

