## Entry 962 — 2026-07-21 20:05 PDT — Wave D: `>>COBOL-WORDS` directive (ISO §7.3.10) Incr A — recognition + parse + SR1/2/5 + the 2023 introduction gate

**Track 1 of the Wave-D residue (owner: "do them all in sequence").** Started the `>>COBOL-WORDS` directive
(§7.3.10; Annex D.12; Annex E.3.3 item 12), the per-compilation-group facility that modifies which words are
reserved / context-sensitive / intrinsic-function words and prohibits specified user-defined words. An 8-part
question was answered by a 6-agent scout workflow (`cobol-words-scout`) before any code: the reserved-rejection
funnel (`VersionConformancePass.ParseArm.VisitCobolWord`→`ReservedWordSet.RejectsAt`→COBOLNET0901, the
hard-coded `_reservedWords = ReservedWordSet.Default` at line 406 being the seam), the intrinsic set
(`IntrinsicCatalog.TryGet`, Compiler-layer), the context-word/subscript-mode hazards, the directive-word gate
model (the FLAG/REF-MOD `ConstructRegistry.Check` pattern), and the test template.

**Design SSOT written first:** `docs/rearchitecture/DESIGN-cobol-words-directive.md` (decision-complete).
Mechanism = the owner's recorded direction — a **post-lex token rewriter + composed `ReservedWordSet`** (NOT the
prior scouts' "text substitution for EQUATE, not-supported for SUBSTITUTE/UNDEFINE" recommendation, which the
owner superseded): the rewriter operates on token TYPE↔TEXT and is strictly more capable than text substitution;
the frozen-lex subscript hazard is resolved by making the lexer's `PreviousTokenCouldBeDataName()` map-aware
(the map exists pre-lex). Four increments (A recognition/gate → B RESERVE/UNDEFINE reserved-word semantics →
C token rewriter EQUATE/SUBSTITUTE → D intrinsic-function-name synonyms).

**Incr A (this commit) — recognition + parse + gate, no behavior yet:**
- `CobolWordsMap` (Editions) — the pure-string override carrier (Synonyms / DeReserved / Reserved derived
  views), threaded frontend→compiler like `FlagState`/`RefModZeroLengthState`. `Empty` ⇒ every future consumer
  is a no-op (the zero-overhead invariant).
- `CobolWordsDirectiveProcessor` (Frontend) — the text-stage parser: the four options (EQUATE/UNDEFINE/
  SUBSTITUTE/RESERVE), a quoted-literal operand tokenizer (rejecting hex/national-prefixed and space-bearing
  literals per SR2), SR1 (before the first IDENTIFICATION DIVISION), SR5 (a word in ≤1 directive), the §8.3.2.2
  user-word form for the fresh word (SR4 frontend half), and the **COBOLNET0900 introduction gate** via the ONE
  `ConstructRegistry` (`Constructs.CobolWordsDirective2023`). Blanks the directive line (line-count preserving —
  the H3 discipline). SR3/SR4 CATEGORY checks (reserved/context/intrinsic membership) deferred to Incr B where
  all three registries are reachable.
- Wiring: `Frontend.CobolWordsMap` property + the stage after the FLAG stage (H3 assert); a
  `leaveCobolWordsDirectives` flag on `ConditionalCompilationProcessor.Process` so the greenfield emitting-branch
  directive survives to the new stage while `COBOL-WORDS` stays in `KnownIgnoredDirectives` (legacy oracle
  byte-identical).
- Diagnostics: **COBOLNET1623** `cobol-words-directive-invalid` (the malformed/SR channel; the message names the
  specific rule). `constructs.json` `cobol-words-directive-2023` row (introducedIn 2023, COBOLNET0900) + regen.
  `DIAGNOSTICS.md` regenerated (the drift test caught the missing row — regen via `gen-diagnostics-doc.ps1`).

**Gate (wave-local):** fresh `CobolSharp.sln` build (0 warn) · `CobolWordsDirectiveTests` **21/21** ·
Construct/Diagnostic registry drift **8/8** · characterization **33/33** byte-exact (zero-overhead holds) ·
CLI-probed all four cases (RESERVE@2023 runs; RESERVE@2014→0900; BOGUS→1623; SR1-misplacement→1623). Below-2023
negative golden `cobol-words-below-2023` added. **NEXT = Incr B** (RESERVE/UNDEFINE via the composed
`ReservedWordSet` + SR3/SR4 category validation).

