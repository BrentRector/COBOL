# DESIGN — conditional-compilation directives inside COPY (merged text-manipulation driver, ISO §7.2.1)

> **STATUS: IMPLEMENTED (DEVLOG 966).** Subsystem deep-dive for merging the conditional-compilation (CC) and
> COPY text-manipulation stages so directives INSIDE copybooks are processed (ISO §7.2.1). Design SSOT; the plan
> (`COBOLNET_REARCHITECTURE_PLAN.md` §0) points here. Keep current with the code (rule 4). The merged
> `ConditionalCompilationProcessor.ProcessWithCopy` driver is wired into `Frontend`; the legacy `Process`/COPY
> paths stay byte-identical. Gate: characterization 33/33 · CC/COPY unit 26/26 · CopyConditional 8/8 · the
> `cc_in_copy` golden · GnuCOBOL +2 fixes/0 regressions · legacy guard ALL GREEN · greenfield Conformance 3806/0.

## §1 The defect

ISO §7.2.1 orders the text-manipulation stage: **Step 1** incorporate COPY library text (false-path IF/EVALUATE
lines, *including* COPY'd text, may be omitted); **Step 2** process DEFINE/IF/EVALUATE + variable substitution +
PUSH/POP + COPY REPLACING, in encounter order over the expanded group; **Step 3** REPLACE; **Step 4** COBOL-WORDS.

The current greenfield pipeline (`Frontend.Preprocess`) runs **CC (Step 2a) BEFORE COPY (Step 1)** —
deliberately, so a main-source `>>IF` can gate a `COPY` statement. Consequence: a `>>DEFINE`/`>>IF`/`>>EVALUATE`
**inside a copybook** is never processed — it survives COPY expansion and reaches the lexer as a stray token
(**verified:** a copybook `>>IF` → `COBOL0001: unexpected '>'`). Steps 3/4 (REPLACE, COBOL-WORDS) and Step 2d
(COPY REPLACING) are already correctly ordered — the ONLY defect is Steps 1/2.

## §2 What must be preserved (verified firsthand)

1. A main-source `>>IF USEIT = 0` gating a `COPY` → the COPY is skipped (prints the un-copied value).
2. A **missing copybook in a false branch** must NOT error (today it works because CC blanks the false-branch
   COPY line before `CopyProcessor` runs). **This is why a simple order-swap (COPY-first) is unacceptable** —
   it would expand the false-branch COPY and raise a spurious CBL3620.
3. Byte-identity of the standalone entry points: `ConditionalCompilationProcessor.Process` and `CopyProcessor.Process` keep their exact
   behavior (the standalone `preprocess` CLI + the direct unit tests depend on them; the legacy compiler that also
   called them is deleted, P15 Cut 2).
4. The H3 line-count-preserving discipline: the five downstream directive-collection stages
   (TURN/PROPAGATE/REF-MOD/FLAG/COBOL-WORDS) run AFTER, on the final expanded text; the `linesBefore` baseline is
   captured after the merged driver. The merged driver itself changes line counts (COPY inserts lines) — it runs
   BEFORE the baseline snapshot, exactly where CC+COPY run today.

## §3 Mechanism — one interleaved recursive driver (owner-directed)

A new greenfield class **`CopyConditionalProcessor`** fuses the CC branch-selection state machine with COPY
expansion into ONE pass with **shared directive state**, so:
- an emitting-branch `COPY` is expanded and the copybook's incorporated text is fed through the SAME pass (so
  copybook `>>DEFINE`/`>>IF`/… are processed, with the DEFINE table shared across the copybook boundary);
- an omitted-branch `COPY` is DROPPED (never expanded → constraint §2.2 holds);
- a main-source `>>IF` still gates a `COPY` (constraint §2.1) — the driver reaches the `>>IF` first and its
  false branch omits the following COPY line.

This is the spec model (Step 1 flatten ⊕ Step 2 CC, processed in encounter order over the expanded group)
realized lazily so false-path COPYs are never incorporated.

### §3.1 Shared state (`CcState`)

Extracted from the current `ConditionalCompilationProcessor.Process` locals into a per-run object threaded
through recursion: the `defines` map (DEFINE/OFF/OVERRIDE), the `>>IF`/`>>EVALUATE` frame `stack`, the
`FlagScanState` (the frontend-inline FLAG options b/c), and the shared `CompileTimeExpressionEvaluator`. One
`CcState` per compilation group; recursion into a copybook shares it (so a copybook DEFINE is visible to
following main-source directives — spec Step-2 encounter order).

### §3.2 COPY expansion reuse

