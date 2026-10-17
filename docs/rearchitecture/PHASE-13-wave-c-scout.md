# PHASE-13 Wave C — persisted spec-first anchor re-scout

> **⚠ 2026-07-19 NOTE:** `PHASE-13-audit.md` (cited throughout as provenance) was RE-VERIFIED by the
> plan-vs-spec review and DELETED in the plan consolidation — do not look for it; the verified record is
> `PHASE-13-plan-vs-spec-review.md` and the live worklist is the plan §0. This scout stays the WORKING
> DESIGN for the remaining P13 waves; delete it at P13 close.


> **STATUS: the Wave C worklist (trusted over the drift-prone phase plan — [[feedback_persist_anchor_rescout]]).**
> Produced by an 8-agent parallel spec-first re-scout (each construct independently derived from
> `specs/ISO_COBOL.md`, CLI-probed against the as-built compiler, and adversarially checked against the
> `PHASE-13-audit.md` claims). Each section is a decision-complete derivation: exact §, quoted format,
> GR-level semantics, the below-2023 gate, file:line code anchors, a golden program + hand-derived stdout,
> and gotchas. Implement FROM this doc + the spec; do not re-derive.

## C1. Boolean shift operators B-SHIFT-L / B-SHIFT-R / B-SHIFT-LC / B-SHIFT-RC (ISO/IEC 1989:2023 §8.8.2 boolean expressions)

**Spec sections:** §8.7.2 (spec :8866-8885) — the four shift operators listed as a distinct operator class alongside binary B-AND/B-OR/B-XOR and unary B-NOT (L/LC/R/RC = SHIFT/circular SHIFT left/right).
§8.8.2 (:9322-9420) — boolean expressions: operand list (:9327-9334 includes "a boolean expression and an integer operand separated by a boolean shift operator"), formation rules 1-10.
§8.8.2 rule 2 (:9345-9354) — a sub-expression whose preceding operator is a shift shall END with ')' or an integer operand.
§8.8.2 rule 5 (:9366) — first operand shall NOT be figurative ALL literal; second operand SHALL be an integer operand.
§8.8.2 rule 6 / Table 4 (:9370-9382) — adjacency: after a shift operator ONLY "Identifier or literal" is permissible (the integer); a shift can only be preceded by an operand-end.
§8.8.2 rule 7b (:9388-9395) — precedence: B-NOT>B-AND>B-XOR>B-OR; shift precedence = "same as the preceding operation, if any; if not preceded, same as B-AND".
§8.8.2 rule 8 (:9408-9414) — the exact bit semantics of the four shifts + the repeat-N-times rule.
§8.8.2 rule 9 (:9416) — result of a shift is boolean, length = number of boolean positions of the FIRST operand.
Annex A A.7 example (:44551-44585) + Table A.2 (:44594-44605) — worked results oracle (1100 shifted by 3).
Annex (2023 new features) item 3 (:49060-49066) — "Boolean shifting operators … have been added" = the COBOL-2023 introduction proof. Also :49322-49325 and Annex E.2 (VCR row 32) reserved-word additions.

**Syntax / format:** §8.8.2 has no boxed BNF for the operators; the operand-list + Table 4 ARE the grammar. Distilled:

  boolean-expression ::= … | boolean-expression boolean-shift-operator integer-operand | …
  boolean-shift-operator ::= B-SHIFT-L | B-SHIFT-R | B-SHIFT-LC | B-SHIFT-RC

Table 4 permissible pairs (P) constrain it exactly:
 - Row "Identifier or literal" -> {B-AND B-OR B-XOR B-SHIFT-L B-SHIFT-R B-SHIFT-LC B-SHIFT-RC}: P  (a shift may FOLLOW an operand end)
 - Row "B-SHIFT-*" -> only "Identifier or literal": P; all of {B-op, B-NOT, '(', ')'}: — (a shift may be FOLLOWED ONLY by the integer identifier/literal)
 - Row ")" -> {B-op incl. shift}: P (a shift may also follow a ')')

Key asymmetry vs B-AND/B-OR/B-XOR: the SECOND operand is an INTEGER operand (rule 5), NOT a boolean factor. Spec example forms (Annex A :44567,:44570): `COMPUTE My-flag = My-flag B-SHIFT-L 2` and `COMPUTE My-flag = My-flag B-SHIFT-RC 3`. All four words are reserved words (no optional/abbreviated forms).

**Introduced edition:** 2023 — proven by the 2023 new-features annex item 3 (spec :49060 "The boolean operators B-SHIFT-L, B-SHIFT-R, B-SHIFT-LC and B-SHIFT-RC have been added") and Annex E.2 reserved-word additions (VCR row 32). NOTE: this is STRICTLY LATER than the base boolean operators B-AND/B-OR/B-XOR/B-NOT, which were introduced in COBOL-2002 (construct boolean-operators-2002, gated COBOLNET0900 below 2002). The shift ops need their OWN 2023 gate — do not fold them into the 2002 boolean-operators construct.

**Semantics:** Operand-1 = a boolean value of N positions (its "boolean digits"), read "without regard for usage" (rule 8 — BIT/DISPLAY/NATIONAL all treated as the bit string). Operand-2 = a nonnegative integer count K. One iteration (rule 8), positions numbered 1..N left(high-order)->right(low-order):
 - B-SHIFT-L (logical left): each position i := position i+1 (its immediate successor to the right) for i=1..N-1; position N (rightmost) := boolean 0; original leftmost digit discarded. (== drop MSB, append '0' at LSB.)
 - B-SHIFT-LC (circular left): same, but position N := the ORIGINAL leftmost digit (rotate, no loss).
 - B-SHIFT-R (logical right): each position i := position i-1 (immediate successor to the left) for i=2..N; position 1 (leftmost) := boolean 0; original rightmost discarded. (== drop LSB, prepend '0' at MSB.)
 - B-SHIFT-RC (circular right): same, but position 1 := the ORIGINAL rightmost digit (rotate, no loss).
If K>1 the operation is REPEATED until iterations == K (rule 8). K=0 => value unchanged. Result: a boolean value whose length is N = the number of boolean positions of the FIRST operand (rule 9) — the integer operand does not affect length. Zero-length first operand => zero-length result (NOTE 2). A logical shift by K>=N yields all boolean zeros; a circular shift by K is equivalent to K mod N (the literal repeat-loop already produces this). Concrete Annex A Table A.2 oracle with A=1100: B-SHIFT-L 3 -> 0000; B-SHIFT-R 3 -> 0001; B-SHIFT-LC 3 -> 0110; B-SHIFT-RC 3 -> 1001 (Table prints "B-SHIFT-RS" — a spec TYPO for RC). Precedence (rule 7b): a shift takes the precedence of the operator immediately to its LEFT, or B-AND's precedence if none precedes; same-precedence chains evaluate left-to-right (rule 7c).

