## Entry 475 — COBOL.NET G2-0: the legacy differential-diff harness (+ an ISO-vs-legacy DISPLAY finding)

Starting G2 (the bound-tree + Place + data-model + native-numerics foundation rebuild, COBOLNET_DESIGN §16). Per
the advisor, the **first** thing G2 needs is a verification path stronger than hand-typed expected strings: the G2
checkpoint is "binds and DISPLAYs its data *byte-identically to the legacy*," and hand assertions re-derive exactly
the semantics under test (padding, scale, edited/overpunch images). So this commit stands up the differential
harness the design reserved for G5 — but scoped to **hand-picked, in-scope G2 fragments**, NOT the auto-discovered
364 NIST theories (those stay G5).

- **`ICompilerUnderTest`** + two impls in `tests/Cobol.Net.Tests.Conformance/`: `CobolNetCompiler` (drives
  `CompilerDriver`) and `LegacyCompiler` (the in-process `CobolSharp.Compiler.Compilation` oracle — the same path
  `EndToEndTestBase` uses, 364-NIST-green). The conformance project now references the legacy compiler+runtime
  (settled: §18 #7 / Q13 — the harness depends on the legacy as a differential oracle until the G8 cut-over). Both
  reference graphs deploy side-by-side (distinct namespaces + runtime DLLs); compiled programs each load only their
  own runtime.
- **`DataDisplayDifferentialTests`** — 18 fragments (alphanumeric/numeric/scaled DISPLAY; MOVE-then-DISPLAY;
  full-field-width), all green.

**The ISO-vs-legacy finding (owner directive: "always reference the ISO specification for correct behavior").** The
harness immediately caught a divergence: legacy `DISPLAY` of an alphanumeric operand **trims trailing spaces**
(`DISPLAY "[" WS-X "]"`, `WS-X PIC X(10)`="HI" → `[HI]`); COBOL.NET emits the full field → `[HI        ]`. The ISO
spec is unambiguous — **§14.9.11.4 GR1** ("the content of each operand … transferred … in the order listed") +
**GR6** ("the size of the sending item is the sum of the sizes associated with the operands") — so COBOL.NET is
**spec-correct** and the legacy trim is a **non-conforming quirk**. It survives in the legacy precisely because the
guard's `normalize()` strips per-line trailing spaces before diffing, so NIST never exercises it. Resolution:
  1. COBOL.NET `DISPLAY` follows the spec (full field width) — the current emitter is right; do NOT copy the quirk.
  2. The harness compares on the **NIST acceptance basis** — per-line trailing-trim, matching `guard.sh normalize()`
     — so the legacy stays a sound oracle for everything NIST validates (`AssertSameAsLegacy`).
  3. Where the quirk shows through normalization (internal trailing spaces, full-field-width tests), the case is
     pinned to the **spec** value with the §14.9.11.4 citation (`AssertSpec`), not the legacy.

This is the load-bearing lesson for the G5 differential harness: the legacy is a convenience regression net, not
authoritative — where it diverges from the spec, the spec wins and the quirk is documented (memory
`feedback_use_the_spec` broadened to cover behavior, not just grammar). Also surfaced, deferred to the data-model
slice: figurative `ZERO` in a numeric VALUE emits `ZEROL` (a real COBOL.NET gap — figurative constants in VALUE).

Verified: full-solution build clean Debug (0/0, warnings-as-errors); new Unit (3) + Conformance (18) green; legacy
oracle untouched. RESUME AT → G2 data model: nested `record struct` + `T[]` OCCURS + composed VALUE initializers +
the richer `DataBinder` (`ByName` multimap, OCCURS dims, INDEXED-BY, figurative-constant VALUE) + `ReferenceResolver`
→`Place`, with the bound tree (`BoundProgram` + `StatementBinder` + a loud `BoundUnsupported` node replacing the
silent `// TODO` no-ops that violate §1.4), building emitters into the §17 §2.2 decomposed structure.

DEVLOG 475.

