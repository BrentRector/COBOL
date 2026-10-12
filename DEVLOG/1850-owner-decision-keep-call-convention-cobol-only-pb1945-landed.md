## Entry 1850 — 2026-10-03 12:51 PDT — Owner decision: keep >>CALL-CONVENTION COBOL-only (PB1945 landed)

The owner decided that `>>CALL-CONVENTION` stays COBOL-only unless a .NET mapping is added, and that a separate .NET mapping
may not be required at all. This closes PB1945, the decision note opened after the cobc probes of DEVLOG 1848 and 1849 showed
that GnuCOBOL 3.1.2 and 3.2.0 also accept EXTERN, STDCALL and STATIC. `docs/CONFORMANCE.md` section 3 now states both facts
beside PB1383's COBOLNET1911 rule: the GnuCOBOL names and the owner's decision. The note says when to reopen: only if a real
program needs one of the three names, and then first ask whether the existing CALL resolution (a registered WiseOwl COBOL
program or a sibling module) already covers it, since no new mapping may be needed.

**Files:** `docs/CONFORMANCE.md`, `kb/Work/PB1945.md` (status landed). No code changed, no diagnostic code used.