The copybook mechanics stay in `CopyProcessor` (find/read, `NormalizeCopybook` fixed→free, COPY REPLACING via
`ApplyReplacements`, the circular/`MaxCopyDepth` guards). The merged driver calls a NEW `CopyProcessor` method
that expands ONE COPY statement at a given position — parse name + REPLACING (to the terminating period, possibly
multi-line), resolve, normalize, apply REPLACING — and returns the copybook text (NOT recursively expanded; the
merged driver recurses so nested COPY *and* nested CC both process). `alreadyIncluded` + `depth` thread through
for the GR12 circular / depth-20 guards.

### §3.2b A directive never stands inside a COPY or REPLACE statement (kb/Work PB1384, §7.3.3 SR8 b)

The driver ends its text block at every directive line, which is also the only place it can SEE the violation: before it
flushes, `Render` asks `CopyProcessor.OpenStatementAt` whether the text since a COPY or REPLACE keyword has reached its
separator period (the §3.2a words, pseudo-text delimiters toggling, so a period inside pseudo-text ends nothing). The open
statement's text is carried from its keyword across further directive lines, so a statement split by several is reported at
each (COBOLNET2697, `DirectiveDiag.WithinStatement`). The directive then takes effect like any other (superset-continue),
and a truncated COPY still draws its own COBOLNET2449 after it.

A `>>SOURCE FORMAT` line never reaches `Render` (kb/Work PB1353): logical conversion DISCARDS it (§6.5 1)) and leaves a blank
line, with its physical line recorded in the text's `ReferenceFormatMap.DirectiveLines`. `Render` therefore asks, of each
BLANK line, `CopyProcessor.IsDiscardedFormatDirective(origin)` — the map registered under the line's file, which is the
main source's or the library text's own — and puts the same `OpenStatementAt` question over the pending block WITHOUT
flushing it (the discarded line takes nothing from the text). One question, one answer, whichever stage consumed the
directive. Inside pseudo-text the diagnostic also quotes §7.2.3.3 SR10 / §7.2.4.3 SR10 (`OpenStatementAt`'s `InPseudoText`).

### §3.2a Text-words — the one scanner under COPY and REPLACE (kb/Work PB1350 / PB1351 / PB1354)

Everything the text-manipulation stage does is decided over §7.2.2.5 TEXT-WORDS, and ONE type forms them:
`TextWordScanner` (`src/Cobol.Net.Frontend/Preprocessor/TextWord.cs`). A space or a comment is no text-word; the
colon and parentheses are separator text-words outside literals; a period, comma or semicolon separates only when
a space (or a line end, or a closing `==`) follows (§8.3.5 2)/3)), so `Z,ZZ9` and `ZZ.ZZ` are one word; a literal
is ONE word from its prefix (`X" N" NX" B" BX"`) through its closing delimiter with doubled quotes inside; `&` is a
word of its own; `==` is the pseudo-text delimiter. Its three consumers:

- **Locating COPY and REPLACE** — `FindStatementKeyword(text, pos, keyword, onGlued)` returns the next text-word
  `COPY` / `REPLACE`, so it cannot fire in a literal, a comment, a directive line or a longer word, and a REPLACE is
  found wherever it stands, not only first on a line (§7.2.4.3 SR1, kb/Work PB1358); a keyword glued behind
  `.`/`,`/`;` forms no text-word and is reported (SR2: COBOLNET2451 for COPY, COBOLNET2449 for REPLACE).
- **Parsing the statements** — `ParseCopyStatement` reads the §7.2.3.2 general format in order
  (`{text-name-1 | literal-1} [{OF|IN} …] [SUPPRESS [PRINTING]] [REPLACING …] .`) through a `StatementCursor`, and
  the REPLACE statement uses the same cursor and the same `ParseReplacingOperands`. A statement ends at its
  SEPARATOR period (§7.2.3.4 GR6), never at a `.` inside pseudo-text or a literal; a word out of order is
  COBOLNET2449, SR4/SR5 literal forms COBOLNET2450, a COPY within a COPY statement COBOLNET2451 (SR1). Each operand
  pair is screened by ONE `ScreenOperandPair` against its statement's `OperandRules` row — the §7.2.3.3 / §7.2.4.3
  content rules are word-for-word twins (COBOLNET2572) — and `ReadOperand` checks the §8.3.5 6) separation of each
  `==` (COBOLNET2573) (kb/Work PB1353). A directive line CONSUMED before the operand is read leaves only a blank
  line, so the operand screen cannot see it; §3.2b's driver check does (§7.3.3 SR8 b), and names §7.2.3.3 SR10 /
  §7.2.4.3 SR10 when the open statement stands inside pseudo-text. The LEADING / TRAILING phrases are gated at their
  introducing edition by constructs row `replacing-partial-word-2002` — ONE check, `OperandScreen.PartialWord`, asked
  by `ParseReplacingOperands` for both statements, once per statement (kb/Work PB1670).