**Below-2023 gate:** New construct required: add `BooleanShiftOperators2023 = "boolean-shift-operators-2023"` to tests/version-matrix/constructs.json (regen Constructs.g.cs), introducedIn:2023, diagnosticCode COBOLNET0900, expectDiagnostic COBOLNET0900. The gate fires via VersionConformancePass ParseArm on RECOGNITION (the DEVLOG-724 fire-on-recognition doctrine — never a bound arm, so a below-edition occurrence names its edition even if the statement also fails to bind), exactly mirroring the existing BooleanOperators2002 gate: extend the parse-arm HasBoolOp helper (VersionConformancePass.cs:1344 — currently only B_AND/B_OR/B_XOR/B_NOT token types) with a parallel HasShiftOp scan over the new B_SHIFT_* token types, and in VisitComputeStatement (:1298) and VisitPrimaryCondition (:1289) call `_p.Check(Constructs.BooleanShiftOperators2023, "the boolean shift operators (B-SHIFT-L/R/LC/RC)")` when a shift op is present. Diagnostic = COBOLNET0900 rejecting at editions 85, 2002 AND 2014 (valid ONLY at 2023). Because the lexer will tokenize B-SHIFT-L unconditionally (like B-AND), a below-2023 program does NOT silently treat it as a user word — it is recognized and gated, same as B-AND at --std 85. Ship a tests/conformance/negative/boolean_shift_below_2023.{cob,err} expecting COBOLNET0900 (probe all three lower editions in the version-matrix, matching boolean_conv_below_2002.cob's pattern).

**Code anchors:** MISSING — lexer tokens: src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4:142-145 has ONLY `B_AND/B_OR/B_XOR/B_NOT`. Add 4 rules, ORDER-SENSITIVE (longer literals FIRST so B-SHIFT-LC/RC win over any prefix): `B_SHIFT_LC : 'B-SHIFT-LC';  B_SHIFT_RC : 'B-SHIFT-RC';  B_SHIFT_L : 'B-SHIFT-L';  B_SHIFT_R : 'B-SHIFT-R';` (ANTLR first-match — memory feedback_grammar_precedence). Regen the parser (Generated/ is a build output).
MISSING — grammar tier: src/Cobol.Net.Frontend/Grammar/Core/CobolExpressions.g4:132-138 (booleanExpression/XorTerm/AndTerm/Factor). The shift's 2nd operand is an INTEGER, not a booleanFactor. Recommended: a shift-suffix tier, e.g. `booleanShiftTerm : booleanFactor ( (B_SHIFT_L|B_SHIFT_R|B_SHIFT_LC|B_SHIFT_RC) integerOperand )* ;` inserted between booleanAndTerm and booleanFactor, with `booleanAndTerm : booleanShiftTerm (B_AND booleanShiftTerm)* ;`. integer operand rule: reuse `integerLiteral` (:392) plus a data-ref for the identifier form. WARNING: this fixed placement gives shift TIGHTER-than-B-AND precedence, which is correct only for the default (no-preceding-op) case; see gotchas for the rule-7b context-sensitive deviation.
MISSING — bound node: src/Cobol.Net.Compiler/Binding/Bound/BoundTree.cs:266-269 has BoundBoolBinary/BoundBoolNot. Add `public sealed record BoundBoolShift(BoundBoolExpr Operand, char Kind, BoundExpr Count) : BoundBoolExpr;` (Kind in {'L','R','C'-variants}; Count is an integer BoundExpr). This forces the generated IBoundBoolExprVisitor to gain a Visit(BoundBoolShift) — an exhaustive-visitor COMPILE error at BooleanRenderer.cs:22 until handled (the intended loud gate; the `_ =>` was removed P7 Step 6).
MISSING — binder: src/Cobol.Net.Compiler/Binding/Procedure/Verbs/ConditionBinder.cs — add BindBoolShift producing BoundBoolShift; enforce rule 5 (reject figurative ALL as first operand; require integer 2nd operand); and EXTEND Gr3Width (:144-148) with `BoundBoolShift s => Gr3Width(s.Operand)` (rule 9 — result length = FIRST operand only, the count does NOT count). BindComputeBoolean (ArithmeticBinder.cs:160) needs no change beyond the new node flowing through.
MISSING — emitter: src/Cobol.Net.Compiler/CodeGen/Emit/BooleanRenderer.cs:22-30 RenderVisitor — add Visit(BoundBoolShift) -> RuntimeApi.BoolShift(kind, Render(operand), countExpr). Add BoolShift helper to src/Cobol.Net.Compiler/CodeGen/Roslyn/RuntimeApi.cs (nameof-anchored like BoolOp at :24-41).
MISSING — runtime: src/Cobol.Net.Runtime/Values/Text/CobolBool.cs — add ShiftLeft/ShiftRight/ShiftLeftCircular/ShiftRightCircular(string? v, int k) operating on the D-B1 '0'/'1' string (result length = v.Length; logical fills '0', circular rotates; k iterations, guard k<=0 => unchanged). Mirror the existing And/Or/Xor/Not style + Annex A Table A.2 doc-comments.
EXISTING groundwork (verified, audit-accurate): the 4 words ARE registered 2023-reserved in src/Cobol.Net.Editions/ReservedWords.Table.cs:46-49 and tests/version-matrix/reserved-words.json:302-336 (r85/r2002/r2014=false, r2023=true). No other support exists.

**Golden program:**

```
IDENTIFICATION DIVISION.
       PROGRAM-ID. BSHIFT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A   PIC 1(4) VALUE B"1100".
       01 R   PIC 1(4).
       PROCEDURE DIVISION.
       MAIN.
      *> ISO 8.8.2 rule 8 shift semantics; Annex A Table A.2 oracle (A=1100).
           COMPUTE R = A B-SHIFT-L 3.
           DISPLAY "SL3=" R.
           COMPUTE R = A B-SHIFT-R 3.
           DISPLAY "SR3=" R.
           COMPUTE R = A B-SHIFT-LC 3.
           DISPLAY "SLC=" R.
           COMPUTE R = A B-SHIFT-RC 3.
           DISPLAY "SRC=" R.
      *> Single logical left shift: drop MSB, LSB<-0 (rule 8, one iteration).
           COMPUTE R = A B-SHIFT-L 1.
           DISPLAY "SL1=" R.
           STOP RUN.
```

**Golden expected stdout:**

```
SL3=0000
SR3=0001
SLC=0110
SRC=1001
SL1=1000

(exact bytes: five lines, each terminated by the platform newline the DISPLAY writer emits; the conformance harness's boolean_ops.out shows a trailing newline after the last line.)

Byte-by-byte derivation (A = 1100, category boolean; R is PIC 1(4) so it renders as a 4-char '0'/'1' string per D-B1; DISPLAY concatenates the literal and R with NO separator — confirmed against conformance-suite/cases/8.8.4.2-Simple-relation-conditions/boolean_ops.out where `DISPLAY "AND=" R` -> `AND=0100`):
- SL3: 1100 B-SHIFT-L 3 -> 0000. §8.8.2 rule 8 (logical left, MSB discarded/LSB<-0): 1100->1000->0000->0000; == Annex A Table A.2 row "Shift left | 1100 | B-SHIFT-L | 3 | 0000". Result length 4 = first-operand length (rule 9). => "SL3=0000"
- SR3: 1100 B-SHIFT-R 3 -> 0001. rule 8 (logical right): 1100->0110->0011->0001; == Table A.2 "Shift right ... 0001". => "SR3=0001"
- SLC: 1100 B-SHIFT-LC 3 -> 0110. rule 8 (circular left rotate): 1100->1001->0011->0110; == Table A.2 "Shift left circular ... 0110". => "SLC=0110"
- SRC: 1100 B-SHIFT-RC 3 -> 1001. rule 8 (circular right rotate): 1100->0110->0011->1001; == Table A.2 "Shift right circular ... 1001" (Table prints operator "B-SHIFT-RS", a spec typo for RC). => "SRC=1001"
- SL1: 1100 B-SHIFT-L 1 -> 1000. rule 8, one iteration: leftmost '1' discarded, everything shifts left, rightmost <- boolean 0. => "SL1=1000"
```

**Gotchas / audit-drift:** 1. PRECEDENCE (rule 7b) is CONTEXT-SENSITIVE and does NOT map to a fixed grammar tier — the #1 correctness trap. A shift takes the precedence of the operator IMMEDIATELY TO ITS LEFT (B-AND's if none precedes), and same-precedence chains go left-to-right (rule 7c). So `A B-OR B B-SHIFT-L 2` must evaluate as `(A B-OR B) B-SHIFT-L 2` (shift inherits B-OR precedence), NOT `A B-OR (B B-SHIFT-L 2)`. A naive tight-binding postfix tier (recommended above for the first landing) gets the DEFAULT/unmixed case right but DEVIATES on mixed `B-OR/B-XOR ... shift`. Spec-faithful full support needs a flat/precedence-climbing bind that assigns the shift the precedence of the preceding operator. RECOMMENDATION: land unmixed shifts (golden stays unmixed to avoid the ambiguity) + either restrict/diagnose mixing or implement the rule-7b climb; document the choice. Do NOT let the golden silently encode the wrong precedence.
2. ASYMMETRIC OPERANDS: unlike B-AND/B-OR/B-XOR (boolean RHS), the shift's 2nd operand is an INTEGER (rule 5 / Table 4 — after a shift op ONLY an identifier-or-literal integer may appear, not '(', not B-NOT). Do NOT reuse booleanFactor for the RHS; the existing MakeBoolBinary/BoundBoolBinary(char Op) shape (ConditionBinder.cs:78) does NOT fit — add a distinct BoundBoolShift, not another BoundBoolBinary op char.
3. RESULT LENGTH = FIRST operand (rule 9), NOT max-of-operands. The existing Gr3Width for BoundBoolBinary takes Max(left,right) (ConditionBinder.cs:147); for a shift it must be Gr3Width(Operand) ONLY (the integer count contributes zero). Getting this wrong mis-sizes the COMPUTE F2 store (§14.9.8).
4. Rule 5 forbids figurative ALL literal as the FIRST operand (BoundBoolAll) — reject with COBOLNET1511 (mirror the rule-4 ALL check at ConditionBinder.cs:80). ALL is fine nowhere here; the count is an integer so ALL can't appear there either.
5. K edge cases are spec-underspecified beyond "repeat until iterations==K" (rule 8): K=0 => no-op; K>=N logical => all zeros; circular K => K mod N (a literal repeat-N loop yields all three correctly, so implement the naive loop rather than a modular formula). NEGATIVE K is undefined by the spec — decide (recommend: treat <=0 as no-op OR a bind diagnostic) and document; do not silently loop negative.
6. "without regard for usage" (rule 8): correct automatically on the D-B1 '0'/'1' substrate, but note if a future USAGE BIT physical packing lands, the shift still operates on boolean-digit positions, not bytes.
7. Table A.2 (:44605) prints the last operator as "B-SHIFT-RS" — a SPEC TYPO for B-SHIFT-RC (the row header and result 1001 confirm RC). Do not add a phantom RS operator.
8. Fire-on-RECOGNITION gate (DEVLOG 724): put the 2023 introduction gate in the VersionConformancePass ParseArm HasShiftOp scan, NOT a bound arm — a below-edition shift must name its edition even when binding also fails. Keep the existing HasBoolOp (B-AND family) gate separate: shift is a DIFFERENT construct/edition (2023 vs 2002), so a program using only shifts at --std 2002 must get the shift's COBOLNET0900, not the boolean-operators-2002 message.
9. Exhaustive-visitor tripwire: adding BoundBoolShift without a Visit method breaks the BooleanRenderer build (intended — BooleanRenderer.cs:16-30 removed the loud `_ =>`); also update any other IBoundBoolExprVisitor impls (Gr3Width switch, BoolExprAllLengthOne at ConditionBinder.cs:183) or they throw/mis-handle.
10. No EC is defined for boolean shift (no EC-BOUND/EC-SIZE) — do not invent one. The only compile constraints are rule 5 (integer 2nd operand, non-ALL first operand). A non-integer 2nd operand is a formation error (bind-time COBOLNET1511), not a runtime EC.
11. Ship the conformance test in the SAME commit (memory feedback_conformance_tests_per_feature): conformance-suite/cases/8.8.2-Boolean-expressions/boolean_shift.{cob,out} + the negative below-2023 gate; run the LEGACY guard because Core/*.g4 changed (memory feedback_autonomous_grammar_nist / shared-.g4 => full legacy guard).

**Complexity:** M — 4 lexer tokens + 1 asymmetric grammar tier (integer RHS) + 1 new bound node + binder/emitter/runtime shift methods + a new 2023 construct/gate + golden & negative are mechanical and mirror the proven boolean-operators-2002 path; the one genuine risk that could push toward L is the rule-7b context-sensitive precedence for shifts MIXED with B-OR/B-XOR — if full spec-faithful mixed-precedence is in scope it needs a precedence-climbing bind rather than a fixed tier.

---

## C2. USAGE ... PACKED-DECIMAL WITH NO SIGN phrase (unsigned Packed-Decimal, no sign nibble)

**Spec sections:** §13.18.60.2 General format (line 22683: the syntax diagram entry `PACKED-DECIMAL [ WITH NO SIGN ]`) — WITH NO SIGN is an optional tail ONLY on the PACKED-DECIMAL alternative.
§13.18.60.3 Syntax rule 31 (line 20397: "The symbol 'S' shall not be specified in character-string-1 when the NO SIGN phrase of the USAGE Clause is specified") — the S-prohibition.
§13.18.60.4 General rule 11 (line 22807: PACKED-DECIMAL radix-10 minimum-config storage; "If the WITH NO SIGN phrase is specified the representation ... reserves no storage for representing any sign value. The PICTURE character string ... shall not contain the symbol 'S'; the data item is always considered to have a zero, or positive value.") — the operative semantics.
Annex E.3.2 item 5 (line 49436: "The NO SIGN phrase of the USAGE clause ... enhanced to allow ... no sign value") + summary line 1223 ("Unsigned Packed-Decimal items defined by the NO SIGN phrase") — confirms 2023 new-feature.
§15.14 BYTE-LENGTH (golden probe, compile-time byte-count fold) and §14.9.24 MOVE / §8.5.1.2 algebraic sign (unsigned receiver stores the magnitude) — for the hand-derived output.
Annex A.3 item 40166:23 ("The USAGE PACKED-DECIMAL clause is dependent upon the capabilities of the processor") — packed storage is implementor-defined, so the nibble layout choice is ours to pin.

**Syntax / format:** From the §13.18.60.2 syntax diagram (line 22683), the PACKED-DECIMAL alternative of the [ USAGE IS ] clause is:

    PACKED-DECIMAL [ WITH NO SIGN ]

The whole `[ WITH NO SIGN ]` is a single optional tail attached ONLY to PACKED-DECIMAL (not to BINARY, COMP, or any other usage keyword). WITH is a reserved word appearing inside the optional bracket; by COBOL optional-word convention it is conventionally elidable, so the grammar should accept `WITH? NO SIGN` (golden uses the full `WITH NO SIGN`). Constraint SR31: character-string-1 shall not contain 'S' when NO SIGN is specified.

**Introduced edition:** 2023

**Semantics:** GR11 (§13.18.60.4): PACKED-DECIMAL uses radix 10, each digit occupying the minimum configuration (one nibble) in storage; the implementor fixes alignment/representation including any algebraic sign, allocating enough storage for the picture's max range. Standard packed (signed OR S-less-but-plain) reserves a trailing sign nibble. The WITH NO SIGN phrase changes exactly two things: (a) storage reserves NO sign nibble — the item occupies only the digit nibbles, ceil(Digits/2) bytes; (b) the item "is always considered to have a zero, or positive value" — identical value semantics to an unsigned (S-less) packed item: a received negative value is stored as its magnitude (§14.9.24 / §8.5.1.2; CobolNum.TryStore does `Int128.Abs(v)` for an unsigned receiver). SR31 forbids 'S' in the picture. Net runtime value behavior is IDENTICAL to plain unsigned packed; the ONLY observable difference is the reduced byte width (byte-length / storage layout / REDEFINES / record framing). Truncation is unchanged: the runtime truncates packed at 10^Digits (the picture digit count), and StorageLength is inert for the packed store path — so NO SIGN needs no capacity/truncation change.

**Below-2023 gate:** COBOLNET0900 (the standard new-feature introduction gate), fired by a NEW recognition-based ParseArm override `VisitUsageClause` in VersionConformancePass.cs that detects the presence of the parsed noSignPhrase child and calls `_p.Check(Constructs.UsagePackedNoSign2023, "USAGE PACKED-DECIMAL WITH NO SIGN (ISO §13.18.60.4 GR11)")`. Gating is via a NEW constructs.json row `usage-packed-no-sign-2023` ("introducedIn": 2023, "diagnosticCode": COBOLNET0900) → generated `Constructs.UsagePackedNoSign2023`. NOT a UsageConstructId row: NO SIGN is a modifier on Usage.Packed (which exists at every edition), so it cannot be keyed on the resolved Usage enum — it MUST be detected on the parse-tree phrase (recognition-based, mirroring VisitDeleteFileStatement/DeleteFile2023 at VersionConformancePass.cs:955). MUST reject (emit 0900) at --std 85, 2002, AND 2014; silent at 2023. A continuity test must confirm plain `PACKED-DECIMAL` (no NO SIGN) stays valid at all four editions.

**Code anchors:** EXISTS:
- Lexer tokens WITH/NO/SIGN already present — CobolLexer.g4:618 (WITH), :500 (NO), :583 (SIGN). No new tokens needed.
- Grammar usageClause with PACKED_DECIMAL alternatives — CobolData.g4:368 (bare `PACKED_DECIMAL`) and usageKeyword:396; full form `USAGE IS? usageKeyword binarySign?` at :347; binarySign SIGNED|UNSIGNED at :421-423.
- Usage.Packed enum — PicInfo.cs:62; ParseUsage "COMP-3"/"PACKED-DECIMAL"→Usage.Packed at PictureAnalyzer.cs:297.
- Packed StorageWidth = Digits/2+1 — PicInfo.cs:330 (unconditional; sign nibble always counted).
- ByteWidth→StorageWidth for packed — DataItem.cs:385-386.
- FUNCTION BYTE-LENGTH fold → DataItem.ByteWidth — IntrinsicBinder.cs:700 (BindByteLengthFold).
- Runtime packed store: truncates at 10^Digits and stores magnitude for unsigned — CobolNum.cs:132-134. NO runtime change needed (StorageLength is inert for packed; only WrapBinary/InBinaryRange at :145/:158 use it, for BinaryCapacity only).
- Introduction-gate pattern — VersionConformancePass.cs:955 (VisitDeleteFileStatement→Constructs.DeleteFile2023); Constructs.g.cs:15 id→constant convention; constructs.json:39-50 the delete-file-2023 row template.
- DataBinder usageClause read site — DataBinder.cs:1651-1667 (reads binarySign UNSIGNED); ParseUsage call at :1696; picture-prohibition reject blocks at :1730-1788 (the pattern for a new reject).

MISSING (to add):
- Grammar: new rule `noSignPhrase : WITH? NO SIGN ;` and thread it — full form `USAGE IS? usageKeyword binarySign? noSignPhrase?` (CobolData.g4:347) + bare `PACKED_DECIMAL noSignPhrase?` (:368). Requires ANTLR regen (both OSes).
- PicInfo: add `bool PackedNoSign` field; branch StorageWidth `Usage.Packed => PackedNoSign ? (Digits + 1) / 2 : Digits / 2 + 1` (PicInfo.cs:330).
- DataBinder: read `usage.noSignPhrase() is not null` near :1657; carry `noSign` into PicInfo build (:1790); reject NO SIGN on a non-Packed usage (new COBOLNET1565) and reject S-picture + NO SIGN per SR31 (new COBOLNET1566, after Analyze sets pic.Signed).
- VersionConformancePass: new `VisitUsageClause` ParseArm override; new constructs.json row + regen Constructs.g.cs (UsagePackedNoSign2023).
- New conformance golden conformance-suite/cases/13.18.60-USAGE/usage_packed_no_sign.{cob,out}; edition-matrix continuity/introduction rows.

**Golden program:**

```
*> ISO/IEC 1989:2023 §13.18.60.4 GR11 — PACKED-DECIMAL WITH NO SIGN reserves no sign
      *> nibble (SR31 forbids 'S'); the value is always considered zero-or-positive.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NOSIGN23.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-PLAIN   PIC 9(6) PACKED-DECIMAL.
       01 WS-NOSIGN  PIC 9(6) PACKED-DECIMAL WITH NO SIGN.
       PROCEDURE DIVISION.
       MAIN.
           MOVE -150 TO WS-NOSIGN
           DISPLAY "VAL=" WS-NOSIGN
           DISPLAY "BL-PLAIN=" FUNCTION BYTE-LENGTH(WS-PLAIN)
           DISPLAY "BL-NOSIGN=" FUNCTION BYTE-LENGTH(WS-NOSIGN)
           STOP RUN.
```

**Golden expected stdout:**

```
VAL=000150
BL-PLAIN=4
BL-NOSIGN=3

Byte derivation (each DISPLAY line + trailing newline, no inter-operand spaces, §14.9.13):
- Line 1 `VAL=000150`: literal "VAL=" (4 bytes) then WS-NOSIGN's image. MOVE -150 into an unsigned receiver stores the magnitude 150 (§14.9.24 MOVE / §8.5.1.2; GR11 "always ... zero, or positive value" — CobolNum.TryStore does Int128.Abs) → the 6-digit unsigned display image "000150".
- Line 2 `BL-PLAIN=4`: FUNCTION BYTE-LENGTH(WS-PLAIN) folds to ByteWidth = packed StorageWidth = Digits/2+1 = 6/2+1 = 4 (a plain S-less packed STILL reserves a sign nibble, GR11). Integer intrinsic prints bare (precedent: intrinsics_byte_length.out `BL-PACK=3`).
- Line 3 `BL-NOSIGN=3`: NO SIGN drops the sign nibble → StorageWidth = ceil(Digits/2) = (6+1)/2 = 3 bytes = 6 nibbles = exactly 6 digit positions.
```

**Gotchas / audit-drift:** 1. VALUE semantics are IDENTICAL to plain unsigned packed — do NOT try to make a golden distinguish them by value; the ONLY observable difference is byte width. Use FUNCTION BYTE-LENGTH (compile-time fold to ByteWidth) as the probe.
2. Byte-width delta is visible ONLY for EVEN digit counts: NO SIGN = ceil(n/2), plain = n/2+1; for odd n both equal (n+1)/2 (e.g. 9(5): both 3). The golden uses 9(6) (4→3). A 9(5) golden would show no delta — a naive test author could "prove" nothing.
3. Truncation is UNCHANGED. CobolNum.TryStore:132 truncates packed at Pow10Wide(Digits) (the picture digit count), and StorageLength is inert for the packed store path (only BinaryCapacity/WrapBinary use it). So NO SIGN must NOT be wired into any capacity/SIZE-ERROR check. WARNING: NumProfile.cs:15 documents packed capacity as "2×byteLength−1" — that is ASPIRATIONAL, not what TryStore implements; do not "fix" TryStore to consult StorageLength as part of this feature, and do not reduce StorageLength expecting a truncation change.
4. Grammar tolerance: threading `noSignPhrase?` onto the shared full-form `usageKeyword binarySign? noSignPhrase?` will grammatically accept `USAGE BINARY-CHAR WITH NO SIGN` — the binder MUST reject NO SIGN on any non-Packed usage (new COBOLNET1565), mirroring the "binarySign tolerated after any keyword, rejected for non-binary" pattern.
5. SR31 enforcement: PictureAnalyzer.Analyze sets pic.Signed=true for an 'S' picture; after Analyze, `noSign && (pic.Signed || 'S' in picture)` → reject (new COBOLNET1566). Don't let an S-picture + NO SIGN silently produce a signed-but-no-nibble contradiction.
6. Token collision risk: the SIGN token is also the head of the separate signClause (SIGN IS LEADING/TRAILING) — a distinct dataDescriptionClause alternative. NO SIGN sits INSIDE usageClause after PACKED-DECIMAL, so there is no ambiguity, but run the full legacy + conformance guard after grammar regen (shared-.g4 change). Keep grammar-doc comments in sync.
7. WITH is conventionally an optional noise word; make it `WITH?` so both `PACKED-DECIMAL NO SIGN` and `PACKED-DECIMAL WITH NO SIGN` parse; spec format shows the full `WITH NO SIGN`.
8. Deferred subsystem note: the confined-byte[] packed codec (P11 Step D, deferred) and record framing / REDEFINES must, when built, lay out a NO SIGN item as pure digit nibbles with NO trailing sign nibble — flag PackedNoSign through to that codec.
9. EC interactions: none new. NO SIGN raises no exception condition itself; EC-SIZE (SIZE ERROR) behavior is unchanged because truncation stays at the picture digit count.
10. Edition matrix: gate at 85/2002/2014 (COBOLNET0900); silent at 2023. Add a continuity test that plain PACKED-DECIMAL stays valid at all editions so the new phrase-detector doesn't over-fire on plain packed.

**Complexity:** M — small edits fanned across layers, no runtime/truncation change. Grammar: 1 new rule + thread into 2 usageClause alternatives (triggers ANTLR regen + full legacy guard). PicInfo: 1 flag + 1 StorageWidth branch. DataBinder: read the phrase + 2 loud rejects (non-packed / SR31). VersionConformancePass: 1 ParseArm VisitUsageClause override + 1 constructs.json row + regen. 1 conformance golden + edition-matrix rows. No CobolNum/NumProfile runtime change (StorageLength inert for packed).

---

## C3. CONTINUE AFTER arithmetic-expression SECONDS (timed pause) + EC-CONTINUE-LESS-THAN-ZERO / EC-CONTINUE-IMP

**Spec sections:** §14.9.9 CONTINUE statement (26615-26648): General/Format/SR/GR — the whole construct. §14.9.9.4 GR1 (26638-26646) = the timed-pause + negative-value semantics. §14.9.9.4 GR2 (26648) = implicit CONTINUE ≡ AFTER ZERO. §14.6.13.1.1 (Table 13, 24648-24650) = EC-CONTINUE / EC-CONTINUE-IMP(Imp) / EC-CONTINUE-LESS-THAN-ZERO(NF) registration. §14.6.13.1.4 = nonfatal-EC continuation path (referenced by GR1b). §7.3.25 (4975-5003) = >>TURN directive (default EC-ALL CHECKING OFF — a NF EC only fires when turned ON). §15.33 EXCEPTION-STATUS (35681-35701) = 31-char left-justified exception-name return (golden observation vector). Annex E.3.3 item 14 (50265) = "Additional functionality added to the CONTINUE statement" (the 2023 introduction). Annex F.6/D item 8 (40088) + Annex-required-doc item 39 (39355) = precision >.99 is processor-dependent; implementor must document precision(m)/max size(n). §11396/10894 = SECONDS is a context word for the CONTINUE/RETRY phrases.

**Syntax / format:** Quoted verbatim from §14.9.9.2 (spec line 26626):

CONTINUE [AFTER arithmetic-expression-1 SECONDS]

Reserved/context words: CONTINUE (reserved since 1985), AFTER (reserved), SECONDS (context word for this phrase). arithmetic-expression-1 is any arithmetic-expression. The AFTER…SECONDS phrase is optional; when omitted, SR2 makes it equivalent to AFTER 0 SECONDS. SR1: usable anywhere a conditional or imperative-statement may be used.

**Introduced edition:** 2023

**Semantics:** Plain CONTINUE (no AFTER) = no-op, continue with next executable statement (§14.9.9.1) — this leg is edition-invariant since 1985. The AFTER…SECONDS phrase is the 2023 addition. §14.9.9.4 GR1: arithmetic-expression-1 gives the seconds to SUSPEND execution. The value is conceptually stored into a temporary item PIC 9(n)V9(m) — implementor picks m≥0 (m>2 processor-dependent) and n>0, and the implementor-defined MAXIMUM meaningful value; if the expression exceeds that max, the max is stored; otherwise the value moves in via an implicit COMPUTE WITHOUT ROUNDED (i.e. fractional seconds beyond m digits are TRUNCATED, not rounded). Then: (i) if the resulting value < 0 → GR1a set it to 0; GR1b if EC-CONTINUE-LESS-THAN-ZERO checking is ENABLED, set that exception to exist and continue per §14.6.13.1.4 (nonfatal — record status, run any matching USE declarative, then continue with next statement); GR1c if not enabled, just continue with next statement. (ii) otherwise suspend execution for that many seconds, then continue with the next executable statement. GR2: an implicit CONTINUE (empty paragraph, NEXT SENTENCE target, EXIT PERFORM/PARAGRAPH fall-through, RESUME NEXT, etc.) is processed as AFTER ZERO SECONDS. EC-CONTINUE-IMP (Imp/implementor-defined "timing exception") has NO general rule raising it — it is reserved for an implementor-defined timing-facility failure and must not be raised on the normal deterministic path.

**Below-2023 gate:** A new-feature introduction gate on the AFTER…SECONDS PHRASE ONLY (plain CONTINUE stays legal at 85/2002/2014 — ReservedWords.Table.cs:95 marks it continuous since 1985). Mechanism = the parse-tree arm of VersionConformancePass: add a `VisitContinueStatement` override modelled exactly on `VisitRetryPhrase` (VersionConformancePass.cs:1252-1256) that fires `_p.Check(Constructs.ContinueAfter2023, "the CONTINUE AFTER phrase")` ONLY when the AFTER/arithmeticExpression/SECONDS children are present in the parse context (presence IS the gate — the phrase's existence is grammar-governed). Register a new construct row "continue-after-2023" in tests/version-matrix/constructs.json (introducedIn 2023, diagnosticCode/expectDiagnostic COBOLNET0900) → regenerated into ConstructRegistry.g.cs alongside retry-phrase-2002 (line 127). Result: emits COBOLNET0900 ("construct not available below its introducing edition") at --std 85, 2002, and 2014; accepted at --std 2023. Parse-arm (not bound-arm) is mandatory here — the below-edition path would drop the bound node (BoundNop) and silently lose the 0900 (the DEVLOG-724 lesson cited in the pass header).

**Code anchors:** GRAMMAR (needs extension): src/Cobol.Net.Frontend/Grammar/Core/CobolControlFlow.g4:258-260 `continueStatement : CONTINUE ;` → `continueStatement : CONTINUE (AFTER arithmeticExpression SECONDS)? ;`. LEXER TOKENS (all EXIST, none new): CobolLexer.g4:325 AFTER, :490 SECONDS, CONTINUE token exists; `arithmeticExpression` rule EXISTS (used at CobolControlFlow.g4:57). BINDER (drops the phrase today): StatementBinder.cs:293 `_ when s.continueStatement() is not null => new BoundNop()` — MISSING the AFTER handling; add a ControlFlowBinder.BindContinue (ControlFlowBinder.cs currently has NO Continue handler) that, when the AFTER phrase is present, binds the arithmetic expression via ExpressionBinder.BindExpr(ArithmeticExpressionContext) (ExpressionBinder.cs:308) and returns a new BoundContinueAfter; else BoundNop. BOUND NODE (MISSING): src/Cobol.Net.Compiler/Binding/Bound/BoundTree.cs — add `sealed record BoundContinueAfter(BoundExpression Seconds) : BoundStatement;` next to BoundNop (line 468). EMITTER (MISSING arm): src/Cobol.Net.Compiler/CodeGen/StatementEmitter.cs:104 has `Visit(BoundNop)=>false`; add `Visit(BoundContinueAfter)` that evaluates Seconds (decimal), clamps to implementor max, and calls the runtime pause; the nonfatal EC-CONTINUE-LESS-THAN-ZERO raise rides the existing enabled-check idiom `ecState.Info.Enabled.Any(p => p.Ec == "EC-CONTINUE-LESS-THAN-ZERO")` (EcEmitter.cs pattern at lines 73/198) → ExceptionState.Set. RUNTIME (MISSING helper; ECs already catalogued): add a RunUnit/RuntimeServices timing helper (e.g. ContinueAfter(decimal seconds)); precedent that a SECONDS pause is a single-run-unit no-op is the RETRY phrase's FOR n SECONDS form, whose timeout period ISO 14.7.9.3 GR2 clamps to this implementation's maximum meaningful value of ZERO (Annex A.1 item 166; docs/COBOLNET_FILES_DESIGN.md "D8") - note that RETRY FOREVER is NOT such a no-op, it reports the 9.1.13.8 item 2 deadlock. EC registry EXISTS: ExceptionCatalog.cs:82 EC-CONTINUE-IMP(Imp,2023), :83 EC-CONTINUE-LESS-THAN-ZERO(NF,2023), :61 EC-CONTINUE level-2(2023). EDITION GATE (MISSING): VersionConformancePass.cs new VisitContinueStatement (model VisitRetryPhrase:1252) + constructs.json "continue-after-2023" row.

**Golden program:**

```
*> CONTINUE AFTER arithmetic-expression SECONDS (ISO 14.9.9, COBOL-2023):
      *> a timed pause. A negative interval is forced to 0 (GR1a) and, when
      *> EC-CONTINUE-LESS-THAN-ZERO checking is enabled, sets that nonfatal
      *> exception (GR1b) then continues. Observed via FUNCTION EXCEPTION-STATUS.
      >>TURN EC-CONTINUE-LESS-THAN-ZERO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CONT-AFTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC S9 VALUE -3.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "A".
           CONTINUE AFTER 0 SECONDS.
           DISPLAY "B".
           CONTINUE AFTER WS-N SECONDS.
           DISPLAY "EC=" FUNCTION EXCEPTION-STATUS.
           STOP RUN.
```

**Golden expected stdout:**

```
Line 1: `A`  — DISPLAY "A" (§14.9.13).
Line 2: `B`  — the intervening `CONTINUE AFTER 0 SECONDS` produces NO output: §14.9.9.4 GR1 value 0 is not <0, suspend 0 seconds, continue; no EC raised. Then DISPLAY "B".
Line 3: `EC=EC-CONTINUE-LESS-THAN-ZERO     `  — `CONTINUE AFTER WS-N SECONDS` with WS-N=-3<0 → GR1a sets it to 0, GR1b (checking ON via >>TURN) sets EC-CONTINUE-LESS-THAN-ZERO to exist, nonfatal → §14.6.13.1.4 continue with next statement, status retained; then DISPLAY "EC=" FUNCTION EXCEPTION-STATUS: "EC=" (3 bytes) immediately followed (DISPLAY concatenates operands with no separator) by FUNCTION EXCEPTION-STATUS = §15.33.3 r1 a 31-char left-justified uppercase name "EC-CONTINUE-LESS-THAN-ZERO" (26 chars) padded with 5 alphanumeric spaces. So the line is exactly: EC=EC-CONTINUE-LESS-THAN-ZERO + 5 trailing spaces (34 bytes total).

Full stdout (3 lines, note the 5 trailing spaces on line 3):
A
B
EC=EC-CONTINUE-LESS-THAN-ZERO
```

**Gotchas / audit-drift:** 1. SPEC TYPO — §14.9.9.4 GR1b writes "FC-CONTINUE-LESS-THAN-ZERO" but Table 13 and every index entry name it EC-CONTINUE-LESS-THAN-ZERO; the catalog (ExceptionCatalog.cs:83) is correct. Do NOT invent an FC- name. 2. EC-CONTINUE-IMP (Imp) has NO raising general rule — it is an implementor-defined "timing exception" reserved for a genuine pause-facility failure; do NOT raise it on the normal path or goldens break. It is already catalogued and available for RAISE/declaratives. 3. NONFATAL default is OFF — §7.3.25.4 GR1 default is `>>TURN EC-ALL CHECKING OFF`; without the >>TURN the negative-value path takes GR1c (silent continue, no EC), so the golden REQUIRES the >>TURN line. 4. GATE THE PHRASE, NOT THE VERB — plain CONTINUE is 1985-continuous; the introduction gate must fire only when AFTER…SECONDS is present, and must be in the PARSE arm (bound-arm drops BoundNop and loses the 0900 — DEVLOG-724 lesson in the pass header). 5. TRUNCATION not rounding — GR1's implicit move into 9(n)V9(m) is COMPUTE WITHOUT ROUNDED; fractional seconds beyond m digits truncate. m/n and the max meaningful value are implementor-defined and MUST be documented (Annex item 39); precision >.99 is processor-dependent (Annex D item 8 / F.6). 6. PAUSE realism — the RETRY phrase precedent (FileLockBinder.cs:64) treats SECONDS as a single-run-unit no-op; for deterministic conformance the pause should be a no-op / bounded (the golden uses 0 and a clamped-to-0 negative so it never actually blocks) — an actual Thread.Sleep on a positive literal would make CI goldens slow/flaky, so keep positive-value pausing out of the golden. 7. DO NOT CONFLATE with RECEIVE's CONTINUE AFTER — the spec's MCS example (lines 44126/44144) is the RECEIVE statement's own AFTER…SECONDS phrase (message-wait timeout), a DIFFERENT construct from the standalone §14.9.9 statement; scope is §14.9.9 only. 8. DISPLAY concatenation has no separator between "EC=" and the 31-char status — mirror the existing EC-SIZE golden (ec_size_truncation_prohibited.cob:15) whose .out has the same shape; remember the 5 trailing spaces on the observation line. 9. AUDIT CLAIM VERIFIED — the audit's ExceptionCatalog.cs:82-83 pointer is accurate (both L3 ECs + the EC-CONTINUE L2 at line 61 already registered at 2023); the ONLY missing pieces are grammar-tail/binder/emitter/runtime/parse-arm-gate/golden, NOT the EC catalog. 10. Use a signed WS item (VALUE -3) for the negative operand rather than a bare `-3` literal in the AFTER phrase to avoid unary-minus/arithmeticExpression lexing ambiguity in the golden.

**Complexity:** M — a self-contained statement, but touches all layers: grammar tail + new BoundContinueAfter node + StatementEmitter arm + a runtime pause helper + the nonfatal-EC raise (reuse the Enabled-check idiom) + a parse-arm COBOLNET0900 introduction gate + constructs.json/ConstructRegistry regen + one golden. No cross-cutting redesign; the EC catalog and lexer tokens already exist.

---

## C4. PICTURE clause EDITING phrase — user-defined arbitrary-size literal simple insertion and sign-sensitive fixed insertion editing (Format 1, `EDITING character-1 [ IS literal-1 | FOR { NEGATIVE IS literal-2 | POSITIVE IS literal-3 } ] ...`)

**Spec sections:** §13.18.40.2 (line 20242–20254) — Format 1 diagram; the EDITING phrase is the trailing optional/repeatable bracket on the PICTURE clause. §13.18.40.3 SR8–12 (20289–20312) — character-1 domain (any basic letter except A B C D E N P R S V X Z and the currency symbol), SR9 literal size ≤50, SR10 character-1 must appear ≥1 in character-string-1, SR11 no two EDITING phrases share character-1, SR12 IS⇒fixed editing sign control / FOR⇒extended editing sign control (numeric/numeric-edited only; literal-2 & literal-3 equal length; POSITIVE/NEGATIVE default to spaces of the specified literal's length); SR24–26 (20354–20369) count/placement of extended sign control symbols. §13.18.40.4 GR14 'es' (line 20595) — character-1 is a position that receives literal-1/2/3; SIZE COUNTING: simple/fixed insertion counts literal-1's size, fixed editing sign control counts each char of the symbol, extended-with-fixed counts each char of the associated literal, floating counts one literal + one char per repetition. §13.18.40.5 rule 3 (20766 simple insertion — character-1 with literal-1), rule 5 (20778 fixed insertion — character-1 as editing sign control), Table 8 (20796 Results of fixed insertion editing), rule 6 + Table 9 (20812/20850 floating insertion). Annex D.24 (48133–48179) — the DEFINITIVE worked examples with exact expected bytes (the golden oracle). Annex E.3.3 item 19 (50275) — EDITING phrase is a NEW 2023 addition. Annex E.2 item 25 (49330 region) — EDITING added as a 2023 RESERVED WORD.

**Syntax / format:** Format 1 (basic), the EDITING phrase portion (§13.18.40.2, transcribed from the diagram at line 20248):

  { PICTURE } IS character-string-1  [ EDITING character-1
  { PIC     }                          [ { IS literal-1 }
                                         { FOR { NEGATIVE IS literal-2 } } ] ] ...
                                               { POSITIVE IS literal-3 }

Reserved words (all underlined/required minimum-abbrev in the diagram): PICTURE|PIC, IS, EDITING, FOR, NEGATIVE, POSITIVE. The whole `EDITING character-1 [...]` group is bracketed (optional) with a trailing `...` (repeatable). Within the inner optional bracket the choice is EITHER `IS literal-1` OR `FOR {NEGATIVE IS literal-2 | POSITIVE IS literal-3}`. NOTE the diagram's inner FOR curly shows NEGATIVE/POSITIVE as a stacked exclusive choice, but SR12c ("if only POSITIVE…", "if only NEGATIVE…") + Table 8/9 (both a NEGATIVE and a POSITIVE row) imply a single phrase MAY carry both — an ambiguity to resolve toward `FOR (NEGATIVE IS? literal | POSITIVE IS? literal)+`.

**Introduced edition:** 2023

**Semantics:** The EDITING phrase lets the programmer bind an otherwise-illegal picture character (character-1) to an arbitrary-size insertion literal, extending editing beyond the built-in single-char symbols.

Two modes (SR12, GR14 'es', editing-rule 3/5):
1. Simple/fixed insertion (`EDITING character-1 IS literal-1`): character-1 becomes a FIXED editing sign control symbol; at every character-1 position, literal-1 is unconditionally inserted (value-independent). literal-1's full length is counted in the item size (GR14). This is the arbitrary-size analogue of the built-in `0`/`/`/`,` simple-insertion symbols.
2. Sign-sensitive fixed insertion (`EDITING character-1 FOR NEGATIVE IS literal-2` and/or `FOR POSITIVE IS literal-3`): character-1 is an EXTENDED editing sign control symbol, legal only on numeric / numeric-edited items (SR12). Per Table 8 / Annex D.24 the inserted string depends on the SIGN of the edited value — the NEGATIVE literal appears when the value is negative, the POSITIVE literal when the value is positive-or-zero; the unspecified sign case defaults to as-many-spaces-as-the-specified-literal (SR12c). literal-2 and literal-3 must be equal length (SR12a) and each occurrence's chars are counted in the item size (GR14). Floating extended editing (character-1 repeated ≥2, e.g. LLLL9.99) is the arbitrary-size analogue of `$$$`/`++`/`--` floating strings (editing-rule 6, Table 9): one literal instance plus one char per repetition is counted, the literal floats to just left of the first significant digit.

DECIMAL-POINT IS COMMA and the currency symbol interact unchanged (the character-1 map is orthogonal to the `.`↔`,` role swap already canonicalized in CobolEdit). BLANK WHEN ZERO (allowed — L/character-1 is neither S nor `*`, SR22) short-circuits a zero value to all spaces before EDITING is applied.

**Below-2023 gate:** A NEW introduction-gate construct row `editing-phrase-2023` (add to tests/version-matrix/constructs.json → regenerates Constructs.EditingPhrase2023 + ConstructRegistry.g.cs; introducedEdition 2023, removedEdition null, code COBOLNET0900, cite "ISO 2023 §13.18.40.2/.3 SR8–12; Annex E.3.3 item 19"). Gate it in the PARSE arm, not a bound arm, mirroring VisitDeleteFileStatement (VersionConformancePass.cs:955–956) and VisitDynamicLengthClause (620–624): add `ParseArm.VisitEditingClause(ctx) => _p.Check(Constructs.EditingPhrase2023, "the EDITING phrase of the PICTURE clause")`, fired on syntactic recognition so a below-edition occurrence always emits the 0900 (avoids the DEVLOG-724 bound-arm drop-on-error-path flaw). Must REJECT at --std 85, 2002, and 2014 (0900 "not available below the introducing edition"); accepted at 2023. COMPANION row (separate, for completeness): `user-word-editing-2023` (85, removed 2023, COBOLNET0901) — EDITING becomes a reserved word only at 2023 (Annex E.2 item 25), so below 2023 it must still be admissible as a user-defined word; the interval-encoding pattern already used for user-word-commit-2023 / user-word-xor-2023.

**Code anchors:** EXISTS:
- Grammar host: dataDescriptionClause alternatives, CobolData.g4:237–258 (add an `editingClause` alternative); pictureClause rule CobolData.g4:340–342 (`PIC PIC_STRING`).
- Lexer: PICMODE, CobolLexer.g4:741–773 — PIC_STRING pops mode at whitespace (line 745 + `-> popMode` at 773). NOTE the audit HINT that "EDITING is swallowed" is WRONG: PIC strings contain no spaces, so `PIC L999.99 EDITING …` lexes the picture as "L999.99" and EDITING starts a fresh default-mode token — no PICMODE change is needed. Tokens FOR (CobolLexer.g4:423), NEGATIVE (430), POSITIVE (429), IS (471) already EXIST and are reserved.
- Binder: PictureAnalyzer.Analyze, PictureAnalyzer.cs:29 — the §13.18.40.3 SR2 whitelist (COBOLNET0808) currently REJECTS character-1 like 'L'/'F'; it must be told the EDITING character-1 set so those chars classify as 'es' simple/fixed/floating positions. PicInfo (Binding/Model, referenced PictureAnalyzer.cs:11) is the analyzed-facts record that must carry the character-1→(literal-1 | negLit/posLit) map and the adjusted item size.
- Emitter: CodeGen/Emit/NumericRenderer.cs and CodeGen/Verbs/MoveEmitter.cs route numeric-edited stores to CobolEdit.Format — must forward the EDITING map.
- Runtime: CobolEdit.Format, CobolEdit.cs:38 (pre-scan of fixed/floating sign+currency at ~57; SwapSeparators at 26).
- Edition machinery: tests/version-matrix/constructs.json (canonical) → Constructs.g.cs / ConstructRegistry.g.cs (gen-constructs.ps1); gate site VersionConformancePass.cs ParseArm (class at :351, sibling gates 620/955).

MISSING (all new):
- `editingClause` grammar rule + EDITING lexer token (only new token required).
- character-1 map on PicInfo + PictureAnalyzer awareness (suppress 0808 for mapped chars, classify numeric-edited, size = Σ literal lengths).
- DataBinder collection of the editingClause into the PicInfo map + SR8–12/24–26 bind checks.
- A CobolEdit.Format overload accepting the character-1→literal map; core loop emitting the literal per Table 8/D.24 (sign-sensitive) and floating placement per Table 9.
- The two constructs.json rows + VisitEditingClause gate + a tests/conformance/2023 golden.

**Golden program:**

```
IDENTIFICATION DIVISION.
PROGRAM-ID. EDITPHR.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 ITM PIC L999.99
       EDITING "L" FOR NEGATIVE IS "DEBIT "
       BLANK WHEN ZERO.
PROCEDURE DIVISION.
    MOVE -123.45 TO ITM.
    DISPLAY ITM.
    MOVE  123.45 TO ITM.
    DISPLAY ITM.
    STOP RUN.
```

**Golden expected stdout:**

```
Line 1: `DEBIT 123.45`  (bytes: D E B I T space 1 2 3 . 4 5, then newline)
Line 2: `      123.45`  (6 leading spaces, then 1 2 3 . 4 5, then newline)

Byte derivation (this is Annex D.24 example 3 verbatim — the spec states both results):
- Item size = 12: character-1 'L' contributes 6 (each char of literal-2 "DEBIT " counted per §13.18.40.4 GR14 'es' extended-editing-with-fixed), plus "999.99" = 6.
- MOVE -123.45 (negative): 'L' → literal-2 "DEBIT " (Annex D.24: "would result in 'DEBIT 123.45'"; Table 8 character-1 NEGATIVE-phrase, negative value → literal-2); digits per §13.18.40.4 GR14 '9'/'.' → "123.45". Result "DEBIT 123.45".
- MOVE 123.45 (positive): 'L' → spaces × len("DEBIT ")=6 (§13.18.40.3 SR12c default for the unspecified POSITIVE case; Annex D.24: "would result in 'bbbbbb123.45'"); digits → "123.45". Result "      123.45".
- DISPLAY writes the 12-byte item content then a newline (§14.9.13).
- (BLANK WHEN ZERO is present but neither MOVE stores zero, so it does not fire.)
```

**Gotchas / audit-drift:** 1. AUDIT HINT IS WRONG (adversarial finding): the hint says PICMODE "captures up to whitespace so EDITING is swallowed" — self-contradictory. Capturing UP TO whitespace means the picture STOPS at the space before EDITING; PIC strings never contain spaces (the lexer's own stated invariant), so `PIC L999.99 EDITING` cleanly yields PIC_STRING="L999.99" then a default-mode EDITING token. NO PICMODE change is needed. Do not "fix" the lexer for a non-problem.
2. Currently a program using EDITING FAILS TO PARSE (no editingClause alternative, no EDITING token) — so the below-2023 behavior today is a raw syntax error, not a clean 0900. The gate must be added alongside the grammar so 85/2002/2014 emit COBOLNET0900 (feature-introduction) rather than a parse error.
3. PictureAnalyzer SR2 whitelist (COBOLNET0808, PictureAnalyzer.cs) will REJECT character-1 (e.g. 'L') as an invalid picture symbol unless the analyzer is passed the EDITING map — the binder must parse editingClause and thread the map INTO Analyze, else valid 2023 programs are spuriously rejected.
4. Table 8 TRANSCRIPTION HAZARD: the markdown Table 8 (line 20805) renders the character-1 NEGATIVE-phrase row as [positive-or-zero]=literal-2 / [negative]="literal-2 or spaces", which is INVERTED relative to Annex D.24's worked bytes (negative→literal-2, positive→spaces). Trust Annex D.24 (concrete, unambiguous, spec-authoritative) over the extracted Table 8 columns; verify against the real ISO PDF before coding. This is exactly the kind of extraction inversion the persist-anchor-rescout rule guards against.
5. SR12a: literal-2 and literal-3 MUST be equal length — a bind check (new diagnostic). SR12b: with the FOR phrase, character-string-1 may contain ONLY character-1 and 9 . cs P V Z (no built-in +/-/CR/DB). SR11: two EDITING phrases can't reuse the same character-1. SR10: character-1 must actually occur in the picture.
6. Zero is treated as POSITIVE for the POSITIVE phrase (Table 8/9 columns are "positive OR zero" | "negative"); but BLANK WHEN ZERO takes precedence for a zero value.
7. Size counting is the subtle part (GR14 'es'): fixed simple-insertion counts the WHOLE literal length once per occurrence; FLOATING extended editing (LLLL9.99, D.24 example) counts one literal instance + one char per extra character-1 — Annex D.24 warns 'LLLL9,88 … "DEBIT "' is size 13 (6 + 3 + 4), a classic miscount trap. Scope the first golden to a single fixed character-1 (as above) and defer floating extended editing to a follow-up golden.
8. DECIMAL-POINT IS COMMA: the character-1 map is orthogonal to the existing `.`↔`,` canonicalization in CobolEdit.Format — ensure the map is applied AFTER the separator swap so literals aren't corrupted.
9. National path: SR9 — if USAGE NATIONAL or the picture has 'N', literal-1/2/3 must be NATIONAL literals; the display path in CobolEdit is alphanumeric — national EDITING is a separate, deferrable leg.
10. Companion reserved-word gate: adding EDITING as an always-on lexer token reserves it below 2023 too; add the user-word-editing-2023 (0901) interval row so pre-2023 programs using EDITING as a data-name still behave per the edition model.

**Complexity:** L — spans the entire PICTURE pipeline: new lexer token + grammar rule, PicInfo model extension, PictureAnalyzer whitelist/classification/size changes, DataBinder SR8–12/24–26 checks, a new sign-sensitive/multi-char CobolEdit.Format overload (a genuinely new editing dimension in the renderer), emitter threading, two constructs.json rows + a ParseArm gate, and a 2023 conformance golden; sign-sensitive fixed insertion is achievable in one pass, but floating extended editing (Table 9, D.24 size rules) is an additional sub-feature best goldened separately.

---

## C5. PERFORM — the COBOL-2023 additions: (A) the UNTIL EXIT phrase of until-phrase (Formats 1/2 → an infinite loop), and (B) Format 3, the exception-checking PERFORM (PERFORM [WITH LOCATION] imperative-1 WHEN … / WHEN OTHER / WHEN COMMON / FINALLY … END-PERFORM). Both are one scout item; both are 2023 introductions.

**Spec sections:** §14.9.28 PERFORM statement (NOT §14.9.31 — the hint is WRONG; §14.9.31 is RECEIVE). §14.9.28.1 general (line 29360: "with or without exception checking"). §14.9.28.2 Format 1 (out-of-line), Format 2 (inline), Format 3 (exception-checking, lines 29377-29394); until-phrase figure (29416-29426) = "UNTIL { condition-1 | EXIT }"; varying-phrase (29428-29465). §14.9.28.3 SR1-16: SR7 (condition-1 any conditional expr), SR8 (line 29509 — UNTIL EXIT shall NOT be under/with a VARYING or TEST BEFORE/AFTER PERFORM), SR14-16 (Format-3 WHEN file/exception-name rules). §14.9.28.4 GR: GR10 (until-phrase w/ condition-1), GR11 (line 29591 — UNTIL EXIT: "with condition-1 specified except that condition-1 never evaluates as true"), NOTE 4 (line 29593 — inline escape = EXIT PERFORM, NOT EXIT PERFORM CYCLE; out-of-line escape = GOBACK/STOP; EXIT PARAGRAPH/SECTION do NOT escape); GR14-22 (Format 3 semantics). §14.9.29.3 SR4 (line 29752 — RAISE only in imperative-statement-1 of a Format-3 PERFORM). Annex E.3.3 items 36 (exception-checking variant added) + 37 (UNTIL EXIT added), lines 50316-50318 — confirms BOTH are 2023 new features. §14.6.13.1.3 (fatal EC handling, ref'd by GR20). §14.9.49 USE GR3a-3g (WHEN-match rules, ref'd by GR17).

**Syntax / format:** until-phrase (Formats 1&2), §14.9.28.2: [ WITH TEST { BEFORE | AFTER } ] UNTIL { condition-1 | EXIT }  — EXIT is a required-choice alternative to condition-1, and the TEST phrase is optional (but SR8 forbids TEST BEFORE/AFTER together with EXIT). Format 3, §14.9.28.2:  PERFORM [ WITH LOCATION ]  imperative-statement-1  { WHEN { EXCEPTION [ {file-name-1}… | INPUT | OUTPUT | IO | EXTEND ] | {exception-name-1}… | {exception-name-2 FILE file-name-2}… }  imperative-statement-2 } …  [ WHEN OTHER EXCEPTION imperative-statement-3 ]  [ WHEN COMMON EXCEPTION imperative-statement-4 ]  [ FINALLY imperative-statement-5 ]  END-PERFORM . (Reserved/context words: PERFORM, WITH, LOCATION, WHEN, EXCEPTION, OTHER, COMMON, FINALLY, EXIT, UNTIL, TEST, BEFORE, AFTER, FILE, END-PERFORM.)

**Introduced edition:** 2023 — BOTH. UNTIL EXIT = Annex E.3.3 item 37; the exception-checking Format 3 = Annex E.3.3 item 36. Neither exists in 85/2002/2014. (Format 1/2 with condition-1, TIMES, VARYING are all pre-1985 and already implemented; only the EXIT alternative and Format 3 are new.)

**Semantics:** UNTIL EXIT (GR11): execution proceeds exactly as PERFORM … UNTIL condition-1 with a condition-1 that NEVER evaluates true — i.e. an unconditional infinite loop of the specified set of statements. Because the loop never self-terminates, escape is the programmer's responsibility (NOTE 4): inline ⇒ an EXIT PERFORM statement (NOT EXIT PERFORM CYCLE — CYCLE only ends the current iteration); out-of-line ⇒ GOBACK or STOP RUN (EXIT PARAGRAPH / EXIT SECTION do NOT escape). SR8: UNTIL EXIT may not carry TEST BEFORE/AFTER and may not appear in, or be nested under, a VARYING PERFORM. Runtime: none — it lowers to `while(true){ body }`. Format 3 exception-checking (GR14-22): a scoped try/handler over imperative-statement-1. GR14 — for each exception-name in a WHEN, an implicit `TURN … CHECKING ON [LOCATION]` (LOCATION iff WITH LOCATION written) is asserted before imperative-statement-1 if not already enabled; an implicit `PUSH ALL` + `TURN OFF ALL` is assumed at the END of imperative-statement-1, and immediately before END-PERFORM an implicit `POP ALL` + `TURN OFF` for the implicitly-enabled names. GR17 — if during imperative-statement-1 an EC associated with a WHEN is raised, imperative-statement-2 runs; match order per USE GR3a-3g; any matching USE declarative is IGNORED. GR18 — an EC matching no WHEN but with a WHEN OTHER EXCEPTION runs imperative-statement-3. GR19 — WHEN COMMON EXCEPTION's imperative-statement-4 always runs after a handled WHEN/OTHER. GR16/NOTE8 — FINALLY's imperative-statement-5 is the end of the PERFORM and always runs; an EXIT PERFORM in FINALLY ⇒ implicit CONTINUE after END-PERFORM; no transfer of control out of FINALLY allowed. GR20 — after handling, resumption depends on fatality: NONFATAL ⇒ implicit CONTINUE immediately after the raising statement in imperative-statement-1 (if it was the last, fall to end-of-PERFORM); FATAL ⇒ per §14.6.13.1.3. GR21 — ECs raised inside imperative-statement-2/3/4/5 are NOT caught by this PERFORM (they behave as a Format-2 PERFORM). GR22 — checking-enabled state after the PERFORM: names enabled by an outer TURN before entry stay enabled; a TURN inside the range is retained; otherwise WHEN-only names are disabled. §14.9.29.3 SR4 — a RAISE inside a Format-3 PERFORM is legal only in imperative-statement-1.

**Below-2023 gate:** TWO new construct rows, both COBOLNET0900 (EditionCodes.Introduction), both parse-arm / recognition-based per the DEVLOG-724 rule (a below-edition occurrence must name its edition even when it also fails to bind/drops to BoundUnsupported). (1) Constructs.PerformUntilExit2023 = "perform-until-exit-2023", introducedIn 2023, citation "ISO §14.9.28.2 until-phrase; Annex E.3.3 item 37" — fired from a new ParseArm override VisitPerformUntil(ctx) when `ctx.EXIT() is not null` → Check(…, "the PERFORM UNTIL EXIT phrase"). (2) Constructs.PerformExceptionChecking2023 = "perform-exception-checking-2023", introducedIn 2023, citation "ISO §14.9.28.2 Format 3; Annex E.3.3 item 36" — fired from ParseArm VisitPerformStatement when the new Format-3 alternative's marker tokens are present (WHEN EXCEPTION / FINALLY / WITH LOCATION) → Check(…, "the exception-checking PERFORM statement"). Both MUST reject (emit COBOLNET0900) at --std 85, 2002, and 2014; accept silently at 2023. Add matching rows to tests/version-matrix/constructs.json (expectDiagnostic COBOLNET0900) and the generated Constructs.g.cs consts. Registry funnel: ConstructRegistry.Check → EditionCodes.Introduction message "…requires COBOL-2023 (targeting COBOL-YYYY)…".

**Code anchors:** GRAMMAR (src/Cobol.Net.Frontend/Grammar/Core/CobolControlFlow.g4): performUntil rule at :44-46 — EXISTS as `(WITH? TEST (BEFORE|AFTER))? UNTIL condition`; MISSING the EXIT alternative → change to `(WITH? TEST (BEFORE|AFTER))? UNTIL condition | UNTIL EXIT` (EXIT as its own alt so SR8's no-TEST rule is structural and the ParseArm can test ctx.EXIT()). performStatement :18-28 — MISSING Format 3 entirely; add a new alternative `PERFORM (WITH? LOCATION)? statementBlock+ performWhenClause+ (WHEN OTHER EXCEPTION statementBlock*)? (WHEN COMMON EXCEPTION statementBlock*)? (FINALLY statementBlock*)? END_PERFORM` + new sub-rules performWhenClause/performWhenSelector (reuse useOnTarget shape + useEcEntry for exception-names). performOptions :34 (hint's anchor — correct). LEXER (src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4): EXIT :266, UNTIL :610, PERFORM :282, EXCEPTION :418, OTHER :517, COMMON :362, CYCLE :382, EC :545 all EXIST; FINALLY and LOCATION are ABSENT → add two new tokens (LOCATION also needed by the TURN directive if not already lexed elsewhere). BINDER (src/Cobol.Net.Compiler/Binding/Procedure/Verbs/ControlFlowBinder.cs): BindPerformControl :128-143 — add EXIT leg: when `(p.performUntil() ?? opt?.performUntil())?.EXIT() is not null` return a new PerformForever() control (do NOT reuse PerformUntil with a false condition — a distinct node keeps the emitter honest). BindPerform :96-119 — add Format-3 detection → delegate to a new binder (BoundExceptionCheckingPerform) that resolves each WHEN's exception-names via host.Ec / ExceptionCatalog + file-modes via existing useOnTarget resolution. BindExit :56-79 already yields BoundExitPerform(Cycle) — reused unchanged. EcBinder.cs:122 (hint anchor) is the deferral note in BindResume; the Format-3 handler binding is the "later wave" it references — new methods land on EcBinder (WHEN exception-name resolution, TurnState PUSH/POP/TURN modeling). BOUND TREE (src/Cobol.Net.Compiler/Binding/Bound/BoundTree.cs): BoundPerformControl :428, PerformOnce/Times/Until/Varying :430-444 — add `PerformForever : BoundPerformControl`. Add new `BoundExceptionCheckingPerform` statement record (body + WHEN arms + WhenOther + WhenCommon + Finally + WithLocation) with StatementChildren wired for the generated visitor/Recurse. EMITTER (src/Cobol.Net.Compiler/CodeGen/Verbs/ControlFlowEmitter.cs): EmitPerform :59-87 — add `case PerformForever: using(w.Block("while (true)")) body(); break;`. StatementEmitter.cs:102 Visit(BoundExitPerform) already emits `break`(escape)/`continue`(cycle) — correct for inline UNTIL EXIT. New EmitExceptionCheckingPerform (Format 3) → try/handler over the EC engine. RUNTIME (src/Cobol.Net.Runtime): UNTIL EXIT needs NONE. Format 3 needs the ExceptionEngine/RunUnit + TurnState (implicit PUSH ALL/TURN OFF ALL/POP ALL of GR14; fatal/nonfatal resumption of GR20 per §14.6.13.1.3) — the existing EC infrastructure (ExceptionEngine, ExceptionCatalog.IoMaskNames, BoundEcChecked wrapper). GATING: VersionConformancePass.cs ParseArm — add VisitPerformUntil + VisitPerformStatement overrides; Constructs.g.cs + tests/version-matrix/constructs.json — 2 new rows.

**Golden program:**

```
IDENTIFICATION DIVISION.
PROGRAM-ID. UNTILEXIT.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-N PIC 9(2) VALUE 0.
PROCEDURE DIVISION.
MAIN.
    PERFORM UNTIL EXIT
        ADD 1 TO WS-N
        DISPLAY WS-N
        IF WS-N = 3
            EXIT PERFORM
        END-IF
    END-PERFORM
    DISPLAY "DONE"
    STOP RUN.
```

**Golden expected stdout:**

```
01
02
03
DONE
(exact bytes: "01\n02\n03\nDONE\n") — Derivation: inline PERFORM UNTIL EXIT is an infinite loop (§14.9.28.4 GR11 — condition never true). Iteration 1: ADD 1 TO WS-N ⇒ WS-N=01; DISPLAY WS-N of an unsigned PIC 9(2) writes its 2 character positions "01" + newline (§14.9.13/DISPLAY of a display numeric = its digit characters, no sign, no suppression); IF WS-N=3 false. Iteration 2 ⇒ "02\n". Iteration 3 ⇒ WS-N=03 ⇒ "03\n"; IF WS-N=3 TRUE ⇒ EXIT PERFORM, which per §14.9.28.4 NOTE 4 (inline escape) + §14.9.14 terminates the inline PERFORM (lowers to C# `break` out of `while(true)`). Control falls to DISPLAY "DONE" ⇒ "DONE\n". STOP RUN. No further output.
```

**Gotchas / audit-drift:** (1) AUDIT-DRIFT: the hint's "§14.9.31 (E.3.3 items 36/37)" is WRONG for the section number — PERFORM is §14.9.28; §14.9.31 is RECEIVE. The E.3.3 item numbers (36/37) ARE correct. (2) UNTIL EXIT escape asymmetry (NOTE 4): only EXIT PERFORM (Cycle=false ⇒ `break`) escapes an inline loop; EXIT PERFORM CYCLE (`continue`) does NOT; for an OUT-OF-LINE `PERFORM para UNTIL EXIT` the current emitter renders `while(true) Dispatch(start,end);` and the ONLY spec-legal escapes are GOBACK/STOP — an EXIT PERFORM lexically inside the performed paragraph does NOT escape (it would emit break/continue against the dispatcher switch, which is wrong/undefined per NOTE 4) — so either forbid or carefully scope; keep the golden INLINE to stay deterministic. (3) SR8: reject UNTIL EXIT combined with TEST BEFORE/AFTER (structural if EXIT is its own grammar alt) AND under/with a VARYING PERFORM — the "under" (lexical-nesting) half is a non-trivial static check; at minimum reject the direct combinations, note the nested-under case. (4) A bare condition cannot lex EXIT (EXIT is a reserved word, not a conditionName), so today `PERFORM … UNTIL EXIT` FAILS to parse — confirming UNTIL EXIT is genuinely MISSING, not silently mis-accepted. (5) Format-3 tokens FINALLY and LOCATION do NOT exist in the lexer — must be added; the figure writes "IO" but the real file-mode word is I-O (existing I_O token / useOnTarget) — do not add a bare IO token. (6) Format 3 is LARGE and EC-entangled: GR14's implicit PUSH ALL/TURN OFF ALL/POP ALL, GR17's "matching USE declarative is IGNORED" (Format 3 SHADOWS declaratives), GR20 fatal-vs-nonfatal resumption (nonfatal ⇒ implicit CONTINUE after the raising stmt, NOT restart of imperative-1), GR21 (ECs in the WHEN/FINALLY handlers are NOT re-caught — behave as Format 2), and §14.9.29.3 SR4 (RAISE only in imperative-statement-1). WHEN-match ordering defers to USE GR3a-3g. (7) EcBinder.cs:122's note "the exception-checking PERFORM WHEN form is 2023, a later wave" is THIS wave's hook — RESUME stays declarative-only (§14.9.33.3 SR1); it does NOT become legal inside a Format-3 WHEN handler. (8) Both gates MUST be parse-arm/recognition (COBOLNET0900) not bound-arm — a below-2023 PERFORM UNTIL EXIT or Format 3 also fails to bind (dropped to BoundUnsupported), so a bound-arm gate would silently lose the 0900 (DEVLOG 724). (9) Per-feature conformance test obligation: ship the UNTIL EXIT golden in tests/conformance/2023/ AND a negative (below-2023 ⇒ COBOLNET0900) row in the same commit.

**Complexity:** L — split: UNTIL EXIT alone is S (one grammar alt + PerformForever control node + `while(true)` emit + one 0900 gate + golden); the exception-checking Format 3 is L (2 new lexer tokens, a whole new performStatement alternative + sub-rules, a new BoundExceptionCheckingPerform node, an EC-engine try/handler emitter honoring GR14 PUSH/POP/TURN + GR17 declarative-shadowing + GR20 fatal/nonfatal resumption + GR21 handler-non-recatch, plus WHEN-match reuse of USE GR3a-3g). Combined scout item = L; recommend landing UNTIL EXIT first (independent, no runtime) then Format 3.

---

## C6. Two COBOL-2023 constructs: (A) WRITE … BEFORE AND AFTER ADVANCING (both phrases together in one WRITE); (B) SUPPRESS WHEN literal phrase on the ALTERNATE RECORD KEY clause (indexed-file alternate-key access suppression).

**Spec sections:** A (WRITE BEFORE+AFTER):
- §14.9.51.2 General formats — Format 1 (sequential) is the only format carrying the ADVANCING phrase (SR2: sequential org ⇒ Format 1; SR3: indexed/relative ⇒ Format 2, no ADVANCING).
- §14.9.51.3 SR17 — "The BEFORE and AFTER phrases shall not both be specified if the PAGE phrase is specified" (this rule ONLY makes sense because both MAY now be specified — the load-bearing 2023 evidence).
- §14.9.51.3 SR18 — ADVANCING PAGE and END-OF-PAGE mutually exclusive; SR14/15 (identifier-2 integer, integer-1 ≥0).
- §14.9.51.4 GR25 (a–h) — the advancing engine; GR25e (BEFORE: present-then-advance), GR25f (the combined case: AFTER advance happens AFTER the line was presented per 25e).
- §14.9.51.4 GR26/GR27/GR28 — LINAGE end-of-page (EC-I-O-EOP / EC-I-O-EOP-OVERFLOW) interaction.
- §13.18.34 GR7c/GR7d — LINAGE-COUNTER: =1 at OPEN OUTPUT, incremented by the advance line count (the observable used by the golden).
- Annex E.3.3 item 2 (page ~1212) — "BEFORE and AFTER phrases. Both BEFORE and AFTER are allowed together in WRITE ADVANCING." (E.3.3 = "Not affecting" = pure additive, accepts previously-invalid programs.)

B (SUPPRESS WHEN):
- §12.4.5.6 ALTERNATE RECORD KEY clause — .6.2 general format ("[ WITH DUPLICATES ] [ SUPPRESS WHEN literal-1 ]"), .6.3 SR7 (literal-1 = alphanumeric/national/figurative, same category as key; ALL literal ⇒ 1 char), .6.4 GR6 (literal-1 establishes the WRITE/REWRITE key-suppression value; NOTE: READ/START unaffected but suppressed records "as if they did not exist").
- §12.4.5.1 Format 1 (Indexed SELECT) — shows the same "[ SUPPRESS WHEN literal-2 ]" in the ALTERNATE RECORD KEY line.
- §14.9.51.4 GR41 (WRITE, INDEXED) — on WRITE, for each alt key whose value == its suppression literal: (a) no alt access path provided, (b) record positioned so it is not found via that key; suppressed entries never cause a duplicate-key condition (no DUPLICATES needed).
- §14.9.35.4 GR24 (REWRITE) — dynamic maintenance: when the rewritten alt value == literal ⇒ drop the path; when it changes away from literal ⇒ (re)provide + reposition the path.
- §14.9.30.4 GR21c (READ NEXT/PREVIOUS, indexed) — "If the key of reference is an alternate key, any record identified as being suppressed … is not considered to exist."
- §14.9.41.4 GR (START) — "If the key of reference is an alternate key, any record identified as being suppressed … is ignored."
- §14.9.35.4 GR ("If the key of reference is an alternate key, any record identified as being suppressed … is not considered to exist" also echoed for the read-family).
- Annex E item 42 (page 1228) + the page-27 new-features summary bullet ("Alternate key suppression on indexed files using the SUPPRESS WHEN phrase of the ALTERNATE RECORD KEY clause").

**Syntax / format:** A — §14.9.51.2 Format 1 (sequential), exact quote of the ADVANCING block:
> [ { BEFORE } ADVANCING { { identifier-2 } [ LINE  ]
>   { AFTER  }             { integer-1    }   { LINES }
>                          { mnemonic-name-1 }
>                          { PAGE            } } ]
⛔ **THE GOTCHA PARAGRAPH BELOW IS REFUTED — IT IS WHAT BUILT kb/Work PB712.** The format diagram was NOT read off the PDF when this scout was written; the transcription it was read from had DROPPED the CHOICE INDICATORS. Rendered (`python scripts/render-spec-page.py 815`, printed folio 785), Format 1's print-control bracket carries a pair of `|` bars, enclosed by braces, around **the two WORDS BEFORE and AFTER only** — `ADVANCING` and the whole operand group sit OUTSIDE them, ONCE. §5.2.6.4 then says one or more of the enclosed alternatives, each at most once, in any order. So the diagram DOES express the 2023 combination, the spec text is NOT internally inconsistent, and the source spelling is `WRITE R BEFORE AFTER ADVANCING 2 LINES` — one `ADVANCING`, one operand — not two ADVANCING phrases. Implementing the paragraph below produced a grammar that rejected the only printed spelling and accepted a two-operand form §14.9.51.4 GR25 a)–d) cannot interpret. The landed shape is `writeBeforeAfter : (BEFORE AFTER? | AFTER BEFORE?) ADVANCING? (…)`, gated on the CO-OCCURRENCE OF THE TWO WORDS, with SR17 checked in `StatementValidation.CheckWriteBeforeAfterPage` (COBOLNET1910). Read the rest of entry A as a record of what was TRIED, not as a design.

GOTCHA/DRIFT: the FORMAT DIAGRAM still renders BEFORE/AFTER as a single stacked CHOICE (one optional block) and was NOT updated for 2023 — but SR17 and GR25f and E.3.3 item 2 authoritatively allow BOTH. The spec text is internally inconsistent; the General Rules + SR17 govern (owner rule #1). There is NO "AND" reserved word — "BEFORE AND AFTER" is descriptive; source specifies TWO ADVANCING phrases, e.g. `WRITE R BEFORE ADVANCING 1 AFTER ADVANCING 2` (order of the two phrases is not pinned by the spec; PAGE forbidden in either when both present, SR17).

B — §12.4.5.6.2 ALTERNATE RECORD KEY clause general format (exact quote):
> ALTERNATE RECORD KEY IS  { data-name-1                              }
>                          { record-key-name-1 SOURCE IS { data-name-2 } ... }
>          [ WITH DUPLICATES ]     [ SUPPRESS WHEN literal-1 ]
Ordering: WITH DUPLICATES precedes SUPPRESS WHEN; both optional and independent. literal-1 may be a figurative constant (SR7). (This compiler does not yet implement the record-key-name-1 SOURCE IS … form — out of scope here; only the data-name-1 leg is wired.)

**Introduced edition:** 2023

**Semantics:** A — Dual-advance (§14.9.51.4 GR25). Advancing amount rules GR25a–d unchanged: positive n ⇒ advance n lines; zero ⇒ no reposition; negative identifier ⇒ undefined; mnemonic ⇒ implementor rule; PAGE ⇒ next logical/physical page. The COMBINED sequence, derived literally from GR25e+GR25f: (1) the line is PRESENTED at the current position (GR25e, because BEFORE is present); (2) the page is advanced by the BEFORE amount (GR25e); (3) THEN the page is advanced by the AFTER amount (GR25f: "the printed page is advanced … after the line was presented as specified in General rule 25e"). Net effect: the record prints once at the current line, followed by (before+after) blank-line advances. This is the exact opposite of the naïve "AFTER advances before printing" intuition — when BEFORE co-occurs, AFTER's advance is RELOCATED to after presentation. LINAGE-COUNTER (§13.18.34 GR7c2) therefore increments by (before + after). SR17 forbids PAGE with the combined form. A single BEFORE or single AFTER keeps its classic meaning and is edition-invariant (85+).

B — Alternate-key suppression. On WRITE (§14.9.51 GR41) and REWRITE (§14.9.35 GR24), for each alternate key declared WITH SUPPRESS WHEN literal-1, if that record's alternate-key VALUE compares equal (per the file's collating sequence, relation-condition rules — §14.9.51 GR35) to literal-1, NO alternate access-path entry is created for that record under that key; the record is positioned so it will not be found via that key. Multiple records may share the suppression value with NO DUPLICATES clause, and such suppressed values never raise the '22' duplicate-alternate-key condition (GR41 tail). REWRITE maintains this dynamically: moving the value TO the literal drops the path; moving it AWAY re-provides and repositions the path (GR24). On READ with an alternate key of reference (§14.9.30 GR21c) and on START with an alternate key of reference (§14.9.41 GR), suppressed records "are not considered to exist" / "are ignored" — sequential reads skip them and START never positions on them. The prime-key path and non-suppressed alternate paths are unaffected; the physical record is fully intact and reachable via prime key or any other alternate.

**Below-2023 gate:** Both reject below 2023 via the standard introduction gate — COBOLNET0900 ("construct not available before the introducing edition"), single-sourced through VersionConformancePass.ParseArm (fire-on-RECOGNITION, DEVLOG-724 rule: never a bound-arm, so a below-edition occurrence still diagnoses even on a parse/bind error path). Must REJECT at --std 85, 2002, 2014; ACCEPT at 2023 (default).

A: The gate is on the CO-OCCURRENCE, not on ADVANCING itself (single BEFORE or single AFTER is legal in ALL editions — no gate). With the grammar refactor to `writeBeforeAfter : writeAdvancePhrase writeAdvancePhrase?`, add `ParseArm.VisitWriteBeforeAfter`: `if (ctx.writeAdvancePhrase().Length == 2) _p.Check(Constructs.WriteBeforeAndAfterAdvancing2023, "the combined BEFORE and AFTER ADVANCING phrases")`. New construct id `write-before-and-after-advancing-2023`, introducedIn 2023.

B: New parse-arm `ParseArm.VisitAlternateKeyClause`: `if (ctx.SUPPRESS() is not null) _p.Check(Constructs.AlternateKeySuppressWhen2023, "the SUPPRESS WHEN phrase")` — sibling to the existing VisitSharingClause/VisitLockModeClause gates (VersionConformancePass.cs ~818). New construct id `alternate-key-suppress-when-2023`, introducedIn 2023. Add both to tests/version-matrix/constructs.json (drives the generated Constructs.g.cs constants + the VersionMatrixTests per-(construct×edition) compile/reject assertions) with `expectDiagnostic: "COBOLNET0900"`. NO new numeric diagnostic in the P13 1565+ band is needed — introduction gates reuse 0900 (confirmed by every existing constructs.json entry).

**Code anchors:** A — WRITE BEFORE+AFTER:
- Grammar (EXISTS, single-choice): src/Cobol.Net.Frontend/Grammar/Core/CobolIO.g4:355-361 `writeBeforeAfter : (BEFORE|AFTER) ADVANCING? (PAGE | (dataReference|integerLiteral|literal) (LINE|LINES)?)`; write rule at :339-348 uses `writeBeforeAfter?`. MISSING: refactor to allow two phrases (`writeBeforeAfter : writeAdvancePhrase writeAdvancePhrase?;` + new `writeAdvancePhrase` fragment).
- Lexer tokens (ALL EXIST): BEFORE CobolLexer.g4:341, AFTER :325, ADVANCING :324, PAGE/LINE/LINES present. No new tokens.
- Binder (EXISTS, single): src/Cobol.Net.Compiler/Binding/Procedure/Verbs/SequentialIoBinder.cs — BindWrite ~95-120 (calls BindAdvancing(w.writeBeforeAfter()) and CheckWriteEopAdvancingPage on `writeBeforeAfter()?.PAGE()`); BindAdvancing :163-175. MISSING: build TWO advancings; enforce SR17 (reject PAGE when both present) + at-most-one-BEFORE/at-most-one-AFTER.
- Bound tree (EXISTS, single): BoundTree.cs:627 `BoundAdvancing(bool Before, bool Page, BoundOperand? Lines)`; :635 `BoundWrite(… BoundAdvancing? Advancing …)`. MISSING: carry a pair — e.g. `BoundAdvancing? Before, BoundAdvancing? After` (or IReadOnlyList) on BoundWrite.
- Emitter (EXISTS, single): src/Cobol.Net.Compiler/CodeGen/Verbs/SequentialIoEmitter.cs:231-236 emits one FileWriteAdvancing; RuntimeApi.FileWriteAdvancing. MISSING: emit combined present+before-advance+after-advance.
- Runtime (EXISTS, single-advance): src/Cobol.Net.Runtime/IO/SequentialConnector.cs:308-320 `WriteAdvancing(image, lines, before)`; Advance :336 (writes `\r\n`×n, or `\f` for PAGE); LINAGE via AdvanceLinageCounter :143. FileRegistry :181, CobolFile :73. MISSING: an overload/path applying BOTH advances after a single presentation (present → Advance(before) → Advance(after), counter += before+after).

B — SUPPRESS WHEN:
- Grammar (EXISTS, no SUPPRESS): CobolIO.g4:146-149 `alternateKeyClause : ALTERNATE RECORD? KEY? IS? dataReference (WITH? DUPLICATES)?`. MISSING: append `(SUPPRESS WHEN literal)?` (`literal` rule already exists).
- Lexer tokens (ALL EXIST): SUPPRESS CobolLexer.g4:295 (currently referenced ONLY by COPY … SUPPRESS PRINTING), WHEN :617, ALTERNATE :331, DUPLICATES :398. No new tokens.
- Binder (EXISTS, drops SUPPRESS): src/Cobol.Net.Compiler/Binding/DataBinder.cs:640-643 (captures name/quals/DUPLICATES into FileModel.AlternateKeyNames) + :867-869 (resolves to AlternateKeys). Model: FileModel.cs:75 `AlternateKeyNames : (Name, Qualifiers, bool Duplicates)`, :78 `AlternateKeys : (DataItem, bool Duplicates)`. MISSING: carry the suppression literal (decoded string) through both tuples.
- Emitter (EXISTS, no suppress arg): src/Cobol.Net.Compiler/CodeGen/Verbs/KeyedIoEmitter.cs:63-71 loops AlternateKeys and emits FileAddAlternateKey(name, off, width, dups); RuntimeApi.FileAddAlternateKey :273. MISSING: pass the suppression literal (or null) as a new arg.
- Runtime (EXISTS, no suppression): src/Cobol.Net.Runtime/IO/IndexedConnector.cs — `_alts : (Off,Len,Dups)` :27; AddAlternateKey :78; KeyOf :450; Ordered :444; Write :277-307; Rewrite :313-345; ReadSequential :172-238 (GR27 '02' lookahead); ReadRandom :240-266; START (SetStart) + FileRegistry/CobolFile shims. MISSING: add a per-alt `Suppress` value; in Write/Rewrite compute per-alt `IsSuppressed = KeyOf(image,i)==suppress`; in Ordered(keyIndex≥0)/ReadSequential/ReadRandom/START skip records where that alt key is suppressed (GR21c/GR41/START); ensure suppressed alt values bypass the '22' duplicate check (GR41 tail) and the GR27 '02' lookahead skips suppressed neighbors.

Gating: VersionConformancePass.cs ParseArm ~818 (add VisitAlternateKeyClause + VisitWriteBeforeAfter); Constructs.g.cs is generated from tests/version-matrix/constructs.json (add two entries). Goldens: tests/conformance/2023/*.cob/.out (stdout-asserted).

**Golden program:**

```
*> Golden A — WRITE … BEFORE ADVANCING 1 AFTER ADVANCING 2 (ISO 2023, §14.9.51 GR25e/25f).
*> Observable = LINAGE-COUNTER (§13.18.34 GR7c/GR7d) — combined advance increments it by before+after.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WBA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF ASSIGN TO "wba-conf.prt".
       DATA DIVISION.
       FILE SECTION.
       FD PF LINAGE IS 20 LINES.
       01 P-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01 LC PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PF.
           MOVE LINAGE-COUNTER TO LC.
           DISPLAY "OPEN LC=" LC.
           MOVE "AAA" TO P-REC.
           WRITE P-REC BEFORE ADVANCING 1 AFTER ADVANCING 2.
           MOVE LINAGE-COUNTER TO LC.
           DISPLAY "WRITE LC=" LC.
           CLOSE PF.
           STOP RUN.

*> ---------------------------------------------------------------------
*> Golden B — SUPPRESS WHEN on ALTERNATE RECORD KEY (ISO 2023, §12.4.5.6 GR6, §14.9.51 GR41, §14.9.30 GR21c).
*> The record whose alt key = "SUP" gets no alternate access path; reading via the alt key skips it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUPWHEN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "supwhen.ix"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-PRIME
               ALTERNATE RECORD KEY IS IX-ALT
                   WITH DUPLICATES SUPPRESS WHEN "SUP"
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD IX.
       01 IX-REC.
          05 IX-PRIME PIC X(3).
          05 IX-ALT   PIC X(3).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IX.
           MOVE "P01" TO IX-PRIME. MOVE "AAA" TO IX-ALT. WRITE IX-REC.
           MOVE "P02" TO IX-PRIME. MOVE "SUP" TO IX-ALT. WRITE IX-REC.
           MOVE "P03" TO IX-PRIME. MOVE "BBB" TO IX-ALT. WRITE IX-REC.
           CLOSE IX.
           OPEN INPUT IX.
           MOVE LOW-VALUES TO IX-ALT.
           START IX KEY IS NOT LESS THAN IX-ALT
               INVALID KEY DISPLAY "START-INV".
           PERFORM UNTIL WS-ST = "10"
               READ IX NEXT RECORD
                   AT END DISPLAY "END"
                   NOT AT END DISPLAY "GOT " IX-PRIME " " IX-ALT
               END-READ
           END-PERFORM.
           CLOSE IX.
           STOP RUN.
```

**Golden expected stdout:**

```
Golden A (wba.out) — exact bytes, two lines each terminated by the platform newline:
OPEN LC=001
WRITE LC=004
Derivation: OPEN OUTPUT sets LINAGE-COUNTER=1 (§13.18.34 GR7d) → MOVE to PIC 9(3) → "001". WRITE presents "AAA" at line 1 (GR25e), advances BEFORE amount 1 (GR25e) then AFTER amount 2 (GR25f) → counter 1+1+2=4 (GR7c2, incremented by the total advance) → "004". 20-line page, no footing, 4<20 ⇒ no end-of-page (GR26), WRITE successful. (A single AFTER ADVANCING 2 alone would give "003"; a single BEFORE ADVANCING 1 alone "002" — so "004" uniquely proves BOTH phrases were parsed and both amounts applied.)

Golden B (supwhen.out) — exact bytes, three lines:
GOT P01 AAA
GOT P03 BBB
END
Derivation: three records written; P02/"SUP" matches SUPPRESS WHEN "SUP" so no alternate-index entry is created for it (§14.9.51 GR41a/b). OPEN INPUT; MOVE LOW-VALUES TO IX-ALT then START KEY NOT LESS THAN IX-ALT sets the alternate as key of reference and positions at the lowest alt value; suppressed records are ignored by START (§14.9.41). READ NEXT walks alternate-key ascending order ("AAA" < "BBB"), and P02 "is not considered to exist" (§14.9.30 GR21c): first read → P01/AAA → "GOT P01 AAA"; second → P03/BBB → "GOT P03 BBB"; third → AT END, WS-ST='10' → "END"; the PERFORM UNTIL WS-ST="10" then exits. DISPLAY concatenates the literal "GOT ", IX-PRIME(3), the literal " ", IX-ALT(3). (P02 remains fully present and readable by prime key — suppression is per-alternate-path only, GR6 NOTE.)
```

**Gotchas / audit-drift:** 1. FORMAT-DIAGRAM DRIFT (A): §14.9.51.2's Format-1 diagram STILL shows BEFORE/AFTER as a single stacked CHOICE and was not updated for 2023; only SR17 + GR25f + E.3.3 item 2 authorize the combination. Do not "trust the format" — implement per the General Rules (owner rule #1). The audit row confirms current grammar is single-choice (CobolIO.g4:355).
2. COUNTERINTUITIVE ORDER (A): when BEFORE co-occurs with AFTER, AFTER's advance is RELOCATED to AFTER presentation (GR25f: "advanced … after the line was presented as specified in 25e"), so both advances stack after the printed line — NOT the classic "AFTER advances before printing". A naïve implementation that keeps AFTER as advance-then-present will mis-place the line. The two source phrases' order is unpinned by the spec; normalize at bind.
3. SR17 (A): PAGE may not co-occur with the combined BEFORE+AFTER; the current CheckWriteEopAdvancingPage only handles the single-PAGE case — extend the SR17 check for the pair. Also SR18 (ADVANCING PAGE vs END-OF-PAGE) and the LINAGE EOP EC family (EC-I-O-EOP / EC-I-O-EOP-OVERFLOW, GR26/27) can be triggered by the summed advance — the combined advance must feed the SAME end-of-page detection.
4. GATE IS ON CO-OCCURRENCE, NOT THE PHRASE (A): single BEFORE / single AFTER is edition-invariant (85+) and must NOT diagnose. Gate only when Length==2 (parse-arm recognition). A bound-arm gate would drop the 0900 on a bind-error path (DEVLOG-724).
5. SUPPRESS ≠ DELETE (B): the physical record is intact and reachable by prime key or any non-suppressed alternate — only the one alternate access PATH is withheld (GR6 NOTE). Do not remove the record from _recs.
6. DUPLICATES INDEPENDENCE (B): suppressed alt values must bypass BOTH the no-DUPLICATES '22' write check AND the READ-NEXT GR27 '02' duplicate-lookahead — GR41 tail: "Key entries that are suppressed shall not cause a duplicate key condition." Any number of records may carry the suppression value with no DUPLICATES clause. A naïve `_recs.Any(KeyOf==value)` duplicate check (IndexedConnector.cs:296-301) will wrongly reject the 2nd suppressed record — must skip suppressed entries in that scan.
7. REWRITE DYNAMICS (B): §14.9.35 GR24 requires live maintenance — moving an alt value TO the literal drops its path; moving it AWAY re-provides+repositions it. Suppression is recomputed per WRITE/REWRITE from the record image, not cached at open.
8. COLLATING (B): the equal-to-literal test uses the file's collating sequence per relation-condition rules (§14.9.51 GR35), and the literal may be a figurative constant (SR7) — decode SPACE/LOW-VALUE/ALL "x"/HIGH-VALUE to the key width; ALL literal must be exactly 1 char (SR7).
9. START/READ skip surface (B): the skip must apply to the DERIVED alternate ordering only (Ordered(keyIndex≥0) and ReadRandom's alt scan and SetStart's alt search) — prime-key reads (keyIndex=-1) never suppress. A suppressed-only alt value that no record exposes must yield '23' invalid-key on START and at-end/'23' on random READ, not a false hit.
10. Golden-A observability caveat: LINAGE-COUNTER proves the summed advance and that both phrases parsed, but not the physical line placement; the present→before→after ordering (gotcha 2) is additionally the runtime's WriteAdvancing responsibility and would be pinned by a byte-level print-stream unit test (FileIoDifferential/Linage pattern) alongside the stdout golden.

**Complexity:** M — A (WRITE BEFORE+AFTER) is S/M: bounded grammar refactor (one repeatable phrase), a paired BoundAdvancing on BoundWrite, one new runtime advance-path, SR17 extension, one co-occurrence gate. B (SUPPRESS WHEN) is M/L: literal plumbed through two binder tuples + FileModel + emitter + RuntimeApi, and per-alt suppression woven into IndexedConnector Write/Rewrite/ReadSequential/ReadRandom/START/Ordered plus the duplicate-check and GR27 lookahead exclusions — more touch-points and more adversarial edge cases (figurative literals, collating, DUPLICATES interplay). Combined: two goldens, two constructs.json rows, both below-2023 COBOLNET0900 gated. No new numeric diagnostics.

---

## C7. COBOL-2023 word-length relaxation (COBOL words up to 63 characters) + SET dynamic-length form (SET [SIZE OF] data-name TO … — §14.9.39 Format 16). Two related but distinct 2023 deltas bundled in this scout.

**Spec sections:** §8.3.2.1 General (COBOL words) — "A COBOL word is a character-string of not more than 63 characters" (line 5405): the current 2023 hard cap.
§8.3.1/§8.3.2.4 — word formation (hyphen/underscore not first/last; ≥1 letter §8.3.2.2 line 5495).
§8.3.1 line 5238 (c) — combining/extended letters count as SEPARATE characters for word length.
Annex E.3.3 item 11 (line 50237) — "COBOL Words. COBOL words may now be 63 characters long"; category "Not affecting" (a prior-valid ≤31 word stays valid) — confirms 2023 RELAXATION from the prior 31.
§8.5.1.10 Dynamic-length elementary items (line 8260); §8.5.1.10.1 max size = smallest of {LIMIT, largest int in PREFIXED usage, implementor max} (line 8267); §8.5.1.10.4 Operations (line 8292) — sender/ref-mod treated as fixed-length of current length; initial length 0 unless VALUE (line 8784); line 8300 cross-refs the SET form + EC-STORAGE-NOT-AVAIL.
§14.9.39 SET statement — §14.9.39.1 purpose bullet "setting the length of a dynamic-length elementary data item" (line 31128); §14.9.39.2 Format 16 syntax (line 31248); §14.9.39.3 SR33/SR34 (lines 31463-31467); §14.9.39.4 GR37/GR38/GR39 (lines 31737-31743).
Annex E.3.3 item 17 (line 50271) — "the SET statement was enhanced to allow the setting of the length of a dynamic-length elementary item" — confirms Format 16 is a 2023 introduction.
EC table: EC-STORAGE-NOT-AVAIL (NF) row (line 24857) explicitly names "a dynamic-length elementary data item format of a SET statement". (NOT EC-BOUND — the audit hint's EC-BOUND assumption is wrong for this form.)

**Syntax / format:** WORD LENGTH (§8.3.2.1, exact quote): "A COBOL word is a character-string of not more than 63 characters that forms a compiler-directive word, a context-sensitive word, an intrinsic-function-name, a reserved word, a system-name, or a user-defined word. Each character of a COBOL word that is not a special character word shall be selected from the set of basic letters, basic digits, extended letters, and the basic special characters hyphen and underscore. The hyphen or underscore shall not appear as the first or last character in such words." (No BNF — it is a lexical constraint of "not more than 63 characters".)

SET Format 16 (dynamic-length-elementary-data-item), §14.9.39.2 line 31248, exact:
  SET [SIZE OF] data-name-3 TO { integer-2 / arithmetic-expression-5 }
Reserved/keyword: SET, TO. Optional phrase: SIZE OF (both SIZE and OF are keywords here). Required choice: integer-2 | arithmetic-expression-5. NOTE the keyword is SIZE OF — NOT "LENGTH OF" as the task hint stated.

**Introduced edition:** 2023 (for BOTH deltas). Word-length 63 = §8.3.2.1 / E.3.3 item 11 (a relaxation from the prior 31). SET SIZE OF dynamic-length = §14.9.39 Format 16 / E.3.3 item 17. (The DYNAMIC LENGTH clause the SET form operates on is separately gated at 2014 in this codebase — Constructs.DynamicLengthItem2014, constructs.json id "dynamic-length-item-2014" line 306 — landed in P12; that gating is out of scope here and is trusted, not re-litigated.)

**Semantics:** WORD LENGTH: a COBOL word (reserved, user-defined, system, function, directive, context-sensitive) may be up to 63 characters at 2023 (§8.3.2.1). Character count, not byte count — combining/extended letters each count as one character (§8.3.1 c); for ASCII portable source char==byte. Below 2023 the ceiling is lower (31 in 2002/2014, 30 in 1985 — these prior numbers come from the PRIOR standards; the in-repo 2023 spec documents only 63). Because the change is a relaxation ("Not affecting", E.3.3 item 11), the compiler must: (a) accept ≤63 at 2023; (b) REJECT a 32–63-char word below 2023; (c) reject a >63-char word at EVERY edition (hard cap, always-on). This is a length ceiling, the OPPOSITE direction from a new-feature introduction gate.

SET Format 16 (SR33: data-name-3 is a dynamic-length elementary item; SR34: a LITERAL integer-2 shall be non-negative AND ≤ the item's maximum size — a COMPILE-TIME syntax rule). Runtime GRs:
 - GR38 (line 31741): if integer-2 given, current length := integer-2. If arithmetic-expression-5 given, length := its value; but if that value > the item's maximum size, length := the maximum and EC-STORAGE-NOT-AVAIL is set. If the storage to expand is unavailable, length is UNCHANGED and EC-STORAGE-NOT-AVAIL is set.
 - GR37 (line 31739): if arithmetic-expression-5 evaluates < 0, length := 0 and EC-STORAGE-NOT-AVAIL is set; if it evaluates to a non-integer, truncate toward nearest integer first.
 - GR39 (line 31743): if the NEW length > previous length, the ADDED character positions are initialized to alphanumeric or national SPACES (per the item's class) — NOT to any previously-truncated content. Shrinking simply drops the trailing positions (their content is not restorable). This makes shrink-then-grow yield retained head + fresh spaces.

**Below-2023 gate:** TWO independent below-edition diagnostics, both currently MISSING.

(1) SET SIZE OF form — a standard new-feature INTRODUCTION gate = COBOLNET0900 emitted at every edition < 2023 (85/2002/2014). Home: VersionConformancePass.ParseArm as a new override VisitSetSizeStatement(...) → _p.Check(Constructs.SetDynLengthSize2023, "the SET … TO length form of a dynamic-length elementary item") — mirrors VisitDynamicLengthClause (VersionConformancePass.cs:620) and the DELETE FILE / other 2023 parse-arm gates. Add a Constructs.SetDynLengthSize2023 registry row (introducedIn 2023) + a constructs.json row.

(2) 63-char word length — NOT a 0900 introduction gate (wrong direction; it is a ceiling that TIGHTENS below 2023). Needs a NEW dedicated diagnostic code (e.g. COBOLNET15xx "COBOL word exceeds the N-character limit for COBOL-<year>"), emitted when word length > maxForEdition, where maxForEdition = 63 (year 2023) / 31 (2014, 2002) / 30 (1985). Fire at editions < 2023 for 32–63-char words; fire at ALL editions for >63-char words. Natural home: VersionConformancePass.ParseArm.VisitCobolWord (VersionConformancePass.cs:1424) — it already has ctx.Start.Text and _p._edition.Year and a per-distinct-word dedup set (_flaggedWords, cf. the 0901 reserved-word funnel at line 1461); add the length test alongside RejectsAt. Dedup one report per distinct over-long word per compilation, like the 0901 band.

**Code anchors:** GRAMMAR (src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4):
 - setStatement rule :962-971 — MISSING a Format-16 alternative. Add `| setSizeStatement` and a new rule `setSizeStatement : SET SIZE OF dataReference TO arithmeticExpression ;` placed BEFORE setToValueStatement (SIZE is a keyword, not a dataReference head, so no ambiguity). SR34 permits integer-2 OR arithmetic-expression-5 → use arithmeticExpression to cover both.
 - setToValueStatement :994-997 (`SET dataReference+ TO arithmeticExpression`) — the OPTIONAL "SIZE OF" absent form (`SET dyn TO n`) parses here today and mis-binds. Handle it with a binder PEEK (see SetBinder below), exactly like the existing Format-14 DynTryBindSetCapacity peek.

LEXER (src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4): tokens ALREADY EXIST — SIZE :586, OF :507. NAME_BODY fragment :646-651 has NO length cap (all four alternatives are unbounded) — this is why over-long words are silently accepted; the cap is enforced in the validation pass, not the lexer.

BINDER (src/Cobol.Net.Compiler/Binding/Procedure/Verbs/SetBinder.cs):
 - BindSet dispatch :23-55 — add `if (set.setSizeStatement() is {} ss) return BindSetSize(ss);` (place before setToValueStatement).
 - BindSetTo :191-224 — add a `DynTrySetSize(...)` peek at the TOP (mirror DynTryBindSetCapacity :249-263): if ctx.Refs.Resolve(target0) is a dynamic-length elementary item, reroute the bare `SET dyn TO n` to the size form.
 - NEW: BindSetSize producing a NEW BoundSetSize node. Resolve data-name-3, verify it is dynamic-length (SR33 → new diagnostic if not), verify literal integer-2 ≤ max (SR34, compile-time).
 - Need a DynamicLength predicate on the resolved item — DataItem.IsDynamicLength exists (referenced by StorageFormPass / DataBinder; VersionConformancePass.cs:617 comment cites DataItem.IsDynamicLength).

BOUND TREE (src/Cobol.Net.Compiler/Binding/Bound/BoundStores.cs) — add BoundSetSize(Place target, BoundExpression amount, bool isLiteral) alongside BoundSetCapacity/BoundSetTo.

EMITTER (src/Cobol.Net.Compiler/CodeGen/Verbs/SetEmitter.cs) — add EmitSetSize mirroring EmitSetCapacity :66 (which emits a runtime "SetCapacity"/"CapacityUpBy" call). Wire into StatementEmitter dispatch + RuntimeApi.cs.

RUNTIME (src/Cobol.Net.Runtime/Values/Text/CobolDynString.cs) — currently only Store(value,limit) :23. Add SetSize(string current, int newLen, int limit, bool national) → newLen<=len ? current[..newLen] : current + new string(national?nationalSpace:' ', newLen-len); clamp to limit + signal EC-STORAGE-NOT-AVAIL; negative → "" + EC. (Parallels CobolDynTable capacity ops.)

VERSION GATE (src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs): SET-SIZE 0900 gate → new VisitSetSizeStatement override near VisitDynamicLengthClause :620. WORD-63 gate → extend VisitCobolWord :1424 with the length ceiling. EditionContext.DialectLevel => Edition.Year (EditionContext.cs:49) gives 85/2002/2014/2023.

CONSTRUCTS: tests/version-matrix/constructs.json (dynamic-length row at :306 is the template) — add a "set-dyn-length-2023" row (introducedIn 2023, expectDiagnostic COBOLNET0900) and a word-length row. Golden dirs tests/conformance/2023/ + manifest.json.

**Golden program:**

```
*> ISO 2023 §14.9.39 Format 16 — SET [SIZE OF] data-name TO n sets the
      *> current length of a DYNAMIC LENGTH elementary item. GR38 sets the
      *> length; GR39 space-fills positions ADDED when growing (never restores
      *> truncated content); shrinking drops trailing positions. Requires
      *> --std 2023 (below 2023 => COBOLNET0900 introduction gate).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SET-SIZE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D  PIC X DYNAMIC LENGTH.
       01 WS-N  PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "HELLO" TO WS-D.
           DISPLAY "A[" WS-D "]".
           SET SIZE OF WS-D TO 3.
           DISPLAY "B[" WS-D "]".
           SET SIZE OF WS-D TO 6.
           DISPLAY "C[" WS-D "]".
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "LEN=" WS-N.
           STOP RUN.
```

**Golden expected stdout:**

```
A[HELLO]
B[HEL]
C[HEL   ]
LEN=06

Byte derivation:
- Line 1 `A[HELLO]`: MOVE "HELLO" TO WS-D sets content "HELLO", current length 5 (§8.5.1.10.4 — receiving, no padding). DISPLAY treats the sender as fixed-length of current length 5 → HELLO.
- Line 2 `B[HEL]`: SET SIZE OF WS-D TO 3 (Format 16 GR38) → length := 3; 3 < previous 5 so GR39 does not apply; content is the first 3 chars "HEL". DISPLAY → HEL.
- Line 3 `C[HEL   ]`: SET SIZE OF WS-D TO 6 → length := 6; 6 > previous 3, so GR39 initializes the 3 ADDED positions (4,5,6) to alphanumeric SPACES (NOT the previously-dropped "LO"). Content = "HEL" + three spaces = `HEL   `. DISPLAY shows all 6 chars incl. trailing spaces inside the brackets.
- Line 4 `LEN=06`: FUNCTION LENGTH(WS-D) = current length 6 (§8.5.1.10.4 sender rule); MOVE to WS-N PIC 9(2) → "06".
(No LIMIT phrase → maximum size is implementor-unbounded, so SET SIZE TO 6 raises no EC-STORAGE-NOT-AVAIL; SR34 satisfied.)
```

**Gotchas / audit-drift:** AUDIT-DRIFT CATCHES (do NOT trust the hint):
1. The task hint said the keyword is "SET LENGTH OF id / SET id … LENGTH". WRONG. §14.9.39 Format 16 (line 31251) is `SET [SIZE OF] data-name-3 TO {integer-2 / arithmetic-expression-5}` — the keyword is SIZE OF, and it is OPTIONAL. There is no "LENGTH" keyword and no trailing-LENGTH form. Implementing LENGTH OF would be non-spec.
2. The hint framed the exception as EC-BOUND. WRONG for this form. The EC is EC-STORAGE-NOT-AVAIL (§8.5.1.10.4 line 8300; GR37/GR38; EC table line 24857). EC-BOUND-SET belongs to the DYNAMIC-CAPACITY table SET (Format 14, GR line 31686) — a different format. Do not cross-wire them.

SPEC SUBTLETIES:
3. Optional "SIZE OF" ⇒ the bare `SET dyn TO n` is legal and must also route to Format 16. Because a dynamic-length item is alphanumeric/national (not numeric/index), the bare form cannot be a Format-1 value SET, so a binder peek on the first target's type (mirror DynTryBindSetCapacity, SetBinder.cs:249) disambiguates. A grammar alt alone (SIZE-OF-present) is insufficient.
4. Literal vs runtime bound differ: a LITERAL integer-2 that exceeds the max size is a COMPILE-TIME syntax error (SR34); a runtime arithmetic-expression-5 that exceeds the max is a RUNTIME clamp-to-max + EC-STORAGE-NOT-AVAIL (GR38). Negative arith → length 0 + EC (GR37). Non-integer arith → truncate toward nearest integer BEFORE applying (GR37). Four distinct branches — a golden should eventually hit each.
5. GR39 grow initializes added positions to SPACES, never to previously-truncated bytes. A naive impl that keeps a full backing .NET string and only tracks a length int would leak the old "LO" and print `HELLO ` instead of `HEL   ` on line 3. The golden is deliberately shrink-then-grow to catch exactly this.
6. National dyn items (PIC N DYNAMIC LENGTH) pad with NATIONAL spaces, not alphanumeric (GR39). Runtime SetSize needs the class flag.

WORD-LENGTH GOTCHAS:
7. It is a RELAXATION (E.3.3 "Not affecting"), so the gate direction is inverted from a 0900 introduction: at 2023 accept ≤63, below 2023 REJECT 32–63, and reject >63 at ALL editions. Do NOT model it as introducedIn-2023-COBOLNET0900 the way a new construct is (a 40-char word program COMPILES at 2023 and FAILS below — the matrix row's expected outcome is "compiles at 2023, diagnostic below").
8. Length is CHARACTERS, and §8.3.1(c) makes extended/combining letters count separately — ctx.Start.Text.Length (UTF-16 code units) can diverge from character count for non-ASCII words; for the portable ASCII repertoire (the practical corpus) they coincide. Flag if national/extended user words are ever exercised.
9. Prior-edition caps (31 for 2002/2014, 30 for 1985) are NOT in the in-repo spec (which is 2023-only, showing 63). They come from the earlier ISO standards; cite them as edition history, not from ISO_COBOL.md. The only in-repo authority for the delta is §8.3.2.1 (63) + E.3.3 item 11 (was less).
10. DYNAMIC LENGTH itself is gated 2014 here (Constructs.DynamicLengthItem2014); the golden combines a 2014 clause + a 2023 SET — both legal at --std 2023. If DYNAMIC LENGTH's 2014 gating is ever revisited it does not change the SET-SIZE 2023 gate.

**Complexity:** M — SET SIZE OF is a self-contained new format (grammar alt + bare-form binder peek + BoundSetSize + emitter + one CobolDynString.SetSize runtime method + 0900 gate + one golden), closely mirroring the already-landed Format-14 dynamic-capacity SET, so low risk. The 63-char word gate is small (one VisitCobolWord length test + a new diagnostic code + edition-max table) but adds a matrix construct whose expected outcome is inverted (compiles-at-2023/rejects-below) and needs a per-distinct-word dedup like the 0901 funnel. Two diagnostics, two constructs.json rows, ~2 goldens.

---

## C8. GOBACK status-phrase (COBOL-2023): `GOBACK WITH {ERROR | NORMAL} STATUS [identifier-2 | literal-1]`

**Spec sections:** §14.9.18 GOBACK statement — the authority. Specifically: §14.9.18.2 (General format, line 27660–27670: the outer `[ raising-phrase / status-phrase ]` bracket + the status-phrase sub-format `WITH {ERROR|NORMAL} STATUS [id-2 | lit-1]`); §14.9.18.3 SR6 (id-2 = integer/usage-display/usage-national), SR7 (numeric lit-1 must be integer), SR8 (lit-1 not zero-length); §14.9.18.4 GR3 (GOBACK not under a calling element ≡ STOP with the status phrase; RAISING ignored), GR7 (main + ERROR → OS error termination), GR8 (main + NORMAL → OS normal termination), GR9 (main + neither → normal unless implementor error mechanism), GR10 (main + lit-1/id-2 value passed to OS; constraints implementor-defined). §14.9.42 STOP statement (the SIBLING status phrase — same shape, §14.9.42.2/.3 SR2–4). Annex substantive-changes item 32 (line 50308: "The GOBACK statement now allows the same status phrase as the STOP statement. This only takes effect when the GOBACK statement appears in a COBOL main program.") — confirms 2023 introduction. Conformance item 94 (line 39491) + 192 (line 39751): STATUS value constraints are implementor-defined + required-to-document.

**Syntax / format:** Outer format (§14.9.18.2, line 27662): GOBACK [ raising-phrase | status-phrase ] — raising-phrase and status-phrase are MUTUALLY EXCLUSIVE alternatives. status-phrase (line 27668–27670): WITH { ERROR | NORMAL } STATUS [ identifier-2 | literal-1 ]. Reserved/underlined: WITH is NOT underlined (optional-word-but-present), ERROR and NORMAL are underlined (required keywords, one required), STATUS is required (NOT bracketed — it is mandatory when the phrase is present), identifier-2/literal-1 are the optional value operand (the [...] bracket around them = the value is optional; ERROR/NORMAL alone is legal). NOTE the 2023 GOBACK Format at line 27662 shows ONLY raising-phrase | status-phrase — it does NOT list a GOBACK RETURNING phrase (see gotchas).

**Introduced edition:** 2023

**Semantics:** The status phrase takes effect ONLY when GOBACK executes in a COBOL main program (annex item 32; GR3 routes a not-under-a-caller GOBACK to STOP-statement semantics carrying the status phrase). Precise GRs: GR7 — main + ERROR STATUS → the OS is told the run unit terminated abnormally (error), if the OS supports it. GR8 — main + NORMAL STATUS → OS told normal termination. GR9 — main + no status phrase → normal termination unless an implementor-defined error mechanism already flagged error. GR10 — when lit-1/id-2 is present in a main program, that value (lit-1, or the current contents of id-2) is passed to the OS as the termination/exit status; any value constraints are implementor-defined (conformance item 94/192, must be documented). In a NON-main / called program the status phrase is effectively inert for the value except that GR3 says the program "operates as if executing a STOP statement, with a status phrase, if any" — i.e. the run unit terminates. Full spec semantics therefore = set the process exit code to (id-2/lit-1 value) and mark normal-vs-error termination. The value→exit-code plumbing does NOT yet exist in the codebase (see gotchas) — the STOP-status sibling is presence-only, so the spec-complete VALUE wiring is a separate cross-cutting slice.

**Below-2023 gate:** A NEW-FEATURE INTRODUCTION gate = COBOLNET0900, single-sourced through the post-bind VersionConformancePass (recognition-based on the parse context, exactly like the STOP sibling). Add a construct row `goback-status-2023` to tests/version-matrix/constructs.json (introducedIn: 2023, diagnosticCode/expectDiagnostic: COBOLNET0900, citation ISO §14.9.18), which regenerates Constructs.g.cs → `Constructs.GobackStatus2023`. Then in VersionConformancePass.VisitGobackStatement (CallBinder-adjacent pass, currently at src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs:993) add: `if (ctx.statusPhrase() is not null) _p.Check(Constructs.GobackStatus2023, "the GOBACK … WITH NORMAL/ERROR STATUS phrase");` — mirroring the STOP arm at :459-460. It must REJECT (COBOLNET0900) at --std 85, 2002, and 2014, and ACCEPT at --std 2023. NOTE this is a DISTINCT edition from the STOP-status gate (StopRunStatus2002 = 2002); do NOT reuse that construct id — GOBACK status is strictly 2023.

**Code anchors:** EXISTS: (1) Grammar rule gobackStatement — src/Cobol.Net.Frontend/Grammar/Core/CobolControlFlow.g4 (referenced from CobolParserCore.g4:1087–1089): currently `GOBACK ((RETURNING|GIVING) dataReference)? raisingPhrase?`. (2) Sibling status rule `stopStatusPhrase` — same CobolControlFlow.g4 (`WITH (ERROR|NORMAL) (STATUS (dataReference|literal))? | STATUS (dataReference|literal)`); a near-match to reuse/share. (3) Lexer tokens WITH/ERROR/NORMAL/STATUS all EXIST (CobolLexer.g4:618/417/499/590) — NO new tokens needed. (4) Binder: CallBinder.BindGoback — src/Cobol.Net.Compiler/Binding/Procedure/Verbs/CallBinder.cs:185-204 (the XML doc at :183-184 explicitly says the 2023 status phrase is unhandled). (5) Bound type BoundGoback — src/Cobol.Net.Compiler/Binding/Bound/BoundCall.cs (ctor `BoundGoback(source[, raising])`); sibling BoundStop.HasStatusPhrase presence-only flag at BoundTree.cs:342-348. (6) Emitter: CallEmitter.EmitGoback — src/Cobol.Net.Compiler/CodeGen/Verbs/CallEmitter.cs (emits `throw new ProgramReturn();`); StatementEmitter.Visit(BoundGoback) at :197. (7) Gate pass: VersionConformancePass.VisitGobackStatement at Validation/VersionConformancePass.cs:993-1004 (STOP sibling arm at :453-462). (8) Construct registry input: tests/version-matrix/constructs.json (goback-returning-2002 @ line 91, stop-run-status-2002 @ line 103) → generates src/Cobol.Net.Editions/Constructs.g.cs. MISSING: the grammar status alternative on gobackStatement; a `statusPhrase()` accessor on GobackStatementContext; the `goback-status-2023` construct row + `Constructs.GobackStatus2023`; the VCR VisitGobackStatement status check; a presence flag on BoundGoback (only if matching the STOP presence-only pattern) OR full value→exit-code runtime wiring (StopRun/ProgramReturn currently carry NO status payload; Main is `void Main()` with Environment.ExitCode set only in the fatal-EC catch at ProgramEmitter.cs:413 — NO exit-code plumbing exists).

**Golden program:**

```
IDENTIFICATION DIVISION.
PROGRAM-ID. GBSTAT.
DATA DIVISION.
WORKING-STORAGE SECTION.
01  RC  PIC 9 VALUE 0.
PROCEDURE DIVISION.
MAIN.
    DISPLAY "STATUS PHRASE OK".
    GOBACK WITH NORMAL STATUS RC.
```

**Golden expected stdout:**

```
STATUS PHRASE OK
(one line, "STATUS PHRASE OK" followed by a single trailing newline; no other bytes). Derivation: DISPLAY sends the literal to the default output device with an implied line advance (§14.9.13) → 16 chars + LF. Then GOBACK executes in the MAIN program (GBSTAT is the first/only program) → §14.9.18.4 GR3: not under a calling element ⇒ operates as STOP, terminating the run unit; GR8/GR10: NORMAL STATUS RC (RC=0) ⇒ OS told normal termination with status 0. Emitted GOBACK throws ProgramReturn (CallEmitter.EmitGoback), caught by __Dispatch → RunMain returns → normal run-unit termination, process exit 0. The status VALUE (0) is not yet wired to Environment.ExitCode, but NORMAL/0 coincides with the current normal-exit-0 behavior, so the stdout golden asserts nothing unimplemented. IMPORTANT: keep the golden on NORMAL STATUS 0 (or no value) — do NOT golden an ERROR STATUS n case, whose spec-required nonzero error exit (GR7/GR10) is not yet implemented.
```

**Gotchas / audit-drift:** 1) AUDIT-DRIFT — value wiring does NOT exist even for the STOP sibling: BoundStop.HasStatusPhrase is PRESENCE-ONLY (consumed solely by the VCR introduction gate); the emitter emits bare `throw new StopRun()`, StopRun/ProgramReturn carry NO status payload, and Main is `void Main()` (exit code set only on fatal EC, ProgramEmitter.cs:413). So the resume-banner note "BoundStop.HasStatusPhrase is presence-only" is CORRECT and means the spec-complete GR7/GR10 value→exit-code behavior is UNIMPLEMENTED across BOTH statements. RECOMMENDATION: do the GOBACK slice presence-only to MATCH STOP (parse + 2023 gate + compile-only golden); defer the value→Environment.ExitCode wiring to a SEPARATE slice that does STOP and GOBACK TOGETHER (feedback_singular_pattern — one exit-code mechanism, not two). 2) The 2023 GOBACK Format (§14.9.18.2, line 27662) lists ONLY raising-phrase | status-phrase — it does NOT contain a RETURNING phrase, yet the existing grammar+gate treat `GOBACK RETURNING` as ISO §14.9.18 (constructs.json:91 goback-returning-2002). That is an implementor EXTENSION, not ISO-2023 format; out of scope for this slice but flag it — when adding the status alt, place status/raising as mutually-exclusive alternatives while preserving the (extra-spec) RETURNING prefix: `GOBACK ((RETURNING|GIVING) dataReference)? (raisingPhrase | statusPhrase)?`. 3) status vs raising are MUTUALLY EXCLUSIVE in the format braces — grammar must NOT allow both; use `(raisingPhrase | statusPhrase)?`. 4) SINGULAR-PATTERN: annex item 32 says GOBACK uses "the same status phrase as the STOP statement" — rename `stopStatusPhrase` → a shared `statusPhrase` rule referenced by BOTH stopStatement and gobackStatement (one mechanism). Caveat: the current stopStatusPhrase is LENIENT vs the 2023 format (it makes STATUS optional after WITH and allows a bare `STATUS x` without WITH — neither is in the 2023 GOBACK/STOP format). Sharing inherits that leniency; if strict-2023 rejection of those forms is wanted it is a separate STOP-conformance fix, not this slice. 5) EC interaction: GR3 says a GOBACK with BOTH a RAISING and (via the format) a status phrase is impossible — they are exclusive alternatives; and a not-under-a-caller GOBACK IGNORES RAISING (GR3) — no EC is raised by the status form. The status phrase raises NO exception condition itself. 6) In a NON-main program the status phrase parses+gates but is semantically inert for the exit value (GR3/annex-32 "only takes effect ... in a COBOL main program") — do not special-case; presence-only emit is fine. 7) SR6 allows id-2 to be integer OR usage display/national (a PIC X status string) — if/when value wiring lands, the OS-status conversion must handle non-numeric display/national contents (implementor-defined, item 94), not assume an integer.

**Complexity:** S — presence-only slice matching the STOP-status precedent: one grammar alternative (share `statusPhrase`, no new lexer tokens), one constructs.json row (regenerates the enum), one VisitGobackStatement gate line, one 2023 conformance golden + the below-2023 rejection matrix rows. The spec-complete value→exit-code VALUE wiring (GR7/GR10) is deliberately OUT of this slice; folding it in (StopRun status payload + int Main + retrofit STOP) would raise it to M/L and should be its own singular-pattern slice.

---
