## Entry 967 — 2026-07-21 22:15 PDT — Session close: Wave-D residue Tracks ①② DONE (>>COBOL-WORDS + CC-in-COPY); Track ③ (PERFORM F3 runtime) design-ready, stopped per owner

**Session summary (owner: "do them all in sequence" over the 4 Wave-D residue tracks).** Landed the first two,
both fully gated + pushed on `phase-13-grammar-batch`:
- **① `>>COBOL-WORDS` (§7.3.10) — COMPLETE** (DEVLOG 962–965; commits `6c0c007a`/`b854fa9e`/`107dc385`/`10042f7f`).
  All four options (EQUATE/UNDEFINE/SUBSTITUTE/RESERVE) for reserved, context-sensitive, AND intrinsic-function
  words; the owner-directed post-lex token rewriter + composed `ReservedWordSet` + map-aware lexer + binder
  intrinsic-synonym resolution; SR1–SR5; COBOLNET0900 gate + COBOLNET1623. `DESIGN-cobol-words-directive.md`.
- **② CC-directives-inside-COPY (§7.2.1) — COMPLETE** (DEVLOG 966; `c3377315`). The merged interleaved
  text-manipulation driver (`ProcessWithCopy`) — copybook directives processed, false-branch COPY never expanded,
  legacy byte-identical. GnuCOBOL +2 fixes/0 regressions · legacy guard ALL GREEN · greenfield Conformance 3806/0.
  `DESIGN-cc-in-copy.md`.
- **③ PERFORM Format-3 RUNTIME interceptor — DESIGN-READY, NOT STARTED (stopped here per owner).** The binder is
  already complete (`BoundExceptionPerform`); the runtime interceptor rewires the byte-critical EC runtime every
  program's exception handling flows through — the owner chose to land ①② and resume ③ FRESH next session rather
  than rush it (it was deliberately staged behind COBOLNET0899 in the grammar batch for exactly this reason). Full
  resume groundwork is in plan §0 (the ▶ ③ block: binder done, `EmitExceptionPerform` stub, the dispatch protocol +
  `EcDispatchExpr`/`__RunUse`/`ResumeSignal`/`ExceptionEngine` seams, the §5 10-seam plan, the 5 staged GAPs). A
  5-agent runtime scout ran (`wf_ddb8dd1e-0f7`).
- **④ §24 fix-queue → Wave I merge** — pending after ③.

**Process notes (honest friction):** two `>>COBOL-WORDS` test failures + two CC-in-COPY test failures were bad
TEST DATA (reserved words USE/MODE as compilation-variable names → correct COBOLNET1619), not code bugs. A first
CC-in-COPY guard run FALSE-RED'd 2 NIST tests under JOBS=32 parallel cold-start — verified serially, re-ran clean
(don't `taskkill dotnet.exe` before a guard). The long background gates (full guard ~4 min, greenfield Conformance
~19 min, GnuCOBOL ~12 min) made for slow turns — batch them / avoid per-increment full-Conformance.