- **The REPLACE states** — `ApplyReplaceStatements` parses format 1 `REPLACE [ALSO] …` and format 2
  `REPLACE [LAST] OFF` and drives `ReplaceStates` (§7.2.4.4 GR4–GR7): the active operands plus a LIFO stack of
  inactive ones; ALSO pushes and activates current-then-pushed operands, LAST OFF pops, a plain format 1 or OFF
  cancels the queue (kb/Work PB1357). ALSO/LAST are gated by constructs row `replace-also-last-2002`.
- **Matching** — `ApplyReplacements` compares text-words with `TextWord.MatchesForReplacing`, the ONE
  implementation of §7.2.3.4 9) c) / §7.2.4.4 8) c): character-strings case-insensitively; literals by prefix
  (case-insensitive), content un-doubled, the quotation symbol not compared, content case-SENSITIVE in `"…"` and
  `N"…"` and insensitive in the hexadecimal and boolean formats. Separator commas/semicolons are dropped (c) 1.).
  What a match PRODUCES is screened in the same pass by the statement's `ResultRule` (`ForbiddenIn`: §7.2.3.4 GR13 /
  §7.2.4.4 GR9 — no COPY statement, REPLACE statement (REPLACE only), SOURCE FORMAT directive or comment, the
  comment asked of the produced TEXT by `TextWordScanner.HoldsComment` since the scanner skips a comment; a blank
  line cannot arise, §6.5 2) discarded it; COBOLNET2574), and a COPY with REPLACING met inside library text is COBOLNET1640 (GR12) (kb/Work PB1356).
- **Locating library text** — `FindCopybook` over `LibraryPlaces()` (the working directory, then each configured
  search path) and `LocateInLibrary` (as spelled, then — unless the FILE NAME holds a period —
  `.CPY .CBL .COB .cpy .cbl .cob`): the DOC-A.1-40
  determination; the source file's own directory is not searched, and a text not found is CBL3620 on every path
  (kb/Work PB1355).

A fixed-form debugging line reaches the stage as `ReferenceFormatProcessor.DebugLineCarrier` (`*>` plus the Unicode
noncharacter U+FDD0) + its text; the scanner skips only the carrier, so the line's text-words take part in matching (the
COBOL-85 rule) while no comment a programmer writes can be mistaken for one. The lexer later reads the carrier as the
hidden `DEBUG_LINE` marker and `DebuggingLineRewriter` keeps or hides the line's tokens (kb/Work PB1705).

### §3.3 The `leave*` flags + the collection stages

The driver keeps the exact `leave*` behavior: an emitting-branch `>>TURN`/`>>PROPAGATE`/`>>REF-MOD-ZERO-LENGTH`/
`>>FLAG-02`/`>>FLAG-14`/`>>COBOL-WORDS` survives to its downstream stage; an omitted-branch one drops; the FLAG
options b/c update the shared `FlagScanState`. REPLACE (Step 3) runs after the whole interleaved expansion
(`CopyProcessor.ApplyReplaceStatements` over the merged text); the five collection stages + COBOL-WORDS run
unchanged on the final text.

### §3.4 Copybook reference format (§7.3.24.3 GR3 / GR5 — kb/Work PB1067)

`NormalizeCopybookMapped` runs library text through the ONE §6.5 walker the main source uses
(`ReferenceFormatProcessor.NormalizeToFreeFormMapped`), starting in the format in effect for the COPY statement (GR3).
Every normalization reports a `ReferenceFormatMap` (the format of each physical line); `Frontend.Preprocess` registers
the compilation group's map with `CopyProcessor.RegisterReferenceFormat`, each library text's map is registered as it
is normalized, and `ResolveOneCopy` asks the map of the text holding the COPY at the COPY's origin line
(`ReferenceFormatMap.LibraryTextDefaultAt`: a stated or inherited format, or a DETECTED fixed one, is handed on; a
text detected free hands nothing on and the library text is detected on its own — CONFORMANCE.md DOC-A.1-158). The
copybook's own `>>SOURCE FORMAT` segments are resolved within it and the COPY's text was converted on its own, so
GR5's revert holds by construction.

### §3.5 The origin line map (kb/Work PB82 — landed 2026-08-18)

Every stage of the chain that CHANGES the line count is MAPPED: it takes and returns a `MappedText` (the text plus
ONE `SourceOrigin(File, Line)` per `Split('
')` piece — the invariant the constructor asserts), and its string
overload is the mapped one's `.Text` (one implementation, never two). `OriginWriter` assembles the outputs: an
output line piece takes the origin of the FIRST content written into it. The stages and their splice rules —
- the fixed→free normalizer (`ConvertFixedToFree(..., origins)`): a continuation join strips the previous output
  line's newline and REOPENS its origin, so the joined line keeps its head line's number; a discarded
  `>>SOURCE FORMAT` directive line keeps its slot;
