## Entry 966 — 2026-07-21 22:00 PDT — Wave D: CC-directives-inside-COPY (ISO §7.2.1) — the merged interleaved text-manipulation driver

**Track 2 of the Wave-D residue (owner: "do them all in sequence").** Closed the defect that
conditional-compilation directives INSIDE a copybook were never processed: the pipeline ran CC (Step 2) BEFORE
COPY (Step 1), so a copybook `>>DEFINE`/`>>IF`/`>>EVALUATE` survived COPY expansion and hit the lexer as a stray
token (**verified firsthand:** a copybook `>>IF` → `COBOL0001: unexpected '>'`).

A 6-agent scout mapped the pipeline + blast radius; I confirmed the two invariants firsthand (a main `>>IF` gates
a COPY; a **false-branch missing copybook must not error** — today it works because CC blanks the false-branch
COPY line before COPY runs). That last invariant is why a simple order-swap (COPY-first) is UNACCEPTABLE — it would
expand the false-branch copybook and raise a spurious CBL3620. So the **interleaved driver** (the owner's recorded
direction) is required. Two-pass (CC→COPY→CC) also fails — pass 1 consumes the main `>>DEFINE` before the copybook
`>>IF` in pass 2 can see it (my primary test case).

**The merged driver** (design SSOT `docs/rearchitecture/DESIGN-cc-in-copy.md`):
- `ConditionalCompilationProcessor` refactored into a `Run` holding ALL directive state (defines / IF-EVALUATE
  stack / FlagScanState / evaluator). `Render()` accumulates consecutive emitting non-directive lines into a block
  and flushes (COPY-expanded when interleaving) at each directive/omitted-line boundary — a COPY statement always
  lies wholly within one emitting block, so multi-line COPY REPLACING + mid-line COPY ride the copybook engine's
  char scan. An emitting COPY is expanded and its copybook fed back through the SAME `Run` (shared state across the
  boundary — a copybook `>>DEFINE` is visible to following source, Step-2 encounter order); an omitted COPY is
  DROPPED (never expanded). `emitting` for a boundary directive is computed AFTER the flush so a copybook
  `>>DEFINE` in the just-flushed block is visible.
- `CopyProcessor`: extracted `ResolveOneCopy` (one non-recursive COPY resolve) shared by `ExpandCopyStatements`
  (legacy, byte-identical) and the new `ExpandCopiesOneLevel` (the COPY half, which calls back into the CC `Run`).
- `Frontend.Preprocess`: the two separate CC + COPY calls become ONE `ProcessWithCopy`. Legacy `Process`/COPY
  stay byte-identical (the frozen oracle + `preprocess` CLI + direct tests use them).

**⚠ Two test failures were bad TEST DATA, not merge bugs:** `>>DEFINE USE`/`MODE` — USE and MODE are RESERVED
words, so the CCE evaluator correctly rejected them as compilation-variable names (COBOLNET1619). Renamed to
USEIT/XMODE.

**Gate (high blast radius, shared preprocessor):** characterization **33/33** byte-exact + the existing CC/COPY
unit tests **26/26** (refactor byte-identity) + `CopyConditionalProcessorTests` **8/8** (the merged behavior) +
the `cc_in_copy` conformance golden (copybook `>>IF` selecting a VALUE → `2`, run+compared) + GreenfieldOnly.
**GnuCOBOL external differential before/after: +2 FIXES (`listings:3788`/`3884` WE_REJECT_THEY_ACCEPT →
AGREE_ACCEPT), 0 REGRESSIONS.** Legacy guard ALL GREEN + full greenfield Conformance 3806/0. **⭐ TRACK 2 COMPLETE.
NEXT = Track 3 (PERFORM Format-3 RUNTIME interceptor — the pc-RANGE architecture).**

