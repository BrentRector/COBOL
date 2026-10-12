## Entry 485 — COBOL.NET G4: the PC dispatcher (GO TO / PERFORM / fall-through) — the sequential-paragraph stopgap is retired

The control-flow backbone (COBOLNET_DESIGN §5), the biggest unlock for real NC programs (NC101A's 95 unsupported
statements were ~88 GO TO + EXIT). Paragraphs are no longer methods called in sequence (which double-executed a
performed-then-fallen-through paragraph — the known-latent bug); they are now **pc cases in one `__Dispatch(startPc,
exitPc)` method**, control by pc value:

- **GO TO p** → `__pc = idxP; break;`; **GO TO … DEPENDING ON sel** → a 1-based switch that sets pc, out-of-range
  falls through (ISO §14.9.20). **Fall-through** → `__pc = i+1`.
- **out-of-line PERFORM p [THRU q] [TIMES/UNTIL]** → a recursive **bounded `__Dispatch(idxP, idxQ)`** (the C# call
  stack is the return-address stack); the §5.1 pre-body `atExit` check returns when a named THRU exit paragraph
  falls off its end. **EXIT PARAGRAPH** → `__pc = myIdx+1`. STOP RUN/GOBACK → `StopRun` caught at `Main`.
- **Dead-code suppression** (§5.3): a statement list stops emitting after an unconditional transfer; a paragraph
  case emits its fall-through only if not already terminated. IF branches + inline-PERFORM bodies route through the
  same `EmitStatementList`, so a GO TO inside an IF works (the `break` exits the dispatcher switch).
- Bound tree: `BoundGoTo`/`BoundGoToDepending`/`BoundExitParagraph`/`BoundExitPerform`/`BoundNop`;
  `BoundOutOfLinePerform` now carries the resolved pc range; `BoundParagraph` drops its method name (pc = position).

**Collision fix (found by the regression net):** the dispatcher's `N`/`pc`/`Dispatch` collided with COBOL fields
named `N`/`pc`/etc. (`01 N` → `private static long N` vs `private const int N`). All dispatcher internals are now
`__`-prefixed (`__N`/`__pc`/`__startPc`/`__exitPc`/`__atExit`/`__Dispatch`) — COBOL data-names cannot contain a
double underscore, so the collision class is structurally eliminated (the design's NameAllocator reserved-set goal).

Verified: full-solution build clean Debug (0/0); Conformance 116 green (12 new control-flow differential tests: GO
TO forward/backward, GO TO DEPENDING in-range + out-of-range fall-through, fall-through, PERFORM single/THRU/TIMES/
UNTIL, EXIT PARAGRAPH — all byte-identical to the legacy) + Unit 3 green; legacy oracle untouched. Deferred (loud):
ALTER / bare GO TO, EXIT SECTION (needs section bounds), NEXT SENTENCE, GO-TO-out-of-an-inline-PERFORM. NEXT → the
remaining gaps to a first full NC program (file I/O = G5, whole-group MOVE = G6), then drive the corpus via the
differential harness.

DEVLOG 485.

