## Entry 884 — 2026-07-17 19:18 PDT — PHASE-12 Step 12 — the adversarial review found 6 real defects (incl. a spec misreading I shipped); all fixed

The find→verify adversarial review over the whole P12 diff (5 spec-first finders × the diff dimensions +
adversarial verifiers, a Workflow fan-out; prior phases found ~7 each). It returned 11 CONFIRMED findings
deduplicating to **6 distinct real defects** — every one verified against the spec and reproduced via the CLI.
Fixed all 6, each with a golden/test.

1. **MOVE SPACE/SPACES into a DYNAMIC LENGTH item gave length 0, not 1 — a spec misreading I shipped in wave 2.**
   My wave-2 MoveEmitter special-cased figurative SPACE → `""` on a mis-read "space-fill sets length 0". The
   verifier proved NO such rule: §8.3.3.6.4 GR3b — a figurative constant other than `ALL literal` sends ONE
   character, so MOVE SPACE stores a single space (length 1). The special case was also UNNECESSARY (a zero-length
   literal `""` is a `BoundStringLiteral`, already length 0 via the general path). Deleted it; SPACE now flows
   through `DynStore(AsString(source), limit)` = `new string(' ',1)` (length 1). This is exactly the class of error
   the review exists to catch — a plausible-but-wrong behavior that my own goldens did not exercise.
2. **Figurative VALUE on a DYNAMIC LENGTH item initialized to length 0, not 1** (the same root, ValueInitializer):
   `VALUE SPACE`/`ZERO`/`HIGH-VALUES` → length 1 (§8.6.4 initial length is MOVE-like, §13.18.63.4 GR7; §8.3.3.6.4
   GR3b). Now returns the one-char `FigurativeInitializer` fill, not `""` (the no-VALUE case stays `""`, §8.6.4).
3. **§13.16.3 SR18 omitted GLOBAL (and PROPERTY)** — `01 D PIC X DYNAMIC LENGTH … IS GLOBAL` compiled clean. GLOBAL
   is decoded post-build (CallBindExternalAndGlobal), so it escaped the allowlist; added `hasGlobal`/`hasProperty`
   flags in the clause loop and to the SR18 rejection (COBOLNET1563).
4. **FUNCTION BYTE-LENGTH of FLOAT-BINARY-32/64 folded to 1** — the new 2014 usages were missing from
   `DataItem.ElementaryByteWidth`; the `_` default gave `ElementaryImageWidth` (→ Math.Max(1,·) = 1). Added
   binary32→4, binary64→8 (+ the non-support formats' pinned widths 8/16 for exhaustiveness).
5. **PIC X(01)/N(001) DYNAMIC LENGTH falsely rejected 1561** — the SR1 check string-matched the RAW picture against
   a `"X"|"N"|"X(1)"|"N(1)"` set, so an explicit-count-1 spelling (`X(01)`) failed. Replaced with the regex
   `^[XN](\(0*1\))?$` (accepts every count-1 form, still rejects `XX`/`X(2)`/`A`/`9`).
6. **>>PROPAGATE §7.3.21.3 SR1 (not within a compilation unit) is not enforced** — a pre-parse line-based stage has
   no unit-boundary awareness. Documented as a known limitation in the processor (a placement-diagnostic follow-up),
   not a silent mis-compile of well-placed source.

New locks: `tests/conformance/2014/dynamic_length_figurative` (MOVE SPACE/ZERO → length 1, VALUE SPACE → length 1,
`""` → length 0, PIC X(01)); the `float_binary` golden extended with BYTE-LENGTH (B32BYTES=00004, B64BYTES=00008);
`DynamicLengthTests` +6 (GLOBAL 1563, count-1 accepted ×3, count>1 rejected ×2). The review REFUTED 0 — every
surfaced finding was real (the finders were well-targeted; the SR18/BYTE-LENGTH/figurative issues were each found by
two independent finders). Full battery green + FULL legacy guard (NIST 353 MATCH — these fixes are greenfield-only,
Cobol.Net.Compiler). Next: Step 13 — phase close (merge to main).