- the CC driver's `Render(MappedText)`: an omitted or directive line keeps its own origin (a blank output line), a
  block keeps its lines', a copybook expansion the copybook's (through `RenderCopybook(MappedText, depth,
  lineOffset)` — the offset is where `ExpandCopiesOneLevel` splices the expansion, which lets the driver name the
  output-frame line of every directive it meets, the DIRECTIVE ENCOUNTERS the §14.9.28.4 GR14 fixed point keys its
  implicit PUSH ALL / POP ALL to; `DESIGN-frontend-grammar.md`, kb/Work PB1066);
- `CopyProcessor.ExpandCopiesOneLevel(MappedText)`: the text before a COPY keeps its origins, the two framing
  newlines belong to the COPY statement's own line, the incorporated text carries the copybook's path and physical
  lines (`NormalizeCopybookMapped` — a fixed-form member's continuation joins are tracked exactly like the main
  source's) — and `ResolveOneCopy`, `OnNonPseudoTextOperand`, the CC `DirectiveDiag` all report at the SOURCE
  origin of the position they are looking at, never at an ordinal of the text being processed;
- `ApplyReplaceStatements(MappedText)` / `ApplyReplacements(MappedText)`: a REPLACE statement's own lines vanish
  from the resultant text; kept text keeps its origins, a replacement's text takes the origin of the line its
  match started on. `ReplaceLineMap` is the same pass over line-index origins: where each input line lands in the
  resultant text (the directive encounters' resultant lines, kb/Work PB1066).

`Frontend.Preprocess` returns the final `MappedText`, publishes `Frontend.LineMap` (`SourceLineMap`: resultant
line → origin; `Locate` is the ONE conversion to a 0-based `SourceLocation`), asserts every later stage (NIST
substitution, the six directive stages) is line-count preserving, and hands the map to the parser listener
(`CobolErrorListener` — which also stopped reporting every syntax error one line late), to the directive stages'
diagnostics, and (via `CompilerDriver` → `EditionContext.LineMap`) to the binder's diagnostic cursor and
`EXCEPTION-LOCATION`. The `>>TURN` / `>>FLAG` / `>>REF-MOD-ZERO-LENGTH` event lines are compared with token lines
and therefore stay in RESULTANT space — the map is consulted only at the user-facing boundary. Consumer side:
`docs/COBOLNET_VALIDATION_DESIGN.md` §2 "Positions".

## §4 Wiring (greenfield only)

`Frontend.Preprocess`: replace the two calls `ConditionalCompilationProcessor.Process(...)` (before COPY) +
`CopyProcessor.Process(...)` with ONE `CopyConditionalProcessor.Process(text, sourceDir, copySearchPaths,
dialect, permissive, diag, sourcePath)` returning the fully expanded free-form text (COPY incorporated, CC
applied, REPLACE applied). Everything downstream (NIST, TURN/…/COBOL-WORDS, the H3 baseline) is unchanged. The
legacy `Compilation.cs` and the `preprocess` CLI keep the two separate calls — byte-identical.

## §5 Increments

- **Incr 1 — the merged driver, common case.** `CcState` extraction + `CopyConditionalProcessor` (CC
  branch-selection + emitting-branch COPY expansion + omitted-branch COPY drop + shared DEFINE state) +
  `CopyProcessor.ExpandOneCopy`. Wire into `Frontend`. Preserve §2.1/§2.2. Unit + conformance goldens (copybook
  `>>IF`, copybook `>>DEFINE` seen by main `>>IF`, main `>>IF` gating COPY, false-branch missing copybook).
- **Incr 2 — edges** (only if a probe/differential shows a gap): multi-line COPY REPLACING interleaved with CC;
  mid-line COPY; PUSH/POP across the copybook boundary; nested-copybook directives.

## §6 Gate (per the shared-core / high-blast-radius doctrine)

Wave-local per commit (build + characterization + the new unit/goldens + CLI probes). **Before merge / for the
driver-swap commit:** the FULL NIST guard (`guard.sh`/`guard-fast.sh` ALL GREEN — the NIST verdicts must be
byte-identical) + the FULL greenfield Conformance + **the GnuCOBOL external differential before/after** (the
owner directive: diff per-case verdicts; a divergence→agree flip is a FIX, an agree→divergence flip is a
REGRESSION, 0 tolerated). The GnuCOBOL corpus exercises COPY heavily — it is the real net for this change.

## §7 Tests

- **Unit** `CopyConditionalProcessorTests` — copybook `>>IF` (both branches), copybook `>>DEFINE` visible to a
  later main `>>IF`, main `>>IF` gating COPY (taken/omitted), false-branch missing copybook (no error), nested
  COPY with directives, the `leave*` survival (a copybook `>>TURN` reaches the TURN stage).
- **Conformance** `tests/conformance/2023/cc_in_copy_*.cob` (+ `.out`) — an observable end-to-end (a copybook
  `>>IF` selecting a VALUE, printed).
