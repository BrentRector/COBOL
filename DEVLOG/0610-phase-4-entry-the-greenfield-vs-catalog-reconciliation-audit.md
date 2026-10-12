## Entry 610 — 2026-07-04 22:19 PDT — Phase 4 ENTRY: the greenfield-vs-catalog reconciliation audit (the ratified half-session first step)

⛔🎉 The OO brief set is closed (604-609); Phase 4 (the M2 residual catalog) OPENS with its ratified
first step — the greenfield-vs-catalog reconciliation audit (`docs/COMPLETION_ROADMAP_COUNCIL.md` Phase 4:
"the §3 checkmarks are legacy-era; several items already landed greenfield … add a greenfield-status
column so waves are sized against truth").

**Method.** A 10-agent workflow (one auditor per catalog subsection §3.1-§3.9 + a cross-checking
synthesizer; ~636k tokens, 9 min) verified EVERY M2/M3/M4 catalog item against the GREENFIELD tree
(`src/Cobol.Net.*`, the manifests, DEVLOG 600-609) — NOT the legacy `src/CobolSharp.Compiler`. The
synthesizer re-grepped the 8 riskiest LANDED + all 3 NOT-STARTED claims: **no status mark overturned**
(two cosmetic fixes only — the OO golden count is 24 not 25; INSPECT BACKWARD gates at 0845/2023).

**The finding that reshapes Phase 4:** the catalog's ☑ "done" marks are systematically LEGACY-ONLY
mirages. Every §3.2 M2-DATA row (BINARY-CHAR, FLOAT-*, NATIONAL, BIT, POINTER) and both §3.1 UDF
invocation rows are catalogued done but stage LOUD (COBOLNET0899) in the greenfield — the work lived in
the retired byte engine and was never ported. Conversely two rows are stale-LOW: M2-PROC-4 (exception
handling) is catalogued OPEN but fully LANDED (DEVLOG 577 + the OO-EC waves), and the whole OO umbrella
(catalogued ◐) is effectively done (24 goldens, conformance 1592/1592).

**Census (38 rows — 30 items, M2-OO-1 expanded into parent + 8 sub-features):** LANDED 13 · STAGED-LOUD
12 · PARTIAL 9 · NOT-STARTED 3 · OBSOLETE-by-design 1 (VALIDATE). Per-track remaining work: (a)
national/boolean 2 primary +3 shared legs · (b) pointers/ALLOCATE 2+1 · (c) UDF/prototypes 4 (the
largest cluster) · (d) sharing/lock/retry 2+2 · (e) arithmetic 2 (a lowering bugfix + a golden
rebaseline — the smallest) · (f)/(g) 0 each (no catalog item maps) · Phase 5 intrinsics 2 · Phase 6 4 ·
Phase 7 2.

**Landed:** `docs/PHASE4_RECONCILIATION.md` (the authoritative greenfield-truth table + per-track wave
sizing; DOC_INDEX row added), a truth banner atop the catalog's §3 pointing to it (the catalog's legacy
marks are NOT edited in place — the reconciliation doc IS the flip-to-truth, kept in sync as tracks land).

**NEXT (this drive):** the parallel disjoint tracks, sized against truth. Starting with (e) arithmetic
(smallest — the rounded_mode_prohibited move-COMPUTE checked-store leak + the options_paragraph golden
rebaseline) to validate the Phase-4 machinery, then the larger reservoirs (a national/boolean, c UDF
invocation, b pointers on ManagedPointer).


