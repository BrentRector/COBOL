## Entry 913 — 2026-07-19 14:02 PDT — Review-fix: main-program GOBACK RAISING ignored (§14.9.18.4 GR3) + review batch 2 verdicts folded

**The GOBACK GR3 fix (review C13, confirmed):** §14.9.18.4 GR3 — a GOBACK in a program NOT under the control of
a calling runtime element "operates as if executing a STOP statement … A RAISING phrase, if specified, is
ignored." The emitter staged the RAISING unconditionally (including the checking-off FATAL termination arm), so
a main-program `GOBACK RAISING EXCEPTION <fatal-ec>` killed the run unit. Fix: the staging is now
`__asCalled`-gated in `EmitGoback` — exactly the gate `EmitExitProgram` already had (the two are now
symmetric). Golden `2002/goback_raising_main` (CLI-probed: BEFORE prints, exit 0; before the fix — loud
termination). Probe lesson: the RAISING exception-name form requires the EXCEPTION keyword (`RAISING EXCEPTION
ec-name`) — a bare ec-name binds the identifier leg (0849). Legacy suite 167/167 (no exclusion needed — the
legacy engine agrees), CorpusRunner 286, unit 313, characterization 33 byte-exact (no snapshot carries GOBACK
RAISING — zero drift).

**Review batch 2 (10 agents, 0 errors — verdicts in the ledger §9):** all five worklist majors verified.
PAGE reserved-word table data CONFIRMED (r2023=false with a false provenance from the gen-reserved-words.ps1
PAGE filter; NO live acceptance hole — PAGE is a dedicated lexer keyword — the defect is table/matrix data
fidelity). Ref-mod negative-length CONFIRMED as a duplicate of C14 (+ a new increment: ExceptionState.cs:227
still cites §8.4.2.3c — the §8.4.3.3.4 correction missed it; fix SpliceInto + WriteFill too). EC-DATA-NOT-
FINITE/OVERFLOW and the EC-RANGE four CONFIRMED untracked (catalogued, no seam, no ledger row — spec anchors
recorded). EXIT SECTION doc-vs-impl CONFIRMED (ControlFlowBinder:102 BoundUnsupported vs the docs' pc-move
claim; §14.9.14 F4 GR7).

