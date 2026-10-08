<!-- Authoritative, implementation-ready design SSOT for PERFORM Format 3 (§14.9.28). Produced 2026-07-20
by an adversarially-verified workflow; REPLACES the deleted PHASE-13-c5-perform-format3-rederivation.json
(whose derivation carried known defects). The three figure questions are resolved in
PHASE-13-c5-perform-format3-pdf-resolution.md and applied here. Diagnostic numbers corrected to the true
contiguous free block 1597-1617 (batch 2 used 1585-1596). -->

# PERFORM Format 3 (§14.9.28) — Implementation-Ready Design

**Status:** Design SSOT for the P13 grammar-batch PERFORM Format-3 construct. Supersedes the C5 derivation and folds in the adversarial verification. Design-doc home for the same change set: `docs/COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` D12.

---

> ## ⚙ FULLY IMPLEMENTED — the RUNTIME INTERCEPTOR LANDED 2026-07-22 (DEVLOG 970); §9 IS THE AS-BUILT SSOT (READ IT FIRST)
> Both halves are DONE. The front half landed 2026-07-20 (recognize/validate/diagnose/gate, DEVLOG 940); the **pc-range
> RUNTIME interceptor landed 2026-07-22** (the F3 PERFORM compiles and runs — GR17-22; the 0899 program-path staging is
> lifted). **§9 below is the decision-complete AS-BUILT design (the implementation SSOT).** The residual staged sub-GAPs
> (each a loud COBOLNET0899, never silent) are: the open-mode WHEN operand form, F3-PERFORM-inside-a-method, the
> cross-CALL GR1 "in range" reading, the `>>PROPAGATE` directive, and an exception-OBJECT raise inside imp-1 (§9.7). EC-FLOW-USE left that list when kb/Work PB368 gave §14.9.49.4 GR2 its raise site.
> An 8-agent scout of the actual tree surfaced several places the original plan under-specified reality — each reconciled
> from the spec, not improvised. The **current-state design home is `COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` D12**;
> the body below is the original front-half plan, corrected by these notes (point 6 below is SUPERSEDED by §9):
> 1. **FINALLY is a PURE RESERVED KEYWORD, NOT a `cobolWord`** (contra §2.2's "add to the funnel"). As a trailing phrase
>    keyword after imperative statements, a name-slot FINALLY is swallowed by a preceding DISPLAY/MOVE operand list
>    (`DISPLAY "c" FINALLY` → FINALLY becomes a DISPLAY operand; caught by a parse test). It is reserved at every edition
>    (a documented, negligible continuity deviation — FINALLY was never a COBOL identifier idiom). **LOCATION STAYS a
>    `cobolWord`** — head-only (never after a statement) ⇒ no swallow, and continuity needs it (`PERFORM LOCATION` /
>    paragraph named LOCATION below 2023). So the `whenOperandAhead()` stop-set is the 11 statement-leader verbs ONLY
>    (FINALLY/OTHER/COMMON are not cobolWords ⇒ the grammar element stops the loop at them naturally).
> 2. **The merged inline `performStatement` arm PRECEDES the out-of-line `PERFORM procedureName`** — `PERFORM LOCATION imp…
>    END-PERFORM` is genuinely ambiguous (LOCATION is a cobolWord ⇒ a valid target); only the trailing END-PERFORM
>    disambiguates, so the inline arm is tried first (a period-terminated `PERFORM LOCATION.` falls through).
> 3. **Diagnostics are COMPILER-CHANNEL raw `Edition.Error` codes** (the 1585–1596 pattern), NOT DiagnosticCatalog
>    descriptors (§3's "lands in DiagnosticCatalog" line was stale — both scans confirmed).
> 4. **COBOLNET1598 (operand-form exclusivity) is NOT emitted** — no §14.9.28.3 SR backs it (SR14/15/16 are the only WHEN
>    operand rules; the figure's `{exception-name-1 | exception-name-2 FILE file-name-2}…` permits interleaving).
>    Spec-fidelity: implement the NAMED rules, invent no restriction. **XS-RESUME-PLACEMENT is subsumed by COBOLNET0712**.
>    **XS-POP/XS-PUSH (1602/1603) stay RESERVED** — the >>POP/>>PUSH directives are themselves unimplemented in the
>    greenfield (grep-confirmed), so their F3 ban rides that directive wave.
> 5. **`IsLexicallyWithin` and bound `>>POP`/`>>PUSH` checks DO NOT EXIST** (§3.1's premise) — the region bans (A/B/C/D)
>    are parse-subtree walks that fall out of the F3 node's own sub-lists (simpler than the design implied).
>    **TurnState is IMMUTABLE** ⇒ GR14 is a `WithImplicitEnable` derived-instance overlay (line-0 synthetic enables),
>    NOT a push/pop mutator.
> 6. **⚠ SUPERSEDED BY §9 (the runtime landed 2026-07-22).** This point recorded the runtime as STAGED at 0899 and
>    weighed the lambda-vs-pc-range options; §9 is the as-built pc-RANGE design. Retained for history: *THE RUNTIME
>    (GR17–20) WAS STAGED via a 0899 REJECTION, not a partial compile.* The F3 PERFORM was REJECTED at
>    2023 with COBOLNET0899 (`perform-exception-checking-2023` row = `status:pending`) — safe (no silent on-raise
>    divergence) and honest, per the batch-2 precedent (PICTURE EDITING/VALUE Format 2 staged their forms as 0899
>    rejections). **The interceptor is a proper NEXT WAVE requiring a production architecture decision — do NOT rush a
>    kludge** (owner requirement: commercial-quality, decade-supportable). The design's §5 **lambda-mode-inline** approach
>    is **REJECTED for production**: it needs a "am-I-in-a-lambda" statement-emission mode (RESUME → return-action,
>    `EXIT PERFORM` → a thrown signal because C# cannot `goto` out of a lambda) — a SECOND mechanism for "run a handler
>    in response to an EC," violating the singular-pattern rule (`feedback_singular_pattern`) and a long-term maintenance
>    hazard. The as-built architecture for exactly that job already exists: **`ResumeSignal.cs` records that declaratives
>    are pc-RANGES run by the bounded dispatcher (`__RunUse` wraps `__Dispatch(start,end)` in a `try/catch(ResumeSignal)`
>    and RETURNS the resume action to the raise site).** A WHEN handler IS an inline declarative (GR17: a WHEN match
>    REPLACES a matching USE declarative), so the wave should choose between two SIGNAL-BASED, singular-pattern options
>    and NOT the lambda: **(A)** emit imp-2..5 as synthetic pc-ranges dispatched exactly like declaratives (max reuse of
>    `__RunUse`/`ResumeSignal`; needs anonymous-pc synthesis for inline bodies — cf. `AddAnonymousParagraph`); or **(B)**
>    emit imp-2..5 as ordinary methods invoked by a frame matcher, with RESUME → `ResumeSignal` and `EXIT PERFORM` → a
>    new `ExitPerformSignal` caught at the PERFORM boundary (the established exception-as-control family:
>    `ResumeSignal`/`StopRun`/`ProgramReturn`). Also required: gate `__EcPerform` on "unit HAS an F3 PERFORM" so every
>    existing (non-F3) program's generated code stays BYTE-IDENTICAL (the 33 characterization tests + the battery), and
>    thread `fatal` through the raise-site dispatch seam. §5 below is the feature spec; its emit *shape* is superseded by
>    this note.

**The three mandatory adversarial corrections are applied:**
1. Next-free diagnostic is **COBOLNET1597**. NEXT-FREE is 1597, NOT 1585: this session's batch 2 consumed 1585-1588 (VALUE Format 2) and 1591-1596 (PICTURE EDITING) as COMPILER-CHANNEL raw codes (in DataBinder/PictureAnalyzer, NOT DiagnosticCatalog.cs), so a catalog-only scan is insufficient — BOTH the `grep -rho 'COBOLNET1[5-6][0-9][0-9]' src` scan (max 1596) AND the catalog must agree. The contiguous free block 1597-1617 fits this feature's 21 codes. The stale "1585"/"1581"/"1578" claims are discarded.
2. The **Grammar-Shape rules in §1 are authoritative.** The alternate grammar snippet that appeared in the runtime scouting (the greedy `WHEN cobolWord+` form) is discarded — it reintroduced the canonical-body mis-parse and dropped the `file-name-1` operand form.
3. **`RESUME AT procedure-name` inside any WHEN/OTHER/COMMON phrase is a bind-time error** (§14.9.33.3 SR1). The `ResumeSignal(targetPc)` runtime path is reachable **only from a declarative** RESUME, never from an F3 WHEN body. §6 runtime honors this.

---

## 0. Feature summary

Format 3 PERFORM is an **exception-checking PERFORM**: a per-statement exception interceptor scoped to `imperative-statement-1` (imp-1). It is *not* a block-level try/catch. It intercepts exceptions raised by the statements of imp-1, dispatches to a matching `WHEN` handler (in written order, first match wins), optionally runs `WHEN OTHER` / `WHEN COMMON` / `FINALLY`, and resumes per GR20 (resume-in-place for nonfatal; abnormal-terminate for fatal unless a RESUME redirects).

COBOL-2023 only. Gated in `VersionConformancePass`; diagnosed at `--std 85|2002|2014`.

---

## 1. Grammar (AUTHORITATIVE — greedy-safe)

### 1.1 Files touched
- `src/Cobol.Net.Frontend/Grammar/Core/CobolControlFlow.g4` — `performStatement` merge + new `performWhen*` rules.
- `src/Cobol.Net.Frontend/Parsing/CobolParserCoreBase.cs` — add `whenOperandAhead()` near `retryPhraseAhead` (:172).
- `src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4` — dispatcher (:612) and `statementBlock` (:736) reused unchanged. **`useOnTarget`/`useEcEntry` are NOT reused for PERFORM** (their greedy `cobolWord+` loops are period-terminated-safe in USE but statement-abutted-unsafe in PERFORM).

### 1.2 The mis-parse being avoided (rationale, load-bearing)

The USE rules `useEcEntry : cobolWord (FILE fileName)*` and `useOnTarget` (…`fileName+`, and `fileName : … cobolWord`) are unbounded `cobolWord` loops. They are safe in USE only because a **separator period terminates the USE sentence**. In a WHEN phrase the operand loop **abuts imperative-statement-2 directly** (a `statementBlock*` with no terminator token). For the single most spec-canonical WHEN body —

```
WHEN EC-I-O-PERMANENT-ERROR RESUME AT NEXT STATEMENT
```

— a naive `useEcEntry+` reuse greedily takes `RESUME` (a `cobolWord`) as a **second exception-name**, then `AT` fails to continue and the parse derails. RESUME in a WHEN arm is exactly what §14.9.33.3 SR1 sanctions, so this is a real defect. Therefore PERFORM gets **dedicated operand rules** with a gated continuation predicate. (Placing the predicate *inside* the shared USE rules would regress `USE … ON RESUME.` where RESUME is a legally-named file — different terminator context, so a different rule. This respects the singular-pattern rule: a statement-abutted list is a different job than a period-terminated list.)

### 1.3 The continuation predicate `whenOperandAhead()`

Add to `CobolParserCoreBase.cs` (alongside `is2023()` and `retryPhraseAhead`):

```csharp
// True when LT(1) may CONTINUE a Format-3 WHEN operand list — i.e. it is NOT one of the
// context-sensitive words that ALSO lead a statement, and would otherwise be captured as a
// spurious file-name / exception-name ahead of imperative-statement-2. This set is exactly
// (cobolWord ∩ statement-first-tokens); pure-reserved verbs (MOVE, ADD, DISPLAY, IF, PERFORM…)
// need no exclusion because they are not cobolWords and the `cobolWord` grammar element already
// rejects them, so the loop stops at them naturally.
protected bool whenOperandAhead()
{
    switch (_input.La(1))
    {
        case CobolLexer.RESUME:  case CobolLexer.RAISE:    case CobolLexer.VALIDATE:
        case CobolLexer.UNLOCK:  case CobolLexer.SEND:     case CobolLexer.RECEIVE:
        case CobolLexer.COMMIT:  case CobolLexer.ROLLBACK: case CobolLexer.GET:
        case CobolLexer.ENTER:   case CobolLexer.PARSE:
            return false;
        default:
            return true;
    }
}
```

This is the **exact, complete** overlap set `cobolWord ∩ statement-leaders`. See §1.6 for the mandatory build-time drift guard that keeps it complete.

### 1.4 The rules

```antlr
performStatement
    // ... existing out-of-line alternatives unchanged ...
    // Merged inline Formats 2 & 3 — ONE inline alternative ⇒ zero cross-format lookahead:
    | PERFORM performInlineHead? statementBlock*
        performWhenPhrase* performWhenOther? performWhenCommon? performFinally?
      END_PERFORM
    ;

performInlineHead
    : performOptions+                        // Format 2: TIMES / UNTIL / VARYING
    | performLocationPhrase                  // Format 3 head
    ;

// [WITH] LOCATION — WITH is an optional word (§8.3.2.4.3); bare LOCATION is 2023-only because
// below 2023 `PERFORM LOCATION` is an ordinary out-of-line PERFORM of a paragraph named LOCATION.
performLocationPhrase
    : WITH LOCATION
    | {is2023()}? LOCATION
    ;

// Three DISJOINT operand forms (§14.9.28.2 Format 3). Each operand list is bounded by
// whenOperandAhead() so it cannot annex the leading verb of imperative-statement-2.
performWhenPhrase
    : WHEN EXCEPTION performWhenModeList statementBlock*      // EXCEPTION {modes | file-name-1…}
    | WHEN           performWhenEcList   statementBlock*      // exception-name-1… | exc-name-2 FILE…
    ;

// EXCEPTION { INPUT | OUTPUT | I-O | EXTEND | {file-name-1}… }.
// The figure's "IO" is the I_O token (typesetting/standard defect — I-O in every sibling
// format and §8.9; folio reference recorded in the PDF-resolution doc). A single WHEN EXCEPTION
// selects exactly ONE mode OR a file-name list — never a mix; bind-enforced (SR at §3).
performWhenModeList
    : INPUT | OUTPUT | I_O | EXTEND
    | fileName ({whenOperandAhead()}? fileName)*             // gated CONTINUATION only
    ;

// { exception-name-1 }… | { exception-name-2 FILE file-name-2 }… — open set (EC-USER-*) ⇒ cobolWord.
performWhenEcList
    : performWhenEcItem ({whenOperandAhead()}? performWhenEcItem)*   // gated CONTINUATION only
    ;

performWhenEcItem
    : cobolWord (FILE fileName)*             // inner FILE-loop is self-bounding (see §1.5 proof)
    ;

performWhenOther
    : WHEN OTHER  EXCEPTION? statementBlock*   // 2nd EXCEPTION optional (§8.3.2.4.3)
    ;

performWhenCommon
    : WHEN COMMON EXCEPTION? statementBlock*   // 2nd EXCEPTION optional (§8.3.2.4.3)
    ;

performFinally
    : FINALLY statementBlock*
    ;
```

The predicate gates only the **continuation** of each list, never the first operand. The first operand after `WHEN`/`WHEN EXCEPTION` is taken unconditionally, which (a) makes the canonical body parse correctly and (b) preserves the superset-parse posture: a malformed `WHEN RESUME …` (no exception-name) still binds RESUME as an exception-name so the binder emits the specific COBOLNET0711 "not an exception-name" diagnostic rather than a generic parse error.

### 1.5 Proof the operand list cannot swallow a following statement verb

**Head disjointness (LA after WHEN).** `EXCEPTION`, `OTHER`, `COMMON` are reserved tokens and **none is in the `cobolWord` funnel**, so `WHEN EXCEPTION…`, `WHEN OTHER…`, `WHEN COMMON…`, and `WHEN <ec-name>…` are unambiguous.

**Continuation boundary — the canonical case** `WHEN EC-I-O-PERMANENT-ERROR RESUME AT NEXT STATEMENT`:
1. `WHEN` + not-EXCEPTION/OTHER/COMMON → `performWhenEcList`.
2. First `performWhenEcItem` = `EC-I-O-PERMANENT-ERROR`; its inner `(FILE fileName)*` sees `RESUME ≠ FILE` → no inner match.
3. Outer gate `{whenOperandAhead()}?` inspects LT(1)=`RESUME` → in the exclusion set → **predicate false → loop exits.**
4. `statementBlock*` consumes `RESUME AT NEXT STATEMENT` via `resumeStatement`. **Correct.**

Identical for RAISE, VALIDATE, UNLOCK, SEND, RECEIVE, COMMIT, ROLLBACK, GET, ENTER, PARSE, and for the `WHEN EXCEPTION file-name…` form.

**Two exclusion layers, together complete.** Pure-reserved verbs (DISPLAY, MOVE, IF…) are not `cobolWord`, so the grammar element itself fails to match and the loop exits (`WHEN EC-BOUND-SUBSCRIPT DISPLAY "x"` needs no predicate). The 11 context-sensitive verbs *are* `cobolWord`, so the predicate stops them. Between the grammar element (necessary) and the predicate (funnel overlap), every statement-leading token is excluded.

**Inner FILE-loop self-bounding.** `(FILE fileName)*` re-enters only on `FILE`; `FILE` leads no statement and is not in `cobolWord`. So `EC-I-O-PERMANENT-ERROR FILE F1 FILE F2 RESUME…` consumes both `FILE F_n` pairs inside the item, then the outer gate stops at RESUME. No inner predicate needed.

**Accepted, documented limitation** (record in the grammar comment): a file *literally named* one of the 11 words, used as the 2nd+ operand of `WHEN EXCEPTION` (e.g. `WHEN EXCEPTION F1 RESUME`), reads RESUME as imperative-statement-2, not a second file-name. This is the spec-consistent reading (a word in statement-verb position reads as the verb) and never affects exception-names (all begin `EC-`), so it is harmless.

### 1.6 Mandatory drift guard (build-time assertion, not "should")

Add a CobolWords-style hard build-time assertion: the set `{ words in cobol-words.json (name slot) that are the first token of any dispatcher statement }` **must equal** the `whenOperandAhead()` exclusion list. A future name-slot statement verb (a real GET / PARSE statement, etc.) then **fails the build** until the exclusion set is updated. Correctness of the parse depends on this set staying complete, so the guard is a build gate, not a lint.

---

## 2. Tokens & registry work

### 2.1 New lexer tokens

Two new reserved-word tokens are required (currently lex as IDENTIFIER):

| Token | Word | Notes |
|---|---|---|
| `LOCATION` | `LOCATION` | Used by `performLocationPhrase`. Bare `LOCATION` is 2023-gated in-grammar via `{is2023()}?`. |
| `FINALLY` | `FINALLY` | Used by `performFinally`. |

**Already-existing tokens reused (no new work):** `PERFORM`, `WHEN`, `EXCEPTION`, `OTHER`, `COMMON`, `INPUT`, `OUTPUT`, `I_O`, `EXTEND`, `FILE`, `WITH`, `END_PERFORM`.

**FIXED FIGURE FACT:** the figure's `IO` denotes **I-O**. Use the existing `I_O` token — do **not** mint a new `IO` keyword.

**`COMMON` caution:** `COMMON` already exists as a reserved word in the IDENTIFICATION/PROGRAM context (COMMON program attribute). Confirm the existing token is reusable here as a context word in `performWhenCommon` without regressing that use; it is only consumed positionally after `WHEN`, so no conflict is expected, but include a targeted parse test.

### 2.2 Registry work (three JSON files + generated guards)

`LOCATION` and `FINALLY` become reserved in COBOL-2023:

- **`cobol-words.json`** — add `LOCATION` and `FINALLY` entries with the correct edition/first-appearance metadata (2023). These are reserved words, **not** name-slot (`cobolWord`) words — they must be excluded from the `cobolWord` funnel or they would themselves become spurious operands.
- **`reserved-words.json`** — add both words as reserved in the 2023 edition list (and confirm they are *not* reserved in 85/2002/2014, so the version-matrix continuity invariant holds — `PERFORM LOCATION` naming a paragraph remains legal pre-2023).
- **`constructs.json`** — add the `perform-format-3` construct row with its 2023 gate + the `perform-exception-checking-2023` feature key (see §4) so the construct is discoverable in the traceability inventory.
- Re-run the generated catalog/word guards; the `LOCATION`/`FINALLY` additions must round-trip on both OSes (`feedback_commit_generated_parser`).

### 2.3 The drift-guard registry hook

The `whenOperandAhead()` completeness guard (§1.6) reads the name-slot set from `cobol-words.json` at build time. Wire it as a generated assertion so adding a name-slot verb to the JSON is what trips it.

---

## 3. Complete syntax-rule list with diagnostics

Diagnostics are allocated from **NEXT-FREE = COBOLNET1597** upward (the contiguous free block 1597-1617; 1585-1596 are in use as batch-2 compiler-channel codes — confirm with BOTH the src grep AND the catalog scan). Reuse the existing context-parameterised **COBOLNET0711** for an unknown/invalid exception-name. Each new code lands in `DiagnosticCatalog.cs` in the same change set, plus the catalog-drift guard.

### 3.1 Scope-region taxonomy (load-bearing for the containment walk)

The binder performs a **single lexical-containment walk** over the bound F3 PERFORM sub-tree, classifying each contained statement/directive by region:

- **Region A — imperative-statement-1 ONLY.**
- **Region B — WHOLE PERFORM (imp-1..imp-5, anywhere).**
- **Region C — WHEN phrases only** (imp-2 / imp-3 / imp-4; the handler bodies).
- **Region D — imperative-statement 2/3/4/5 only** (everything except imp-1).

Use one shared `IsLexicallyWithin(node, region)` predicate (the same predicate the POP/PUSH/`>>TURN` checks already use) to drive every rule below.

### 3.2 Format-3's own syntax rules (§14.9.28.3 SR14–SR16) and structural facts

| ID | Rule | Region | Diagnostic |
|---|---|---|---|
| **PF3-STRUCT-END** | `END-PERFORM` required to close the inline Format-3 statement. | structural | Reuse existing missing-`END-PERFORM` scope diagnostic (no new code). |
| **PF3-STRUCT-WHEN-REQUIRED** | At least one ordinary `WHEN` phrase is required (outer brace, ellipsis outside). A PERFORM of only WHEN OTHER / WHEN COMMON / FINALLY is not admitted. | structural (bind) | **COBOLNET1597** — "a Format-3 (exception-checking) PERFORM requires at least one WHEN phrase". |
| **PF3-STRUCT-ORDER** | The three optional trailing lines appear only in fixed order: `WHEN OTHER`, then `WHEN COMMON`, then `FINALLY`. (Structurally enforced by the grammar ordering `performWhenOther? performWhenCommon? performFinally?`.) | structural | Grammar-enforced; no runtime diagnostic. |
| **PF3-STRUCT-WHEN-OPERAND-EXCLUSIVE** | A single ordinary WHEN selects exactly ONE operand form: (a) `EXCEPTION` + one mode/file-name selection, (b) an `exception-name-1` list, or (c) an `exception-name-2 FILE file-name-2` list. No mixing across the three across one WHEN. The grammar's `performWhenEcItem` mildly supersets by allowing bare EC-names and `FILE`-paired items to interleave. | per-WHEN (bind) | **COBOLNET1598** — "a WHEN phrase mixes exclusive operand forms (bare exception-name with a FILE-paired exception-name)". |
| **PF3-SR14** | A `file-name` shall not appear more than once across the WHEN phrases **unless every such instance is paired with an exception-name** (§14.9.28.3 SR14, L29192). | across all WHEN operands (bind) | **COBOLNET1599** — "file-name '{0}' is specified in more than one WHEN phrase without an exception-name pairing". |
| **PF3-SR15** | An `exception-name` shall appear only once **unless each occurrence pairs a different file-name** (§14.9.28.3 SR15, L29194). | across all WHEN operands (bind) | **COBOLNET1600** — "exception-name '{0}' is specified more than once without a distinct file-name". |
| **PF3-SR16** | If `file-name-2` is specified, `exception-name-2` shall begin with the COBOL characters `EC-I-O` (§14.9.28.3 SR16, L29196). | per FILE-paired operand (bind) | **COBOLNET1601** — "exception-name '{0}' paired with FILE must begin with 'EC-I-O'". |

### 3.3 Cross-statement syntax bans (verified verbatim against the spec)

All line numbers and SR ordinals verified against `specs/ISO_COBOL.md`. Each ban uses the shared containment predicate over the indicated region.

| ID | Statement / directive banned | Region | Section (SR, line) | Diagnostic |
|---|---|---|---|---|
| **XS-POP** | `>>POP` directive | B (whole PERFORM) | §7.3.20.3 SR4 (L4705) | **COBOLNET1602** — "the POP directive shall not appear within an exception-checking PERFORM". Hard `shall not`. |
| **XS-PUSH** | `>>PUSH` directive | B | §7.3.22.3 SR4 (L4798) | **COBOLNET1603** — "the PUSH directive shall not appear within an exception-checking PERFORM". Hard. |
| **XS-EXIT-PERFORM-CYCLE** | `EXIT PERFORM CYCLE` (the CYCLE phrase) | B | §14.9.14.3 SR8 (L27097) | **COBOLNET1604** — "EXIT PERFORM CYCLE shall not appear within an exception-checking PERFORM". Plain `EXIT PERFORM` remains legal. **Retracts** the C5 anchor's false "needs no new syntax" claim. |
| **XS-INITIATE-MULTI** | `INITIATE` naming >1 report-name | B | §14.9.21.3 SR3 (L27719) | **COBOLNET1605** — "an INITIATE with more than one report-name shall not appear within an exception-checking PERFORM". |
| **XS-TERMINATE-MULTI** | `TERMINATE` naming >1 report-name | B | §14.9.46.3 SR3 (L32271) | **COBOLNET1606** — "a TERMINATE with more than one report-name shall not appear within an exception-checking PERFORM". |
| **XS-VALIDATE-MULTI** | `VALIDATE` naming >1 identifier | B | §14.9.50.3 SR6 (L32798) | **COBOLNET1607** — "a VALIDATE with more than one identifier shall not appear within an exception-checking PERFORM". |
| **XS-GOTO** | `GO TO` anywhere in a WHEN phrase | C (WHEN phrases) | §14.9.17.3 SR3 (L27300) | **COBOLNET1608** — "GO TO shall not appear in a WHEN phrase of an exception-checking PERFORM". |
| **XS-RESUME-PLACEMENT** | `RESUME` outside a WHEN phrase (i.e. in imp-1 or FINALLY) of an F3 PERFORM | body ∖ C | §14.9.33.3 SR1 (L29950) | **COBOLNET1609** — "RESUME may appear only in a declarative or a WHEN phrase". |
| **XS-RESUME-OPERAND** | `RESUME AT procedure-name` inside any WHEN/OTHER/COMMON phrase (must be `RESUME NEXT STATEMENT`) | C | §14.9.33.3 SR1 (L29950) | **COBOLNET1610** — "RESUME in a WHEN phrase shall specify NEXT STATEMENT". **This is the adversarial reconciliation: the binder rejects it; the runtime never executes a `RESUME AT proc` from a WHEN body.** |
| **XS-RAISE** | `RAISE` in any imperative-statement other than imp-1 | D (imp-2/3/4/5) | §14.9.29.3 SR4 (L29402) | **COBOLNET1611** — "RAISE shall appear only in imperative-statement-1 of an exception-checking PERFORM". |
| **XS-FINALLY-TRANSFER-OUT** | `GO TO`, `EXIT PARAGRAPH/SECTION/PROGRAM/METHOD/FUNCTION`, `GOBACK`, `NEXT SENTENCE` or `STOP RUN` in the FINALLY phrase | E (imp-5 only) | §14.9.28.4 GR16 | **COBOLNET2927** — "There shall be no statements that include a transfer of control out of the PERFORM statement within imperative-statement-5" (kb/Work PB434). `EXIT PERFORM` stays admitted (GR16's third sentence: the implicit CONTINUE after END-PERFORM). `STOP RUN` is a member: "control is transferred to the operating system" (§14.9.42.4 GR6), and a main program's `GOBACK` "operates as if executing a STOP statement" (§14.9.18.4 GR3). |
| **XS-CLOSE-MULTI** | `CLOSE` naming >1 file-name | A (imp-1 only) | §14.9.6.3 SR3 (L26022) | **COBOLNET1612** — "a multi-file CLOSE shall not appear in imperative-statement-1 of an exception-checking PERFORM". |
| **XS-DELETE-FILE-MULTI** | `DELETE FILE` naming >1 file-name | A | §14.9.10.3 SR4 (L26352) | **COBOLNET1613** — "a multi-file DELETE FILE shall not appear in imperative-statement-1". |
| **XS-INITIALIZE-DUP** | `INITIALIZE` naming MORE THAN ONE identifier-1 (a duplicate is one case) | A | §14.9.20.3 SR2 | **COBOLNET1614** — "an INITIALIZE naming more than one identifier-1 shall not appear in imperative-statement-1". The operand COUNT, not repeated text: "specified only once" is the format term, and §14.9.20.4 GR3 calls the multi-operand form "more than one identifier-1 is specified" (kb/Work PB417). |
| **XS-MERGE** | any `MERGE` | A | §14.9.24.3 SR1 (L28272) | **COBOLNET1615** — "MERGE shall not appear in imperative-statement-1 of an exception-checking PERFORM". |
| **XS-OPEN-DUP** | `OPEN` naming MORE THAN ONE file-name, over all open-mode groups (a duplicate is one case) | A | §14.9.27.3 SR3 | **COBOLNET1616** — "an OPEN naming more than one file-name shall not appear in imperative-statement-1". The operand COUNT: §14.9.27.4 GR20 calls the multi-operand form "more than one file-name is specified", the family (CLOSE, DELETE FILE) bans the multi-operand statement (kb/Work PB330). |
| **XS-SORT** | any `SORT` | A | §14.9.40.3 SR3 (L31491) | **COBOLNET1617** — "SORT shall not appear in imperative-statement-1 of an exception-checking PERFORM". |

**Allocated range: COBOLNET1597–1617** (21 codes, contiguous). Re-verify against BOTH the src grep AND `DiagnosticCatalog.cs` at implementation time (two independent scans must agree) before minting. These are COMPILER-CHANNEL raw codes (the 1542/0808 pattern), not catalog descriptors.

**Not enumerated (explicitly out of scope, deliberately):**
- §14.9.28.3 SR8 (UNTIL EXIT "under" a VARYING/TEST PERFORM) — orthogonal to exception-checking; it constrains a nested UNTIL-EXIT PERFORM, not the F3 body. Its meaning IS spec-resolved (the C5 non-redundancy derivation: only an EXPLICIT `WITH TEST BEFORE`/`TEST AFTER` or a `VARYING` on the enclosing PERFORM triggers it — the IMPLIED TEST BEFORE of §14.9.28.3 SR1 does not, else SR8's separate mention of VARYING would be redundant). DEFERRED as a separate work item (orthogonal scope), tracked in the §11 analysis backlog — NOT an owner question and NOT implemented here.
- General rules describing WHEN-precedence over declaratives (ACCEPT, DISPLAY, OPEN GR, RECEIVE, SEND, WRITE) — execution semantics, realized by the runtime in §6, not syntax bans.
- §7.3.x TURN L4937 (EC-I-O-WARNING) — permissive, handled in §6.3.

### 3.4 The RESUME AT-optional grammar fix (§14.9.33.2) — ships in this change set

`§14.9.33.2` makes `AT` an **optional word** in RESUME (it is not underlined in the general format), so `RESUME NEXT STATEMENT` (no `AT`) is legal source. The current `resumeStatement` rule makes `AT` mandatory, which would make XS-RESUME-OPERAND's "must specify NEXT STATEMENT" unsatisfiable without `AT`. Fix:

```antlr
resumeStatement
    : RESUME AT? (NEXT STATEMENT | procedureName)
    ;
```

`AT?` makes both `RESUME NEXT STATEMENT` and `RESUME AT NEXT STATEMENT` legal, and both `RESUME proc` / `RESUME AT proc`. This is a dependency fix (not a Format-3 SR) that must land in the same change set. Add a targeted parse test for all four spellings and re-run the full legacy guard (RESUME is shared with declaratives).

---

## 4. Version gate

- **Feature key:** `perform-exception-checking-2023`.
- **Diagnostic (used at `--std 85|2002|2014`):** **COBOLNET0900** — the `ConstructRegistry.Check` introduction gate, CONFIRMED: this is the exact gate `picture-editing-2023` and `value-table-format-2002` fire this batch (recognition-based, via a `constructs.json` row + a `VisitPerformStatement`/`ParseArm` override). No new code needed. VCR row for exception-checking PERFORM.
- **Gate site:** `VersionConformancePass`. The construct is rejected (edition error) below 2023. Bare-`LOCATION` disambiguation is additionally handled in-grammar (`{is2023()}?`) so that pre-2023 `PERFORM LOCATION` continues to parse as an out-of-line PERFORM of a paragraph named `LOCATION` (continuity invariant).
- **Version matrix:** add rows to `docs/VERSION_TEST_MATRIX_DESIGN.md` / `VERSION_CHANGE_REFERENCE.md`: introduction at 2023; not-present in 85/2002/2014 (both the reserved words `LOCATION`/`FINALLY` and the construct); behavior invariants for 2023.

---

## 5. Runtime — GR14–GR22 with LANDABLE-vs-STAGED boundary

### 5.1 Semantics (implementation terms)

The F3 PERFORM is a **per-statement exception interceptor scoped to imp-1**, not a block try/catch. This dictates the whole integration: do **not** wrap imp-1 in a block `try/catch(CobolException)` — a block catch unwinds past the remaining imp-1 statements and cannot deliver GR20's nonfatal resume-in-place.

- **GR14 — implicit TURN over imp-1, LOCATION-conditional.** For each exception-name named in a WHEN, if checking isn't already enabled at imp-1's first statement, an implicit `>>TURN <name> CHECKING ON` (LOCATION iff `WITH LOCATION`) is assumed over imp-1's textual extent — a scoped overlay that leaks nothing past imp-1 and removes only what it added. A real `>>TURN` on before the PERFORM survives; a purely-implicit enable is dropped. "Enabled" is a **compile-time** decision here (the `TurnState` fold gates whether a raise guard is emitted), so GR14 is a **bind-time overlay on `TurnState`**.
- **GR15** — imp-1 is the guarded body.
- **GR16** — FINALLY (imp-5) is "the end of the PERFORM"; without FINALLY, END-PERFORM is the end. No transfer of control out of imp-5 (enforced at bind: XS-FINALLY-TRANSFER-OUT, COBOLNET2927); an `EXIT PERFORM` in imp-5 degrades to CONTINUE after END-PERFORM.
- **GR17** — a WHEN match runs imp-2; any USE declarative that would otherwise match is **ignored** (the WHEN replaces the USE/`__EcDispatch` path for that condition). Match test = the USE GR3a–3g hierarchy predicate.
- **GR18** — WHEN OTHER runs imp-3 for any *enabled-at-detection* condition not named in a WHEN; USE ignored.
- **GR19** — WHEN COMMON runs imp-4 after imp-2 or imp-3 completes.
- **GR20 — resumption.** imp-1's last statement completes → end of PERFORM. If a WHEN was taken: **nonfatal** → implicit CONTINUE immediately after the statement in imp-1 that raised (resume-in-place; imp-1 is not abandoned); **fatal** → after handlers, continue per §14.6.13.1.3 (abnormal termination) unless a declarative-style RESUME redirected.
- **GR21** — ECs raised inside imp-2/3/4/5 are **not** re-caught by this PERFORM; they behave as in a Format-2 PERFORM (normal USE/fatal). Interceptor scope is imp-1 only.
- **GR22** — a WHEN's exception enabled by a real TURN *before* the PERFORM stays enabled; a real TURN *within* the range is retained; otherwise the WHEN's checking is not enabled afterward = "pop the overlay, keep the real directives".

**Precedence:** a statement's own `ON SIZE ERROR` / `AT END` / `INVALID KEY` / `ON OVERFLOW` phrase precedes the PERFORM WHEN (§14.6.13.1.3 #1 / .4 #1). The emitter already gates via `if (!hasPhrase)`, so the WHEN interceptor slots into the same `!hasPhrase` branch — no new precedence logic.

**WHEN matching = USE GR3a–3g predicate minus the declarative-ordering tiers:** a level-3 name matches itself; a level-2 name covers its children (`ExceptionCatalog.UnderLevel2`); level-1 `EC-ALL` covers all; a file-scoped operand additionally requires the raised condition be associated with that file. `__EcDispatch`'s Tier conditions and `TurnState.NameMatches` already implement this. For WHEN phrases the cross-declarative ordering becomes simply **written order, first match wins**.

### 5.2 Integration seams (the interceptor mechanism)

Reuse the existing per-statement dispatch-result protocol (`-1` continue / `-2` resume-next / `-3` … / `≥0` pc) and `ResumeSignal` — do not invent new control machinery.

1. **New bound node `BoundExceptionPerform`** (`ControlFlowBinder` + `EcBinder`) carrying: bound imp-1 list; ordered WHEN descriptors `(MatchKind, names/files/modes, bound imp-2)`; bound OTHER (imp-3), COMMON (imp-4), FINALLY (imp-5) blocks; `WithLocation` flag.

2. **GR14 overlay (bind time):** before binding imp-1, push an overlay of `(name, withLocation)` for each WHEN-named exception onto the `EcState`/`TurnState`; seed the fold as a synthetic enabling event at line 0 so a real source `>>TURN OFF` inside imp-1 overrides it (GR14) and real enables persist (GR22). **Pop after imp-1.** imp-2/3/4/5 bind **without** the overlay (GR21).

3. **RESUME in a WHEN (binder):** relax `EcBinder.BindResume`'s SR1 ("declarative only") to also accept "inside an F3 PERFORM WHEN/OTHER/COMMON" — **but only `RESUME NEXT STATEMENT`** there (XS-RESUME-OPERAND rejects `RESUME AT proc`). The `ResumeSignal(targetPc)` path (a real pc jump) is bound **only for declarative RESUME**.

4. **Ambient F3-frame stack in `ExceptionEngine`/`ExceptionState`** (run-unit-scoped, push/pop nesting- and CALL-safe): each frame carries a matcher delegate the emitted PERFORM installs — a closure `(string ec, string? file, bool fatal) => int` that does the written-order WHEN match, runs the matching imp-2 (or imp-3 for OTHER) then imp-4 (COMMON) inline, and returns the dispatch-protocol action. A `Handling` flag makes the frame transparent while its own handler bodies run (GR21).

5. **Emit shape** (inside the paragraph dispatcher `case`):
   ```
   __ecFrames.Push(new PerformFrame((ec,file,fatal) => {
       if (__whenN_matches(ec,file)) { /* imp-2 inline */ ...; return __action; }
       ... /* WHEN OTHER → imp-3 */
       return PerformFrame.NoMatch;   // fall through to __EcDispatch (USE)
   }));
   try { /* imp-1, compiled with the GR14 overlay */ }
   finally { __ecFrames.Pop(); }
   /* imp-5 FINALLY block (trailing, reached on normal fall-through) */
   __perfEnd{n}: ;
   ```

6. **Interception wrapper:** replace bare `__EcDispatch(ec, file)` at raise sites with `__EcPerform(ec, file, fatal)` = "if a non-`Handling` F3 frame is on top and its matcher handles `(ec,file)`, return that action (skip USE — GR17/18); else `__EcDispatch(ec,file)`." One-line substitution at each existing dispatch site in `EcEmitter` (`EmitRaise`, `EmitSizeHandling`, `EmitOverflow`, `EmitArgOrPlain`) and `__IoCheckEc`. Existing `!hasPhrase` gate preserved → statement-own-phrase precedence for free.

7. **Resume/return mapping (all via the existing protocol):**
   - nonfatal handled → matcher returns `-1`; raise site continues after the raising statement → GR20 resume-in-place, automatic.
   - `RESUME NEXT STATEMENT` in a WHEN → matcher returns `-2`; raise site falls through past the raising statement, suppressing fatal termination → GR20 / §14.9.33 GR2.
   - **`RESUME AT proc` — NOT reachable from a WHEN body** (bind-rejected by XS-RESUME-OPERAND). The `ResumeSignal(targetPc)` throw→`catch(ResumeSignal){ __pc = rs.TargetPc; break; }` at the PERFORM boundary exists **only for the declarative RESUME path**.
   - fatal, unresumed → matcher returns `-1` after handlers; raise site's existing `throw CobolFatalException` fires; `finally` still pops the frame.

8. **GR21** — `Handling` flag around inline handler bodies; `__EcPerform` skips a `Handling` frame; imp-2..5 also bound without the overlay (belt and suspenders).

9. **EXIT PERFORM** — `BoundExitPerform` currently emits `break` (correct for a C# loop, wrong inside an F3 dispatcher body). Give the emitter a "current F3 PERFORM end label"; inside an F3 body a non-CYCLE `EXIT PERFORM` emits `goto __perfEnd{n};` (the `finally` runs on the way out → frame pop guaranteed). `EXIT PERFORM` in imp-5 → CONTINUE (fall through past END-PERFORM), GR16. `EXIT PERFORM CYCLE` is bind-rejected (XS-EXIT-PERFORM-CYCLE), so it is never emitted.

10. **GR22** — satisfied by popping the GR14 overlay; no runtime code.

### 5.3 EC-I-O-WARNING end-of-PERFORM turn-off (adversarial item, resolved here)

§…L4937: `EC-I-O-WARNING` may be turned **off** only by an explicit `>>TURN` directive **or by the end of an exception-checking PERFORM**. The GR14 overlay as described pops at the **end of imp-1**, which for this one EC is *earlier* than "end of the whole PERFORM." **Resolution:** for an `EC-I-O-WARNING` WHEN operand specifically, the overlay disable is deferred to the **end of the whole PERFORM** (after FINALLY), not the end of imp-1. Because "enabled" is a compile-time `TurnState` decision and imp-2..5 bind without the general overlay anyway, this is a targeted overlay-scope extension for `EC-I-O-WARNING` only; record it in the design doc and cover it with a test. (For all other ECs, end-of-imp-1 pop is correct.)

### 5.4 LANDABLE vs STAGED boundary

**LANDS NOW (one change set — the tractable core):**
- Grammar + tokens + registry + version gate (§1–§4).
- All syntax-rule diagnostics COBOLNET1597–1605 (§3) + the RESUME `AT?` fix.
- `BoundExceptionPerform` binder with the GR14 `TurnState`-seed overlay and WHEN descriptors reusing `ExceptionCatalog.UnderLevel2` / level checks.
- RESUME-in-WHEN relaxation limited to `NEXT STATEMENT`.
- Ambient F3-frame stack + `__EcPerform` wrapper wired into the **common raise families**: RAISE, EC-SIZE, EC-OVERFLOW-STRING/-UNSTRING, the fatal ambient gates (EC-ARGUMENT-FUNCTION, EC-BOUND-REF-MOD), and EC-I-O via `__IoCheckEc`.
- RESUME NEXT via `-2`; nonfatal resume-in-place; EXIT-PERFORM goto-end; FINALLY as a trailing block; GR22 via overlay pop; EC-I-O-WARNING scope (§5.3).
- A conformance test (§7).

This works because the per-statement dispatch protocol and `ResumeSignal` already deliver the resume-in-place and RESUME semantics GR20 / §14.9.33 require — the F3 PERFORM is largely a *reordering* of which handler (frame vs `__EcDispatch`) fires first.

**STAGED (documented P14 / next-wave GAPs, each with a `COBOLNET0899` "documented non-support in current build" disposition emitted at the relevant construct so no silent mis-compile occurs):**

1. **`WHEN EXCEPTION INPUT/OUTPUT/I-O/EXTEND` open-mode operand form** — matching an EC-I-O by the raising file's *current open mode* needs the runtime matcher to query the connector's open mode at the raise site (parallel to `__IoCheckEc`'s open-mode tier, on the WHEN path). Disposition: emit **COBOLNET0899** on this operand form until staged; stage after the name-list/FILE forms land. *(Note: the grammar in §1.4 already parses this form; the GAP is the runtime matcher, so 0899 is emitted at bind for the mode-operand form only.)*
2. **Exhaustive raise-site sweep** — every remaining site that today calls `__EcDispatch` or throws `CobolFatalException` (EC-DATA-CONVERSION, EC-BOUND-OVERFLOW, EC-RANGE-*, and CALL/CANCEL-raised EC-PROGRAM/EC-EXTERNAL surfacing as `CobolCallException` across an activation boundary) must also route through `__EcPerform`. The cross-CALL cases interact with the PERFORM "range" (GR1: performed declaratives / called elements are in range) and frame-stack save/restore across `CallProgram`. Disposition: the un-swept ECs simply fall to normal USE/fatal (a documented behavioral GAP, not a mis-compile); tracked as a mechanical sweep + focused cross-activation design.
3. **FINALLY-on-abnormal-termination / RESUME-AT-bypasses-FINALLY** — a genuine spec ambiguity: NOTE 8 says end-of-PERFORM *includes* FINALLY (implying it runs on the fatal-terminate path); NOTE 9 says a transfer out during WHEN processing (a declarative `RESUME AT proc`) never hits the PERFORM exit (implying FINALLY is bypassed). This is a GENUINE STANDARD CONTRADICTION (the §14.9.28.4 GR18/NOTE-pair defect already recorded in DEVLOG 925 / CONFORMANCE.md) — not an owner question. Per the standing rule (record the defect, choose a behavior, never silently code around it): the CHOSEN default is **FINALLY runs on the normal fall-through path only** (it does NOT run on the fatal abnormal-termination path). Record this choice as a documented standard-defect disposition in `COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` when the abnormal-termination path lands; revisit only if the four-edition inventory surfaces a conformance test that pins the other reading.
4. **`>>PROPAGATE`** interactions with active F3 frames — rides the PROPAGATE wave, not this change set. ✅ **EC-FLOW-USE (§14.9.49.4 GR2) LANDED** (kb/Work PB368): the raise lives in the ONE `__RunUse` re-entrancy guard and reaches declaratives through `EcDispatchExpr`, so in an F3 unit it is `__EcPerform` that sees it first — the frame interaction this item staged.
5. **Exception *object* raised inside imp-1** — GR17 matches exception *names*; an object raise must bypass the F3 frame and fall to `__EcObjDispatch` (Format 4). Small explicit test; can ride the core or stage.

### 5.5 Key files
- Grammar: `src/Cobol.Net.Frontend/Grammar/Core/CobolControlFlow.g4` (`performStatement` :18) + `CobolParserCoreBase.cs` (`whenOperandAhead()` near :172).
- Binder: `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/ControlFlowBinder.cs` (`BindPerform` :123, and the ≥1-WHEN check in `BindPerformControl` :156); `.../Verbs/EcBinder.cs` (`BindResume` :119, overlay/`EcWrap` :289).
- Overlay host: `src/Cobol.Net.Compiler/Binding/TurnState.cs` (`Fold` :83).
- Emitter: `src/Cobol.Net.Compiler/CodeGen/EcEmitter.cs` (raise sites + `EmitDispatchSelector` :253); `.../Verbs/ControlFlowEmitter.cs` (`EmitPerform` :80).
- Runtime: `src/Cobol.Net.Runtime/Exceptions/ExceptionState.cs` (add the F3-frame stack); `.../Control/Signals/ResumeSignal.cs`.
- Dispatch/`__RunUse`: `src/Cobol.Net.Compiler/CodeGen/DispatchEmitter.cs` (:163).
- `BoundExitPerform`: `.../Binding/Bound/BoundTree.cs:517` + emit at `.../CodeGen/StatementEmitter.cs:112`.
- Diagnostics: `src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs` (mint 1585–1605; catalog-drift guard).
- Design SSOT to update same change set: `docs/COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` D12 (:220).

---

## 6. Test plan

Every post-1985 feature ships conformance tests in the same commit (`feedback_conformance_tests_per_feature`); parser + emitter + output-verifying test ship together (`feedback_parse_and_emit_together`).

### 6.1 Parse tests (grammar)
- **Canonical body must parse (the defect-2 regression net):** `WHEN EC-I-O-PERMANENT-ERROR RESUME AT NEXT STATEMENT` — assert `RESUME` is imp-2, not a second exception-name. Repeat for all 11 exclusion verbs (RESUME, RAISE, VALIDATE, UNLOCK, SEND, RECEIVE, COMMIT, ROLLBACK, GET, ENTER, PARSE) as the leading verb of imp-2.
- `RESUME NEXT STATEMENT` and `RESUME AT NEXT STATEMENT` both parse (the §14.9.33.2 `AT?` fix); plus `RESUME proc` / `RESUME AT proc`.
- Each WHEN operand form: `WHEN EXCEPTION INPUT`, `... OUTPUT`, `... I-O`, `... EXTEND`; `WHEN EXCEPTION F1 F2`; `WHEN EC-BOUND-SUBSCRIPT`; `WHEN EC-I-O-PERMANENT-ERROR FILE F1 FILE F2`.
- `WITH LOCATION` and bare `LOCATION` head; the trailing `WHEN OTHER [EXCEPTION]`, `WHEN COMMON [EXCEPTION]`, `FINALLY` lines with and without the optional 2nd `EXCEPTION`.
- `WHEN EC-BOUND-SUBSCRIPT DISPLAY "x"` (pure-reserved verb needs no predicate) parses.
- `COMMON`-as-program-attribute not regressed (targeted).

### 6.2 Negative (diagnostic) tests — one per code
COBOLNET1597 (no WHEN), 1586 (mixed operand forms), 1587 (SR14 dup file-name), 1588 (SR15 dup exception-name), 1589 (SR16 EC-I-O prefix), 1590 POP, 1591 PUSH, 1592 EXIT PERFORM CYCLE, 1593 INITIATE>1, 1594 TERMINATE>1, 1595 VALIDATE>1, 1596 GO TO in WHEN, 1597 RESUME outside WHEN, **1598 `RESUME AT proc` in a WHEN** (the adversarial reconciliation — must reject), 1599 RAISE in imp-2, 1600 CLOSE>1 in imp-1, 1601 DELETE FILE>1 in imp-1, 1602 INITIALIZE dup in imp-1, 1603 MERGE in imp-1, 1604 OPEN dup in imp-1, 1605 SORT in imp-1. Plus COBOLNET0711 for an unknown exception-name operand.
- **Region discrimination:** verify a multi-file CLOSE in imp-2 (not imp-1) is *accepted* (region A only), and RAISE in imp-1 is *accepted* (region D bans only imp-2..5). Verify INITIATE>1 in *any* region is rejected (region B).

### 6.3 Behavior (output-verifying) tests
- WHEN name-list match runs imp-2; unmatched enabled condition → WHEN OTHER (imp-3); WHEN COMMON (imp-4) chains after imp-2 and after imp-3; FINALLY (imp-5) runs on normal completion.
- **Nonfatal resume-in-place:** a nonfatal EC raised by a non-last imp-1 statement runs its WHEN, then imp-1 *continues* at the next statement (assert later imp-1 side effects occur).
- **Fatal terminate:** a fatal EC runs the WHEN then terminates abnormally (no RESUME).
- **RESUME NEXT STATEMENT** in a WHEN suppresses fatal termination and falls through past the raiser.
- **GR21:** an EC raised inside imp-2 is *not* re-caught by the same PERFORM (falls to USE/fatal).
- **GR22:** an exception enabled by a real `>>TURN` before the PERFORM stays enabled after; a purely-implicit WHEN enable does not.
- **GR14 override:** a source `>>TURN OFF` inside imp-1 for a WHEN's exception disables that WHEN.
- **EC-I-O-WARNING:** overlay turn-off deferred to end of whole PERFORM (§5.3).
- **EXIT PERFORM** inside an F3 body jumps to END-PERFORM (runs FINALLY-path fall-through correctly); EXIT PERFORM in imp-5 = CONTINUE.

### 6.4 Version-matrix tests
- Construct at `--std 2023` compiles; at `--std 85|2002|2014` emits COBOLNET0900 (edition gate).
- `PERFORM LOCATION` (paragraph named LOCATION) still parses as out-of-line PERFORM at 85/2002/2014 (continuity invariant); `LOCATION`/`FINALLY` not reserved pre-2023.

### 6.5 Staged-GAP tests
- `WHEN EXCEPTION INPUT/OUTPUT/I-O/EXTEND` emits COBOLNET0899 (documented non-support) at bind until §5.4-1 lands — a test asserting the 0899 disposition, replaced by a behavior test when the mode-matcher lands.

### 6.6 Guard
Full legacy guard (`scripts/guard-fast.sh`) green before each commit (shared `.g4` + shared `resumeStatement`/RESUME touched ⇒ full legacy conformance leg, not guard-fast alone). Run the full CI-equivalent (`-c Release` leg) before push. Update DEVLOG (top, dated) and the plan §0 NEXT-FREE marker (advance to **1618** after this lands).

---

## 7. Doc & plan sweep (same change set)
- `docs/COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` D12 — fold this design in as the current-state SSOT (including the §5.3 EC-I-O-WARNING scope and the §5.4 staged GAPs with their 0899 dispositions).
- `docs/COBOLNET_REARCHITECTURE_PLAN.md` §0 — grammar-batch 7/7 (PERFORM Format-3 core landed; the exception-checking runtime staged per §5.4); NEXT-FREE diagnostic → 1618; battery + branch banner.
- `VERSION_TEST_MATRIX_DESIGN.md` / `VERSION_CHANGE_REFERENCE.md` — new rows.
- Grammar docs synced to the `.g4` changes; `DOC_INDEX.md` if any doc materially changes.
- DEFERRED (NOT owner-gated — both resolved from the spec, see above): §14.9.28.3 SR8 (nested UNTIL-EXIT — spec-resolved by the non-redundancy derivation in §3.4, orthogonal scope, tracked in the §11 analysis backlog) and the FINALLY-on-abnormal-termination path (a recorded standard-defect with the default chosen in §5.4-3).

---

## 8. AS-BUILT RUNTIME SEAM MAP (2026-07-21 runtime scout `wf_ddb8dd1e-0f7` — persisted for the Track-③ resume; trust over re-derivation)

> The binder is COMPLETE; the emitter `ControlFlowEmitter.EmitExceptionPerform` (`:64`) is a STUB (imp1 + FINALLY only). The interceptor's central seam is the ONE funnel below. The two findings that survived the scout (raise-site funnel + the F3-frame-stack home) are the two load-bearing ones; the dispatch protocol / ResumeSignal / `__RunUse` / BoundExceptionPerform shape are documented in §5.2 + §5.5 above and read directly this session.

### Dispatch protocol + __RunUse/ResumeSignal

**Q:** In src/Cobol.Net.Compiler/CodeGen/EcEmitter.cs, document EVERY site that emits a __EcDispatch call (or the -1/-2/-3/>=0 dispatch-result protocol) and the __IoCheckEc site — EmitRai

CRITICAL ARCHITECTURE FACT the implementer must internalize first: there is ONE funnel, not five. Every raise site in EcEmitter.cs (and the two sibling emitters) obtains its dispatch expression from the single method `EcEmitter.EcDispatchExpr(ecNameExpr, fileExpr)` at EcEmitter.cs:36-37:

    public string EcDispatchExpr(string ecNameExpr, string fileExpr) =>
        ecState.UnitHasF3 ? $"__EcDispatch({ecNameExpr}, {fileExpr})" : "-3";

So the literal text "__EcDispatch(...)" is produced in exactly ONE place in the C# source (line 37); the raise sites only choose the ecName/file arguments and consume the returned int. The §5.2 seam-6 "one substitution per site" framing is LOGICAL — the mechanical change is either (a) rewrite this funnel body to emit __EcPerform and thread a `fatal` flag as a new parameter, or (b) add a `fatal` param and emit __EcPerform. The EXCEPTION to the funnel is __IoCheckEc, which hard-codes the string "__EcDispatch(__ec!, __f)" TWICE (lines 328, 349) NOT via EcDispatchExpr — those are two separate hand-edit points.

The dispatch-result protocol (documented in the class header, EcEmitter.cs:21-25) is: -1 = declarative completed / no action → continue; -2 = RESUME AT NEXT STATEMENT → fall through past the raising statement, SUPPRESS fatal termination; -3 = no qualifying declarative; >=0 = RESUME AT procedure-name's pc (== GO TO). The universal consumption idiom at every site is:
    if (__rN >= 0) { __pc = __rN; break; }   // >=0 → pc jump
    if (__rN != -2) throw new CobolFatalException(...);   // fatal sites only; -2 suppresses the throw

=== THE FOUR EcEmitter.cs RAISE SITES (§5.2 seam-6 substitution points, LANDS-NOW core) ===

1. EmitArgOrPlain — the FATAL ambient-gate catch (EC-ARGUMENT-FUNCTION, EC-BOUND-REF-MOD). EcEmitter.cs:132.
   Exact current call text (inside `catch (CobolFatalException __af{id}) when (nameTest)`, after an optional ExceptionState.Set at 130-131):
     int __r{id} = {EcDispatchExpr(ecExpr, "\"\"")};
   Consumption (133-134):
     if (__r{id} >= 0) {{ __pc = __r{id}; break; }}   // RESUME AT procedure-name
     if (__r{id} != -2) throw;   // fatal, unresumed → re-throw to terminate
   No !hasPhrase gate here (this is a try/catch around the whole statement). fatal = TRUE (both gated ECs are fatal, Table 13). ecExpr is either the literal EC name (one gate) or `__af{id}.EcName` (two+ gates). file arg = "".

2. EmitRaise — the RAISE statement (§14.9.29). EcEmitter.cs:164.
   Exact current call text (after ExceptionState.Set at 163):
     int __r{id} = {EcDispatchExpr(CsLiteral(r.EcName), "\"\"")};
   Consumption (165-168):
     if (__r{id} >= 0) {{ __pc = __r{id}; break; }}
     if (r.Fatal)  // only when fatal
         if (__r{id} != -2) throw new CobolFatalException(...);   // 167-168
   No !hasPhrase gate. fatal = r.Fatal (a bound-node bool — the only site where fatal is a runtime-variable choice, not a constant). Note the two early-return arms at 145-159 for `!r.Enabled` (checking-off) emit NO dispatch and are out of scope for the interceptor. file arg = "".

3. EmitSizeHandling — the EC-SIZE-* family (§14.7.5). EcEmitter.cs:200.
   Gate: emitted only inside `if (!hasPhrase)` (line 198) — the statement's own ON SIZE ERROR phrase preempts (§14.6.13.1.3 #1). This is the CANONICAL !hasPhrase precedence gate the design says the WHEN interceptor slots into for free.
   Exact current call text:
     int __r{id} = {EcDispatchExpr(ecnVar, "\"\"")};
   Consumption (201-203):
     if (__r{id} >= 0) {{ __pc = __r{id}; break; }}
     if (__r{id} != -2) throw new CobolFatalException(ecnVar, "size error and not resumed ...");
   fatal = TRUE (every EC-SIZE-* is fatal, Table 13). ecName arg is the runtime local `ecnVar` (the latched size-error name), file arg = "". Enclosing gate: `if ({flag} && ({nameTest}))` at 195.

4. EmitOverflow — EC-OVERFLOW-STRING/-UNSTRING (§14.9.43/§14.9.48). EcEmitter.cs:225.
   Gate: emitted only inside `if (!hasPhrase)` (line 223) — the statement's ON OVERFLOW phrase preempts.
   Exact current call text:
     int __r{id} = {EcDispatchExpr(CsLiteral(ecName), "\"\"")};
   Consumption (226): ONLY the pc arm — NO fatal throw:
     if (__r{id} >= 0) {{ __pc = __r{id}; break; }}
   fatal = FALSE (nonfatal — execution continues either way, §14.6.13.1.4 #3/#4). Enclosing gate: `if ({ovfFlag})` at 220.

=== THE __IoCheckEc SITE (§5.2 seam-6 "and __IoCheckEc"; EC-I-O path) ===

__IoCheckEc is a GENERATED runtime METHOD, body emitted once per program by EcEmitter.EmitIoCheckEc (EcEmitter.cs:314-359). It does NOT go through EcDispatchExpr — it hard-codes the "__EcDispatch(__ec!, __f)" string in two places. Both are substitution points:

  (5a) EcEmitter.cs:328 — the '0x' EC-I-O-WARNING nonfatal path (successful completion, status begins '0'):
     int __w = {(decls.Any(d => d.EcEntries is not null) ? "__EcDispatch(__ec!, __f)" : "-3")};
   Consumed at 329: `return __w == -3 ? -1 : __w;` (nonfatal — never terminates). fatal = FALSE here.

  (5b) EcEmitter.cs:349 — the F3 tiers behind the F1 file/mode tiers (GR3c-g):
     if (__sel == -3 && __en) __sel = __EcDispatch(__ec!, __f);   // F3 tiers behind F1 (GR3c–g)
   fatal for this path = `ExceptionCatalog.IsFatalIoStatus(__st)` (already computed and available in the method body).

  Consumption of __sel across the method (352-356):
     if (__sel >= 0 || __sel == -2) return __sel;   // RESUME redirected/suppressed
     if (__en && ExceptionCatalog.IsFatalIoStatus(__st))
         throw new CobolFatalException(__ec!, "I-O status " + __st + " on " + __f ...);   // 353-355 fatal default
     return -1;   // 356
   Note the __IoCheckEc CALL site (where the returned int is consumed by the statement) is NOT in EcEmitter.cs — it is SequentialIoEmitter.EmitUseHook at SequentialIoEmitter.cs:39-41: `int __ior{id} = __IoCheckEc(...); if (__ior{id} >= 0) {{ __pc = __ior{id}; break; }}`. The fatal throw for I-O lives INSIDE __IoCheckEc (353-355), so the call site needs no fatal handling. The gate for emitting the EC-aware variant at all is `ec.IoMaskFor(file) is not 0` (SequentialIoEmitter.cs:35); AT END / INVALID KEY phrase precedence is handled INSIDE __IoCheckEc at 331-332, not by a !hasPhrase gate at the call.

=== "ANY OTHERS" — the sibling-emitter sites that ALSO funnel through ec.EcDispatchExpr (§5.4-2 STAGED sweep) ===

These live outside EcEmitter.cs but call `ec.EcDispatchExpr(...)`, so changing the funnel changes them too (see Risks). The design §5.4-2 marks them STAGED (fall to normal USE/fatal), a documented behavioral GAP:
  - PtrEmitter.cs:139 — SET…TO ENTRY EC-PROGRAM-NOT-FOUND (fatal): `int __pe{did} = {ec.EcDispatchExpr("\"EC-PROGRAM-NOT-FOUND\"", "\"\"")};` — pc arm only (140), no fatal throw.
  - PtrEmitter.cs:169 — FREE EC-STORAGE-NOT-ALLOC (nonfatal): `int __fr{id} = {ec.EcDispatchExpr("\"EC-STORAGE-NOT-ALLOC\"", "\"\"")};` — pc arm only (170).
  - CallEmitter.cs:150 — EmitProgramEcCatch (CobolCallException, fatal EC-PROGRAM-*), inside `else` of `if (hasPhrase)` (146): `int __r{id} = {ec.EcDispatchExpr($"__ce{id}.EcName", "\"\"")};` — pc arm (151) + `if (__r{id} != -2) throw new CobolFatalException(...)` (152).
  - CallEmitter.cs:177 — EmitPropagationPickup EC-OO-EXCEPTION (fatal): `int __oq{id} = {ec.EcDispatchExpr("\"EC-OO-EXCEPTION\"", "\"\"")};` — pc arm (178) + fatal throw (179-180).
  - CallEmitter.cs:186 — EmitPropagationPickup GOBACK/EXIT…RAISING propagated name: `int __pr{id} = {ec.EcDispatchExpr($"__pn{id}", "\"\"")};` — pc arm (187) + `if (__pr{id} != -2 && __pf{id}) throw ...` (188-190).

Object-dispatch sites (EmitRaiseObject:53, EmitPropagationPickup:172) use the SEPARATE funnel `ObjDispatchExpr` → `__EcObjDispatch` (Format-4), NOT __EcDispatch; §5.4-5 stages object raises to bypass the F3 frame, so leave these alone.

=== SITES THAT RAISE BUT DO NOT DISPATCH (the genuine un-swept GAP, not a substitution point) ===
The two NONFATAL ambient gates in EmitChecked (EcEmitter.cs:81-91: EC-DATA-CONVERSION, EC-BOUND-OVERFLOW) only set/reset `ExceptionState.XxxChecking` flags and emit NO EcDispatchExpr call — the runtime raise merely records last-exception status. These have no dispatch to substitute; they are the post-statement-selection GAP already logged in PHASE-13-plan-vs-spec-review.md:229 and §5.4-2.

**Risks/gotchas:** 1. FUNNEL vs PER-SITE TENSION (highest risk): the design's "one-line substitution at each site" is misleading — all four EcEmitter sites plus the five sibling sites (PtrEmitter/CallEmitter) share the SINGLE method EcDispatchExpr. Editing its body to emit __EcPerform silently sweeps the §5.4-2 STAGED sites too (they route through the frame check for free). That is arguably MORE correct, but it contradicts the design's "un-swept fall to normal USE/fatal" GAP claim — reconcile the doc, or thread a per-site opt-in. 2. THE `fatal` ARGUMENT IS NOT THREADED: EcDispatchExpr(ecNameExpr, fileExpr) has no fatal param today. You must add it and supply the constant at each site: EmitArgOrPlain=true, EmitRaise=r.Fatal, EmitSizeHandling=true, EmitOverflow=false, __IoCheckEc:349=IsFatalIoStatus(__st), __IoCheckEc:328=false. 3. UnitHasF3 GATE IS INSUFFICIENT (EcBindState.cs:41-42): when the unit has an F3 PERFORM but NO F3 USE declaratives, UnitHasF3 is false and the funnel currently emits the literal "-3" with NO call at all — so __EcPerform must ALSO be emitted on the `: "-3"` else-branch, gated on a NEW "unit HAS an F3 PERFORM" flag (proposed DispatchState.PerformWhenScopeId — does not exist yet; must be added and set by ProgramEmitter/the F3 binder). Do not simply text-replace inside the `__EcDispatch(...)` branch. 4. __IoCheckEc has TWO literal __EcDispatch strings (328, 349) that BYPASS the funnel — they are separate hand-edits and easy to miss; and __IoCheckEc is a runtime METHOD, so the frame check must be visible in its generated body (its call site is in SequentialIoEmitter, not EcEmitter). 5. ZERO-SCAFFOLDING/BYTE-IDENTICAL INVARIANT: 32 characterization snapshots + the design's "non-F3 output stays byte-identical" gate require that when no F3 PERFORM is present the emitted text is unchanged — so __EcPerform emission MUST be strictly conditional on the new per-unit F3-PERFORM flag, provably null on the existing path. 6. The two nonfatal ambient gates in EmitChecked (EC-DATA-CONVERSION, EC-BOUND-OVERFLOW, lines 81-91) emit NO dispatch — they cannot be swept by a funnel edit; they need a separate post-statement selection (already a logged GAP), so don't assume seam-6 covers them. 7. Object raises (ObjDispatchExpr/__EcObjDispatch at 41-42, 53, 172) are a DIFFERENT funnel — §5.4-5 stages object-raise-inside-imp-1 to bypass the F3 frame; do not route them through __EcPerform.

---

### ExceptionEngine/ExceptionState (the F3-frame stack home)

**Q:** Where does run-unit EC state live, how is it accessed from generated code, and where/how would the ambient F3-PERFORM frame stack (matcher delegate + Handling flag, nesting- and CA

## 1. Where run-unit EC state lives

The run-unit EC state is the class `ExceptionEngine` (in `src/Cobol.Net.Runtime/Exceptions/ExceptionState.cs`, lines 17-255). ONE instance per run unit, owned by `RunUnit.Exceptions` (`RunUnit.cs:40`: `public ExceptionEngine Exceptions { get; } = new();`). The run unit itself is ambient via `AsyncLocal<RunUnit?>` — `RunUnit.Current` (`RunUnit.cs:22-26`) lazily establishes one. So "the run unit" (and therefore the EC engine and any frame stack on it) is automatically isolated per async-flow / per embedded run unit; it is NOT a process-global static.

`ExceptionEngine` today holds: `LastName/LastFatal/LastFile/LastIoStatus/LastLocation/LastStatement` (the §14.6.13.1.1 last-exception status), `ExceptionObject`, the two mutually-exclusive propagation slots `_propagated` / `_propagatedObject`, the run-unit ambient checking flags (`ArgumentFunctionChecking`, `DataConversionChecking`, `BoundOverflowChecking`, `BoundRefModChecking`), and the EC-EXTERNAL masks (`ExternalCheckMask`, `ActivatorExternalMask`). All are plain instance fields/properties — the AsyncLocal owner is what makes them run-unit-scoped, so **there is no `Stack<T>` and no `[ThreadStatic]` anywhere yet.**

## 2. How generated code accesses it

Generated C# never names `RunUnit` or `ExceptionEngine`. It goes through the static facade `ExceptionState` (`ExceptionState.cs:262-376`), whose every member forwards to `RunUnit.Current.Exceptions` via the private shim `private static ExceptionEngine E => RunUnit.Current.Exceptions;` (line 270). Examples the F3 raise sites already emit (in `EcEmitter.cs`): `ExceptionState.Set(name, fatal, stmt, loc)`, `ExceptionState.SetObject(...)`, `ExceptionState.SetIo(...)`, `ExceptionState.ArgumentFunctionChecking = true/false`, `ExceptionState.ExceptionObject`. The generated dispatch helpers `__EcDispatch` / `__EcObjDispatch` / `__IoCheckEc` are *instance methods on the program class* (emitted by `EcEmitter`/`DispatchEmitter`), and they call `ExceptionState.*` + `ExceptionCatalog.*` for the run-unit-state and catalog pieces. So the F3 frame stack must expose (a) an instance-side store on `ExceptionEngine` and (b) name-stable static entry points on `ExceptionState`, exactly mirroring every other emitted EC surface.

## 3. Where the F3-PERFORM frame stack goes + class shape

Put the stack ON `ExceptionEngine` (run-unit-scoped for free), plus a small `PerformFrame` type, plus `ExceptionState` static delegators. Recommended additions:

```csharp
// NEW type — same file (Exceptions namespace) or a sibling PerformFrame.cs
/// One active Format-3 (exception-checking) PERFORM frame (ISO §14.9.28.4 GR14-22).
public sealed class PerformFrame
{
    /// The written-order WHEN matcher the emitted PERFORM installs. Returns the
    /// per-statement dispatch-protocol action (-1 handled-continue / -2 RESUME NEXT /
    /// >=0 pc [not reachable from a WHEN body — bind-rejected]) or NoMatch when no WHEN
    /// (nor WHEN OTHER) selects (ec,file). fatal is threaded so the matcher can honor
    /// GR20's fatal-vs-nonfatal split. file == null for a non-I-O condition.
    public required Func<string /*ec*/, string? /*file*/, bool /*fatal*/, int> Matcher { get; init; }

    /// GR21 transparency: true only while this frame's OWN handler bodies (imp-2..5)
    /// run, so an EC raised inside a handler is NOT re-caught by this same PERFORM.
    public bool Handling { get; set; }

    /// Distinct from every real action (-3 is __EcDispatch's "no declarative"; use a
    /// value that cannot collide with a pc or -1/-2/-3).
    public const int NoMatch = int.MinValue;
}
```

On `ExceptionEngine` (instance state — nesting handled by the stack, CALL-safety handled per §4):

```csharp
private readonly Stack<PerformFrame> _perform = new();

public void PushPerformFrame(PerformFrame f) => _perform.Push(f);
public void PopPerformFrame()                => _perform.Pop();

/// The single centralized "run the top eligible frame" primitive — keeps the GR21
/// Handling toggle in ONE place (singular-pattern). Peeks the topmost NON-Handling
/// frame; sets Handling around the matcher call; returns its action, or reports
/// handled=false (caller then falls to __EcDispatch). No LINQ over the stack: only the
/// top frame is eligible (an inner PERFORM shadows an outer — written/lexical nesting).
public int RunTopFrame(string ec, string? file, bool fatal, out bool handled)
{
    handled = false;
    if (_perform.Count == 0) { return PerformFrame.NoMatch; }
    var top = _perform.Peek();
    if (top.Handling) { return PerformFrame.NoMatch; }   // GR21 — transparent to its own bodies
    top.Handling = true;
    try
    {
        int a = top.Matcher(ec, file, fatal);
        if (a != PerformFrame.NoMatch) { handled = true; return a; }
        return PerformFrame.NoMatch;                     // fall through to USE (__EcDispatch)
    }
    finally { top.Handling = false; }
}

// Depth snapshot/restore hooks for the CALL boundary (see §4).
internal int PerformDepth => _perform.Count;
internal void TrimPerformTo(int depth) { while (_perform.Count > depth) _perform.Pop(); }
```

On `ExceptionState` (the emitted surface — thin delegators, same pattern as the existing 30-odd forwarders):

```csharp
public static void PushPerformFrame(PerformFrame f) => E.PushPerformFrame(f);
public static void PopPerformFrame()               => E.PopPerformFrame();
public static int  RunTopFrame(string ec, string? file, bool fatal, out bool handled)
    => E.RunTopFrame(ec, file, fatal, out handled);
```

## 4. Nesting-safety and CALL-safety

**Nesting-safe** because it's a `Stack<PerformFrame>` and every emitted F3 PERFORM pushes on entry and pops in a `finally` (see §5). A nested F3 PERFORM inside imp-1 of an outer one pushes a second frame; `RunTopFrame` peeks only the top, so the innermost active interceptor wins — exactly the lexical nesting the standard wants. The `Handling` flag independently guarantees a frame is transparent while its own handler runs (GR21).

**CALL-safe**: two layers.
- (1) Balance: because push is always paired with `finally { PopPerformFrame(); }`, an abnormal unwind through a CALL (a thrown `ProgramReturn`, `CobolFatalException`, `StopRun`, `ResumeSignal`, `CobolCallException`) still pops. So a called program can never leave the caller's stack imbalanced, and a called program's own F3 PERFORMs push/pop on the same shared stack without corrupting outer frames.
- (2) Scope at the activation boundary: the clean, spec-defaulting choice is to have `ProgramTable.CallProgram` snapshot and restore the frame extent around `inst.Call(...)`, mirroring the EXISTING `ExternalCheckMask`/`ActivatorExternalMask` save/restore already at `ProgramTable.cs:152-161`. Concretely, inside `CallProgram`: `int savedDepth = exc.PerformDepth;` before the `try`, and in the `finally` `exc.TrimPerformTo(savedDepth);`. This makes the interceptor scope per-activation by default (a called program's raise is NOT intercepted by the caller's frame), which is the safe reading; the §14.9.28.4 GR1 "called elements are in range" cross-activation behavior then becomes a deliberate future opt-in rather than an accident. This directly matches c5-DESIGN §5.4 staged-item 2, which flags "frame-stack save/restore across CallProgram" as the cross-CALL design point. For the core wave, snapshot/restore is the low-risk default.

## 5. How generated code pushes/pops a frame and calls the matcher

The interceptor is per-statement, NOT a block try/catch (a block catch cannot deliver GR20 nonfatal resume-in-place). Two generated pieces:

(a) `EmitExceptionPerform` (today a stub at `ControlFlowEmitter.cs:64-68`, which merely emits imp-1 then FINALLY) installs and tears down the frame around imp-1 emitted UNDER the GR14 overlay:

```
ExceptionState.PushPerformFrame(new PerformFrame { Matcher = (__ec, __f, __fatal) => {
    // written-order WHEN tests — the same predicate __EcDispatch uses:
    //   level-3 name → __ec == "EC-...";  level-2 → ExceptionCatalog.UnderLevel2(__ec, "EC-...");
    //   EC-ALL → true;  file-scoped operand additionally requires __f == <file key>.
    if (<when1 matches __ec,__f>) { /* imp-2 inline; Handling already set by RunTopFrame */ return <action>; }
    ... // WHEN OTHER → imp-3
    return PerformFrame.NoMatch;   // → caller falls to __EcDispatch (USE)
}});
try { /* imp-1, compiled with the GR14 TurnState overlay */ }
finally { ExceptionState.PopPerformFrame(); }
/* imp-5 FINALLY block — trailing, normal fall-through only (§5.4-3 default) */
__perfEnd{n}: ;
```

(b) At every raise site the existing `EcDispatchExpr(ec, file)` (= `"__EcDispatch(...)"` or the `-3` constant, `EcEmitter.cs:36-37`) is replaced by a new generated instance method `__EcPerform(ec, file, fatal)` that consults the top frame first, then falls to USE:

```csharp
private int __EcPerform(string __ec, string __f, bool __fatal)
{
    int __a = ExceptionState.RunTopFrame(__ec, __f.Length == 0 ? null : __f, __fatal, out bool __h);
    return __h ? __a : __EcDispatch(__ec, __f);   // GR17/18 win over USE; else -3/tiers
}
```

The return value feeds the raise site's EXISTING protocol unchanged (`EcEmitter` pattern: `int __r = <expr>; if (__r >= 0) { __pc = __r; break; }  // RESUME AT proc; if (__r != -2) throw ...  // fatal unresumed`). So: matcher returns -1 → raise site continues after the raising statement (GR20 nonfatal resume-in-place, automatic); -2 → falls through past the raiser suppressing fatal termination (RESUME NEXT STATEMENT); NoMatch → `__h==false` → `__EcDispatch` runs the USE tiers exactly as today. The one-line substitution sites are `EmitRaise`, `EmitSizeHandling`, `EmitOverflow`, the fatal ambient gates in `EmitArgOrPlain`, and `__IoCheckEc` (all in `EcEmitter.cs`), each of which already gates on `if (!hasPhrase)` so statement-own-phrase precedence is preserved for free.

Note the `Handling` toggle lives centrally in `RunTopFrame` (§3), so the matcher closure does NOT manage it — the closure just runs the handler body and returns the action; any re-raise inside that body re-enters `__EcPerform` → `RunTopFrame`, sees `top.Handling==true`, returns NoMatch, and the raise falls to an OUTER frame or `__EcDispatch` (GR21).

## 6. Emitter gating (byte-stability)

Gate all of this on "this unit HAS an F3 PERFORM," which the binder already records: `EcBindState.F3Perform` (`EcBindState.ExceptionPerform.cs:29`, doc'd at `EcBindState.cs:40-43`). Surface it to the emitter as a new `EcState.UnitHasF3Perform` flag alongside the existing `UnitHasF3`/`UnitHasF4` (`EmitterState.cs:82-87`, set in `ProgramEmitter.cs:112-113`). Then `EcDispatchExpr` becomes: emit `__EcPerform(...)` when `UnitHasF3Perform`, else the current `__EcDispatch(...)`/`-3`. A program with no F3 PERFORM emits byte-identical source (the 33 characterization tests + battery invariant). `__EcPerform` is emitted only in that unit; when the unit also has no F3 USE declaratives, `__EcDispatch` is `-3` and `__EcPerform` still resolves the frame path.

**Risks/gotchas:** 1) __EcPerform must be a GENERATED instance method, not a runtime method, because it calls __EcDispatch (a per-class generated method). Only the frame STACK + Handling toggle live in the runtime (ExceptionEngine); keep RunTopFrame as the single place the Handling flag is set/cleared (singular-pattern) so re-raise-in-handler transparency (GR21) can't drift.

2) NoMatch sentinel must not collide with real actions. -1/-2 are handled/resume, >=0 is a pc, -3 is __EcDispatch's "no declarative". Use a value like int.MinValue for PerformFrame.NoMatch AND report matched via the out-bool from RunTopFrame — do NOT overload -3, or a frame that legitimately returns -1 (handled) becomes indistinguishable from "no frame".

3) CALL-safety has two independent requirements: (a) balance via try/finally Pop on every push (covers abnormal unwind through ProgramReturn/CobolFatalException/ResumeSignal/StopRun/CobolCallException), and (b) scope via PerformDepth snapshot/TrimPerformTo in ProgramTable.CallProgram. (a) is mandatory for the core; (b) is the safe DEFAULT (per-activation scope) — the §14.9.28.4 GR1 cross-activation "in range" reading is explicitly a STAGED item (c5-DESIGN §5.4-2), so do not attempt cross-CALL interception in the core wave.

4) Lambda limitation (recorded in D12 / c5-DESIGN §5.4-note): C# cannot `goto` out of a lambda, so EXIT PERFORM inside a WHEN handler emitted as a matcher CLOSURE cannot jump to __perfEnd. The runtime class shape here (Func matcher) is agnostic, but the EMITTER must choose c5-DESIGN option (A) synthetic pc-ranges via __RunUse, or option (B) handler methods + a new ExitPerformSignal — do not ship the lambda form if imp-2..5 can contain EXIT PERFORM.

5) EC-I-O uses SetIo (file+status) — RunTopFrame's `file` param must be the connector key (pass __f, null when empty, as __IoCheckEc already threads __f). The open-mode WHEN operand form (WHEN EXCEPTION INPUT/OUTPUT/I-O/EXTEND) needs the connector's current open mode at the raise site and is a STAGED sub-GAP (c5-DESIGN §5.4-1) — the matcher for that form stays 0899 until staged.

6) Byte-stability: gate every new emission on UnitHasF3Perform; a non-F3 unit must emit zero new source (33 characterization tests + battery). The construct is currently bind-REJECTED (COBOLNET0899); flipping it to accept-and-emit is the same change set that installs __EcPerform, and requires the full legacy guard + GnuCOBOL differential (shared __EcDispatch/raise-site seams touched).

---

## 9. THE pc-RANGE INTERCEPTOR — DECISION-COMPLETE DESIGN (the runtime-wave implementation SSOT)

> Produced 2026-07-21 by an adversarially-verified design panel (3 independent designers → 3 spec/buildability
> verifiers → synthesis) + a direct spec cross-check. **This §9 is the authoritative implementation contract for
> the runtime interceptor and SUPERSEDES §5.4's open A-vs-B question** (the owner-directed pc-RANGE architecture is
> option A refined). §5 remains the feature-semantics spec; §8 remains the raise-site seam map. Every literal C#
> below is the emit contract — reproduce it exactly.
>
> **Spec facts confirmed DIRECTLY this session (not paraphrase):** (i) §14.9.49.4 GR3 is a TIERED priority scan
> (a F1-file → b F1-mode → c F3 file+L3 → d F3 file+L2 → e F3 L3 → f F3 L2 → g F3 L1/EC-ALL; "the first declarative
> that satisfies the selection criteria is executed," tiers "applied in order," source order only WITHIN a tier);
> §14.9.28.4 GR17 binds WHEN matching to exactly GR3a–g ⇒ **the matcher is tier-ordered, NOT written-order** (the
> binder's "first written-order match wins" comment is wrong for cross-tier cases). (ii) GR20 realizes the
> fatal/nonfatal split at the RAISE SITE's static fatal-ness + the existing `-1/-2` protocol ⇒ **`fatal` need NOT be
> threaded into the matcher** (dead plumbing — dropped). (iii) GR16 + NOTE 8 make FINALLY part of "the end of the
> PERFORM"; GR20's fatal branch routes to §14.6.13.1.3 (abnormal termination, never re-entering the end of PERFORM)
> ⇒ the **FINALLY-on-fatal contradiction is NOTE 8 vs GR20**, NOT NOTE 9 (which concerns a programmer's transfer
> out); chosen default = FINALLY on normal/EXIT paths only.

### 9.0 Architecture summary

| Piece | Home | Mechanism | Spec |
|---|---|---|---|
| **imp-1** (guarded body) | INLINE in the host paragraph `case`, inside a `try` | ordinary emission under the GR14 overlay (already bound) | GR15 |
| **WHEN selection** (tier-ordered match + COMMON compose) | a `PerformFrame.Matcher` **closure** — pure match arithmetic + `__RunUse` calls, NO goto/RESUME/EXIT inside it | `RunTopFrame` walks the frame stack | GR17→§14.9.49.4 GR3a-g, GR19 |
| **imp-2 / imp-3 / imp-4 bodies** | **synthetic anonymous pc-range paragraphs APPENDED above the main pc space** | the existing `__RunUse(id,pc,pc)` → nested `__Dispatch` → `catch(ResumeSignal)` | GR17/18/19 |
| **imp-5 (FINALLY)** | INLINE trailing block after imp-1 | ordinary emission | GR16 |

Handler *bodies* are pc-ranges (not lambda bodies), so RESUME reuses `ResumeSignal`→`__RunUse`→`-2` verbatim and the
matcher closure contains nothing C# forbids in a lambda. The one friction (an appended handler region is on the
top-level fall-through chain) is walled off by ONE gated dispatcher-bound change. The one new control primitive is
`ExitPerformSignal` — a sanctioned sibling of `ResumeSignal`/`StopRun`/`ProgramReturn` (NOT a second dispatch
mechanism), required because EXIT-PERFORM-from-a-handler crosses the nested-`__Dispatch` C# call boundary a goto/pc
cannot.

**Resolved decisions (panel disagreements + verdict blockers/majors, each fixed here):**
1. **Placement — APPEND above main pc space + a gated WALL.** Reject plain append (BLOCKER: appended region is
   fall-through-reachable — the last real paragraph runs the handlers on implicit end-of-PD, §14.9.18). Reject the
   below-`EntryPc` pre-binding DFS (must re-derive F3 identity in a 2nd parse walk, reconcile `SentenceContext[]` vs
   `StatementBlockContext[]`, bind handler bodies OUT of lexical order — losing §8.4.2.2 in-section resolution +
   §15.30 LOCATION anchoring). APPEND binds handler bodies EXACTLY where they bind today (in `EcBindExceptionPerform`,
   correct scope/overlay/`InF3When`); the wall costs one gated line.
2. **Matcher — TIERED, not written-order** (spec-mandated, §9 preamble (i)).
3. **`__IoCheckEc` — frame consulted at the TOP**, before the F1 file/mode USE switch (§14.9.49.4 GR6 + GR17: a
   matching WHEN IGNORES the USE).
4. **`fatal` — DROPPED** (§9 preamble (ii); `EcDispatchExpr` keeps its 2-arg signature ⇒ zero caller edits).
5. **`RunTopFrame` — top-down WALK with deferred `Handling`-clear**, not "peek top" (nested F3: an EC in an inner
   imp-1 that matches only the OUTER WHEN must reach the outer handler, GR17 for the outer PERFORM).
6. **FINALLY defect — NOTE 8 vs GR20** (not NOTE 9).
7. **OO-method F3 — loud reject (keep 0899), never silent-drop** (the appended-handler pc falls outside the method's
   contiguous slice ⇒ the WHEN body would silently never run).

### 9.1 pc-range synthesis — data-flow

`CollectParagraphs(pd)` runs first, unchanged; after it `_paras.Count` (`mainCount`) and `Declaratives.Count`
(`declCount`) are frozen (`ProcedureTableBuilder.cs:86`). Handler paragraphs are bound in-context during the main
bind loop and appended after it.

**A. Per-unit side-list on `ProcedureTableBuilder`** (parallel to the appended paragraphs; NOT `_paras`, so the main
bind loop never re-binds them):
```csharp
private readonly List<BoundParagraph> _f3Handlers = [];
private readonly List<int> _f3Owners = [];              // owning PerformId per handler (parallel)
public IReadOnlyList<BoundParagraph> F3Handlers => _f3Handlers;
public IReadOnlyList<int> F3HandlerOwners => _f3Owners;

/// Register one already-bound F3 handler body (imp-2/3/4) as a synthetic, UNREFERENCEABLE pc-range paragraph
/// appended above the whole main pc space (ISO §14.9.28.4 GR17 — a WHEN handler is an inline declarative run by
/// the bounded dispatcher). Returns its pc. An empty body still gets one no-op pc (the empty-USE precedent).
public int AddF3Handler(IReadOnlyList<BoundStatement> body, int performId, int line)
{
    int pc = _paras.Count + _f3Handlers.Count;          // mainCount + ordinal (dense, collision-free)
    _f3Handlers.Add(new BoundParagraph("(exception-checking PERFORM handler)", new[] { body }, line));
    _f3Owners.Add(performId);
    return pc;
}
```
Handler `useId` (for `__useActive[id]`, reused by `__RunUse`) is DERIVED, not stored:
`useId(pc) = declCount + (pc − F3HandlerBasePc)`, where `F3HandlerBasePc` is `mainCount` (`_paras.Count`, frozen
after CollectParagraphs) as `StatementBinder` records it on `BoundProgram` (C below); `__useActive` is sized `declCount + H`.

**B. `EcBindExceptionPerform` redirects imp-2/3/4 into pc-ranges** — the in-context binding (overlay popped at the
current line 69; `InF3When` at 74) is preserved verbatim; only the DESTINATION changes from an inline list to
`AddF3Handler`, and the pc threads onto the node. imp-1 and imp-5 stay inline. Replaces the current lines 73–84:
```csharp
// OO-method F3 PERFORM is a narrow STAGED GAP (the class pc space is per-method contiguous slices; an appended
// handler pc falls outside every method slice → would silently never run). Reject loud, not drop — keep 0899.
if (ctx.CurrentMethodScope is not null)
    return F3StagedInMethodStub(p, imp1, withLocation);   // imp-1 + FINALLY inline as today

int performId = ctx.EcState.NextF3PerformId();            // per-unit counter (labels + ExitPerformSignal id)
int line = p.Start.Line;
bool savedInWhen = ctx.EcState.InF3When;
ctx.EcState.InF3When = true;
var whens = new List<BoundExceptionMatch>();
for (int i = 0; i < whenPhrases.Length; i++)
{
    var body = host.BindBlocks(whenPhrases[i].statementBlock());
    int pc = ctx.Table.AddF3Handler(body, performId, line);
    whens.Add(new BoundExceptionMatch(headers[i].Mode, headers[i].Ops, pc));
}
int? otherPc  = p.performWhenOther()  is { } o ? ctx.Table.AddF3Handler(host.BindBlocks(o.statementBlock()), performId, line) : null;
int? commonPc = p.performWhenCommon() is { } c ? ctx.Table.AddF3Handler(host.BindBlocks(c.statementBlock()), performId, line) : null;
ctx.EcState.InF3When = savedInWhen;
var final = p.performFinally() is { } f ? (IReadOnlyList<BoundStatement>)host.BindBlocks(f.statementBlock()) : null;  // imp-5 inline (GR16)

bool handlerHasExit = HandlerBodiesContainExitPerform(p);   // region-C/handler scan, stops at nested performStatement
var node = new BoundExceptionPerform(imp1, whens, otherPc, commonPc, final, withLocation, performId, handlerHasExit);
```
Binding out of the generic loop but IN lexical context is correct: overlay popped (base `TurnState`, GR21/GR22),
`InF3When` gives the RESUME-NEXT relaxation, GO TO banned (COBOLNET1608). The GR14 overlay wrapped ONLY imp-1 (current
lines 64–69), so imp-2/3/4 bind against base `TurnState` for free.

**C. `StatementBinder.Bind` appends the side-list after the main loop** and records the base pc:
```csharp
int handlerBase = bound.Count;                    // == mainCount (_paras.Count, frozen)
bound.AddRange(table.F3Handlers);                 // pcs handlerBase..handlerBase+H-1, 1:1 with allocation
return new BoundProgram(bound, table.EntryPc, table.Declaratives, Ctx.EcState.BuildFeatures(),
    DebugSubjects: table.DebugSubjects.Count > 0 ? table.DebugSubjects : null,
    F3HandlerBasePc: table.F3Handlers.Count > 0 ? handlerBase : null,
    F3HandlerOwners: table.F3Handlers.Count > 0 ? table.F3HandlerOwners : null);
```
`bound[handlerBase + k]` is handler k, matching the pc on the node. `__N = Paragraphs.Count` now includes handlers,
so `EmitDispatchMethod` emits a `case` per handler automatically; each is reached ONLY via `__RunUse(id, pc, pc)`.
Synthetics are in NO name map (unreferenceable — the `AddAnonymousParagraph` precedent). `EntryPc` and every existing
pc are untouched ⇒ byte-identity holds for non-F3 units. **The OO/method path (`StatementBinder.cs:246`) does NOT
append (F3-in-method is loud-rejected in B) — but MUST still not mis-set `F3HandlerBasePc`.**

### 9.2 Runtime additions (`src/Cobol.Net.Runtime/`)

**`PerformFrame`** (`Exceptions/PerformFrame.cs`) — matcher takes NO `fatal`:
```csharp
public sealed class PerformFrame
{
    /// Tier-ordered WHEN selector the emitted PERFORM installs. Returns the per-statement dispatch action
    /// (-1 handled-continue / -2 RESUME NEXT / >=0 pc [unreachable from a WHEN body — bind-rejected COBOLNET1610])
    /// or NoMatch when no WHEN/OTHER selects (ec,file). file==null for a non-I-O condition. (ISO §14.9.28.4 GR17-19.)
    public required Func<string /*ec*/, string? /*file*/, int> Matcher { get; init; }
    public bool Handling { get; set; }                   // GR21 transparency
    public const int NoMatch = int.MinValue;             // distinct from -1/-2/-3 and any pc
}
```

**`ExceptionEngine` additions** (`ExceptionState.cs`) — a List-backed stack (so `RunTopFrame` can index it) + the
nesting-correct top-down walk:
```csharp
private readonly List<PerformFrame> _perform = new();
public void PushPerformFrame(PerformFrame f) => _perform.Add(f);
public void PopPerformFrame()               => _perform.RemoveAt(_perform.Count - 1);
internal int  PerformDepth => _perform.Count;
internal void TrimPerformTo(int d) { while (_perform.Count > d) _perform.RemoveAt(_perform.Count - 1); }

/// Select and run the innermost matching WHEN handler (ISO §14.9.28.4 GR17 — the closest exception-checking PERFORM
/// whose imperative-statement-1 is executing; GR21 — a frame is transparent to ECs raised while it is handling).
/// Frames tried in THIS resolution stay marked Handling until it completes, so an EC raised inside a selected
/// (outer) handler is not re-caught by a skipped inner frame whose imp-1 is suspended.
public int RunTopFrame(string ec, string? file, out bool handled)
{
    handled = false;
    var marked = new List<PerformFrame>(4);              // per-raise (EC path is rare); re-entrancy-safe
    try
    {
        for (int i = _perform.Count - 1; i >= 0; i--)    // innermost → outermost
        {
            var f = _perform[i];
            if (f.Handling) continue;                    // GR21 — its own imp-1/handler is transparent
            f.Handling = true; marked.Add(f);            // deferred clear ⇒ stays Handling for the whole walk
            int a = f.Matcher(ec, file);                 // runs imp-2 (+COMMON) synchronously iff it matches
            if (a != PerformFrame.NoMatch) { handled = true; return a; }
        }
        return PerformFrame.NoMatch;                      // → caller falls to __EcDispatch (USE) / -3
    }
    finally { foreach (var f in marked) f.Handling = false; }
}
```
**Static facade** (`ExceptionState`): `PushPerformFrame` / `PopPerformFrame` / `RunTopFrame` delegators.

**`ExitPerformSignal`** (`Control/Signals/ExitPerformSignal.cs`):
```csharp
/// EXIT PERFORM raised inside a Format-3 handler pc-range (imp-2/3/4) — unwinds the nested __Dispatch/__RunUse/
/// matcher frames back to the owning PERFORM boundary, where `catch … when (Id==n)` lands control before FINALLY
/// (ISO §14.9.14.4 GR4 / §14.9.28.4 GR16). Id disambiguates nested F3 PERFORMs. EXIT PERFORM inside imp-1 or imp-5
/// is a plain goto, never this signal.
public sealed class ExitPerformSignal(int id) : Exception { public int Id { get; } = id; }
```

**CALL boundary** (`ProgramTable.CallProgram`, mirroring `ExternalCheckMask` at `ProgramTable.cs:152-161`):
`int __d = exc.PerformDepth;` before `inst.Call(...)`, `exc.TrimPerformTo(__d);` in the `finally`. Per-activation
scope (safe default; cross-CALL GR1 "in range" stays STAGED). A propagated EC at a CALL site in imp-1 still hits the
intact caller frame (§14.9.33.4 GR2a2).

### 9.3 Generated emit shapes (literal C#)

**§9.3.1 The funnel + `__EcPerform`** (per class, gated `UnitHasF3Perform`). `EcDispatchExpr` keeps its 2-arg
signature (no `fatal`, no caller edits):
```csharp
public string EcDispatchExpr(string ecNameExpr, string fileExpr) =>
    ecState.UnitHasF3Perform ? $"__EcPerform({ecNameExpr}, {fileExpr})"
    : ecState.UnitHasF3       ? $"__EcDispatch({ecNameExpr}, {fileExpr})"   // byte-identical non-F3 text
    :                           "-3";
```
```csharp
private int __EcPerform(string __ec, string __f)
{
    int __a = ExceptionState.RunTopFrame(__ec, __f.Length == 0 ? null : __f, out bool __h);
    return __h ? __a : {UnitHasF3 ? "__EcDispatch(__ec, __f)" : "-3"};   // GR17/18 win over USE; else USE/-3
}
```
The `__EcDispatch`/`-3` fallback is gated on `UnitHasF3` so a **no-declarative** F3 unit never references the
(unemitted) `__EcDispatch` — THE BLOCKER FIX, paired with §9.5's gate widening.

**§9.3.2 The tier-ordered matcher + COMMON composition** (per F3 PERFORM, `n = PerformId`). The emitter builds
`(tier, whenIdx, opIdx, testExpr, imp2Pc)` for every operand, **sorts by (tier, whenIdx, opIdx)**, emits arms in
that order. **AS-BUILT tier = the full §14.9.49.4 GR3a→g rank** (`0` bare file-name [GR3a], `1` open-mode [GR3b],
`2` file+L3 [GR3c], `3` file+L2 [GR3d], `4` L3 [GR3e], `5` L2 [GR3f], `6` L1/EC-ALL [GR3g]) — file/mode scope
outranks an exception-name, source order only WITHIN a tier. WHEN OTHER (GR18) is the final unconditional fallback:
```csharp
ExceptionState.PushPerformFrame(new PerformFrame { Matcher = (__ec, __f) =>
{
    // GR17 → §14.9.49.4 GR3c-g: tier priority (file+L3 → file+L2 → L3 → L2 → L1), source order only WITHIN a tier.
    if (__f == "MASTER" && __ec == "EC-I-O-PERMANENT-ERROR")                 // tier 0 (file+L3)
        return __RunF3(11, 40, /*common*/ 13, 42);
    if (__ec == "EC-BOUND-SUBSCRIPT")                                        // tier 2 (L3)
        return __RunF3(12, 41, 13, 42);
    if (ExceptionCatalog.UnderLevel2(__ec, "EC-BOUND"))                      // tier 3 (L2)
        return __RunF3(12, 41, 13, 42);
    return __RunF3(/*WHEN OTHER imp-3*/ 14, 43, 13, 42);                     // GR18; else: return PerformFrame.NoMatch;
}});
```
Per-operand test emit (mirrors `__EcDispatch`, `EcEmitter.cs:274-283`, so the two never drift):
```
bare file: ExceptionCatalog.IsIoName(__ec) && __f == {FileKeyExpr(f)}                    // tier 0 (GR3a)
open-mode: ExceptionCatalog.IsIoName(__ec) && __f is not null && CobolFile.OpenModeOf(__f) == {ord}  // tier 1 (GR3b)
file+L3 :  __f == {FileKeyExpr(f)} && __ec == "EC-…"                                     // tier 2 (GR3c)
file+L2 :  __f == {FileKeyExpr(f)} && ExceptionCatalog.UnderLevel2(__ec, "EC-…")         // tier 3 (GR3d)
L3      :  __ec == "EC-…"                                                                // tier 4 (GR3e)
L2      :  ExceptionCatalog.UnderLevel2(__ec, "EC-…")                                    // tier 5 (GR3f)
L1      :  true                                                                          // tier 6 (GR3g, EC-ALL)
```
A WHEN with several operands OR-joins its per-operand arms (each keyed to the same `imp2Pc`), each in its own tier.
The ONE COMMON-composition helper (per class):
```csharp
// GR19: COMMON runs only after imp-2/imp-3 COMPLETES (falls off → -1). RESUME NEXT (-2) is a transfer OUT of imp-2
// (§14.9.33) → skip COMMON, propagate -2. EXIT PERFORM never returns here (it throws).
private int __RunF3(int __u, int __pc, int __cu, int __cpc)
{
    int __a = __RunUse(__u, __pc, __pc);
    if (__a == -1 && __cpc >= 0) __a = __RunUse(__cu, __cpc, __cpc);   // imp-4; -1 → GR20, -2 → RESUME NEXT
    return __a;
}
```
**Action composition** `matcher → RunTopFrame → __EcPerform → raise site` (the raise-site consumption idiom is
UNCHANGED): `-1` at a nonfatal site (no throw) → next inline imp-1 statement = GR20 resume-in-place; `-1` at a fatal
site → `if (__r != -2) throw` fires = GR20 fatal abnormal-termination; `-2` anywhere → suppresses the throw, falls
past the raiser = §14.9.33.4 GR2; `NoMatch` → `handled=false` → `__EcDispatch`/`-3` = USE runs as today.

**§9.3.3 Host `EmitExceptionPerform`** (replaces the `ControlFlowEmitter.cs:64` stub):
```csharp
public void EmitExceptionPerform(BoundExceptionPerform p)
{
    var w = ctx.Writer; int n = p.PerformId;
    /* §9.3.2 frame install (matcher closure) emitted here */
    using (w.Block("try"))
    {
        using (w.Block("try"))
        {
            var s = dispatch.SetF3Region(F3Region.Imp1, n);          // imp-1 EXIT PERFORM → goto __f3fin{n}
            Statements.EmitStatementList(p.Imp1);                    // inline, already bound under GR14 overlay
            dispatch.RestoreF3Region(s);
        }
        if (p.HandlerHasExit)
            w.Line($"catch (ExitPerformSignal __eps{n}) when (__eps{n}.Id == {n}) {{ }}   // handler EXIT PERFORM → §14.9.14.4 GR4");
    }
    w.Line($"finally {{ ExceptionState.PopPerformFrame(); }}");
    w.Line($"__f3fin{n}: ;   // implicit CONTINUE preceding FINALLY (GR4/GR16)");
    if (p.FinallyBody is { } fb)
    {
        var s = dispatch.SetF3Region(F3Region.Finally, n);           // imp-5 EXIT PERFORM → goto __f3end{n}
        Statements.EmitStatementList(fb);                            // imp-5 inline; skipped on fatal throw
        dispatch.RestoreF3Region(s);
    }
    w.Line($"__f3end{n}: ;   // end of PERFORM");
}
```
All three NON-fatal exit paths (normal fall-off imp-1, imp-1 goto, handler throw→catch) converge on `__f3fin{n}`, so
FINALLY runs once on every non-fatal path (GR4/GR16). The frame pops in `finally` BEFORE FINALLY, so imp-5 behaves
"as if in a Format-2 PERFORM" (GR21). The nested `try` is needed so the handler `catch` sits inside the `finally`
that pops the frame. `goto __f3fin{n}`/`__f3end{n}` out of the `try`s is C#-legal (labels are outside the try; a goto
out of a try runs its finally). **FINALLY is skipped on the fatal path** — a `CobolFatalException` is not caught by
`catch (ExitPerformSignal)`, so it unwinds PAST the inline block (§9.6 Q5 default).

**§9.3.4 `BoundExitPerform` emit** (`StatementEmitter.cs:112`):
```csharp
public bool Visit(BoundExitPerform n)
{
    switch (_dispatch.F3Cur.Region)
    {
        case F3Region.Imp1:    _ctx.Writer.Line($"goto __f3fin{_dispatch.F3Cur.Id};"); return true;   // GR4
        case F3Region.Handler: _ctx.Writer.Line($"throw new ExitPerformSignal({_dispatch.F3Cur.Id});"); return true;
        case F3Region.Finally: _ctx.Writer.Line($"goto __f3end{_dispatch.F3Cur.Id};"); return true;   // GR16
        default:               _ctx.Writer.Line(n.Cycle ? "continue;" : "break;"); return false;      // UNCHANGED
    }
}
```
`F3Cur` (region + id) is set to `Imp1`/`Finally` by `EmitExceptionPerform`, to `Handler` by `EmitDispatchMethod`
around a handler `case` (via `F3HandlerOwners`), and **saved/restored to `None` by `EmitInlinePerform`/
`EmitOutOfLinePerform` around their loop bodies** — so a plain EXIT PERFORM inside a nested inline PERFORM within
imp-1 or a handler `break`s that inner loop (§14.9.14.4 GR5a), NOT the F3 PERFORM. This save/restore is load-bearing
(a single ambient flag miscompiles the nested case). Dispatcher hook, around `EmitParagraphBody`
(`DispatchEmitter.cs:115`):
```csharp
bool isHandler = bound.F3HandlerBasePc is int b && i >= b;
var s = isHandler ? dispatch.SetF3Region(F3Region.Handler, bound.F3HandlerOwners![i - b]) : default;
… EmitParagraphBody … ;
if (isHandler) dispatch.RestoreF3Region(s);
```

### 9.4 Binder / bound-node changes
```csharp
public sealed record BoundExceptionPerform(
    IReadOnlyList<BoundStatement> Imp1,
    IReadOnlyList<BoundExceptionMatch> Whens,       // operands + imp-2 pc (no inline body)
    int? OtherPc, int? CommonPc,                     // imp-3 / imp-4 pcs
    IReadOnlyList<BoundStatement>? FinallyBody,       // imp-5 stays INLINE
    bool WithLocation, int PerformId, bool HandlerHasExit) : BoundStatement;

public sealed record BoundExceptionMatch(string? OpenMode, IReadOnlyList<BoundWhenOperand> Operands, int Imp2Pc);
// BoundWhenOperand unchanged. Operands carry Ec/File → the emitter computes each operand's GR3 tier + test.
```
`BoundProgram` gains `int? F3HandlerBasePc = null` and `IReadOnlyList<int>? F3HandlerOwners = null`.

**Source-gen visitor / `StatementChildren`:** imp-2/3/4 are no longer children of `BoundExceptionPerform` (they live
in their own appended `BoundParagraph`s, walked by the per-paragraph pass). Update `BoundStores`/`UsageCollectionPass`
so `BoundExceptionPerform`'s statement-bearing children are **`Imp1` + `FinallyBody` only**; imp-2/3/4 field-usage is
collected at their synthetic paragraphs (automatic once bodies live there — **PROBE: verify no double-count**).
`BoundExceptionMatch` is no longer statement-bearing.

**`EcBindState`/`EcFeatures` flag flow (Q7):** add an 8th field `HasF3Perform` to the `EcFeatures` positional record,
to `BuildFeatures()`, AND to `.Any` (so `BinderDriver` sets `ecActive=true` → `EcState.Active=true` → the int-form
`__RunUse` is emitted — a pure `PERFORM CONTINUE WHEN EC-BOUND-SUBSCRIPT CONTINUE END-PERFORM` has NO other EC
feature, so omitting it from `.Any` selects the void `__RunUse` and the matcher won't compile). The COBOLNET0899
reject at `EcBinder.ExceptionPerform.cs:35-40` is DELETED for the program path (Incr 4) but RETAINED for the
OO-method path via `F3StagedInMethodStub` (§9.1-B).

### 9.5 `__IoCheckEc` frame-first + byte-identity gating

**§9.5.1 `__IoCheckEc`** (spec fix, gated `UnitHasF3Perform`). Per §14.9.49.4 GR6 + GR17 the enclosing PERFORM's WHEN
must be consulted BEFORE the F1 file/mode USE switch. **Warning path** (replace `EcEmitter.cs:328`):
```csharp
if (!__en) return -1;
int __wp = __EcPerform(__ec!, __f);            // GR17 — a matching WHEN ignores USE; warning is nonfatal
return __wp == -3 ? -1 : __wp;                 // (non-F3 unit: emits the current __EcDispatch/-3 line unchanged)
```
**Unsuccessful path** (restructure `EcEmitter.cs:333-356`): the F1 switch runs ONLY when no WHEN matched:
```csharp
int __sel = -3; bool __wh = false;
{ if UnitHasF3Perform: }  __sel = ExceptionState.RunTopFrame(__ec!, __f, out __wh); if (!__wh) __sel = -3;
{ if not __wh: }
    // ... existing F1 file switch (GR3a/GR5), F1 open-mode switch (GR3b/GR6), F3 __EcDispatch (GR3c-g),
    //     outer GLOBAL walk (GR4b) — byte-identical to today ...
if (__sel >= 0 || __sel == -2) return __sel;   // RESUME redirected/suppressed
if (__en && ExceptionCatalog.IsFatalIoStatus(__st)) throw new CobolFatalException(...);   // GR20 fatal default
return -1;
```
A WHEN-handled `-1` correctly falls to the fatal default (GR20 fatal → terminate). When `UnitHasF3Perform` is false
the frame block is not emitted ⇒ byte-identical to today.

**§9.5.2 Gating summary** (every new emission gated so a non-F3 unit is byte-identical):

| Emission | Gate |
|---|---|
| `EcDispatchExpr` → `__EcPerform` (else current `__EcDispatch`/`-3`) | `UnitHasF3Perform` |
| `__EcPerform`, `__RunF3` methods | `UnitHasF3Perform` |
| `__IoCheckEc` frame-first blocks (§9.5.1) | `UnitHasF3Perform` |
| `EmitExceptionPerform` frame install / try-catch(ExitPerformSignal) / finally-pop / labels / matcher | reached only for a `BoundExceptionPerform` node |
| Synthetic handler pcs + `case`s; the wall (§9.5.3); `F3Region.Handler` context | `F3HandlerBasePc is not null` |
| `BoundExitPerform` goto/throw | non-`None` `F3Cur` (`Loop` default unchanged) |
| **Outer** `EmitUseMachinery` CALL gate (`DispatchEmitter.cs:79`) | add `\|\| bound.Ec is { HasF3Perform: true }` |
| **Inner** `__useActive`/`__RunUse` gate (`:166`), sized `declCount + (Paragraphs.Count − F3HandlerBasePc)` | `decls.Count > 0 \|\| bound.Ec is { HasF3Perform: true }` |

`ProgramEmitter.cs:112`: `_ecState.UnitHasF3Perform = unit.Bound.Ec?.HasF3Perform ?? false;`. `OoEmitter.cs:200`:
`ecState.UnitHasF3Perform = false;`. Widening BOTH the outer+inner gates is the no-declarative F3 BLOCKER fix.

**§9.5.3 The fall-through wall** (`DispatchEmitter.cs:73`, gated — reproduce the EXISTING comment verbatim for
byte-identity):
```csharp
int __topExit = bound.F3HandlerBasePc is int __b ? __b - 1 : -1;
w.Line($"try {{ __Dispatch({bound.EntryPc}, {__topExit}); }} catch (ProgramReturn) {{ }}   // GOBACK / called-program EXIT PROGRAM returns to the activator here (ISO §14.9.18 GR2/GR3; §14.9.14 GR3)");
```
Non-F3 unit: `__topExit` renders as literal `-1` and the comment is verbatim ⇒ byte-identical. F3 unit:
`__exitPc = mainCount-1`; when the last real paragraph falls through it sets `__pc = mainCount` and `__atExit`
returns (`:128`) — end of run unit (§14.9.18), never a run into the appended handlers. Only the top-level call is
walled; every bounded `__RunUse`/PERFORM passes its own `__exitPc`, so PERFORM of the last paragraph is unaffected.

### 9.6 RESUME / COMMON / nonfatal / GR21 / FINALLY

- **Q3 — RESUME NEXT skips COMMON (CHOSEN INTERPRETATION, record in D12).** GR17 passes control to imp-4 "at the
  completion of the execution of imperative-statement-2"; RESUME (§14.9.33) is a transfer of control to the implicit
  CONTINUE after the raiser — imp-2 does not "complete," so the GR17→imp-4 hand-off is never taken. `__RunF3` runs
  COMMON only on `__a == -1`; a `-2` short-circuits. The standard does not state this explicitly (COMMON is nominally
  "common") ⇒ a chosen interpretation, not a bare derivation.
- **Q6 — nonfatal resume-in-place is automatic, imp-1 not abandoned.** imp-1 is inline straight-line code; a nonfatal
  raise site (`EmitOverflow`, no throw) does nothing on `-1`, so the next inline statement runs = GR20's implicit
  CONTINUE (§14.6.13.1.4 #2). No block-`try` wraps imp-1.
- **GR21 transparency + nesting** — realized by `RunTopFrame`'s top-down walk with deferred `Handling`-clear (§9.2):
  the selected frame is `Handling` during its own imp-2..5 (transparent to its own re-raises), skipped inner frames
  stay `Handling` for the whole resolution (an EC in a selected outer handler is not re-caught by a suspended inner
  frame), and an inner NoMatch falls to the outer frame (GR17 for the outer PERFORM).
- **Q5 — FINALLY: inline, normal/EXIT paths only, NOT on fatal abnormal-termination (STANDARD DEFECT, record D12).**
  A fatal, unresumed EC throws `CobolFatalException` from the raise site; the host `catch` is `catch(ExitPerformSignal)`
  only, so it unwinds PAST the inline FINALLY. §14.9.28.4 **NOTE 8** ("the end of the PERFORM statement includes the
  statements in a FINALLY phrase") vs **GR20**'s fatal branch ("execution continues as specified in 14.6.13.1.3",
  abnormal termination — never re-entering "the end of the PERFORM") cannot both hold; chosen default = normal/EXIT
  path only. (NOTE 9 is NOT the authority — it concerns a programmer's explicit transfer out during WHEN processing.)

### 9.7 Staged-GAP boundary (each a loud COBOLNET0899 / dedicated diagnostic — never silent)
✅ **The open-mode WHEN operand form LANDED** (`WHEN EXCEPTION INPUT|OUTPUT|I-O|EXTEND` — GR3b, tier 1: matches an
EC-I-O whose file is currently open in that mode via `CobolFile.OpenModeOf(__f)`; an OPEN-failure's mode is
best-effort). ✅ **F3 PERFORM inside an OO method LANDED** (the pc-slice wiring, §9.10 IMPLEMENTED — the
`F3StagedInMethodStub` 0899 is lifted). Remaining staged (kept explicit): cross-CALL / cross-INVOKE GR1 "in range"
(the per-activation `TrimPerformTo`/frame-FLOOR default isolates the callee from the activator's frames — the
cross-activation "in range" reading is the future opt-in, §5.4-2 / §9.10.1-C2);
`>>PROPAGATE` (§5.4-4 — ✅ **its EC-FLOW-USE half LANDED**, kb/Work PB368: §14.9.49.4 GR2 now raises from
the ONE `__RunUse` guard through `EcDispatchExpr`, which is `__EcPerform` in an F3 unit, so the condition meets
the active frames under GR17 before the USE declaratives — only the `>>PROPAGATE` DIRECTIVE is still
unimplemented); exception-OBJECT raise inside imp-1 (`ObjDispatchExpr`/`__EcObjDispatch` untouched, §5.4-5). NOTE: editing the single `EcDispatchExpr` funnel DOES
sweep the `PtrEmitter`/`CallEmitter` sibling sites through the frame — reconcile the §8 doc to record them as SWEPT
(more correct than §5.4-2's "un-swept" claim).

### 9.8 Increment plan (each independently buildable + wave-local-gateable; construct stays 0899-rejected until Incr 4)

| # | Increment | Lands | Gate |
|---|---|---|---|
| **1** | **Runtime-additive** | `PerformFrame`; `ExceptionEngine._perform` (List) + `Push/Pop/RunTopFrame/PerformDepth/TrimPerformTo`; `ExceptionState` delegators; `ExitPerformSignal`; `ProgramTable.CallProgram` snapshot+`TrimPerformTo`. | Runtime unit tests (`RunTopFrame` walk: innermost-wins, GR21 transparency, deferred-clear nesting, NoMatch on empty). Byte-identity trivial (nothing emitted references them). |
| **2** | **Gating flag flow** | `EcFeatures.HasF3Perform` (+`.Any`), `BuildFeatures`, `EcState.UnitHasF3Perform`, `ProgramEmitter`/`OoEmitter` wiring; `BoundProgram.F3HandlerBasePc`/`F3HandlerOwners` (always null yet). | Characterization byte-identity (flag unused). |
| **3** | **Funnel + `__EcPerform` + `__IoCheckEc` frame-first**, behind `UnitHasF3Perform`; outer+inner `EmitUseMachinery` gate widening; `__EcPerform`/`__RunF3` emission; `__useActive` sizing. All dead (no F3 accepted). | Characterization byte-identity + a hand-written USE-F3-declarative fixture proving the non-F3 funnel/`__IoCheckEc` text is unchanged. |
| **4** | **pc-range synthesis + tier matcher + un-reject (program path)** | `AddF3Handler`/side-list; `StatementBinder` append + `F3HandlerBasePc`/`Owners`; node reshape (`BoundExceptionMatch`+`Imp2Pc`, `Other/CommonPc`, `PerformId`, `HandlerHasExit`); `EcBindExceptionPerform` redirect + drop 0899 (program) + `F3StagedInMethodStub` (OO); `EmitExceptionPerform` frame install + tier-sorted matcher + **the F3Region/BoundExitPerform machinery** (fold Incr 6 in to avoid the bare-`break` infinite-loop hazard); the wall; `UsageCollectionPass`/`BoundStores` reconciliation. | Behavior tests: WHEN name-list match; **tier precedence** (`WHEN EC-ALL … WHEN EC-SIZE`→EC-SIZE; `WHEN EC-BOUND … WHEN EC-BOUND-SUBSCRIPT`→L3); OTHER; COMMON after imp-2 and imp-3; nonfatal resume-in-place; fatal terminate; **I/O WHEN preempts a matching USE**; GR21 re-raise-in-handler→USE; nested F3 outer-handles-inner; EXIT PERFORM (imp-1/handler/FINALLY + nested inline PERFORM breaks inner loop). **Full legacy guard + GnuCOBOL differential.** Characterization byte-identity for non-F3. |
| **5** | **RESUME NEXT + COMMON ordering** (isolate the test; lands in #4) | — | Behavior: RESUME NEXT in imp-2 with COMMON present → COMMON not run, imp-1 falls through past raiser. |
| **6** | **FINALLY placement + defect doc** | confirm inline FINALLY; fatal throw bypasses it; record NOTE 8 vs GR20 in D12 + `CONFORMANCE.md`. | Behavior: FINALLY on normal completion + on nonfatal resume; absent on fatal-terminate. |
| **7** | **Conformance + sweep** | conformance program (same commit); GnuCOBOL corpus rows; DEVLOG (top, dated); plan §0 NEXT; D12/`CONFORMANCE.md`; `DOC_INDEX`; this SSOT. | Full CI-equivalent `-c Release` leg + `gh run watch`; GnuCOBOL diff 0 regressions. |

### 9.9 Unresolved risks — PROBE during implementation (verify-by-RUNNING)
1. **Deep re-raise across nested handlers** — the `RunTopFrame` deferred-`Handling` walk is nesting-correct by
   reasoning; add a conformance probe (nested F3 where the outer handler raises an EC an inner skipped frame's WHEN
   would match — confirm the inner does NOT catch it). Single point to adjust if a test pins a different reading.
2. **Bare-file / open-mode operand tier — RESOLVED (tiers 0/1 per GR3a/GR3b).** `WHEN EXCEPTION file-name` maps to
   GR3a (file scope, tier 0) and `WHEN EXCEPTION mode` to GR3b (open-mode, tier 1) — both OUTRANK the exception-name
   forms (GR3c-g, tiers 2-6) per GR3's a→g order. Verified end-to-end (`OpenMode_OutranksExceptionName`,
   `BareFileWhen_...`, `OpenModeWhen_...`). ~~[former:~~ `WHEN EXCEPTION file-name` assigned tier 1.~~]~~
   Probe against GnuCOBOL for a program mixing `WHEN EXCEPTION f` with `WHEN EC-I-O-… FILE f`; record the tier in D12.
3. **`UsageCollectionPass`/`BoundStores` reconciliation** — verify imp-2/3/4 data-usage is collected exactly once
   (at the synthetic paragraphs), not double-counted off the F3 node; regenerate the source-gen visitor and diff.
4. **`EcFeatures` positional-record fan-out** — the 8th field forces edits at `BuildFeatures` + every
   `new EcFeatures(...)`/test site; a missed site is a loud compile break — enumerate before the commit.
5. **Report-writer / SORT-MERGE USE interplay in `__IoCheckEc`** — the frame-first insertion sits above the F1
   GR3a/b switch; confirm the §14.9.49.4 GR7 MERGE/SORT-invoked USE path is unaffected for an F3 unit that also SORTs.

**Files touched:** runtime — `ExceptionState.cs`, new `Exceptions/PerformFrame.cs`, new `Control/Signals/ExitPerformSignal.cs`,
`ProgramTable.cs`; compiler — `ProcedureTableBuilder.cs`, `StatementBinder.cs`, `EcBinder.ExceptionPerform.cs`,
`EcBindState.cs`, `BoundExceptionPerform.cs`, `BoundTree.cs` (`BoundProgram`, `EcFeatures`), `EcEmitter.cs`
(funnel + `__EcPerform`/`__RunF3` + `__IoCheckEc`), `DispatchEmitter.cs` (gates + wall + `__useActive` sizing +
handler-region hook), `ControlFlowEmitter.cs` (`EmitExceptionPerform`), `StatementEmitter.cs` (`BoundExitPerform`),
`EmitterState.cs`/`DispatchState` (`UnitHasF3Perform`, `F3Cur`/`SetF3Region`/`RestoreF3Region`), `ProgramEmitter.cs`/
`OoEmitter.cs` (flag set); source-gen visitor + `UsageCollectionPass`/`BoundStores`. SSOT same change set:
`docs/COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` D12 (RESUME-skips-COMMON, FINALLY-on-fatal defect, bare-file tier),
`CONFORMANCE.md`.

### 9.10 F3 PERFORM inside an OO method — the pc-slice wiring (lifts the §9.7 `F3StagedInMethodStub` 0899)

> STATUS: **IMPLEMENTED** 2026-07-22 (increments M1–M4 landed; the `F3StagedInMethodStub` 0899 is lifted — an F3
> PERFORM inside an OO method compiles clean and runs). Gate: `PerformFormat3MethodBehaviorTests` 12/12 (the imp-2..5
> matrix + the method-local per-activation capture + the §9.10.1-C2 cross-INVOKE isolation, all spec-pinned) +
> characterization 33/33 byte-identical + the comprehensive gate. This section is the as-built record; the C1/C2/C3
> corrections in §9.10.1 are folded into the code.

**The two coupled problems** (both real; the second is the deeper one the §9.1-B comment under-states):
1. **pc-reachability.** A program's `__Dispatch` covers `[0 .. Paragraphs.Count−1]` — the whole pc space, so the
   handler pcs appended at `F3HandlerBasePc = mainCount` are ordinary `case`s it can reach via `__RunUse(id, pc, pc)`.
   A CLASS has NO class-level dispatcher: each METHOD emits its OWN `__MDispatch(startPc, exitPc)` local function whose
   `switch` carries cases ONLY for that method's contiguous slice `[m.Binding.EntryPc .. EndPc]` (`OoEmitter.EmitMethod`
   → `DispatchEmitter.EmitDispatchMethod(bound, w, "int __MDispatch(...)", EntryPc, EndPc)`). A handler pc appended above
   the whole class pc space falls outside every method slice → `default: __pc = __N` → the handler silently never runs.
2. **data-scope (load-bearing).** A method's `__MDispatch` is a LOCAL FUNCTION precisely so it captures the method's
   per-activation locals (LINKAGE formals-as-locals, LOCAL-STORAGE roots re-initialized each activation §14.5.3,
   method-local INDEXED-BY cells) BY REFERENCE — that capture is what makes PERFORM recursion and the implicitly-
   RECURSIVE-method rule (§11.7 / `:12032`) structural. A WHEN handler body (imp-2/3/4) references those same
   per-activation locals, so it MUST run inside the method-local `__MDispatch`. But the program-path handler-dispatch
   machinery — `__RunUse` (calls the instance `__Dispatch`), `__RunF3` (calls `__RunUse`), `__useActive` — are CLASS
   members. A class member can neither call a method-local function nor see its per-activation locals. **So the parts of
   the F3 machinery that reach the dispatcher must themselves be method-local.**

**The singular resolution — the SAME pc-range mechanism, re-scoped per context** (NOT a second mechanism; NOT
local-function-per-handler which would fork the EXIT-PERFORM/`ExitPerformSignal` + RESUME/`ResumeSignal` handling the
pc-range path already gives us). Exactly which generated artifact lives at which scope follows the "reaches
`__MDispatch`?" test:

| Artifact | Program | Method (F3) | Why |
|---|---|---|---|
| handler pc-ranges (imp-2/3/4) in `bound.Paragraphs` | appended `[mainCount..]` | appended `[classCount..]` | one allocation scheme (`AddF3Handler`) — the binder is IDENTICAL |
| the frame `Matcher` closure (`EmitExceptionPerform`) | inline in `__Dispatch` case | inline in `__MDispatch` case | emitted where the F3 PERFORM statement is — captures the enclosing local functions |
| `__RunUse`, `__RunF3` | class members | **method-LOCAL functions** | they call `__MDispatch` and their sole caller (the Matcher / `__RunF3`) is method-local |
| `__useActive` | instance field `bool[]` | **method-LOCAL `bool[]`** | per-activation re-entrancy guard (a recursive method needs its own; a local is per-activation by construction) |
| `__EcPerform` | class member | class member | body is `RunTopFrame(ec,f,out h) ? a : (UnitHasF3 ? __EcDispatch : −3)` — reaches the handler ONLY through `RunTopFrame`→the Matcher, never `__RunF3` directly, so it is class-callable; a method has no F3 USE ⇒ its fallback is `−3`. **Emitted ONCE per class when `bound.Ec.HasF3Perform` (see §9.10.1-C1 — the class-level gate, NOT the per-method flag).** |
| `__IoCheckEc` | class member | class member — **frame-first branch present when `bound.Ec.HasF3Perform`** | its F3 branch routes through `ExceptionState.RunTopFrame` (`EcEmitter.cs:375`) + the class-member `__EcPerform` (`:336`); a class has no F1/F3 declaratives so its `EmitUseTiers` is empty. **The branch is gated on `ecState.UnitHasF3Perform` AT EMIT TIME — §9.10.1-C1 sets it from the class-level `bound.Ec.HasF3Perform` around the `:219` emission (the per-method flag is false there ⇒ WITHOUT this fix the branch is silently absent — the blocker).** |

The whole chain a raise resolves through: raise site in the method → `__EcPerform` (class) → `ExceptionState.RunTopFrame`
(runtime) → the installed frame's `Matcher` closure (method-local, captures the method's locals + `__RunF3`) → `__RunF3`
(method-local) → `__RunUse` (method-local) → `__MDispatch(hpc, hpc)` (method-local, runs the handler over its
per-activation locals). A handler GOBACK throws `MethodReturn` (`StatementEmitter.cs:254`) which propagates the whole
chain up to the method body's `catch (MethodReturn)` (`OoEmitter.cs:482`) — the exact mirror of the program path's
`ProgramReturn`→`__Activate` catch. Nothing in `RunTopFrame`'s try/finally (the deferred-`Handling` clear) swallows it.

**Binder changes** (small — the allocation is already unit-agnostic):
- `EcBindExceptionPerform` (`EcBinder.ExceptionPerform.cs:62-65`): DELETE the `ctx.CurrentMethodScope is not null` early
  return to `F3StagedInMethodStub`; a method binds the handler pc-ranges exactly like a program (the `AddF3Handler`
  block at `:71-90`). Retire `F3StagedInMethodStub`. The GR14 overlay / `InF3When` / cross-statement bans are already
  scope-independent. (`ctx.EcState.F3Perform=true` still fires — the gate.)
- `BindMethodRoster` (`StatementBinder.cs:242-253`): after the statement-bind loop, mirror the program path's
  `:163-168` — `int handlerBase = bound.Count; bound.AddRange(table.F3Handlers);` and pass
  `F3HandlerBasePc`/`F3HandlerOwners` to the class `BoundProgram` (null when `table.F3Handlers.Count == 0`, so a
  no-F3 class is byte-identical). CLASS `EntryPc` stays `0`, `Declaratives` stays `null`.
- **Per-method handler slice.** The second bind loop walks paragraphs in pc order, i.e. method-by-method, and a method's
  F3 handlers are all registered by `AddF3Handler` while that method's paragraphs bind — so the appended handler space
  `[classCount .. classCount+H−1]` is partitioned into CONTIGUOUS per-method sub-ranges in method order (PROVEN by the
  loop structure; assert it). Record each method's sub-range on `OoMethodBinding` as `HandlerStartPc` + `HandlerCount`
  (snapshot `table.F3Handlers.Count` at each method's first/again — simplest: in `EcBindExceptionPerform`, when
  `ctx.CurrentMethodScope` is set, stamp the owning method's `Binding` — but the Binding is reachable from the scope;
  ALTERNATIVELY, and preferred for zero coupling, add a parallel `List<OoMethodScope?> _f3HandlerMethod` filled by
  `AddF3Handler` from `ctx.CurrentMethodScope`, then in `BindMethodRoster` compute each method's `[HandlerStartPc,
  HandlerCount]` from it — a method with no F3 handler gets count 0). The contiguity assertion guards the design
  invariant loudly if a future reorder breaks it.

**Emitter changes** (`OoEmitter` / `DispatchEmitter` / `EcEmitter`, all gated so a no-F3 class stays byte-identical):
- `EmitTypeHalf` (`OoEmitter.cs:200-201`): keep `ecState.UnitHasF3 = false`; set `ecState.UnitHasF3Perform` PER METHOD
  (below), not for the class. If ANY method has F3 handlers, emit the class-member `__EcPerform` ONCE (a new
  `EcEmitter` entry that emits ONLY `__EcPerform`, not `__RunF3`/`__RunUse`) after the file/report members, alongside
  the existing `__IoCheckEc` emission (`:219`).
- `EmitMethod` (`OoEmitter.cs:474-484`), when `m.Binding.HandlerCount > 0`:
  - set `ecState.UnitHasF3Perform = true`, `dispatch.DeclCount = 0`, `dispatch.F3HandlerBasePc = bound.F3HandlerBasePc`
    (the CLASS base — used by `EmitDispatchMethod`'s region marking `:122` AND by `HandlerUseId = DeclCount + (pc −
    F3HandlerBasePc)`; sizing `__useActive` to the class total `H` keeps the ONE `HandlerUseId` formula — a method uses
    only its own contiguous sub-range of slots, the spare slots cost a few bytes/activation), restore after the method.
  - emit the method-LOCAL `__useActive`/`__RunUse`/`__RunF3` (a scope-parameterized `EcEmitter`/`DispatchEmitter`
    helper: same body text as the class-member form minus the `private` modifier and with `bool[] __useActive = new
    bool[H];` as a local) BEFORE the `__MDispatch` local function. **C3 CORRECTION:** the existing `__RunUse` body
    HARDCODES the literal `__Dispatch` (`DispatchEmitter.cs:203,215`) — it does NOT read `DispatchName`. The
    scope-parameterized helper MUST render `{dispatch.DispatchName}` (which is `__MDispatch` inside a method) in place of
    the literal, else a method-local `__RunUse` calls a non-existent class `__Dispatch` (C# CS0103). Generalize the
    literal to `dispatch.DispatchName` in the shared body (byte-safe: for the program path `DispatchName == "__Dispatch"`).
  - `EmitDispatchMethod` must emit the method's real slice `[EntryPc..EndPc]` AND the method's handler sub-range
    `[HandlerStartPc .. HandlerStartPc+HandlerCount−1]` (both contiguous) as `case`s in the ONE `__MDispatch` switch —
    generalize its signature to accept an optional second `(from,to)` handler range; the handler cases get the same
    `F3Region.Handler` marking (`:122-124`) the program path uses. The wall is unchanged: the method body's
    `try { __MDispatch(EntryPc, EndPc); }` bounds normal execution at the last REAL paragraph (`EndPc`); the handler
    cases sit above it, entered ONLY via `__RunUse(id, hpc, hpc)`.
- Raise sites inside the method already route through `EcDispatchExpr` (`EcEmitter.cs:39-42`) → `__EcPerform` once
  `UnitHasF3Perform` is set for the method.

**Edge cases / invariants:**
- **Multiple F3 PERFORMs / nested F3 in one method** — all handlers land in the method's contiguous sub-range; all use
  the method-local `__RunUse`/`__RunF3`. `PerformId` (`NextF3PerformId`, per-class-unit) stays unique within the class;
  the `__f3fin{n}`/`__f3end{n}` labels and `ExitPerformSignal.Id == n` guard are method-body-scoped, so even id reuse
  across methods could not cross-talk (distinct local scopes) — uniqueness is belt-and-suspenders.
- **F3 method that also has EC-I-O checking** — `__IoCheckEc` (class member) frame-first path (`:372-378`) works via
  `RunTopFrame` + `__EcPerform` (both class-callable), BUT ONLY if its frame-first branch is actually emitted — which
  requires the class-level gate of §9.10.1-C1 (the per-method flag is false at the `:219` class-scope emission). No
  method-local `__IoCheckEc` needed.
- **Cross-INVOKE / cross-CALL exception isolation** — an F3 method invoked (or reached via SELF/SUPER/factory/universal
  dispatch) during an outer element's imp-1 must NOT let its own unmatched raises be intercepted by the ACTIVATOR's
  frame (a method is a separate source element, §14.9.18.3 SR2/SR4a; ECs propagate up only via GOBACK/EXIT … RAISING).
  See §9.10.1-C2 — the per-activation frame-stack FLOOR.
- **A class with a mix** (method A: EC-I-O no F3; method B: F3) — A is byte-identical; B gets its method-local
  machinery; the class emits ONE `__EcPerform` + the existing `__IoCheckEc`. No cross-method interference.
- **Byte-identity** — a class in which NO method has an F3 PERFORM sets `F3HandlerBasePc = null`, appends no handlers,
  sets `UnitHasF3Perform=false` on every method, emits no `__EcPerform`/method-local machinery ⇒ generated source is
  byte-for-byte the pre-change output (the zero-scaffolding invariant, guarded by characterization 33/33).

**PROBES (verify-by-RUNNING):**
- P1 the per-method handler-contiguity assertion actually holds on a class with F3 in ≥2 methods and a non-F3 method
  between them.
- P2 `UsageCollectionPass`/`BoundStores` collect the handler bodies' method-local field usage exactly once at the
  synthetic handler paragraphs (same reconciliation §9.9-3, now for the class `bound.Paragraphs`) — regen the source-gen
  visitor and diff.
- P3 a handler GOBACK / EXIT PERFORM / RESUME NEXT inside a method (each throws `MethodReturn` / `ExitPerformSignal` /
  `ResumeSignal` and is caught at the right frame) — the full imp-2..5 behavior matrix, but for a method activation.
- P4 an F3 PERFORM in a FACTORY method + an INSTANCE method of the same class (two `EmitTypeHalf` halves) — both wired.
- P5 a raise inside imp-1 that occurs in a PERFORMed paragraph of the method (not inline) still reaches the frame (the
  frame is on `ExceptionState` for the whole imp-1 extent, spanning nested `__MDispatch` calls).

**Increment plan** (each independently buildable + wave-local-gateable; the construct stays 0899-rejected in a method
until M4):
- **M1** `OoMethodBinding.HandlerStartPc`/`HandlerCount` + the `_f3HandlerMethod` parallel list + the `BindMethodRoster`
  append + class `BoundProgram` F3 fields — all DEAD (still 0899-rejected, `HandlerCount` always 0). Gate: characterization
  byte-identity; a unit test on the contiguity partition.
- **M2** the scope-parameterized `__RunUse`/`__RunF3`/`__useActive`/`__EcPerform` emitters + `EmitDispatchMethod`
  second-range generalization, behind `UnitHasF3Perform`. Still dead. Gate: characterization byte-identity + a
  program-path re-emit diff (the refactor must not move the program output a byte).
- **M3** `EmitMethod` wiring (the per-method F3 context + the emitted machinery + the two-range dispatch), still gated —
  reachable only once M4 un-rejects. Gate: characterization byte-identity.
- **M4** delete the `F3StagedInMethodStub` early return (un-reject); retire the stub. Gate: NEW
  `PerformFormat3MethodBehaviorTests` (the imp-2..5 matrix + tier precedence, in a method — mirrors
  `PerformFormat3BehaviorTests`) + the reshape-regression + characterization 33/33 + **full legacy guard + GnuCOBOL
  differential** (shared EC/OO emit seams). Then the conformance program + doc sweep (this SSOT STATUS→IMPLEMENTED,
  §9.7 boundary, plan §0, `CONFORMANCE.md`).

### 9.10.1 Adversarial-review corrections (4-lens verify `wf_570480e6-06d`, folded 2026-07-22)

The C# emission model lens verified SOUND (a field-stored lambda calling a method-local function compiles — C# promotes
the referenced local function to a closure method; per-activation ref-captured locals, the `__MDispatch`↔`__RunF3`↔
`__RunUse` mutual-local-function recursion, and the two-range switch all hold). Three defects were confirmed:

- **C1 (BLOCKER — `__IoCheckEc`/`__EcPerform` frame-first gate scope; found by all 3 non-C# lenses).** `__IoCheckEc`
  (and the new class-member `__EcPerform`) are emitted at CLASS scope in `EmitTypeHalf` (`OoEmitter.cs:219`), where
  `ecState.UnitHasF3Perform == false` (set at `:201`; flipped true only PER METHOD, later, inside `EmitMethod`). The
  frame-first branches in `EmitIoCheckEc` (`:334` warning path, `:372-378` unsuccessful path) read
  `ecState.UnitHasF3Perform` AT EMIT TIME, so the class `__IoCheckEc` would emit WITHOUT them ⇒ an I/O WHEN (tiers 0-3:
  bare-file / open-mode / EC-I-O-name FILE) inside a method never preempts the (empty) USE tiers → the fatal-status
  default terminates the run instead of running imp-2. **FIX:** in `EmitTypeHalf`, gate the class-member `__IoCheckEc`
  + `__EcPerform` emission on the CLASS-level `bool classF3 = bound.Ec is { HasF3Perform: true }` (which is already true
  when ANY method has an F3 PERFORM — `EcBinder.ExceptionPerform.cs:29` sets `EcState.F3Perform` before the method-stub
  check, flowing into `BuildFeatures`), and set `ecState.UnitHasF3Perform = classF3` for the DURATION of those two
  emissions, then restore. Keep the per-method `UnitHasF3Perform` for the raise-site `EcDispatchExpr` gate (so a non-F3
  method's raises stay `-3`, preserving byte-identity) and for the method-local machinery. Byte-safe: a class with no
  F3 method keeps `HasF3Perform == false` ⇒ the flag stays false ⇒ `__IoCheckEc` renders byte-identically to today.
- **C2 (MAJOR — cross-INVOKE/cross-CALL frame-stack isolation).** `ExceptionState._perform` is one run-unit-wide stack
  and `RunTopFrame` walks ALL of it; `ProgramTable.CallProgram` only does a FINALLY-side `TrimPerformTo` (defensive
  cleanup — it does NOT hide the activator's frames DURING the callee). So an F3 method M2 invoked during outer element
  M1's imp-1 (M1's frame P1 live), raising an EC M2's own frame doesn't match (or raising OUTSIDE any imp-1 — M2's raise
  sites emit `__EcPerform` because `UnitHasF3Perform` is a per-UNIT gate), has `RunTopFrame` walk up into P1 → M1
  wrongly consumes M2's internal EC. The owner-stated default (design §9.2: "a called program's raise is not intercepted
  by the caller's frame") is the CORRECT reading; `TrimPerformTo` alone under-realizes it. **FIX (the singular, O(1),
  byte-safe mechanism):** add a per-activation FLOOR to `ExceptionEngine` — `private int _floor = 0;`,
  `RunTopFrame`'s loop becomes `for (int i = _perform.Count-1; i >= _floor; i--)`, plus `int RaisePerformFloor()` (sets
  `_floor = _perform.Count`, returns the old floor) and `RestorePerformFloor(int old)`. Raise the floor at **F3-method
  ENTRY** (emitted in `EmitMethod` for a method with `HandlerCount > 0`, gated — so byte-identical for non-F3 methods):
  `int __fl = ExceptionState.RaisePerformFloor(); try { <existing __MDispatch call + catch MethodReturn> } finally {
  ExceptionState.RestorePerformFloor(__fl); ExceptionState.TrimPerformTo(<depth-at-entry>); }`. This isolates an F3
  method from its activator's frames regardless of entry path (INVOKE/SELF/SUPER/factory/universal) — no per-INVOKE-site
  emission needed. Default `_floor = 0` ⇒ ZERO behavior change for programs and the CALL path (they never raise the
  floor — the cross-CALL "in range" reading stays the documented STAGED item, §5.4-2, unchanged). NEW probe P6: an F3
  method whose imp-1 INVOKEs a second F3 method that raises an EC the inner's WHEN does not match — confirm the inner's
  EC does NOT reach the outer method's WHEN (falls to the inner's own default), and confirm a raise in the inner F3
  method's ORDINARY code (outside any imp-1) sees no frame.
- **C3 (MINOR — see the M2/M3 emitter-changes note above):** the scope-parameterized `__RunUse` must render
  `dispatch.DispatchName`, not the hardcoded literal `__Dispatch`.

