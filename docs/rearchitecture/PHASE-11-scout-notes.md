# PHASE-11 — Verified spec anchors + code seams (retained reference)

> **STATUS: RETAINED REFERENCE (PHASE-11 is DONE).** This is the persisted output of the P11 anchor re-scout
> (11 parallel read-only agents, 2026-07-17) run per the P10 recurring lesson (*every wave re-scouts its
> anchors spec-first before implementing*). Each section is one scout's verified findings: exact ISO/IEC
> 1989:2023 §/GR/SR anchors as `specs/ISO_COBOL.md` numbers them (with line numbers), hand-derived golden
> values with rule citations, and the code seams (file:line) each family touches. It was the working reference
> DURING P11; it is KEPT as a durable spec-to-code traceability record for future waves that touch these
> functions (e.g. the deferred Tier-C Step D, or a P12/P13 wave over the date/national/boolean families).
> **⚠ Its forward-resume framing ("read this FIRST", "the executing session") is now HISTORICAL** — P11 is
> complete; the current design lives in the design-doc corpus. Line numbers were captured at `45fe74dd` and
> have since shifted. The Step-2 APPENDIX (pre-authored files) is transient scratch, superseded by the landed
> commits.
>
> Companion to `PHASE-11-intrinsics-backlog-tierc-codec.md` (the step plan, STATUS: DONE).

## `spec:boolean` — BOOLEAN-OF-INTEGER §15.13 / INTEGER-OF-BOOLEAN §15.45

### Sections found

| § | Title | spec line |
|---|---|---|
| 15.13 | BOOLEAN-OF-INTEGER function | 34594 |
| 15.13.1 | General | 34597 |
| 15.13.2 | General format | 34604 |
| 15.13.3 | Arguments rules | 34609 |
| 15.13.4 | Returned value rule | 34616 |
| 15.45 | INTEGER-OF-BOOLEAN function | 36245 |
| 15.45.1 | General | 36248 |
| 15.45.2 | General format | 36255 |
| 15.45.3 | Argument rule | 36260 |
| 15.45.4 | Returned value rule | 36265 |
| 15.2 | Types of functions (item 2: Boolean functions = class/category boolean, implicit usage bit; item 5: Integer functions) | 33688 |
| 15.3 | Arguments (types-of-argument list; the EC-ARGUMENT-FUNCTION paragraph is at line 33761) | 33707 |
| 15.4 | Returned values (implementor max returned-value length -> EC-ARGUMENT-FUNCTION) | 34000 |
| 8.3.3.4 | Boolean literals (B"..."/B'...' and BX"..." hex form) | 6082 |
| 8.5.2.5 | Boolean category (includes 'A boolean function') | 8482 |
| 14.6.8.6 | Receiving data items of category boolean (MOVE: left-aligned, zero fill/truncation to the RIGHT) | 24304 |
| 14.9.11.4 GR1 | DISPLAY statement: device conversion is implementor-defined | 26893 |
| 8.8.4.7.4 GR1a | Simple sign condition: POSITIVE true iff value > 0 | 10056 |
| Table (EC list) | EC-ARGUMENT-FUNCTION | Fatal | Function argument error | 24636 |
| D.10 (Annex) | Boolean support and bit manipulation — worked BOOLEAN-OF-INTEGER(integer-item,6)/INTEGER-OF-BOOLEAN example with 544 -> 32 | 44633 |

### Semantics

## §15.13 BOOLEAN-OF-INTEGER (spec file E:/COBOL/specs/ISO_COBOL.md, line 34594)

General (15.13.1): "The BOOLEAN-OF-INTEGER function returns a boolean item of usage bit representing the binary value of argument-1. Argument-2 specifies the length of the boolean data item that is returned. The function type is boolean." Per §15.2 item 2, a boolean function is "of the class and category boolean" with "implicit usage bit".

Format (15.13.2): `FUNCTION BOOLEAN-OF-INTEGER ( argument-1 argument-2 )` — no comma in the general format; the comma is an optional COBOL separator (the Annex D.10 example writes `( integer-item , 6)`).

Arguments rules (15.13.3): "1) Argument-1 shall be a positive integer. 2) Argument-2 shall be a positive nonzero integer." Both are Integer-type arguments per §15.3 item 6 ("An arithmetic expression that will always result in an integer value or an integer data item... The value of the arithmetic expression, including operational sign, is used"). Note argument-2 is a plain integer VALUE (the requested bit length), NOT a data item/length reference. See gotcha on whether arg-1 'positive' admits zero.

Returned value rule (15.13.4, rule 1, verbatim): "The returned value is a boolean item of usage bit that has the same bit configuration as the binary representation of the value of argument-1, where the rightmost boolean position is the low-order binary digit. The boolean value is zero-filled or truncated on the left, if necessary, in order to return a boolean item whose length is specified by argument-2 in terms of boolean positions." NOTE: "Binary representation is a mathematical concept. It is not required that this representation be the same as a COBOL representation." So the result is exactly (argument-1 mod 2^argument-2) written MSB-first in argument-2 bit positions — the plan's MSB-first/unsigned-binary assumption is VERIFIED, with the addition that left truncation (arg-1 too big for arg-2 bits) is NORMAL, not an error (Annex D.10 comment: "the function returns the low order 6 bits").

## §15.45 INTEGER-OF-BOOLEAN (line 36245)

General (15.45.1): "returns the numeric value of the boolean string in argument-1. The function type is integer" (§15.2 item 5: integer function = class/category numeric, operational sign, no fraction digits).

Format (15.45.2): `FUNCTION INTEGER-OF-BOOLEAN ( argument-1 )`.

Argument rule (15.45.3): "1) Argument-1 shall be of class boolean." Per §15.3 item 3 (Boolean argument): "A bit group item, a boolean expression or literal, or an elementary boolean data item shall be specified. The size associated with the argument may be used in determining the value of the function." So literals (B"..."/BX"...") and reference-modified bit items are valid arguments.

Returned value rule (15.45.4, rule 1, verbatim): "a) Argument-1 is assigned to a temporary boolean data item of usage bit described with the same number of boolean positions as argument-1. b) The unsigned binary value represented by the same bit configuration as the bit configuration of that temporary boolean data item is determined. [NOTE: mathematical concept] c) The numeric value determined in subrule 1b is the returned value." So: unsigned, MSB-first interpretation of the whole bit string — the plan's assumption is VERIFIED. The informative summary table (line 34226) paraphrases it as "The numeric value of a BINARY-DOUBLE item whose bit configuration is the same as argument-1, right-justified" — informative only, but hints the practical result width is 64-bit unsigned.

## Error / EC conditions

Neither §15.13 nor §15.45 has function-local EC rules. The EC machinery is entirely the general §15.3 paragraph (line 33761, verbatim): "The rules for a function may place constraints on the permissible values for arguments... If the evaluation of an argument results in an incorrect value for that argument or for the returned value according to the rules specified in the function definition and no exception condition was raised during item identification or expression evaluation, the EC-ARGUMENT-FUNCTION exception condition is set to exist. If an exception condition is raised during item identification or expression evaluation, that exception condition is raised, not EC-ARGUMENT-FUNCTION. If the EC-ARGUMENT-FUNCTION exception condition is set to exist and checking for EC-ARGUMENT-FUNCTION is not enabled, the implementor defines the result of the function reference." EC-ARGUMENT-FUNCTION is FATAL (EC table, line 24636). Implementor-defined item 90 (line 39483) covers the checking-disabled result. Concretely: argument-1 negative (and arguably zero — see gotcha), or argument-2 <= 0, sets EC-ARGUMENT-FUNCTION. Additionally §15.4 (line 34002): "If the length of the returned value exceeds the maximum length specified by the implementor for a returned value, an EC-ARGUMENT-FUNCTION exception condition is set to exist" — the hook for an absurdly large argument-2.

## §8 boolean literals and category (for goldens)

§8.3.3.4: Format 1 `B"[boolean-character-1]..."` or B'...' where each character shall be '0' or '1' (SR2); Format 2 hex `BX"..."`/BX'...' where each hex digit expands per GR5 ('5' is B"0101", 'F' is B"1111", etc.; GR6: runtime value = equivalent B" literal). GR2: "Boolean literals are of the class and category boolean." Max 8191 boolean positions (SR1); BX literals can be zero-length (NOTE after SR3). Boolean data items: PIC 1(n) [USAGE BIT] etc. per §8.5.2.5 (which lists "A boolean function" as category boolean). MOVE into a bit item (§14.6.8.6): "transferred... into the corresponding boolean positions of the receiving data item, with zero fill or truncation to the right" — i.e. LEFT-aligned, opposite end from the function's left-side fill/truncate. DISPLAY of a boolean item is legal (§14.9.11.3 SR1 only excludes message-tag/object/pointer) but GR1 (line 26893) makes the device conversion implementor-defined — we define it (natural rendering: the '0'/'1' characters); a fully spec-pinned alternative golden shape is a boolean relation compare against a B"..." literal (§8.8.4.3 simple boolean condition).

## Annex D.10 worked example (lines 44633-44658, normative-intent illustration)

`01 bit-item PIC 1(8) USAGE BIT.  01 integer-item PIC 9(5) VALUE 544.  01 integer-item-2 PIC 9(3).`
`MOVE FUNCTION BOOLEAN-OF-INTEGER ( integer-item , 6) TO bit-item.` — comment: "the function returns the low order 6 bits of the binary representation of the integer value, and MOVE stores it in bit-item, padding on the right with 0's" (544 = 0b1000100000 -> low 6 bits B"100000"; bit-item becomes B"10000000").
`COMPUTE integer-item-2 = FUNCTION INTEGER-OF-BOOLEAN (bit-item (1:6)).` — comment: "the function returns the numeric value of the leading 6 bits, 32, and COMPUTE puts 032 in integer-item-2."

### Hand-derived expected values (golden material)

| Input | Expected | Rule |
|---|---|---|
| FUNCTION BOOLEAN-OF-INTEGER(5, 8) | B"00000101" (8 boolean positions, usage bit) | §15.13.4 rule 1: bit configuration of the binary representation of argument-1, rightmost position = low-order digit, zero-filled on the left to argument-2 positions |
| FUNCTION BOOLEAN-OF-INTEGER(544, 6) | B"100000" (the low-order 6 bits of 0b1000100000; value 32) | §15.13.4 rule 1 'truncated on the left, if necessary'; confirmed verbatim by the Annex D.10 example comment 'returns the low order 6 bits' |
| MOVE FUNCTION BOOLEAN-OF-INTEGER(544, 6) TO bit-item (PIC 1(8) USAGE BIT) | bit-item = B"10000000" (left-aligned, right zero-fill) | §14.6.8.6: receiving category-boolean items get 'zero fill or truncation to the right'; Annex D.10 comment 'padding on the right with 0's' |
| FUNCTION BOOLEAN-OF-INTEGER(255, 8) | B"11111111" | §15.13.4 rule 1 (exact fit, no fill/truncation) |
| FUNCTION BOOLEAN-OF-INTEGER(256, 8) | B"00000000" (256 mod 2^8 = 0) | §15.13.4 rule 1 left truncation |
| FUNCTION BOOLEAN-OF-INTEGER(1, 1) | B"1" | §15.13.4 rule 1; §15.13.3 rule 2 minimum legal argument-2 = 1 |
| FUNCTION INTEGER-OF-BOOLEAN(B"00000101") | 5 | §15.45.4 rule 1a-c: unsigned binary value of the same bit configuration, same number of boolean positions as argument-1 |
| FUNCTION INTEGER-OF-BOOLEAN(bit-item(1:6)) where bit-item = B"10000000" | 32 (leading 6 bits B"100000") | §15.45.4 rule 1 + §15.3 item 3 (ref-modified boolean argument); Annex D.10: 'the numeric value of the leading 6 bits, 32' |
| FUNCTION INTEGER-OF-BOOLEAN(BX"5") | 5 (BX"5" == B"0101") | §8.3.3.4.4 GR5 ('5' is B"0101") + GR6; §15.45.4 rule 1 |
| FUNCTION INTEGER-OF-BOOLEAN(B"1111") | 15 | §15.45.4 rule 1 |
| FUNCTION INTEGER-OF-BOOLEAN(FUNCTION BOOLEAN-OF-INTEGER(200, 8)) | 200 (round-trip identity for 0 <= n < 2^k) | §15.13.4 rule 1 composed with §15.45.4 rule 1 (both unsigned, MSB-first) |
| FUNCTION BOOLEAN-OF-INTEGER(5, 0) or (5, -1) | EC-ARGUMENT-FUNCTION (fatal) if checking enabled; implementor-defined result otherwise | §15.13.3 rule 2 ('positive nonzero integer') violated -> §15.3 line-33761 paragraph; EC table line 24636 (Fatal) |
| FUNCTION BOOLEAN-OF-INTEGER(-1, 8) | EC-ARGUMENT-FUNCTION (fatal) if checking enabled; implementor-defined result otherwise | §15.13.3 rule 1 ('positive integer') violated -> §15.3 line-33761 paragraph |

### ⚠ Gotchas (trust these over the phase doc)

- AMBIGUITY ON ZERO for BOOLEAN-OF-INTEGER argument-1: rule 1 says 'a positive integer' while the ADJACENT rule 2 says 'a positive nonzero integer'. Under the sign-condition definition (§8.8.4.7.4 GR1a: POSITIVE true iff value > 0) zero is NOT positive, making BOOLEAN-OF-INTEGER(0, n) an argument-rule violation -> EC-ARGUMENT-FUNCTION; but the intra-section contrast with 'positive nonzero' (which would be redundant otherwise) plus the informal 'unsigned/binary value' framing strongly suggests the drafters meant arg-1 to admit 0 and only arg-2 to exclude it. Comparable rules elsewhere use explicit 'greater than or equal to zero' (§15.9.3 rule 2) when zero is intended. RECOMMENDATION: accept 0 (returns all-zero bits) — with checking enabled this is the risky leg only under the strictest reading; do NOT raise EC for 0, do raise it for negatives. The plan's word 'unsigned' is not in the spec text; the spec's constraint is 'positive'.
- The plan's phrase 'the unsigned binary representation of argument-1' is incomplete: §15.13.4 rule 1 explicitly ZERO-FILLS OR TRUNCATES ON THE LEFT to argument-2 positions — i.e. the result is argument-1 mod 2^argument-2, and overflow of argument-2 bits is NORMAL (no EC). Annex D.10 (544 -> low 6 bits) confirms.
- Argument-2 is an Integer-type argument (§15.3 item 6) — a plain integer VALUE giving the result length in boolean positions; it must be >= 1 ('positive nonzero'). There is no upper bound in §15.13; the only cap is §15.4's implementor-defined max returned-value length (exceeding it -> EC-ARGUMENT-FUNCTION). Pick and document an implementor max (the boolean-literal max of 8191 positions, §8.3.3.4.3 SR1, is a natural choice).
- Neither §15.13 nor §15.45 defines any function-specific EC rule — ALL argument-violation behavior routes through the general §15.3 paragraph (line 33761): EC-ARGUMENT-FUNCTION (Fatal) only if no exception arose during item identification/expression evaluation; with checking disabled the function result is implementor-defined (implementor item 90, line 39483).
- The §15 summary table row for INTEGER-OF-BOOLEAN (line 34226) says 'numeric value of a BINARY-DOUBLE item... right-justified' — that 64-bit framing is the informative table only; §15.45.4 itself sizes the temporary item to argument-1's length with NO 64-bit cap. A boolean argument longer than 64 bits (legal: PIC 1(100) USAGE BIT, or literals up to 8191 positions) overflows uint64 — decide a policy (EC-ARGUMENT-FUNCTION via the §15.4 max-returned-length hook, or wider arithmetic) and document it.
- Zero-length boolean argument to INTEGER-OF-BOOLEAN is not addressed by §15.45 (hex-boolean literals CAN be zero-length, §8.3.3.4.3 NOTE after SR3). No explicit rule exists; value 0 is the natural reading of 'unsigned binary value' of an empty configuration, but flag it as an undefined edge.
- DISPLAY of a boolean/bit item: legal (§14.9.11.3 SR1 excludes only message-tag/object/pointer) but the conversion to the device is IMPLEMENTOR-DEFINED (§14.9.11.4 GR1) — the spec does NOT mandate '0'/'1' character rendering; our compiler defines it. A fully spec-pinned golden alternative: IF FUNCTION BOOLEAN-OF-INTEGER(5, 8) = B"00000101" (boolean relation, §8.8.4.3).
- MOVE into a bit item fills/truncates on the RIGHT (§14.6.8.6) — the OPPOSITE end from BOOLEAN-OF-INTEGER's left fill/truncate. A golden that MOVEs a shorter function result into a longer PIC 1(n) item (the Annex 544/6 -> PIC 1(8) case, giving B"10000000") catches conflation of the two.
- General format has NO comma between argument-1 and argument-2 (`( argument-1 argument-2 )`); the comma is just an optional separator (the Annex example uses one) — the argument-list grammar must accept both.
- Function result types: BOOLEAN-OF-INTEGER is a boolean function -> class/category boolean, IMPLICIT USAGE BIT (§15.2 item 2; §8.5.2.5 item 4 lists 'a boolean function' as category boolean); INTEGER-OF-BOOLEAN is an integer function -> class/category numeric with an operational sign (§15.2 item 5), though its value is always >= 0 by §15.45.4 ('unsigned binary value').
- No contradiction found with the P11 phase doc's core assumptions (MSB-first, arg-2 = bit-position count, unsigned interpretation) — the only corrections are the mod-2^k truncation semantics and the arg-1-zero ambiguity above.

## `spec:byte-length` — BYTE-LENGTH §15.14 (vs LENGTH)

### Sections found

| § | Title | spec line |
|---|---|---|
| §15.14 | BYTE-LENGTH function | 34634 |
| §15.14.1 | General (type = integer) | 34637 |
| §15.14.2 | General format: FUNCTION BYTE-LENGTH ( argument-1 [ PHYSICAL ] ) | 34644 |
| §15.14.3 | Argument rule (exactly ONE rule) | 34649 |
| §15.14.4 | Returned value rules (rules 1–7) | 34654 |
| §15.50 | LENGTH function (NOT §15.49 — that is INTEGER-PART) | 36454 |
| §15.3 | Arguments (general; argument-type taxonomy, variable-length-group restriction, EC-ARGUMENT-FUNCTION) | 33707 |
| §15.4/§15.4.1 | Returned values (temporary elementary data item; native-arithmetic representation implementor-defined) | 34000 |
| §15.6 Table 21 | BYTE-LENGTH row: 'Alph1 or Anum1 or Bool1 or Ind1 or Nat1 or Num1 or Obj 1 or Ptr1 or Type1, Key2' | 34171 |
| §13.18.60 | USAGE clause (GR4 BINARY, GR6 COMPUTATIONAL, GR7 DISPLAY, GR8 NATIONAL, GR11 PACKED-DECIMAL, GR12 BINARY-CHAR/SHORT/LONG/DOUBLE, GR14–18 FLOAT-*) | 22636 |
| §8.1.2 | Computer's coded character set (byte-uniformity rules 1–3; bits-per-byte implementor-specified) | 5057 |
| §3.27 | Definition of 'byte' (smallest addressable character unit) | 1548 |
| §13.10 | Constant entry — CONSTANT AS BYTE-LENGTH OF data-name-1 (GR5: ODO group uses MAXIMUM size) | 16790 |
| §8.4.3.2 | Function-identifier (GR1 temp elementary data item; GR2 argument may itself be a function-identifier) | 6881 |
| §8.4.3.3 | Reference-modification (defines a unique data item) | 7023 |
| §13.18.38 | OCCURS clause GR8 (sending = current DEPENDING value; receiving = maximum length) | 19820 |

### Semantics

=== §15.14 BYTE-LENGTH (spec file E:/COBOL/specs/ISO_COBOL.md, line 34634) ===

GENERAL (§15.14.1): "The BYTE-LENGTH function returns an integer equal to the length of the argument in bytes. The type of the function is integer."

FORMAT (§15.14.2): FUNCTION BYTE-LENGTH ( argument-1 [ PHYSICAL ] )  — PHYSICAL is the 'Key2' keyword in Table 21.

ARGUMENT RULE (§15.14.3, single rule): "1) Argument-1 shall be an alphanumeric or national literal, a based entry, a type-name, or a data item of any class or category."
- GROUP: YES — a group item is "a data item of any class or category"; returned-value rules 2, 6, 7 explicitly address occurs-depending and variable-length groups. Variable-length groups are permitted precisely because rules 6/7 name them (per §15.3: "A variable-length group shall be referenced as an argument to a function only when explicitly permitted in the function definition").
- LITERAL: ONLY alphanumeric or national literals. Numeric literals and boolean literals are NOT permitted (contrast LENGTH, which adds boolean literals).
- FUNCTION REFERENCE: YES, indirectly — §8.4.3.2.4 GR1: "A function-identifier references a temporary data item ... the temporary data item is an elementary data item whose description and category are specified by the definition of that intrinsic function"; GR2: "An argument being evaluated may itself be a function-identifier". The temporary item is a data item, satisfying §15.14.3 rule 1. CAVEAT: for numeric/integer functions under native arithmetic, §15.4.1 says "the characteristics and representation of the returned value are defined by the implementor" — so BYTE-LENGTH of a numeric function reference is implementor-defined.
- REFERENCE-MODIFIED: YES — §8.4.3.3.1 "Reference modification defines a unique data item"; §8.4.3.3.3 SR5 "reference modification is allowed anywhere an identifier referencing a data item of class alphanumeric, boolean, or national is permitted". Result = byte length of the ref-mod subset.
- Per Table 21 (line 34171) the argument may be of class Alphabetic/Alphanumeric/Boolean/Index/National/Numeric/Object/Pointer, or a Type-name — i.e., index items, object references, and pointers are legal arguments (their sizes are implementor-defined).
- BASED ENTRY and TYPE-NAME are explicitly legal.

RETURNED VALUE RULES (§15.14.4, rules 1–7, quoted/tight):
1) "The returned value is an integer that is the length of argument-1 in number of bytes."
2) ODO: "If any data description entry subordinate to the data description entry of argument-1 is described with the DEPENDING phrase of the OCCURS clause, then a) if argument-1 is a based entry not associated with actual data or is a type declaration, the length of argument-1 is determined in accordance with the rules of the OCCURS clause for a receiving data item; otherwise b) ... for a sending data item." Per OCCURS §13.18.38 GR8b: sending = "only that part of the table area that is specified by the value of the data item referenced by data-name-1 at the start of the operation"; receiving = "the maximum length of the group will be used". So a normal ODO group argument uses the CURRENT DEPENDING value; a based-entry-without-data or type-name uses the MAXIMUM.
3) "The returned value shall include the number of implicit filler positions, if any, in argument-1." (i.e., SYNCHRONIZED/alignment padding counts.)
4) "When argument-1 does not occupy an integral number of bytes, the returned value is rounded to the next larger integer value." (bit items, packed nibbles.)
5) "If argument-1 is a dynamic-length elementary item, the current length of argument-1 in bytes is returned. NOTE Any prefixed fields or delimiter characters are not included in the current length."
6) Variable-length group WITHOUT PHYSICAL: sum of (a) lengths of all subordinate non-variable-length data items, (b) current lengths of all subordinate dynamic-length elementary items, (c) lengths of subordinate dynamic-capacity tables at current capacity (matching fixed-capacity table per 8.5.1.12).
7) Variable-length group WITH PHYSICAL: "the returned value is the length of argument-1 in number of bytes. If argument-1 is not physically located where it is defined, the returned value includes only the length of the implementor-defined pointer. If argument-1 is physically located where it is defined, BYTE-LENGTH returns the same value same value [sic — spec typo] that would be returned had the PHYSICAL argument not been specified."

=== HOW MANY BYTES PER USAGE — ⚠ ALMOST ENTIRELY IMPLEMENTOR-DEFINED ⚠ ===
The spec defines the COUNTING rules but NOT the byte widths; widths come from §13.18.60 USAGE GRs + §8.1.2:
- §3.27: byte = "sequence of bits representing the smallest addressable character unit in the memory of a given computer"; §8.1.2: "The implementor shall specify the number of bits in a byte for each supported computer."
- DISPLAY (§13.18.60 GR7): "Each implementor shall specify the size and representation of characters stored for usage DISPLAY." §8.1.2 rule 1: byte count per alphanumeric character is uniform across the charset and fixed at compile time.
- NATIONAL (GR8): "National characters shall be represented ... as characters of a uniform size equal to or a multiple of the size of characters in the computer's alphanumeric character set. Each implementor shall specify the size and representation of characters stored for usage NATIONAL." §8.1.2 rule 3: national char bytes >= alphanumeric char bytes.
- BINARY (GR4): radix 2; "Each implementor specifies the precise effect ... upon the alignment and representation ... Sufficient computer storage shall be allocated by the implementor to contain the maximum range of values implied by the associated decimal picture character-string." NO byte ladder in ISO — the 2/4/8-byte convention is vendor practice, not spec.
- COMPUTATIONAL/COMP (GR6): "a radix and format specified by the implementor" — fully implementor-defined (need not even be binary).
- PACKED-DECIMAL (GR11): radix 10; "each digit position shall occupy the minimum possible configuration in computer storage"; representation incl. sign implementor-specified; WITH NO SIGN reserves no sign storage. No explicit nibble/byte formula — ceil((digits+1)/2) is convention only.
- BINARY-CHAR/-SHORT/-LONG/-DOUBLE (GR12): defined by MINIMUM RANGES (2**7/2**15/2**31/2**63 signed; 2**8/16/32/64 unsigned); "The implementor may allow a wider range" — byte widths implied (1/2/4/8) but not literally mandated.
- FLOAT-BINARY-32/64/128 (GR14–16): fixed to ISO/IEC 60559:2020 binary32/binary64/binary128 interchange formats → 4/8/16 bytes — these ARE spec-pinned. FLOAT-DECIMAL-16/34 (GR17–18): decimal64/decimal128 → 8/16 bytes, spec-pinned.
- INDEX (GR10), MESSAGE-TAG (GR9), OBJECT REFERENCE, POINTER family: representation/size implementor-specified.

=== CONTRAST WITH FUNCTION LENGTH (§15.50, line 36454 — NOT §15.49) ===
- LENGTH counts POSITIONS, class-dependent (§15.50.1): "in alphanumeric character positions, national character positions, or boolean positions, depending on the class of the argument". Returned-value rules: r1 boolean/bit → boolean positions; r2 national usage → national character positions; r3 everything else → alphanumeric character positions. BYTE-LENGTH always counts BYTES.
- Argument rule difference (§15.50.3 r1): "an alphanumeric, national, or boolean literal; a data item of any class or category; a based entry; or a type-name" — LENGTH additionally permits a BOOLEAN literal; BYTE-LENGTH does not.
- Same machinery in both: [PHYSICAL] keyword; ODO sending/receiving split (LENGTH r4 = BYTE-LENGTH r2); implicit FILLER included (LENGTH r5 = B-L r3); dynamic-length elementary → "the current length of argument-1 in bytes is returned" (LENGTH r6 uses the SAME 'in bytes' wording — a quirk: LENGTH of a dynamic-length item is defined in bytes, not positions); variable-length-group sum / PHYSICAL legs (LENGTH r7/r8 = B-L r6/r7, with r8 phrased "in number of character positions"); rounding (LENGTH r9 rounds non-integral alphanumeric positions; B-L r4 rounds non-integral bytes).
- Practical divergence: PIC N(3) → LENGTH=3 always; BYTE-LENGTH=3×national-char-size. PIC X(5) → LENGTH=5; BYTE-LENGTH=5×alphanumeric-char-size (equal when char=1 byte). USAGE BIT PIC 1(12) → LENGTH=12 boolean positions; BYTE-LENGTH=2 (12 bits rounded up).

=== RELATED: CONSTANT entry (§13.10) ===
'01 name CONSTANT AS BYTE-LENGTH OF data-name-1.' — §13.10.4 GR5: value "determined as specified in the BYTE-LENGTH intrinsic function with the exception that when data-name-1 is an occurs-depending group item, the maximum size of the data item is used" (compile-time → MAX, unlike the runtime intrinsic's sending-rule). SR10: data-name shall not be ANY LENGTH; SR12: not dynamic-length or variable-length group; SR3: all subscripts shall be literals.

=== EXCEPTIONS ===
§15.3: bad argument value → EC-ARGUMENT-FUNCTION ("If the evaluation of an argument results in an incorrect value ... the EC-ARGUMENT-FUNCTION exception condition is set to exist"; if checking not enabled, "the implementor defines the result of the function reference"). §15.4: returned value longer than the implementor max → EC-ARGUMENT-FUNCTION.

### Hand-derived expected values (golden material)

| Input | Expected | Rule |
|---|---|---|
| BYTE-LENGTH of 01 A PIC X(5) USAGE DISPLAY | 5 * (bytes per alphanumeric character). With the conventional 1-byte alphanumeric coded character set => 5. The per-character byte count is IMPLEMENTOR-specified. | §15.14.4 rule 1 + §13.18.60 GR7 ('Each implementor shall specify the size ... for usage DISPLAY') + §8.1.2 rule 1 (uniform, fixed at compile time) |
| BYTE-LENGTH of 01 B PIC N(3) (usage national) | 3 * (bytes per national character) — IMPLEMENTOR-specified, must be uniform and a multiple of the alphanumeric char size. UTF-16 encoding => 6; UCS-4 => 12. Contrast FUNCTION LENGTH(B) = 3 always. | §15.14.4 rule 1 + §13.18.60 GR8 + §8.1.2 rules 2–3; contrast §15.50.4 rule 2 |
| BYTE-LENGTH of 01 C PIC S9(4) USAGE COMP (or BINARY) | IMPLEMENTOR-DEFINED. The spec mandates only 'sufficient computer storage ... to contain the maximum range of values implied by the associated decimal picture character-string' (±9999). Conventional vendor answer: 2 bytes — but that is convention, not ISO. The compiler must pin and document its own width. | §13.18.60 GR4 (BINARY) / GR6 (COMPUTATIONAL: 'a radix and format specified by the implementor') + §15.14.4 rule 1 |
| BYTE-LENGTH of 01 D PIC S9(5) USAGE PACKED-DECIMAL (task said 'COMP-3' — not an ISO usage) | IMPLEMENTOR-DEFINED. GR11 requires radix 10 and 'each digit position shall occupy the minimum possible configuration'; sign representation implementor-specified. Conventional packed layout ceil((5+1)/2) = 3 bytes. If the layout yields non-integral bytes, §15.14.4 rule 4 rounds UP. | §13.18.60 GR11 + §15.14.4 rules 1 and 4 |
| BYTE-LENGTH("ABCDE") (alphanumeric literal) | 5 (5 characters * alphanumeric char size; 1-byte charset => 5). Legal per the argument rule; numeric and boolean literals are ILLEGAL arguments. | §15.14.3 rule 1 ('an alphanumeric or national literal') + §15.14.4 rule 1 |
| BYTE-LENGTH of a byte-aligned PIC 1(12) USAGE BIT item | 2 (12 bits = 1.5 bytes, rounded to the next larger integer). Contrast FUNCTION LENGTH = 12 boolean positions. | §15.14.4 rule 4 ('rounded to the next larger integer value'); contrast §15.50.4 rule 1 |
| BYTE-LENGTH(G) where 01 G contains 05 CNT PIC 9(3) DISPLAY and 05 E PIC X(10) OCCURS 1 TO 5 DEPENDING ON CNT, with CNT = 2 at evaluation | 23 (3 + 2*10, 1-byte chars) — a plain data-item argument uses the OCCURS SENDING rules = current DEPENDING value. If G were a based entry not associated with data, or a type-name: 53 (3 + 5*10, receiving rules = maximum). | §15.14.4 rule 2a/2b + §13.18.38 GR8b (sending = current value; receiving = maximum length) |
| 01 K CONSTANT AS BYTE-LENGTH OF G. (same G as above) | 53 — the CONSTANT-entry variant uses the MAXIMUM size for an occurs-depending group, diverging from the runtime intrinsic. | §13.10.4 GR5 ('...with the exception that when data-name-1 is an occurs-depending group item, the maximum size of the data item is used') |
| BYTE-LENGTH(X(2:3)) where X is PIC X(10) | 3 (the reference-modified subset is a unique data item of 3 alphanumeric positions; 1-byte chars => 3). | §8.4.3.3.1 + §8.4.3.3.3 SR5 (ref-mod creates a data item, allowed wherever such an identifier is permitted) + §15.14.4 rule 1 |
| BYTE-LENGTH of a USAGE FLOAT-BINARY-32 item | 4 — one of the FEW spec-pinned widths (binary32 interchange format per ISO/IEC 60559:2020). FLOAT-BINARY-64 => 8, FLOAT-BINARY-128 => 16, FLOAT-DECIMAL-16 => 8, FLOAT-DECIMAL-34 => 16. | §13.18.60 GR14–GR18 + §15.14.4 rule 1 |
| BYTE-LENGTH(FUNCTION UPPER-CASE(X)) — function reference as argument | LEGAL: the argument is the temporary elementary data item produced by the inner function (same size as X for UPPER-CASE). For NUMERIC inner functions under native arithmetic the temp item's representation — hence its byte length — is IMPLEMENTOR-DEFINED. | §8.4.3.2.4 GR1–GR2 (temp data item; 'An argument being evaluated may itself be a function-identifier') + §15.4.1 (native arithmetic: representation defined by the implementor) |
| BYTE-LENGTH of a dynamic-length elementary item currently holding 7 bytes of data | 7 — 'the current length of argument-1 in bytes'; prefix/delimiter storage excluded per the NOTE. | §15.14.4 rule 5 |

### ⚠ Gotchas (trust these over the phase doc)

- LENGTH is §15.50 (line 36454), NOT §15.49 as the task guessed — §15.49 is INTEGER-PART. Cite §15.50 everywhere.
- LOUD: the spec does NOT define byte counts for DISPLAY, NATIONAL, BINARY, COMP, or PACKED-DECIMAL — every one is implementor-specified (§13.18.60 GR7, GR8, GR4, GR6, GR11; §8.1.2 even makes bits-per-byte implementor-specified). Only FLOAT-BINARY-32/64/128 and FLOAT-DECIMAL-16/34 have spec-pinned sizes (via ISO/IEC 60559 interchange formats, GR14–18). The compiler must pin and DOCUMENT its widths (e.g., 1-byte alphanumeric chars, national=UTF-16 2 bytes vs UCS-4 4 bytes, COMP ladder, packed nibbles) — these become implementor-defined items, not spec derivations.
- COMP-3 is NOT an ISO/IEC 1989:2023 usage — the §13.18.60.2 general format lists only COMPUTATIONAL/COMP (no COMP-1/2/3); the ISO name is PACKED-DECIMAL [WITH NO SIGN]. Supporting the spelling COMP-3 is a dialect extension decision.
- BINARY-CHAR/SHORT/LONG/DOUBLE (GR12) are specified by MINIMUM RANGES (2**7…2**64), and 'The implementor may allow a wider range' — 1/2/4/8 bytes is implied, not literally mandated.
- Argument asymmetry vs LENGTH: BYTE-LENGTH permits only alphanumeric/national literals (§15.14.3 r1); LENGTH (§15.50.3 r1) additionally permits BOOLEAN literals. Neither permits numeric literals.
- Table 21 (line 34171) lists 'Num1' and §15.3(10) defines Numeric arguments as 'an arithmetic expression or a numeric data item' — but §15.14.3 rule 1's specific text permits only data items (+ the named literals/based-entry/type-name). An arithmetic expression has no defined storage representation; the specific §15.14.3 rule should govern the binder (reject bare arithmetic expressions; accept numeric DATA ITEMS of any usage). Flag as a deliberate binder decision.
- Groups: YES (any class/category, incl. index/object/pointer/boolean items per Table 21). Variable-length groups are explicitly permitted (rules 6/7 — required by §15.3's blanket restriction). BUT §15.3 also says a TYPE-NAME argument shall not describe a variable-length group.
- ODO split is subtle: a normal ODO-group data-item argument uses SENDING rules = CURRENT DEPENDING value (§15.14.4 r2b + §13.18.38 GR8b); only based-entries-not-associated-with-data and type-names use receiving rules = MAXIMUM (r2a). The CONSTANT AS BYTE-LENGTH OF variant (§13.10.4 GR5) flips ODO groups to MAXIMUM — compile-time constant ≠ runtime intrinsic on the same operand.
- Rule 3: implicit FILLER positions (alignment/SYNCHRONIZED padding) ARE included in the returned value — a typed-native compiler that has no physical padding must decide what its 'implicit filler' story is for BYTE-LENGTH of groups.
- Rule 7 (PHYSICAL on a variable-length group 'not physically located where it is defined') returns only the length of the implementor-defined POINTER — and contains a spec typo: 'returns the same value same value'.
- LENGTH quirk worth mirroring: §15.50.4 r6 measures dynamic-length elementary items 'in bytes' — identical wording to BYTE-LENGTH r5 — even though LENGTH otherwise counts positions; for a national dynamic-length item the literal spec text makes LENGTH return BYTES, not characters.
- Function references are legal arguments (§8.4.3.2.4 GR1/GR2), but BYTE-LENGTH of a NUMERIC function's temp item under native arithmetic is implementor-defined (§15.4.1) — avoid goldens that depend on it, or pin the compiler's temp-item representation explicitly.
- ANY LENGTH items: §15.14 says nothing about them; only the CONSTANT entry explicitly forbids ANY LENGTH data-names (§13.10.3 SR10). Runtime BYTE-LENGTH of an ANY LENGTH formal is spec-silent — treat as a design decision (current length is the natural reading via the data-item leg).
- PHYSICAL keyword parsing: it is a plain keyword INSIDE the argument parens after argument-1 (Key2), not a separate argument — grammar must accept FUNCTION BYTE-LENGTH ( arg PHYSICAL ).
- EC funnel: bad argument evaluation → EC-ARGUMENT-FUNCTION (§15.3); over-max returned length → EC-ARGUMENT-FUNCTION (§15.4); with checking disabled the function result is implementor-defined.
- §8.4.3.2.3 SR12: 'An integer function other than the integer form of the ABS function shall not be specified where an unsigned integer is required' — BYTE-LENGTH is an integer function, so it is NOT usable where an unsigned integer is required (e.g., PICTURE repetition); the CONSTANT AS BYTE-LENGTH OF form exists for the compile-time case.

## `spec:date-window` — DATE-TO-YYYYMMDD §15.23 / DAY-TO-YYYYDDD §15.25 / YEAR-TO-YYYY §15.100 / SECONDS-PAST-MIDNIGHT §15.80

### Sections found

| § | Title | spec line |
|---|---|---|
| 15.23 | DATE-TO-YYYYMMDD function (15.23.1 General / 15.23.2 General format / 15.23.3 Argument rules / 15.23.4 Returned value rule) | 35152 |
| 15.25 | DAY-TO-YYYYDDD function (15.25.1 General / 15.25.2 General format / 15.25.3 Argument rules / 15.25.4 Returned value rule) | 35265 |
| 15.80 | SECONDS-PAST-MIDNIGHT function (15.80.1 General / 15.80.2 General format / 15.80.3 Returned value rules — NO argument-rules subsection, the function takes no arguments) | 37996 |
| 15.100 | YEAR-TO-YYYY function (15.100.1 General / 15.100.2 General format / 15.100.3 'Arguments rule' [sic, spec's own heading] / 15.100.4 Returned value rules) | 39063 |
| 15.5.5 | Standard numeric time form (range definition used by 15.80) | 34087 |
| 7.3.17 | LEAP-SECOND directive (GR1 default OFF; GR4/GR5 bound the standard numeric time form range) | 4612 |
| 15.3 | Arguments — EC-ARGUMENT-FUNCTION on argument-rule violation (paragraph at line 33761; EC table row 'Fatal' at line 24636) | 33761 |
| D.31.5.4 | Annex D SECONDS-PAST-MIDNIGHT example (informative): 5:14:27.812479168304 -> 18867.812479168304 | 48885 |

### Semantics

ALL THREE WINDOWING FUNCTIONS share one argument shape and one formula, with DATE-TO-YYYYMMDD and DAY-TO-YYYYDDD defined BY REFERENCE to YEAR-TO-YYYY.

=== §15.100 YEAR-TO-YYYY ===
General format (15.100.2): `FUNCTION YEAR-TO-YYYY ( argument-1 [ argument-2 [ argument-3 ] ] )` — nested optionality: argument-3 requires argument-2.
Arguments rule (15.100.3):
 1) "Argument-1 shall be a nonnegative integer that is less than 100." (0 ALLOWED here.)
 2) "Argument-2 shall be an integer." (Negative legal — spec NOTE uses (–15).)
 3) "If argument-2 is omitted, the function shall be evaluated as though 50 were specified for argument-2."
 4) "Argument-3 shall be an integer greater than 1600 and less than 10000." (1601..9999.)
 5) "If argument-3 is omitted, the function shall be evaluated as though the following were specified for argument-3: FUNCTION NUMVAL (FUNCTION CURRENT-DATE (1:4)))" [sic — unbalanced paren is a transcription artifact; = the current year at execution].
 6) "The sum of the values of argument-2 and argument-3 shall be less than 10000 and greater than 1699." (So maximum-year ∈ [1700, 9999].)
Returned value rules (15.100.4) — THE EXACT WINDOWING FORMULA:
 1) "Maximum-year is calculated as follows: (argument-2 + argument-3)"
 2a) "When the following condition is true: FUNCTION MOD (maximum-year, 100) >= argument-1 — The equivalent arithmetic expression is (argument-1 + 100 * (FUNCTION INTEGER (maximum-year/100)))"
 2b) "Otherwise, the equivalent arithmetic expression is (argument-1 + 100 * (FUNCTION INTEGER (maximum-year/100) – 1))"
FUNCTION INTEGER is floor; maximum-year >= 1700 so plain truncating division is equivalent. Net effect: result is the unique year in the 100-year window [maximum-year-99, maximum-year] whose low-order two digits equal argument-1. argument-2 is a SIGNED OFFSET from the execution year (argument-3) to the window's ENDING year — NOT a window size; the window is always exactly 100 years. NOTE 2 (15.100.4): omitting argument-3 gives a SLIDING window; a FIXED window is achieved by "specifying suitable values for argument-2 and argument-3, such that the sum of argument-2 and argument-3 defines the ending year of the desired 100-year interval."
Type: integer (15.100.1).

=== §15.23 DATE-TO-YYYYMMDD ===
Format (15.23.2): `FUNCTION DATE-TO-YYYYMMDD ( argument-1 [ argument-2 [ argument-3 ] ] )`.
Argument rules (15.23.3): 1) "Argument-1 shall be a positive integer less than 1000000." (POSITIVE — 0 excluded; NOTE: "This function does not check argument-1 to ensure that it is a valid date. The returned value can be an argument to the TEST-DATE-YYYYMMDD function to check its validity."). 2) arg-2 integer. 3) omitted arg-2 -> 50. 4) arg-3 integer >1600 and <10000. 5) omitted arg-3 -> (FUNCTION NUMVAL (FUNCTION CURRENT-DATE (1:4))). 6) "The sum of the year at the time of execution and the value of argument-2 shall be less than 10000 and greater than 1699." [wording differs from 15.25/15.100 — see gotchas].
Returned value rule (15.23.4 rule 1): "(FUNCTION YEAR-TO-YYYY (YY, argument-2, argument-3) * 10000 + mmdd) where YY = FUNCTION INTEGER (argument-1/10000), mmdd = FUNCTION MOD (argument-1, 10000), and where argument-1, argument-2 and argument-3 are the same as argument-1, argument-2, and argument-3 of the DATE-TO-YYYYMMDD function reference itself." Type: integer.

=== §15.25 DAY-TO-YYYYDDD ===
Format (15.25.2): `FUNCTION DAY-TO-YYYYDDD ( argument-1 [ argument-2 [ argument-3 ] ] )`.
Argument rules (15.25.3): 1) "Argument-1 shall be a positive integer less than 100000." (NOTE points to TEST-DAY-YYYYDDD for validity.) 2) arg-2 integer. 3) omitted -> 50. 4) arg-3 1601..9999. 5) omitted -> (FUNCTION NUMVAL (FUNCTION CURRENT-DATE (1:4))). 6) "The sum of the values of argument-2 and argument-3 shall be less than 10000 and greater than 1699."
Returned value rule (15.25.4 rule 1): "(FUNCTION YEAR-TO-YYYY (YY, argument-2, argument-3) * 1000 + nnn) where YY = FUNCTION INTEGER (argument-1/1000), nnn = FUNCTION MOD (argument-1, 1000)". Type: integer.

=== §15.80 SECONDS-PAST-MIDNIGHT ===
Format (15.80.2): `FUNCTION SECONDS-PAST-MIDNIGHT` — NO arguments, NO parentheses. Type (15.80.1): "The type of this function is numeric." — NUMERIC, NOT INTEGER; fractional seconds are intended.
Returned value rules (15.80.3): 1) "The returned value is in standard numeric time form." 2) "The returned value is the current local time of day provided by the system on which the function is evaluated, expressed in seconds past midnight." 3) "The implementor shall specify the precision to which this value is returned." (implementor-defined item 171, line 39697 — required, must be documented). 4) "The implementor shall specify whether the returned value may be greater than or equal to 86,400 when the LEAP-SECOND directive with the ON phrase is in effect." (item 112, line 39541).
Range via §15.5.5: "If the LEAP-SECOND directive with the OFF phrase is in effect, the value shall be greater than or equal to zero and less than 86,400. If ... ON ... greater than or equal to zero and less than 86,401." §7.3.17 GR1: LEAP-SECOND defaults OFF -> practical range [0, 86400). Annex D.31.5.4 (informative): "if the current time in the operating environment is 5:14:27.812479168304, the returned value would be as close to 18867.812479168304 as the operating environment was capable of providing."

=== Error conditions (all four) ===
§15.3 Arguments (line 33761): "If the evaluation of an argument results in an incorrect value for that argument or for the returned value according to the rules specified in the function definition and no exception condition was raised during item identification or expression evaluation, the EC-ARGUMENT-FUNCTION exception condition is set to exist. ... If the EC-ARGUMENT-FUNCTION exception condition is set to exist and checking for EC-ARGUMENT-FUNCTION is not enabled, the implementor defines the result of the function reference." EC-ARGUMENT-FUNCTION is Fatal (table row, line 24636). Violations for the trio: arg-1 out of range (negative/zero-where-positive-required/too large), arg-3 outside 1601..9999, arg-2+arg-3 outside [1700, 9999]. SECONDS-PAST-MIDNIGHT has no argument rules to violate.

### Hand-derived expected values (golden material)

| Input | Expected | Rule |
|---|---|---|
| FUNCTION YEAR-TO-YYYY (4, 23, 1995)  [spec NOTE 1 form: 'in the year 1995 ... (4, 23)'] | 2004 | §15.100.4 RVR 1+2a: maximum-year = 23+1995 = 2018; MOD(2018,100)=18 >= 4 -> 4 + 100*INTEGER(2018/100) = 4 + 100*20 = 2004. Confirmed by §15.100.4 NOTE 1. |
| FUNCTION YEAR-TO-YYYY (98, -15, 2008) | 1898 | §15.100.4 RVR 1+2b: maximum-year = -15+2008 = 1993; MOD(1993,100)=93 < 98 -> 98 + 100*(INTEGER(1993/100)-1) = 98 + 100*(19-1) = 1898. Confirmed by §15.100.4 NOTE 1. |
| FUNCTION YEAR-TO-YYYY (49, 50, 1999)  [window-boundary pair, upper leg] | 2049 | §15.100.4 RVR 2a: maximum-year = 50+1999 = 2049; MOD(2049,100)=49 >= 49 -> 49 + 100*INTEGER(2049/100) = 49 + 100*20 = 2049 (the window's ENDING year). |
| FUNCTION YEAR-TO-YYYY (50, 50, 1999)  [window-boundary pair, wraps to window start] | 1950 | §15.100.4 RVR 2b: maximum-year = 2049; MOD(2049,100)=49 < 50 -> 50 + 100*(INTEGER(2049/100)-1) = 50 + 100*19 = 1950. Window is [1950, 2049] — the 49-vs-50 flip exists ONLY because MOD(maximum-year,100)=49 here. |
| FUNCTION YEAR-TO-YYYY (0, 50, 2026)  [arg-1 = 0 is legal: 'nonnegative'] | 2000 | §15.100.3 AR1 permits 0; §15.100.4 RVR 2a: maximum-year = 2076; MOD(2076,100)=76 >= 0 -> 0 + 100*20 = 2000. |
| FUNCTION YEAR-TO-YYYY (77, 50, 2026)  [default-args shape pinned to exec year 2026] | 1977 | §15.100.4 RVR 2b: maximum-year = 2076; MOD=76 < 77 -> 77 + 100*(20-1) = 1977. (And (76, 50, 2026) -> 2076 via 2a: the sliding window for exec-year 2026 with default arg-2=50 is [1977, 2076].) |
| FUNCTION DATE-TO-YYYYMMDD (851003, 10, 2002) | 19851003 | §15.23.4 RVR1: YY = INTEGER(851003/10000) = 85, mmdd = MOD(851003,10000) = 1003; YEAR-TO-YYYY(85,10,2002): maximum-year = 2012, MOD(2012,100)=12 < 85 -> 85 + 100*(20-1) = 1985; 1985*10000 + 1003 = 19851003. Confirmed by §15.23.4 NOTE 1. |
| FUNCTION DATE-TO-YYYYMMDD (981002, -10, 1994) | 18981002 | §15.23.4 RVR1: YY=98, mmdd=1002; YEAR-TO-YYYY(98,-10,1994): maximum-year = 1984, MOD=84 < 98 -> 98 + 100*(19-1) = 1898; 1898*10000 + 1002 = 18981002. Confirmed by §15.23.4 NOTE 1. |
| FUNCTION DATE-TO-YYYYMMDD (490101, 50, 1999) vs (500101, 50, 1999)  [boundary through the composite function] | 20490101 vs 19500101 | §15.23.4 RVR1 + §15.100.4: YY=49 -> MOD(2049,100)=49>=49 -> 2049 -> 2049*10000+0101 = 20490101; YY=50 -> 49<50 -> 1950 -> 19500101. |
| FUNCTION DAY-TO-YYYYDDD (10004, 20, 2002) | 2010004 | §15.25.4 RVR1: YY = INTEGER(10004/1000) = 10, nnn = MOD(10004,1000) = 4; YEAR-TO-YYYY(10,20,2002): maximum-year = 2022, MOD=22 >= 10 -> 10 + 100*20 = 2010; 2010*1000 + 4 = 2010004. Confirmed by §15.25.4 NOTE 1. |
| FUNCTION DAY-TO-YYYYDDD (95005, -10, 2013) | 1995005 | §15.25.4 RVR1: YY=95, nnn=5; YEAR-TO-YYYY(95,-10,2013): maximum-year = 2003, MOD=3 < 95 -> 95 + 100*(20-1) = 1995; 1995*1000 + 5 = 1995005. Confirmed by §15.25.4 NOTE 1. |
| FUNCTION SECONDS-PAST-MIDNIGHT at local time 05:14:27.812479168304 | 18867.812479168304 (to implementor-documented precision; 5*3600 + 14*60 + 27 = 18867) | §15.80.3 RVR 1-3 + Annex D.31.5.4 (informative): 'the returned value would be as close to 18867.812479168304 as the operating environment was capable of providing'. Range [0, 86400) under default LEAP-SECOND OFF (§15.5.5, §7.3.17 GR1/GR5). |

### ⚠ Gotchas (trust these over the phase doc)

- PHASE-DOC ASSUMPTION WRONG (the exact thing the task asked to verify): argument-2 is NOT a 'window size' and NOT a yy-cutoff. The window is ALWAYS exactly 100 years. Argument-2 is a SIGNED integer offset added to argument-3 (the year at time of execution) to produce maximum-year = the ENDING year of the window (§15.100.4 rule 1; §15.100.1). Default arg-2 = 50 (§15.100.3 rule 3) => default sliding window [exec_year-49, exec_year+50]. 'argument-3 = base year default current' is correct: default is FUNCTION NUMVAL (FUNCTION CURRENT-DATE (1:4)) (§15.100.3 rule 5).
- The '49 vs 50' boundary is NOT intrinsic to the default: the flip point is at argument-1 = MOD(maximum-year, 100). With arg-2=50, arg-3=1999 (max=2049) the flip is exactly 49->2049 vs 50->1950; with arg-3=2000 (max=2050) it moves to 50->2050 vs 51->1951. Goldens exercising the boundary MUST pin argument-3 (a golden using only defaults is time-dependent and flips every calendar year).
- Argument-1 sign constraints DIFFER across the trio: YEAR-TO-YYYY arg-1 is 'nonnegative ... less than 100' (0 legal, §15.100.3 rule 1); DATE-TO-YYYYMMDD arg-1 is 'positive ... less than 1000000' (0 ILLEGAL, §15.23.3 rule 1); DAY-TO-YYYYDDD arg-1 is 'positive ... less than 100000' (0 ILLEGAL, §15.25.3 rule 1). Neither date function validates that arg-1 is a real calendar date (explicit NOTEs pointing at TEST-DATE-YYYYMMDD / TEST-DAY-YYYYDDD).
- §15.23.3 rule 6 is worded differently from its siblings: 'The sum of the year at the time of execution and the value of argument-2 shall be less than 10000 and greater than 1699' — vs §15.25.3 rule 6 / §15.100.3 rule 6 which say 'the sum of the values of argument-2 and argument-3'. Since argument-3 is defined as 'the year at the time of execution' (§15.23.1) they coincide when arg-3 is defaulted; if a program passes an explicit arg-3 different from the real current year, a hyper-literal reading of 15.23 would check exec-year+arg-2 rather than arg-3+arg-2. Recommend implementing arg-2+arg-3 uniformly (matches 15.25/15.100 and the by-reference delegation to YEAR-TO-YYYY, whose own rule 6 governs the nested call anyway) — but the wording difference is real in the spec text.
- SECONDS-PAST-MIDNIGHT is type NUMERIC, not integer (§15.80.1) — fractional seconds are intended (Annex D.31.5.4 example has 12 fractional digits). Precision is implementor-defined AND MUST BE DOCUMENTED (§15.80.3 rule 3; implementor-defined item 171 at line 39697, 'required'). Pick and document a precision (e.g. what the .NET clock gives).
- SECONDS-PAST-MIDNIGHT takes NO arguments and its general format has NO parentheses: `FUNCTION SECONDS-PAST-MIDNIGHT` (§15.80.2). Range: [0, 86400) under LEAP-SECOND OFF which is the default (§7.3.17 GR1 implies OFF; GR5/§15.5.5); under LEAP-SECOND ON the ceiling is 86401 and whether >=86400 can actually be returned is implementor-defined (§15.80.3 rule 4; item 112 at line 39541). It is 'current LOCAL time of day' (§15.80.3 rule 2) — local, not UTC.
- Optionality is NESTED in all three formats: ( argument-1 [ argument-2 [ argument-3 ] ] ) — argument-3 cannot be supplied without argument-2. Negative argument-2 is legal and the spec NOTEs write it parenthesized: (−10), (−15) — the FUNCTION-arg grammar must accept a unary-minus/parenthesized expression there.
- Argument-rule violations => EC-ARGUMENT-FUNCTION (Fatal) per §15.3 (line 33761), unless another EC was raised during item identification/expression evaluation (that one wins); with EC checking not enabled the function result is implementor-defined (implementor item 90, line 39483). Violation set for the trio: arg-1 out of range, arg-3 outside 1601..9999, and arg-2+arg-3 outside [1700, 9999] (rule 6).
- FUNCTION INTEGER in the formulas is floor, but maximum-year is guaranteed >= 1700 (rule 6), so truncating integer division is safe; MOD's divisor 100 is positive so MOD(maximum-year,100) is in 0..99. No negative-operand edge cases can arise for conforming arguments.
- Markdown transcription artifacts in the spec file (cosmetic, do not implement literally): §15.100.3 rule 5 shows 'FUNCTION NUMVAL (FUNCTION CURRENT-DATE (1:4)))' with an unbalanced trailing paren (lost opening paren; §15.23/§15.25 show the balanced form); §15.23.4 shows the multiply as '\*'; §15.100.3's heading is 'Arguments rule' (singular/plural inverted vs siblings) — cite headings exactly as printed.
- The two composite functions are defined purely BY DELEGATION to YEAR-TO-YYYY (§15.23.4 / §15.25.4: 'argument-2 and argument-3 are the same as ... of the DATE-TO-YYYYMMDD/DAY-TO-YYYYDDD function reference itself') — implement ONE windowing core (YEAR-TO-YYYY) and have the other two split arg-1 with /10000 (or /1000) + MOD and recombine *10000 (or *1000). Matches the singular-mechanism rule.

## `spec:validators` — TEST-DATE-YYYYMMDD §15.90 / TEST-DAY-YYYYDDD §15.91 / TEST-NUMVAL §15.93 / TEST-NUMVAL-C §15.94

### Sections found

| § | Title | spec line |
|---|---|---|
| 15.67 | NUMVAL function | 37288 |
| 15.68 | NUMVAL-C function | 37363 |
| 15.90 | TEST-DATE-YYYYMMDD function | 38483 |
| 15.91 | TEST-DAY-YYYYDDD function | 38543 |
| 15.93 | TEST-NUMVAL function | 38633 |
| 15.94 | TEST-NUMVAL-C function | 38699 |
| D.31.3.8 | TEST-DATE-YYYYMMDD function (informative, confirms codes 0/1/2/3) | 48770 |
| D.31.3.9 | TEST-DAY-YYYYDDD function (informative, confirms codes 0/1/2) | 48775 |

### Semantics

Header format in E:/COBOL/specs/ISO_COBOL.md: sections are `## 15.NN Title`, subsections `### 15.NN.M` (EXCEPT §15.67.4 and §15.68.4 which are mis-leveled as `## 15.67.4` / `## 15.68.4`).

=== §15.90 TEST-DATE-YYYYMMDD (type: integer; argument rule 1: "Argument-1 shall be an integer") ===
§15.90.4 Returned value rule 1 — an if/else-if CHAIN, so precedence is year → month → day:
 a) "If the value of argument-1 is less than 16 010 000 or greater than 99 999 999" → (1). NOTE 1: "The year is not within the range 1601 to 9999."
 b) "Otherwise, if the value of FUNCTION MOD (argument-1 10000) is less than 100 or greater than 1299" → (2). NOTE 2: "The month is not within the range 1 through 12." (MOD 10000 yields MMDD; valid band is 0100..1299.)
 c) "Otherwise, if the value of FUNCTION MOD (argument-1 100) is less than 1 or greater than the number of days in the month determined by FUNCTION INTEGER (FUNCTION MOD (argument-1 10000) / 100) of the year determined by FUNCTION INTEGER (argument-1 / 10000)" → (3). NOTE 3: "The day is not valid for the given year and month." (Gregorian leap rules: div-by-4 except centuries unless div-by-400.)
 d) "Otherwise" → (0). Confirmed by informative D.31.3.8: 0 valid; 1 year subfield out of range; 2 month; 3 day.

=== §15.91 TEST-DAY-YYYYDDD (type: integer; argument-1 shall be an integer) ===
§15.91.4 Returned value rule 1 (chain, year → day):
 a) "If the value of argument-1 is less than 1 601 000 or greater than 9 999 999" → (1). NOTE 1: year not in 1601..9999.
 b) "Otherwise, if the value of FUNCTION MOD (argument-1 1000) is less than 1 or greater than the number of days in the year determined by FUNCTION INTEGER (argument-1 / 1000)" → (2). NOTE 2: "The day is not valid in the given year." (365 or 366 by Gregorian leap rule.)
 c) "Otherwise" → (0). Confirmed by D.31.3.9: 0 valid; 1 year; 2 day. There is NO code 3 for this function.

=== §15.93 TEST-NUMVAL (type: integer; argument rule 1: alphanumeric or national literal, or data item of class alphanumeric or national) ===
§15.93.4 Returned value rule 1 — THREE legs, not two:
 a) content conforms to NUMVAL argument rules → (0).
 b) "Otherwise, if one or more characters are in error, the position of the first character in error" (1-based ordinal position), with four numbered sub-notes:
   1. "Because one or more spaces following one or more digits is valid, if one or more spaces are embedded within a string of numeric characters, the returned value is the position of the first non-space character following the spaces. If argument-1 is '0 1', the returned value will be 3." (i.e. the error position is the digit AFTER the embedded spaces, not the space itself.)
   2. native arithmetic: >31 digits → position of the 32nd digit (if no prior error).
   3. standard-binary arithmetic: >35 digits → position of the 36th digit.
   4. standard-decimal arithmetic: >34 digits → position of the 35th digit.
 c) "Otherwise, (FUNCTION LENGTH (argument-1) + 1)." NOTE: "These errors include, but are not limited to: argument-1 is zero-length, argument-1 contains only spaces, argument-1 contains valid characters but is incomplete, such as the string ' +.'." So all-spaces returns LENGTH+1 (not the position of any space), zero-length returns 1, and a syntactically-open prefix like ' +.' returns LENGTH+1.

=== §15.94 TEST-NUMVAL-C (type: integer) ===
General format (§15.94.2): FUNCTION TEST-NUMVAL-C ( argument-1 [LOCALE [locale-name-1]] [ANYCASE] ) where argument-2 is STACKED as the alternative to the LOCALE phrase — i.e. YES, TEST-NUMVAL-C takes the currency string as argument-2 (mutually exclusive with LOCALE), plus an orthogonal optional ANYCASE keyword. §15.94.3 rule 1: "The argument rules for the TEST-NUMVAL-C function are the same as those specified in 15.68, NUMVAL-C function, Arguments."
§15.94.4 Returned value rule 1: textually IDENTICAL structure to §15.93.4 — a) conforms to NUMVAL-C argument rules → (0); b) position of first character in error with the SAME four sub-notes ('0 1'→3; 32nd/36th/35th-digit positions per arithmetic mode); c) otherwise (FUNCTION LENGTH (argument-1) + 1) with the same NOTE (zero-length / only spaces / incomplete like ' +.').

=== §15.67 NUMVAL accepted syntax (what TEST-NUMVAL must parse) ===
Rule 1, two formats: (A) [space-string] [+|−] [space-string] {digit [. [digit]] | . digit} [space-string]  — leading sign; (B) [space-string] {digit [. [digit]] | . digit} [space-string] [+|−|CR|DB] [space-string] — trailing sign. digit = string of one or more 0-9; space-string = one or more spaces; CR/DB "in uppercase or lowercase, or a combination". Note 'digit [. [digit]]' makes a trailing decimal point with no fraction ('5.') VALID; a bare '.' is not. Rule 2: "Leading and trailing spaces in argument-1 are ignored. Embedded spaces in argument-1 are ignored only if they appear before the first digit." Rules 3–4: total digits ≤31 (native) / ≤35 (standard-binary) / ≤34 (standard-decimal). Rule 5: period is the decimal separator; under DECIMAL-POINT IS COMMA the comma "shall be used ... instead of the character period". Returned value rules: 1) the numeric value; 2) negative if argument-1 contains CR, DB, or the minus sign.

=== §15.68 NUMVAL-C accepted syntax (what TEST-NUMVAL-C must parse) ===
General format: FUNCTION NUMVAL-C ( argument-1 [LOCALE [locale-name-1] | argument-2] [ANYCASE] ). Argument rules: 1) argument-1 category alphanumeric or national. 2) argument-2 same class as argument-1, "shall contain at least one non-space character", leading/trailing spaces in it ignored, and it "shall not contain any of the digits 0 through 9; the characters '*', '+', '−', ',', or '.'; or the two consecutive letters 'CR' or 'DB'" in any case; interior spaces ARE allowed in the currency string. 3) If neither argument-2 nor LOCALE: exactly one currency string for the compilation unit (the default currency sign or the SPECIAL-NAMES one). 4) Non-LOCALE formats: (A) [space-string] [+|−] [space-string] [currency] [space-string] {digit [, digit]... [. [digit]] | . digit} [space-string] — sign BEFORE currency; (B) [space-string] [currency] [space-string] {digit [, digit]... [. [digit]] | . digit} [space-string] [+|−|CR|DB] [space-string] — trailing sign/CR/DB. currency matches argument-2 (or the default currency sign) "character for character". 4d) comma = grouping separator, period = decimal separator; DECIMAL-POINT IS COMMA swaps BOTH roles. 4f) ANYCASE → currency matching done as if both sides were lowercased per LOWER-CASE. 5) LOCALE mode uses LC_MONETARY fields (currency_symbol/int_curr_symbol, sign posn, mon_decimal_point, mon_thousands_sep/mon_grouping); EC-LOCALE-MISSING if unavailable — but LOCALE is the project's ledgered decision-3 non-support. 6–7) digit caps 31/35/34 as in NUMVAL. Returned value rules: 1) the numeric value; 2) "The currency string, if any, and any grouping separators preceding the decimal separator are ignored"; 3) non-LOCALE: negative iff argument-1 contains CR, DB, or a minus sign. NOTE the grouping groups are arbitrary-length digit strings ('digit [, digit]...' where digit is ANY run of ≥1 digits) — there is NO 3-digit-group constraint, so '1,23,4.5' conforms.

### Hand-derived expected values (golden material)

| Input | Expected | Rule |
|---|---|---|
| FUNCTION TEST-DATE-YYYYMMDD (20240229) | 0 | §15.90.4 rule 1d — 2024 is a Gregorian leap year, Feb has 29 days |
| FUNCTION TEST-DATE-YYYYMMDD (16001231) | 1 | §15.90.4 rule 1a — 16001231 < 16010000 (year 1600 < 1601) |
| FUNCTION TEST-DATE-YYYYMMDD (20241301) | 2 | §15.90.4 rule 1b — MOD(arg,10000)=1301 > 1299 (month 13) |
| FUNCTION TEST-DATE-YYYYMMDD (20240001) | 2 | §15.90.4 rule 1b — MOD(arg,10000)=0001 < 100 (month 0) |
| FUNCTION TEST-DATE-YYYYMMDD (20230229) | 3 | §15.90.4 rule 1c — 2023 not a leap year, Feb has 28 days |
| FUNCTION TEST-DATE-YYYYMMDD (20240431) | 3 | §15.90.4 rule 1c — April has 30 days |
| FUNCTION TEST-DATE-YYYYMMDD (16000230) | 1 | §15.90.4 chain precedence — year check (1a) fires before month/day |
| FUNCTION TEST-DAY-YYYYDDD (2024366) | 0 | §15.91.4 rule 1c — 2024 leap year has 366 days |
| FUNCTION TEST-DAY-YYYYDDD (2023366) | 2 | §15.91.4 rule 1b — 2023 has 365 days |
| FUNCTION TEST-DAY-YYYYDDD (1900366) | 2 | §15.91.4 rule 1b — 1900 is NOT leap (century not divisible by 400) |
| FUNCTION TEST-DAY-YYYYDDD (2000366) | 0 | §15.91.4 rule 1c — 2000 divisible by 400, leap |
| FUNCTION TEST-DAY-YYYYDDD (1600100) | 1 | §15.91.4 rule 1a — 1600100 < 1601000 (year 1600) |
| FUNCTION TEST-DAY-YYYYDDD (2024000) | 2 | §15.91.4 rule 1b — MOD(arg,1000)=0 < 1 |
| FUNCTION TEST-NUMVAL ('123.45') | 0 | §15.93.4 rule 1a via §15.67.3 format A |
| FUNCTION TEST-NUMVAL (' + 12 ') | 0 | §15.93.4 rule 1a; §15.67.3 rule 2 — embedded spaces before the first digit are ignored |
| FUNCTION TEST-NUMVAL ('123cr') | 0 | §15.93.4 rule 1a; §15.67.3 rule 1 — CR/DB any case combination, trailing (format B) |
| FUNCTION TEST-NUMVAL ('5.') | 0 | §15.93.4 rule 1a; §15.67.3 'digit [ . [ digit ] ]' — fraction digits optional after the period |
| FUNCTION TEST-NUMVAL ('0 1') | 3 | §15.93.4 rule 1b sub-note 1 — verbatim spec example: first non-space character AFTER embedded spaces |
| FUNCTION TEST-NUMVAL ('1,234') | 2 | §15.93.4 rule 1b; §15.67.3 rule 5 — comma is not a valid character unless DECIMAL-POINT IS COMMA (then it IS the decimal separator: '1,234' → 0) |
| FUNCTION TEST-NUMVAL ('1.2.3') | 4 | §15.93.4 rule 1b — second '.' at position 4 is the first character in error |
| FUNCTION TEST-NUMVAL ('') [zero-length] | 1 | §15.93.4 rule 1c — LENGTH(arg)+1 = 0+1; NOTE lists zero-length explicitly |
| FUNCTION TEST-NUMVAL ('    ') [4 spaces] | 5 | §15.93.4 rule 1c — all-spaces is a c) case: LENGTH+1, NOT the position of a space |
| FUNCTION TEST-NUMVAL (' +.') | 4 | §15.93.4 rule 1c NOTE — verbatim 'incomplete' example: LENGTH(3)+1 |
| FUNCTION TEST-NUMVAL ('1234567890123456789012345678901234567890') [40 digits, native arithmetic] | 32 | §15.93.4 rule 1b sub-note 2 — position of the 32nd digit (>31-digit cap, §15.67.3 rule 3) |
| FUNCTION TEST-NUMVAL-C ('$1,234.56') [currency '$'] | 0 | §15.94.4 rule 1a via §15.68.3 rule 4a format A/B |
| FUNCTION TEST-NUMVAL-C ('-$123.45') [currency '$'] | 0 | §15.94.4 rule 1a — §15.68.3 format A: sign precedes currency |
| FUNCTION TEST-NUMVAL-C ('$123.45CR') [currency '$'] | 0 | §15.94.4 rule 1a — §15.68.3 format B: currency + trailing CR |
| FUNCTION TEST-NUMVAL-C ('$-123') [currency '$'] | 2 | §15.94.4 rule 1b — sign AFTER currency matches neither §15.68.3 format; '-' at position 2 is first in error |
| FUNCTION TEST-NUMVAL-C ('1234$') [currency '$'] | 5 | §15.94.4 rule 1b — currency precedes digits in BOTH §15.68.3 formats; trailing '$' at position 5 in error |
| FUNCTION TEST-NUMVAL-C ('$12X4') [currency '$'] | 4 | §15.94.4 rule 1b — 'X' at ordinal position 4 |
| FUNCTION TEST-NUMVAL-C ('0 1') | 3 | §15.94.4 rule 1b sub-note 1 — same verbatim example as §15.93 |
| FUNCTION TEST-NUMVAL-C ('   ') [3 spaces] | 4 | §15.94.4 rule 1c — LENGTH+1; NOTE lists only-spaces |
| FUNCTION TEST-NUMVAL-C ('usd 12' 'USD') [no ANYCASE] | 1 | §15.94.4 rule 1b — §15.68.3 rule 4a: currency matches 'character for character'; 'u' at position 1 fails |
| FUNCTION TEST-NUMVAL-C ('usd 12' 'USD' ANYCASE) | 0 | §15.94.4 rule 1a — §15.68.3 rule 4f: ANYCASE lowercases both sides for currency matching |
| FUNCTION TEST-NUMVAL-C ('1,23,4.5') [currency '$' omitted in string] | 0 | §15.94.4 rule 1a — §15.68.3 rule 4a 'digit [ , digit ] ...': grouping groups are arbitrary-length; currency itself is optional in the format |

### ⚠ Gotchas (trust these over the phase doc)

- THIRD returned-value leg the P11 doc Step 6 summary (PHASE-11-intrinsics-backlog-tierc-codec.md:405-406) omits: §15.93.4/§15.94.4 rule 1c returns FUNCTION LENGTH(argument-1)+1 when no specific character is in error — zero-length → 1, all-spaces → LENGTH+1 (NOT the position of a space), and 'valid but incomplete' strings like ' +.' → LENGTH+1. The doc says only '0 else the 1-based position of the first offending character'; goldens must cover the c) leg.
- The P11 doc's runtime signature `CobolIntrinsics.TestNumvalC(string, string currency, bool commaMode)` (line 410-411) has no ANYCASE parameter, but §15.94.2's general format includes an optional ANYCASE keyword that is ORTHOGONAL to the LOCALE-vs-argument-2 choice (usable with argument-2 or with the default currency). Only the LOCALE phrase is the ledgered decision-3 non-support (doc lines 52, 104, 472, 650) — ANYCASE is not covered by that exclusion. Check whether the existing NumvalC runtime handles ANYCASE; TEST-NUMVAL-C must mirror whatever NUMVAL-C accepts (§15.94.1: 'verify that the NUMVAL-C function will produce a valid numeric result').
- The digit-cap error positions are ARITHMETIC-MODE dependent (§15.93.4/§15.94.4 rule 1b sub-notes 2-4): native → 32nd digit, standard-binary → 36th digit, standard-decimal → 35th digit. The returned value is the ordinal position of that DIGIT character in argument-1 (separators/spaces in between shift it), so the runtime needs the active ARITHMETIC mode (P10 landed ARITHMETIC IS STANDARD, so this is live).
- Embedded-space error position: per §15.93.4 rule 1b sub-note 1 the reported position is the first NON-SPACE character AFTER the embedded spaces ('0 1' → 3, not 2). A naive parser that reports the space's own position is wrong per verbatim spec example.
- §15.90.4 date checks form an if/else-if CHAIN: year (1) is checked before month (2) before day (3) — 16000230 returns 1, not 2. Same for §15.91 (year before day). TEST-DAY-YYYYDDD has NO code 3.
- TEST-DATE/TEST-DAY take an INTEGER argument (§15.90.3/§15.91.3 rule 1), not a string — no character positions; codes confirmed twice (normative rules + informative D.31.3.8/D.31.3.9). Year band is 1601–9999 (lower bounds 16 010 000 and 1 601 000 written with thousands-spaces in the spec).
- NUMVAL 'digit [ . [ digit ] ]' makes a trailing decimal point with no fraction digits VALID ('5.' → TEST-NUMVAL 0); a bare '.' or ' +.' is invalid (c-leg). Do not require a digit after the period when digits precede it.
- NUMVAL-C currency placement: currency PRECEDES the digits in BOTH §15.68.3 rule 4a formats — there is NO trailing-currency form ('1234$' invalid) and NO currency-then-sign form ('$-123' invalid; leading sign only via format A where sign comes BEFORE currency). Also a leading sign and a trailing sign/CR/DB cannot coexist (they are in different formats).
- NUMVAL-C grouping: 'digit [ , digit ] ...' where digit is ANY run of >=1 digits — NO 3-digit-group constraint ('1,23,4.5' conforms). §15.68.4 rule 2 ignores 'grouping separators preceding the decimal separator' — commas after the decimal separator are not part of the format (error).
- DECIMAL-POINT IS COMMA: for NUMVAL (§15.67.3 rule 5) comma REPLACES period as decimal separator; for NUMVAL-C (§15.68.3 rule 4d) the two characters SWAP roles (comma=decimal, period=grouping). The commaMode flag in the planned runtime signatures must implement the swap for -C, not just a decimal-char substitution.
- TEST-NUMVAL/TEST-NUMVAL-C argument-1 may be NATIONAL as well as alphanumeric (§15.93.3; §15.68.3 rule 1); CR/DB and E-case rules are stated per character set. Positions are ordinal CHARACTER positions.
- §15.94.3 imports ALL §15.68.3 argument rules, including the argument-2 constraints (same class as argument-1; >=1 non-space character; must not contain digits, '*', '+', '−', ',', '.', or 'CR'/'DB' in any case; interior spaces allowed, leading/trailing spaces ignored) and rule 3 (no argument-2/LOCALE → the ONE compilation-unit currency string, i.e. the SPECIAL-NAMES/default currency sign — matches the planned bind-time default-currency injection).
- Markdown quirk in the spec file: §15.67.4 and §15.68.4 'Returned value rules' headers are at H2 level ('## 15.67.4') instead of H3 — grep for '## 15.6[78].4' if anchoring on them.
- The spec's own D.31 annex (informative) matches the normative rules exactly — no contradiction found between §15.90/§15.91 and D.31.3.8/9, and no contradiction with the P11 doc's edition attribution (2002) claims; the only P11-doc drifts found are the missing c)-leg (LENGTH+1) nuance and the missing ANYCASE parameter noted above.

## `spec:concat-smallest` — CONCAT(ENATE) §15.18 / SMALLEST-ALGEBRAIC §15.83

### Sections found

| § | Title | spec line |
|---|---|---|
| §15.18 | CONCAT function | 34819 |
| §15.18.1 | General (argument-type → function-type table) | 34822 |
| §15.18.2 | General format: FUNCTION CONCAT ( argument-1, argument-2 … ) | 34839 |
| §15.18.3 | Argument rules (3 rules) | 34844 |
| §15.18.4 | Returned value rules (4 rules) | 34853 |
| §15.83 | SMALLEST-ALGEBRAIC function | 38111 |
| §15.83.1 | General (Integer→Integer, Numeric→Numeric) | 38114 |
| §15.83.2 | General format: FUNCTION SMALLEST-ALGEBRAIC ( argument-1 ) | 38128 |
| §15.83.3 | Argument rules (4 rules; AR4 = native-arith implementor-defined) | 38133 |
| §15.83.4 | Returned value rules (2 rules + NOTE table of expected results) | 38144 |
| §15.3 | Arguments (general intrinsic argument rules; EC-ARGUMENT-FUNCTION at line 33761) | 33707 |
| Table 21 | Table of functions (CONCAT row 34175; SMALLEST-ALGEBRAIC row 34300 — both rows defective vs normative rules) | 34147 |
| §8.3.3.6.3/8.3.3.6.4 | Figurative constants: substitutable for 'literal' (rule 1, line 6318); length rules GR3 a/b/c (lines 6365-6371) | 6318 |
| §8.9 word list | Intrinsic-function-name list: CONCAT at 10954, SMALLEST-ALGEBRAIC at 11020 (CONCATENATE absent) | 10941 |
| Annex E.2 item 13 | FUNCTION ALL INTRINSIC — CONCAT and SMALLEST-ALGEBRAIC listed among NEW intrinsic function names (BASECONVERT, CONCAT, CONVERT, FIND-STRING, MODULE-NAME, SMALLEST-ALGEBRAIC, SUBSTITUTE) | 49178 |
| Annex E.3 item 23 | "FUNCTION CONCAT ... has been added" — new in 2023 | 50283 |
| Annex E.3 item 29 | "FUNCTION SMALLEST-ALGEBRAIC ... has been added" — new in 2023 | 50302 |
| Annex D.32 | Alternatives to HIGHEST/LOWEST/SMALLEST-ALGEBRAIC (informative; loosely says numeric-edited — do NOT follow) | 48935 |
| Annex A item 180 | Implementor-defined: SMALLEST-ALGEBRAIC argument usage under native arithmetic (cites 15.83 AR4) | 39721 |
| §15.43 | HIGHEST-ALGEBRAIC (contrast: its AR1 DOES allow numeric-edited; SMALLEST-ALGEBRAIC's does not) | 36126 |

### Semantics

== NAMING VERDICT (load-bearing) == The 2023 spec §15.18 defines CONCAT. The word CONCATENATE has ZERO occurrences anywhere in specs/ISO_COBOL.md — not in Annex D substitution/alternative lists, not in archaic/obsolete lists, not in the reserved/intrinsic word lists. Annex E.3 item 29->23 (line 50283): \"FUNCTION CONCAT. The CONCAT function has been added to be able to concatenate data items in the same fashion as the concatenation operator does for literals.\" It also appears in the Foreword new-intrinsics list (line 1230) and Annex E.2 item 13 (line 49178) as one of the \"new intrinsic function names\" prohibited as user-defined words under FUNCTION ALL INTRINSIC. So per this spec CONCAT is NEW-IN-2023, and there is no evidence CONCATENATE was ever an ISO name (see gotchas).

== §15.18 CONCAT ==
Format (15.18.2): FUNCTION CONCAT ( argument-1, argument-2 … ) — variable arity, >= 2 arguments.
AR1: \"Argument-1 and argument-2 shall be data items or literals of class alphabetic, alphanumeric, boolean, numeric or national.\"
AR2: \"If any argument is usage national, all arguments shall be usage national, otherwise all arguments shall be usage display.\" (no display/national mixing — mixed is a compile-time violation).
AR3: \"If argument-1 or argument-2 is numeric, it shall be usage display or national and shall be an unsigned integer.\" (signed or non-integer numeric operands illegal; binary/packed numeric illegal via AR2+AR3 usage restriction).
RVR1: \"The returned value ... shall contain all of the characters in argument-1 followed by all of the characters in argument-2.\"
RVR2: \"If argument-1 is of class or usage national, the function will return a national value.\"
RVR3: \"if argument-1 is usage display, then if argument-1 and all argument-2 are of class alphabetic, the function will return an alphabetic value, otherwise the function will return an alphanumeric value.\" (Literals are class alphanumeric per §8.3.3.2, to which §8.5.2.1 delegates a literal's class — so all-literal CONCAT returns alphanumeric even if every character is a letter.)
RVR4: \"If more than one argument-2 is specified, execution proceeds as if the CONCAT function was performed on each argument 2 where the result of the concatenation as specified in Returned value rule 1 becomes argument-1 in the next iteration\" — left fold.
Type table (15.18.1): All alphabetic→Alphabetic; Alphanumeric→Alphanumeric; Boolean usage display→Alphanumeric; Numeric usage display→Alphanumeric; Boolean usage national→National; Numeric usage national→National; National→(cell empty in transcription; National per RVR2).
Numeric operands: a usage-display unsigned-integer data item contributes its digit-character string per its PICTURE (PIC 9(4) VALUE 7 contributes \"0007\"); a numeric literal contributes its digit characters (\"123\"); the result is alphanumeric (display) / national (national) — never numeric.
Figurative constants: §15.18 is silent, but §8.3.3.6.3 rule 1 (line 6318): \"A figurative constant may be used whenever 'literal' appears in a format or when a rule allows it\" (AR1 says \"literals\"; no syntax rule in 15.18.3 prohibits). Length via §8.3.3.6.4 GR3 (context does not specify a length): (b) non-ALL figurative constant = ONE character; (c) ALL literal-1 = the length of literal-1. So CONCAT(SPACE, \"X\") = \" X\" (derived, not explicit — see gotchas). Class per GR1: national character value when context requires national (i.e., other args national, AR2), else alphanumeric.
Errors: no CONCAT-specific EC; general §15.3 (line 33761): a bad argument value sets EC-ARGUMENT-FUNCTION; if checking not enabled, implementor defines the result of the function reference.

== §15.83 SMALLEST-ALGEBRAIC ==
General (15.83.1): \"returns a value that is equal to the smallest algebraic value that may represent the difference between two values represented in argument-1.\" Function type: Integer argument → Integer; Numeric argument → Numeric. NOTE: argument types limited because of returned-value rules tied to the arithmetic mode.
Format (15.83.2): FUNCTION SMALLEST-ALGEBRAIC ( argument-1 ) — exactly one argument.
AR1: \"Argument-1 shall be a data item of category numeric and shall not be an integer function or numeric function.\" → the argument is a DATA ITEM ONLY: no literals, no arithmetic expressions, no function results, and NO numeric-edited items (contrast §15.43.3 AR1 HIGHEST-ALGEBRAIC: \"category numeric or numeric-edited\").
AR2: standard-decimal arithmetic in effect → argument-1 shall not have standard-binary floating-point usage. AR3: standard-binary in effect → not standard-decimal floating-point usage. AR4: native arithmetic → \"the usage restrictions for argument-1 shall be implementor-defined\" (Annex A implementor-defined item 180, line 39721: must be documented in the implementor's user documentation).
RVR1: for a floating-point argument, its data description shall be such that any value permitted by that description passes an IN-ARITHMETIC-RANGE test.
RVR2: \"The value returned is equal to the positive algebraic value of smallest finite magnitude that may be used to increment argument-1.\" → the resolution/step implied by argument-1's DATA DESCRIPTION (PICTURE scale incl. P-scaling, usage); it depends ONLY on the description, never on runtime contents — statically foldable per description (except native-arith implementor latitude, AR4).
Spec NOTE table of expected results (lines 38152-38166): S999→+1; S9PP→+100; S9(4) BINARY→+1; 99V9(3)→+.001; BINARY-CHAR SIGNED→+1; BINARY-CHAR UNSIGNED→+1.
Annex D.32 (informative): MOVE SMALLEST-ALGEBRAIC (numeric-item) TO numeric-item ≡ SET numeric-item TO NEAREST-TO-ZERO IN-ARITHMETIC-RANGE — a useful cross-check identity for goldens.

### Hand-derived expected values (golden material)

| Input | Expected | Rule |
|---|---|---|
| FUNCTION CONCAT("AB", "CD") | "ABCD" (class alphanumeric — literals are class alphanumeric, so RVR3 'otherwise' leg) | §15.18.4 RVR1 + RVR3 |
| FUNCTION CONCAT("A", "B", "C", "D") | "ABCD" via left fold: ((A+B)+C)+D | §15.18.4 RVR4 + RVR1 |
| 01 N PIC 9(4) VALUE 7.  FUNCTION CONCAT(N, "X") | "0007X" (usage-display unsigned-integer item contributes its full digit string; result alphanumeric) | §15.18.3 AR3 + §15.18.4 RVR1/RVR3 + §15.18.1 type table (Numeric usage Display → Alphanumeric) |
| FUNCTION CONCAT(123, "-A") | "123-A" (unsigned integer literal permitted; result alphanumeric) | §15.18.3 AR1+AR3, §15.18.4 RVR1/RVR3 |
| 01 A1 PIC AA VALUE "AB". 01 A2 PIC A VALUE "C".  FUNCTION CONCAT(A1, A2) | "ABC" with function type ALPHABETIC (all args class alphabetic, usage display) | §15.18.4 RVR3 + §15.18.1 type table (All alphabetic → Alphabetic) |
| FUNCTION CONCAT(N"AB", N"CD") | N"ABCD" (national result) | §15.18.3 AR2 + §15.18.4 RVR2 |
| FUNCTION CONCAT(N"AB", "CD") — mixed national + display | COMPILE-TIME REJECT (all arguments must be usage national if any is) | §15.18.3 AR2 |
| 01 S PIC S9(3) VALUE 5.  FUNCTION CONCAT(S, "X") — signed numeric arg | REJECT (numeric argument shall be an unsigned integer) | §15.18.3 AR3 |
| FUNCTION CONCAT(SPACE, "X") | " X" (non-ALL figurative constant = one character) — DERIVED, §15.18 itself is silent on figurative constants | §8.3.3.6.3 rule 1 + §8.3.3.6.4 GR3b + §15.18.4 RVR1 |
| 01 X PIC S999.  FUNCTION SMALLEST-ALGEBRAIC(X) | +1 (regardless of X's runtime contents) | §15.83.4 RVR2 + spec NOTE table (S999 → +1) |
| 01 X PIC 99V9(3).  FUNCTION SMALLEST-ALGEBRAIC(X) | 0.001 | §15.83.4 RVR2 + NOTE table (99V9(3) → +.001) |
| 01 X PIC S9PP.  FUNCTION SMALLEST-ALGEBRAIC(X) | +100 (P-scaling: representable values are multiples of 100) | §15.83.4 RVR2 + NOTE table (S9PP → +100) |
| 01 X BINARY-CHAR UNSIGNED.  FUNCTION SMALLEST-ALGEBRAIC(X) | +1 (integer arg → integer function type) | §15.83.4 RVR2 + NOTE table + §15.83.1 type table |
| FUNCTION SMALLEST-ALGEBRAIC(5) or SMALLEST-ALGEBRAIC(A + B) or SMALLEST-ALGEBRAIC(edited-item) | COMPILE-TIME REJECT (argument shall be a DATA ITEM of category numeric; not a literal/expression/function; numeric-edited NOT permitted) | §15.83.3 AR1 |

### ⚠ Gotchas (trust these over the phase doc)

- LOUD — CONCATENATE DOES NOT EXIST in the 2023 spec: zero grep hits in all 53731 lines, including Annex D alternatives/substitution lists and the archaic/obsolete lists. CONCAT is documented as ADDED in this edition (Annex E.3 item 23 line 50283 'has been added'; Foreword new-intrinsics list line 1230; Annex E.2 item 13 line 49178 lists it among NEW intrinsic function names). The P11 plan/task premise ('CONCATENATE = the 2002/2014 ISO name removed in 2023') is NOT supported by this spec — a 2023 removal/rename of an ISO-2002 function would appear in the E.2 incompatibility or D substitution lists, and it does not. CONCATENATE is a vendor-extension name (MF/GnuCOBOL/ACU); any 'CONCATENATE window' should be re-scoped as a dialect/extension aliasing decision, not an ISO edition window.
- SMALLEST-ALGEBRAIC's argument is a DATA ITEM ONLY: §15.83.3 AR1 'shall be a data item of category numeric and shall not be an integer function or numeric function' — no literals, no arithmetic expressions, no function results, and NO numeric-edited items. This is stricter than HIGHEST/LOWEST-ALGEBRAIC (§15.43.3 AR1: 'category numeric or numeric-edited'). Informative Annex D.32 (line 48946) loosely says the three functions 'apply to any type of numeric and numeric-edited item' — normative AR1 governs; do not follow D.32.
- Table 21 rows are defective synopses — normative section rules govern: the SMALLEST-ALGEBRAIC row (line 34300) says Arguments 'Anum1 or Nat1 or Num1', contradicting AR1 (numeric data item only), and its 'Value returned' phrase ('smallest positive number that can be represented') is looser than RVR2 ('smallest finite magnitude that may be used to increment'). The CONCAT row (line 34175) has generic boilerplate in its 'Value returned' cell.
- §15.18.1 type table's last row is '| National | |' — the function-type cell is empty (transcription/spec defect). RVR2 supplies the answer: national argument-1 → national result.
- CONCAT returns ALPHABETIC only when argument-1 is usage display AND every argument is class alphabetic (RVR3). Literals are class alphanumeric, so an all-letter all-literal CONCAT still returns alphanumeric — only alphabetic DATA ITEMS (PIC A...) yield an alphabetic result.
- CONCAT numeric operands: AR3 restricts to UNSIGNED INTEGERS of usage display or national. PIC S9.., non-integer (V), and binary/packed numeric arguments are invalid. A valid usage-display integer item contributes its full PICTURE-width digit string (leading zeros included).
- Figurative constants in CONCAT are permitted only by derivation (§8.3.3.6.3 rule 1: usable 'whenever literal appears ... or when a rule allows it'; §15.18.3 AR1 says 'literals'; nothing prohibits them). Length comes from §8.3.3.6.4 GR3: one character for non-ALL forms (GR3b), length of literal-1 for ALL "lit" (GR3c). §15.18 never mentions them explicitly — treat the CONCAT(SPACE,...) golden as an interpretation, or gate it accordingly.
- SMALLEST-ALGEBRAIC under NATIVE arithmetic (the project default situation): AR4 makes the argument usage restrictions implementor-defined — Annex A item 180 (line 39721) REQUIRES this be documented in the implementor's user documentation. The implementing session must pick + document the native-arithmetic usage set (the NOTE table implies at least DISPLAY, BINARY, BINARY-CHAR, and V/P-scaled fixed-point should work).
- SMALLEST-ALGEBRAIC depends only on argument-1's DATA DESCRIPTION, never runtime content — it is compile-time constant-foldable per description. Function type: integer description → integer function, else numeric (§15.83.1 table).
- Runtime argument-value failures in intrinsics raise EC-ARGUMENT-FUNCTION per the general §15.3 rule (line 33761); if EC checking is not enabled the function result is implementor-defined.
- Cross-check identity for a SMALLEST-ALGEBRAIC golden (Annex D.32, line 48952-48958): MOVE SMALLEST-ALGEBRAIC (numeric-item) TO numeric-item ≡ SET numeric-item TO NEAREST-TO-ZERO IN-ARITHMETIC-RANGE.
- FUNCTION ALL INTRINSIC interaction (Annex E.2 item 13, line 49178): within a REPOSITORY paragraph specifying FUNCTION ALL INTRINSIC, CONCAT and SMALLEST-ALGEBRAIC (and BASECONVERT/CONVERT/FIND-STRING/MODULE-NAME/SUBSTITUTE) are prohibited as user-defined words — a 2023-vs-2014 incompatibility the version matrix may want to cover.

## `spec:locale` — A.4.9(?) locale module + LOCALE keyword variants + §4.2.7 disposition basis

### Sections found

| § | Title | spec line |
|---|---|---|
| 4.2.7 | Optional language elements | 2440 |
| 8.1.5 | Collating sequences (routes 'specific comparisons' to LOCALE-COMPARE / STANDARD-COMPARE) | 5267 |
| 8.2 / 8.2.1 | Locales — General (locale categories LC_COLLATE/LC_CTYPE/LC_MONETARY/LC_TIME etc.; current-locale model; EC-LOCALE-MISSING / EC-LOCALE-INVALID) | 5291 |
| 12.3.7 | SPECIAL-NAMES paragraph — ORDER TABLE ordering-name-1 IS literal-9 (format line 14157; GR 17 line 14627; NOTE 5 default table name line 14629) | 14157 |
| 14.6.13.1.6 | Exception-names and exception conditions (EC-LOCALE family lines 24720-24726; EC-ORDER-NOT-SUPPORTED line 24759) | 24611 |
| 15.3 | Arguments — rule 8 Locale-name (line 33736), rule 12 Ordering-name (line 33744) | 33707 |
| 15.6 | Summary of functions ('Loc means a locale' 34109; 'Ord means an ordering table' 34113) | 34096 |
| 15.51 | LOCALE-COMPARE function | 36527 |
| 15.52 | LOCALE-DATE function | 36585 |
| 15.53 | LOCALE-TIME function | 36626 |
| 15.54 | LOCALE-TIME-FROM-SECONDS function | 36675 |
| 15.57 | LOWER-CASE function (LOCALE keyword leg) | 36786 |
| 15.68 | NUMVAL-C function (LOCALE keyword leg) | 37363 |
| 15.85 | STANDARD-COMPARE function | 38217 |
| 15.94 | TEST-NUMVAL-C function (LOCALE keyword leg) | 38699 |
| 15.97 | UPPER-CASE function (LOCALE keyword leg) | 38902 |
| A.3 | Processor-dependent language element list (item 25, line 40170: STANDARD-COMPARE + EC-ORDER-NOT-SUPPORTED + ORDER TABLE clause dependent on ISO/IEC 14651:2020) | 40052 |
| A.4 / A.4.1 | Optional language element list — General (accept-syntax-only-when-claimed rule, line 40234) | 40229 |
| A.4.9 | Locale support and related functions (the locale optional module, items 1-13, lines 40379-40405) | 40379 |

### Semantics

ANNEX VERIFICATION — A.4.9 CONFIRMED. The locale module is exactly "A.4.9 Locale support and related functions" (line 40379). Its 13 items: 1) EC-LOCALE and EC-ORDER-NOT-SUPPORTED exception conditions in RAISING/USE/PERFORM WHEN/RAISE/TURN; 2) LOCALE-COMPARE (15.51); 3) LOCALE-DATE (15.52); 4) LOCALE-TIME (15.53); 5) LOCALE-TIME-FROM-SECONDS (15.54); 6) LOWER-CASE function, LOCALE keyword (15.57); 7) OBJECT-COMPUTER paragraph, CHARACTER CLASSIFICATION clause (12.3.6); 8) PICTURE clause, format 2: locale (13.18.40); 9) SET statement, format 11: set-locale and format 12: save-locale (14.9.39); 10) SPECIAL-NAMES paragraph: LOCALE clause and LOCALE phrases in the ALPHABET clause (12.3.7); 11) STANDARD-COMPARE function (15.85); 12) TEST-NUMVAL-C function, LOCALE keyword and locale-name-1 (15.94); 13) UPPER-CASE function, LOCALE keyword (15.97).

CONFORMANCE ROUTE FOR NON-SUPPORT — two governing sentences. §4.2.7 (line 2442): "Language elements that an implementor may, but need not, implement are listed in A.4, Optional language element list. An implementor shall identify in user documentation the optional language elements for which that implementor claims support. If an implementor provides support for parts of an optional feature, user documentation shall identify the elements that are supported and those that are not supported. The provisions of 4.2.5, Implementor-defined language elements, apply for each optional language element for which support is claimed." A.4.1 General (line 40234): "An implementation shall accept the syntax and provide the functionality for an optional element only when support for that language element is claimed by the implementor." — i.e. when support is NOT claimed the implementation is not to accept the syntax at all; a loud compile-time rejection + user-doc listing IS the conforming disposition. A.4.1 line 40236 additionally: "Any associated syntax rules, general rules, other rules, exception conditions, and I-O status values are also optional, even if not explicitly listed." A.4.1 line 40242: "An implementor's user documentation shall identify optional language elements for which support is claimed."

STANDARD-COMPARE (§15.85) — IS in A.4.9 (item 11, line 40401) but is NOT semantically locale-dependent. GF (15.85.2): FUNCTION STANDARD-COMPARE ( argument-1 argument-2 [ ordering-name-1 ] [ argument-4 ] ). AR1/AR2: args of class alphabetic/alphanumeric/national; AR3 may differ; AR4 neither shall be a zero-length literal; AR5: ordering-name-1 "shall be associated with a cultural ordering table in the ORDER TABLE clause of the SPECIAL-NAMES paragraph... If ordering-name-1 is not specified, the default ordering table 'ISO 14651_2020_TABLE1' described in Annex A of ISO/IEC 14651:2020 shall be used"; AR6: argument-4 a positive nonzero integer (comparison level). RVR1: argument-4 unspecified => highest level defined in the table; RVR2: table/level unavailable => EC-ORDER-NOT-SUPPORTED (fatal); RVR3: mixed-class with national => other converted to national; RVR4: trailing-space truncation (all-spaces operand -> single space); RVR5: compared per ordering table+level; RVR6: returns "=", "<", ">"; RVR7: length 1. So it consumes an ISO/IEC 14651 cultural ordering table via SPECIAL-NAMES ORDER TABLE (§12.3.7 GR17, line 14627) and ordering-name (§8.x line 5661: "An ordering-name identifies a cultural ordering table used in the execution of the STANDARD-COMPARE intrinsic function"; §15.3 arg rule 12) — no locale category involved. INDEPENDENT second escape hatch: A.3 Processor-dependent list item 25 (lines 40170/40178): "The STANDARD-COMPARE intrinsic function, the EC-ORDER-NOT-SUPPORTED exception condition, and the ORDER TABLE clause in the SPECIAL-NAMES paragraph are dependent upon an implementation of ISO/IEC 14651:2020. The implementor need not accept the syntax or set the EC-ORDER-NOT-SUPPORTED exception condition to exist when support for ISO/IEC 14651:2020 is not provided." Cite BOTH A.4.9 item 11 and A.3 item 25 in the diagnostic; A.3 item 25 is the only Annex-A home for the ORDER TABLE clause (it is NOT listed in A.4.9).

THE FOUR LOCALE-* FUNCTIONS — all take a bare OPTIONAL POSITIONAL locale-name-1 (no LOCALE keyword):
- §15.51 LOCALE-COMPARE: GF: FUNCTION LOCALE-COMPARE ( argument-1 argument-2 [ locale-name-1 ] ). Type alphanumeric. AR1/AR2 class alphabetic/alphanumeric/national, AR3 may differ, AR4 locale-name-1 shall be associated with a locale in SPECIAL-NAMES. RVR1 mixed-with-national => convert other to national; RVR2 trailing-space truncation (all-spaces -> single space); RVR3 locale-name-1 specified => that locale, else current locale; unavailable => EC-LOCALE-MISSING; RVR4 compared using the locale's cultural ordering (NOTE: not necessarily character-by-character); RVR5 returns '=', '<', '>'; RVR6 length 1. Per §12.3.6-region rule (line 24221): uses category LC_COLLATE of the specified (else current) locale.
- §15.52 LOCALE-DATE: GF: FUNCTION LOCALE-DATE ( argument-1 [ locale-name-1 ] ). Type alphanumeric. AR1: argument-1 class alphanumeric or national, exactly 8 character positions; AR2: content = YYYYMMDD as in CURRENT-DATE positions 1-8 and valid per that function; AR3 locale-name association. RVR1 locale selection + EC-LOCALE-MISSING as above; RVR2 formatted per locale field d_fmt; RVR3 returned length depends on the locale format.
- §15.53 LOCALE-TIME: GF: FUNCTION LOCALE-TIME ( argument-1 [ locale-name-1 ] ). AR1: 6 character positions, class alphanumeric or national; AR2: hhmmss as CURRENT-DATE positions 9-14; AR3 exceptions: a) hours 00 through 24, b) seconds 00 through 99 (leap-second NOTE); AR4 locale-name association. RVR1 locale selection + EC-LOCALE-MISSING; RVR2 formatted per locale field t_fmt; RVR3 length locale-dependent.
- §15.54 LOCALE-TIME-FROM-SECONDS: GF: FUNCTION LOCALE-TIME-FROM-SECONDS ( argument-1 [ locale-name-1 ] ). AR1: argument-1 a numeric value in standard numeric time form; AR2 locale-name association. RVR1-RVR3 identical shape to 15.53 (t_fmt; EC-LOCALE-MISSING).

FUNCTIONS WITH A LOCALE KEYWORD PHRASE — exhaustive (grep of LOCALE + locale-name across all of clause 15; no others exist):
- §15.57 LOWER-CASE: GF: FUNCTION LOWER-CASE ( argument-1 [ LOCALE locale-name-1 ] ) — keyword form; locale-name-1 REQUIRED after LOCALE. RVR2: locale-name-1 specified => case correspondence from category LC_CTYPE of that locale; RVR3: not specified but a locale in effect for character classification per 12.3.6 OBJECT-COMPUTER => LC_CTYPE of that; RVR4: no locale in effect => implementor-defined correspondence; RVR5: result may differ in length when correspondence not one-to-one; RVR6: letters with no correspondence unchanged.
- §15.97 UPPER-CASE: GF: FUNCTION UPPER-CASE ( argument-1 [ LOCALE locale-name-1 ] ) — mirror rules RVR2-RVR6 (LC_CTYPE, lowercase->uppercase).
- §15.68 NUMVAL-C: GF (two-row stack): FUNCTION NUMVAL-C ( argument-1 [ LOCALE [ locale-name-1 ] ] [ ANYCASE ] ) where the second position is ALTERNATIVELY [ argument-2 ] (currency string) — i.e. LOCALE [locale-name-1] and argument-2 are mutually exclusive; locale-name-1 OPTIONAL after LOCALE (bare LOCALE => current locale). AR3: neither argument-2 nor LOCALE => single compilation-unit currency string; AR4: LOCALE not specified => the two literal monetary formats; AR5 (LOCALE specified): a) locale-name-1 shall be associated with a locale in SPECIAL-NAMES; category LC_MONETARY of that locale (else of the current locale) evaluates the monetary format; unavailable => EC-LOCALE-MISSING; AR5 ANYCASE sub-rule matches currency_symbol + first three characters of int_curr_symbol case-insensitively via LOWER-CASE with LOCALE locale-name-1. Returned-value rule 3 (line 37472): with LOCALE, negative iff argument-1 matches locale fields negative_sign and n_sign_posn; without LOCALE, negative iff argument-1 contains CR, DB, or a minus sign.
- §15.94 TEST-NUMVAL-C: GF: FUNCTION TEST-NUMVAL-C ( argument-1 [LOCALE [ locale-name-1 ]] [ ANYCASE ] ) with the same [argument-2] alternative stack. AR1: argument rules = those of 15.68. RVR1: a) conforming => 0; b) else position of first character in error (sub-rule 1: '0 1' => 3; sub-rule 2: native arithmetic >31 digits => position of the 32nd digit; sub-rule 3: standard-binary >35 digits => 36th; sub-rule 4: standard-decimal >34 digits => 35th); c) otherwise LENGTH(argument-1)+1 (zero-length, all spaces, incomplete such as ' +.').
- NUMVAL (§15.67) and TEST-NUMVAL (§15.93) have NO LOCALE phrase — line 37297 NOTE: "Locale-based functionality equivalent to NUMVAL can be obtained by using the NUMVAL-C function with the LOCALE keyword... locale category LC_MONETARY will be used because there is no sign convention specified in locale category LC_NUMERIC."

EXCEPTION CONDITIONS (§14.6.13.1.6 table, lines 24720-24726, 24759): EC-LOCALE (any locale related exception, category header); EC-LOCALE-IMP (Imp); EC-LOCALE-INCOMPATIBLE (Fatal, "The referenced locale does not specify the expected characters in LC_COLLATE"); EC-LOCALE-INVALID (Fatal, "Locale content is invalid or incomplete"); EC-LOCALE-INVALID-PTR (Fatal, "Pointer does not reference a saved locale"); EC-LOCALE-MISSING (Fatal, "The specified locale is not available"); EC-LOCALE-SIZE (Fatal, "Digits were truncated in locale editing"); EC-ORDER-NOT-SUPPORTED (Fatal, "Cultural ordering table or ordering level specified for STANDARD-COMPARE function not supported"). §8.2.1 (line 5335) backs the runtime model: locale not found => EC-LOCALE-MISSING and the operation is unsuccessful; invalid/incomplete content => EC-LOCALE-INVALID.

DIAGNOSTIC WORDING CITATIONS: reject unsupported locale-module syntax citing "ISO/IEC 1989:2023 Annex A §A.4.9 (Locale support and related functions), an optional language element; non-support documented per §4.2.7 / §A.4.1". For STANDARD-COMPARE and the ORDER TABLE clause additionally cite "Annex A §A.3 item 25 (dependent upon an implementation of ISO/IEC 14651:2020; the implementor need not accept the syntax)".

### Hand-derived expected values (golden material)

| Input | Expected | Rule |
|---|---|---|
| FUNCTION LOCALE-COMPARE ( "ABC" "ABC" ) [if the module were supported] | '=' (alphanumeric, length 1) | §15.51.4 returned-value rules 5 and 6 (equal operands compare equal regardless of locale ordering) |
| FUNCTION LOCALE-COMPARE ( "A  " "A" ) | '=' — trailing spaces truncated before comparison | §15.51.4 rule 2 (trailing spaces truncated; all-spaces operand truncates to a single space) + rule 5 |
| FUNCTION LOCALE-COMPARE ( X Y LOC-NAME ) where LOC-NAME's locale is unavailable | EC-LOCALE-MISSING (Fatal) set to exist | §15.51.4 rule 3; EC table §14.6.13.1.6 |
| FUNCTION STANDARD-COMPARE ( "X" "X" ) | "=" (length 1); default table 'ISO 14651_2020_TABLE1', highest level defined | §15.85.3 AR5 (default table), §15.85.4 rules 1, 6, 7 |
| FUNCTION STANDARD-COMPARE with ordering table/level unavailable on the processor | EC-ORDER-NOT-SUPPORTED (Fatal) — but an implementor without ISO/IEC 14651:2020 support 'need not accept the syntax or set the... exception condition' | §15.85.4 rule 2 + Annex A §A.3 item 25 |
| FUNCTION STANDARD-COMPARE ( "" "X" ) (zero-length literal) | compile-time violation — neither argument shall be a zero-length literal | §15.85.3 argument rule 4 |
| FUNCTION TEST-NUMVAL-C ( "0 1" ) | 3 (position of first non-space character after embedded spaces) | §15.94.4 rule 1 b) 1 (worked example given in the rule itself) |
| FUNCTION TEST-NUMVAL-C ( " +." ) | 4 = LENGTH(argument-1)+1 (valid characters but incomplete) | §15.94.4 rule 1 c) + trailing NOTE |
| FUNCTION NUMVAL-C ( "1,234.56CR" ) — no LOCALE keyword | -1234.56 (negative because CR present) | §15.68 returned-value rule 3: 'When the LOCALE keyword is not specified, the returned value is negative if argument-1 contains CR, DB, or a minus sign' + §15.68.3 AR4 format |
| FUNCTION LOWER-CASE ( "AbC" ) — no locale in effect | "abc" (implementor-defined correspondence when no locale in effect; A-Z to a-z, same length) | §15.57.4 rules 1, 4, 5 |
| FUNCTION LOCALE-DATE ( WS-7CHAR ) | compile-time violation — argument-1 shall be 8 character positions in length (YYYYMMDD per CURRENT-DATE 1-8) | §15.52.3 argument rules 1-2 |
| FUNCTION LOCALE-TIME ( "240000" ) | valid input — hours 00 through 24 allowed; "250000" invalid; seconds up to 99 allowed | §15.53.3 rule 3 a) and b) |
| Any use of LOCALE-COMPARE / LOCALE-DATE / LOCALE-TIME / LOCALE-TIME-FROM-SECONDS / STANDARD-COMPARE, or of the LOCALE keyword in LOWER-CASE / UPPER-CASE / NUMVAL-C / TEST-NUMVAL-C, when the A.4.9 module is not claimed | loud compile-time rejection (planned COBOLNET1518) naming the element + citing A.4.9 and §4.2.7/A.4.1; the non-LOCALE forms of LOWER-CASE / UPPER-CASE / NUMVAL-C / TEST-NUMVAL-C keep working (they are NOT optional) | A.4.1: 'An implementation shall accept the syntax and provide the functionality for an optional element only when support for that language element is claimed by the implementor'; §4.2.7 documented-non-support sentence; A.4.9 items 2-6, 11-13 |

### ⚠ Gotchas (trust these over the phase doc)

- VERIFIED: the locale module IS A.4.9 ('Locale support and related functions', spec line 40379) — the plan's annex number is correct.
- STANDARD-COMPARE IS listed in A.4.9 (item 11) — but it is NOT locale-dependent semantically: it uses an ISO/IEC 14651:2020 cultural ordering table via the SPECIAL-NAMES ORDER TABLE clause (§12.3.7 GR17) and ordering-name, never a locale category. It additionally has an INDEPENDENT non-support route: A.3 Processor-dependent list item 25 — 'The implementor need not accept the syntax or set the EC-ORDER-NOT-SUPPORTED exception condition to exist when support for ISO/IEC 14651:2020 is not provided.' Cite A.3 item 25 (not only A.4.9) for STANDARD-COMPARE and ORDER TABLE.
- The ORDER TABLE clause is NOT listed in A.4.9 (item 10 lists only the LOCALE clause + LOCALE phrases of ALPHABET) — its only Annex-A home is A.3 item 25. Dispose of ORDER TABLE via A.3 item 25, and of STANDARD-COMPARE via BOTH A.4.9 item 11 and A.3 item 25.
- SPEC ANOMALY (LOUD): NUMVAL-C's LOCALE keyword appears NOWHERE in Annex A — A.4.9 item 12 covers only TEST-NUMVAL-C's LOCALE keyword (15.94), and there is no NUMVAL-C row anywhere in A.3/A.4 (verified by grep over the whole annex region). Strictly read, NUMVAL-C LOCALE is not declared optional — but it is inoperable without the optional SPECIAL-NAMES LOCALE clause / SET locale machinery (A.4.9 items 9-10), and TEST-NUMVAL-C AR1 defers to NUMVAL-C's argument rules. Treat as a spec list omission: reject NUMVAL-C LOCALE with the same diagnostic, documenting the reasoning (A.4.9 items 9/10/12 + §15.68.3 AR5a dependence on SPECIAL-NAMES locale association).
- Three DISTINCT LOCALE-argument syntax shapes — do not unify them: (a) LOCALE-COMPARE/LOCALE-DATE/LOCALE-TIME/LOCALE-TIME-FROM-SECONDS take a bare positional [ locale-name-1 ], no keyword; (b) LOWER-CASE/UPPER-CASE take [ LOCALE locale-name-1 ] — locale-name REQUIRED after the keyword; (c) NUMVAL-C/TEST-NUMVAL-C take [ LOCALE [ locale-name-1 ] ] — locale-name OPTIONAL (bare LOCALE = current locale), and the LOCALE phrase is a stacked ALTERNATIVE to argument-2 (currency string), followed by optional ANYCASE.
- Exhaustive LOCALE-phrase list across clause 15 (verified by grep of both 'LOCALE' and 'locale-name' over lines 33670-39600): exactly 15.51, 15.52, 15.53, 15.54 (positional) + 15.57, 15.68, 15.94, 15.97 (keyword). NUMVAL (15.67), TEST-NUMVAL (15.93), NUMVAL-F, TEST-NUMVAL-F have NO locale leg (15.67 NOTE at line 37297 confirms by pointing users to NUMVAL-C LOCALE).
- §15.6 summary-table defects (individual sections govern): the LOWER-CASE (line 34250) and UPPER-CASE (line 34337) rows OMIT the LOCALE keyword argument entirely; line 34247 misspells the function as 'LOCAL-TIME-FROM-SECONDS'; the LOCALE-DATE row (34245) garbles its description ('format specified by argument-2 and a locale identified by argument-2' — actually locale field d_fmt per §15.52.4 rule 2).
- Spec-internal name inconsistency for the default ordering table: §15.85.3 AR5 prints 'ISO 14651_2020_TABLE1' (space after ISO) while §12.3.7 NOTE 5 prints 'ISO_14651_2020_TABLE1' (underscore). ISO/IEC 14651's own name uses the underscore; treat the §15.85 spelling as a transcription artifact.
- A.4.1 (line 40236) makes the whole rule-closure optional, not just the listed syntax: 'Any associated syntax rules, general rules, other rules, exception conditions, and I-O status values are also optional, even if not explicitly listed' — so the EC-LOCALE family and EC-ORDER-NOT-SUPPORTED (A.4.9 item 1) need not be raisable/TURN-able when the module is not claimed.
- Non-support is only conforming when DOCUMENTED: §4.2.7 requires the user documentation to identify claimed (and, for partial support, unclaimed) elements — the disposition commit should update user-facing docs (e.g. the conformance/optional-modules ledger) in the same change set, per process rule 4.
- A.4.9 items 7-10 mean the locale module also spans NON-intrinsic surface the disposition must cover: OBJECT-COMPUTER CHARACTER CLASSIFICATION (12.3.6), PICTURE format 2 locale editing (13.18.40, EC-LOCALE-SIZE at line 20907), SET formats 11/12 set-locale/save-locale (14.9.39, EC-LOCALE-INVALID-PTR/EC-LOCALE-MISSING at lines 31639/31660), and the SPECIAL-NAMES LOCALE clause + LOCALE phrases in ALPHABET (12.3.7).
- LOWER-CASE/UPPER-CASE subtlety if only the LOCALE keyword is rejected: their returned-value rules 3-4 still reference a 'locale in effect for character classification' via 12.3.6 (itself A.4.9 item 7) — with the whole module unclaimed, only rule 4 (implementor-defined correspondence) is reachable, which is the current invariant-culture behavior.

## `spec:tier-c-spec` — Tier-C spec basis (§13.18.44 REDEFINES / USAGE GR / SAME RECORD AREA)

### Sections found

| § | Title | spec line |
|---|---|---|
| 13.18.44 | REDEFINES clause | 21470 |
| 13.18.44.3 | REDEFINES Syntax rules (SR1-SR17) | 21487 |
| 13.18.44.4 | REDEFINES General rules (GR1-GR3) | 21538 |
| 13.18.60 | USAGE clause | 22636 |
| 13.18.60.3 | USAGE Syntax rules (SR1-SR21) | 22712 |
| 13.18.60.4 | USAGE General rules (GR1-GR22+) | 22769 |
| 12.4.6.4 | SAME clause (covers SAME RECORD AREA; formats 1-3) | 15938 |
| 12.4.6.4.4 | SAME clause General rules (GR2 = record-area format) | 15996 |
| 13.16.3 | Data description entry Syntax rules (REDEFINES clause-combination bans) | 17207 |
| 13.18.45 | RENAMES clause (SR8 = pointer/object/strong-typed exclusion from range) | 21553 |
| 13.18.22 | EXTERNAL clause (SR at line 18693: not for class object or pointer) | 18672 |

### Semantics

ALL § numbers verified against E:/COBOL/specs/ISO_COBOL.md (ISO/IEC 1989:2023).

== §13.18.44 REDEFINES clause (line 21470) ==
General (13.18.44.1): "The REDEFINES clause allows the same computer storage area to be described by different data description entries."

STRONG-TYPING + POINTER/OBJECT BANS — the plan's SR12/14 claim is CORRECT, and these SAME rules also answer the pointer/OBJECT REFERENCE question:
- SR12 (line 21521): "The REDEFINES clause shall not be specified for a data item of class object, message-tag, or pointer or a strongly-typed group item." [the SUBJECT of the entry cannot be pointer/object/message-tag/strong group]
- SR14 (line 21525): "Data-name-2 shall not be of class object, message-tag, or pointer, a strongly-typed group item, or an item subordinate to a strongly-typed group item." [the REDEFINED item cannot be any of those either]

VARIABLE-LENGTH / ODO BANS — the plan's SR5/17 claim is CORRECT:
- SR5 (line 21497): "The data description entry for data-name-2 shall not contain an OCCURS clause. However, data-name-2 may be subordinate to an item whose data description entry contains an OCCURS clause. In this case, the reference to data-name-2 in the REDEFINES clause shall not be subscripted. Neither the original definition nor the redefinition shall include an occurs-depending table."
- SR17 (line 21533): "Neither data-name-2 nor the subject of the entry shall be a variable-length group or a dynamic-length elementary item." + NOTE 3: "REDEFINES can however be specified in an entry subordinate to a variable-length group or a dynamic-capacity table."

Other load-bearing SRs: SR2 level-numbers identical, not 66/88. SR3: not in level 1 file-section entries nor under an FD with FORMAT clause. SR4: no lower-level entry between data-name-2 and the subject. SR6: data-name-2 not qualified. SR7: multiple redefinitions each name the ORIGINAL definer. SR8: subject shall not be LARGER than data-name-2 "unless the data item referenced by data-name-2 has been specified with level number 1 and without the EXTERNAL clause." SR9: no VALUE in the entry or subordinates except level-88. SR10: new descriptions follow without intervening new-storage entries. SR11: data-name-2 may itself be subordinate to a REDEFINES entry. SR13: data-name-2 shall not contain CONSTANT RECORD. SR15: "The description of the subject of the entry shall be such that its required alignment is the same as the alignment of the data item referenced by data-name-2." (NOTE 2: implementations can add alignment requirements). SR16: data-name-2 not ANY LENGTH.

GRs: GR1 (line 21540): "Storage association for the subject of the entry starts at the first bit of the data item referenced by data-name-2 and continues over an area sufficient to contain the number of bits required by the data item referenced by the subject of the entry. If the subject of the entry requires more bits than the data item referenced by data-name-2, the storage area allocated for the data item referenced by data-name-2 and the subject of the entry is the number of bits required by the data item referenced by the subject of the entry. The size used for references to the data item referenced by data-name-2 is not changed." GR2: any of the data-names describing the same storage may reference it. GR3: VALIDATE uses each redefinition independently (PRESENT WHEN may select).

== §13.18.60 USAGE clause GR4 (line 22785) ==
GR4 is BINARY-SPECIFIC, not a general rule: "The USAGE BINARY clause specifies that a radix of 2 is used to represent a numeric item in the storage of the computer. Each implementor specifies the precise effect of the USAGE BINARY clause upon the alignment and representation of the data item in the storage of the computer, including the representation of any algebraic sign. Sufficient computer storage shall be allocated by the implementor to contain the maximum range of values implied by the associated decimal picture character-string."
The GENERAL representation rule is GR2 (line 22773): "The USAGE clause specifies the manner in which a data item is represented in the storage of a computer. It does not affect the use of the data item..." Parallel implementor-defined-representation language appears in GR6 (COMPUTATIONAL), GR7 (DISPLAY: "Each implementor shall specify the size and representation of characters stored for usage DISPLAY"), GR8 (NATIONAL), GR9 (MESSAGE-TAG), GR10 (INDEX), GR11 (PACKED-DECIMAL). So the 'representation is implementor-defined' basis for a Tier-C byte codec is GR4+GR6+GR7+GR8+GR11 collectively, anchored by GR2.

== SAME RECORD AREA: §12.4.6.4 SAME clause, General rules §12.4.6.4.4, GR2 (line 16000) ==
Verified — GR2 is the record-area format rule with the exact 'implicit redefinition ... leftmost byte' text: "A record-area format SAME clause specifies that two or more files referenced by file-name-1, file-name-2 are to share a memory area for processing the current logical record. All of these files may be in the open mode at the same time, except that only one file that is also specified in a file-area format SAME clause may be open at that time. A logical record in the shared memory area is a logical record of each file open in the output mode and of the most recently-read file open in the input mode. This is equivalent to an implicit redefinition of the area with records aligned on the leftmost byte position. The record area is available to the runtime element when any file connector referenced by file-name-1, file-name-2, ... is open. When none of the file connectors is open, the record area is not available to the runtime element."

== POINTER / OBJECT REFERENCE redefinition & storage sharing — the paths are ALL closed by named rules ==
1) REDEFINES: §13.18.44 SR12 (subject) + SR14 (data-name-2) — quoted above. No pointer/object/message-tag item on either side.
2) RENAMES: §13.18.45 SR8 (line 21586): "None of the items within the range, including data-name-2 and data-name-3, if specified, shall be of class object, message-tag, or pointer, a strongly-typed group item, an item subordinate to a strongly-typed group item, a variable-length data item, or an occurs-depending table."
3) Group-containment loophole is closed by USAGE §13.18.60.3 SR14 (line 22752): "A USAGE clause with the MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, or PROGRAM-POINTER phrase may be specified only for an elementary data item at level 1 or an elementary data item subordinate to a type declaration that includes the STRONG phrase." Derivation: a pointer either IS a level-1 elementary item (class pointer → REDEFINES SR12/14 bar it directly) or lives inside a STRONG typedef (any containing group is strongly-typed → the strong-group legs of SR12/14 + RENAMES SR8 bar it). Hence NO conforming program can overlay pointer/object storage via REDEFINES or RENAMES.
4) EXTERNAL sharing: §13.18.22 SR (line 18693): "The EXTERNAL clause shall not be specified for a data item of class object or pointer."
5) Initialization corroboration (§13.18.63.4 GR4c): when VALUE clauses take effect, "data items of class message-tag, class object, and class pointer are initialized to null"; §13.16.3 SR10 (line 17237): "The VALUE clause shall not be specified for data items of class index, message-tag, object, or pointer."
6) OBJECT REFERENCE in file section: USAGE §13.18.60.3 SR15 (line 22754): "The USAGE OBJECT REFERENCE clause shall not be specified in the file section." (No equivalent explicit file-section ban found for POINTER — see gotchas re: SAME RECORD AREA.)

== Clause-combination bans relevant to Tier-C classification (§13.16.3, line 17207) ==
SR3 (line 17215): "The REDEFINES clause shall not be specified in the same data description entry as the BASED, CONSTANT RECORD, or TYPEDEF clause." SR5 (line 17219): "The EXTERNAL clause shall not be specified in the same data description entry as the REDEFINES or BASED clause." §13.18.63.3 VALUE SR12: no VALUE in an entry containing REDEFINES or subordinate to one. Also §12.4.6.4.2 note: report files may appear only in file-area SAME (SR5 of §12.4.6.4.3); sort/merge files never in file-area SAME (SR6).

### Hand-derived expected values (golden material)

| Input | Expected | Rule |
|---|---|---|
| 01 P USAGE POINTER. 01 Q REDEFINES P PIC X(8). | Compile-time REJECT: the redefined item (data-name-2) is class pointer | §13.18.44 SR14: 'Data-name-2 shall not be of class object, message-tag, or pointer, a strongly-typed group item, or an item subordinate to a strongly-typed group item.' |
| 01 A PIC X(8). 01 P2 REDEFINES A USAGE POINTER. | Compile-time REJECT: the subject of the REDEFINES entry is class pointer | §13.18.44 SR12: 'The REDEFINES clause shall not be specified for a data item of class object, message-tag, or pointer or a strongly-typed group item.' |
| 01 T-STRONG TYPEDEF STRONG. ... 01 G TYPE T-STRONG. 01 H REDEFINES G PIC X(n). | Compile-time REJECT: data-name-2 is a strongly-typed group item | §13.18.44 SR14 (strongly-typed group leg); mirror case (strong group as subject) rejects under SR12 |
| 01 R. 05 N PIC 9. 05 T PIC X OCCURS 1 TO 5 DEPENDING ON N. 01 R2 REDEFINES R PIC X(6). | Compile-time REJECT: the original definition includes an occurs-depending table | §13.18.44 SR5: 'Neither the original definition nor the redefinition shall include an occurs-depending table.' |
| 01 D PIC X DYNAMIC LENGTH. 01 E REDEFINES D PIC X(4). | Compile-time REJECT: data-name-2 is a dynamic-length elementary item (same for a variable-length group on either side) | §13.18.44 SR17: 'Neither data-name-2 nor the subject of the entry shall be a variable-length group or a dynamic-length elementary item.' |
| 01 G OCCURS 5 TIMES PIC X. 01 H REDEFINES G PIC X(5). (data-name-2 itself has OCCURS) | Compile-time REJECT: data-name-2's entry contains an OCCURS clause | §13.18.44 SR5 first sentence: 'The data description entry for data-name-2 shall not contain an OCCURS clause.' |
| WORKING-STORAGE: 01 A PIC X(4). 01 B REDEFINES A PIC 9(4). MOVE '1234' TO A; DISPLAY B | Displays 1234 — A and B name the same storage starting at the same first bit | §13.18.44 GR1 ('Storage association ... starts at the first bit of the data item referenced by data-name-2') + GR2 (either data-name references that storage) |
| 01 A PIC X(2) (level 1, no EXTERNAL). 01 B REDEFINES A PIC X(10). | LEGAL; allocated area grows to 10 bytes; references to A still use size 2 | §13.18.44 SR8 (larger subject allowed only when data-name-2 is level 1 without EXTERNAL) + GR1 last two sentences ('the storage area allocated ... is the number of bits required by the ... subject'; 'The size used for references to the data item referenced by data-name-2 is not changed.') |
| 05 A PIC X(2). 05 B REDEFINES A PIC X(10). (non-level-1 data-name-2, subject larger) | Compile-time REJECT: subject larger than data-name-2 and data-name-2 is not level 1 | §13.18.44 SR8 |
| 01 A PIC X(4). 01 B REDEFINES A PIC 9(4) VALUE 5. | Compile-time REJECT: VALUE (non-88) inside a REDEFINES entry | §13.18.44 SR9 + §13.18.63.3 VALUE SR12 |
| SAME RECORD AREA FOR F1 F2; both OPEN OUTPUT; MOVE 'ABCDEF' TO REC-OF-F1 (PIC X(6)); DISPLAY REC-OF-F2 (PIC X(4)) | Displays ABCD — one shared record area, records overlaid leftmost-byte-aligned; a record written into it is simultaneously a logical record of every output-mode file in the clause | §12.4.6.4.4 GR2: 'This is equivalent to an implicit redefinition of the area with records aligned on the leftmost byte position.' |
| SAME RECORD AREA FOR F1 F2; neither file open; reference REC-OF-F1 | Record area NOT available to the runtime element (unavailable-data condition) | §12.4.6.4.4 GR2 last sentences: 'The record area is available ... when any file connector ... is open. When none of the file connectors is open, the record area is not available to the runtime element.' |
| 01 X REDEFINES Y ... where Y's entry has BASED (or the same entry has BASED/CONSTANT RECORD/TYPEDEF, or EXTERNAL) | Compile-time REJECT: forbidden clause combination | §13.16.3 SR3 (REDEFINES not with BASED/CONSTANT RECORD/TYPEDEF in same entry) + §13.16.3 SR5 (EXTERNAL not with REDEFINES or BASED); also §13.18.44 SR13 (data-name-2 shall not contain CONSTANT RECORD) |
| 66 R RENAMES A THRU B where the A..B range contains a USAGE POINTER item (via strong typedef) | Compile-time REJECT: pointer-class item within a RENAMES range | §13.18.45 SR8 (line 21586): range items shall not be 'of class object, message-tag, or pointer, a strongly-typed group item, an item subordinate to a strongly-typed group item, a variable-length data item, or an occurs-depending table' |

### ⚠ Gotchas (trust these over the phase doc)

- USAGE 'GR4 = representation implementor-defined' is IMPRECISE: §13.18.60.4 GR4 is the USAGE BINARY rule specifically ('Each implementor specifies the precise effect of the USAGE BINARY clause upon the alignment and representation...'). The general representation statement is GR2; the same implementor-defined language recurs per-usage in GR6 (COMPUTATIONAL), GR7 (DISPLAY char size/representation), GR8 (NATIONAL), GR9 (MESSAGE-TAG), GR10 (INDEX), GR11 (PACKED-DECIMAL). A Tier-C codec doc should cite GR2 + the per-usage GR for each usage it encodes, not GR4 alone.
- The SAME RECORD AREA rule is NOT its own §: it is Format 2 of §12.4.6.4 'SAME clause'; §12.4.6.4.4 is that clause's General rules subsection and GR2 is the record-area-format rule. The plan's citation '§12.4.6.4.4 ... GR2' resolves correctly, but name it 'SAME clause GR2 (record-area format)'.
- REDEFINES SR12/14 do MORE than ban strong typing: they equally ban class object, message-tag, and pointer on both sides — these two SRs ARE the answer to the 'may a POINTER/OBJECT REFERENCE be redefined' question; there is no separate rule. RENAMES SR8 closes the RENAMES route, and USAGE §13.18.60.3 SR14 (pointer/object/message-tag usable only as level-1 elementary or inside a STRONG typedef) closes the 'ordinary group containing a pointer' loophole — such a group cannot exist, so SR12/14's strong-group legs cover every containment case.
- REDEFINES GR1 is stated in BITS, not bytes ('starts at the first bit ... number of bits required') — bit items (USAGE BIT) redefinition must be bit-precise in a Tier-C codec.
- SR8 asymmetry: the redefinition MAY be larger than data-name-2 when data-name-2 is level 1 and not EXTERNAL; GR1 then grows the allocated area to the subject's size while references to data-name-2 keep the original size. A codec that sizes the byte[] from the level-01 record must take max(subject sizes), not the original record size.
- SR15 (alignment of subject must equal alignment of data-name-2) + NOTE 2 permits implementor-ADDITIONAL alignment requirements — relevant if the codec introduces alignment for BINARY/POINTER-adjacent layouts.
- SR5 ODO ban is BIDIRECTIONAL ('Neither the original definition nor the redefinition shall include an occurs-depending table') — reject when the ODO is in EITHER tree, not just data-name-2's. But per SR17 NOTE 3, REDEFINES IS legal in an entry SUBORDINATE to a variable-length group or dynamic-capacity table — do not over-reject.
- SR3 bans REDEFINES in FILE SECTION level-1 entries AND in any entry subordinate to an FD carrying a FORMAT clause; additionally §13.18.24 FORMAT (line 18813) bans REDEFINES/RENAMES in record descriptions of a FORMAT-clause FD outright.
- POTENTIAL SPEC HOLE worth noting for Tier-C: SAME RECORD AREA GR2's 'implicit redefinition' is a different mechanism from the REDEFINES clause, so §13.18.44 SR12/14 do not textually govern it. USAGE §13.18.60.3 SR15 bans OBJECT REFERENCE from the file section, but I found NO explicit rule banning a level-1 USAGE POINTER record description in the file section — meaning pointer storage could in principle overlap via SAME RECORD AREA without violating any quoted rule. If the compiler bans pointers in the file section, that is an implementation choice, not a cited ISO rule (flag in the ledger rather than cite a §).
- Cross-reference hub: §13.16.3 SR13 NOTE (line 17255) enumerates the related restriction sites — OCCURS SR19/23/33, PROPERTY SR5, REDEFINES SR13, SAME AS SR10, USAGE SR4/SR21 — useful checklist for the classifier.
- SAME AS interaction (§13.18.49 GR1, line 21769): SAME AS copies the source description EXCLUDING its REDEFINES clause (among CONSTANT RECORD/EXTERNAL/GLOBAL/SELECT WHEN) — a SAME AS of a redefining item does NOT re-redefine.
- MOVE CORRESPONDING (line 27951): subordinate items with REDEFINES (or subordinate to one) are excluded from correspondence, but identifier-1 itself MAY have/inherit REDEFINES; compatibility determination (line 8364) likewise ignores REDEFINES-bearing subordinates.

## `code:catalog-binder` — Code: IntrinsicCatalog + IntrinsicBinder seams

### Findings

# Intrinsic subsystem scout report (read-only; all paths absolute)

## 1. IntrinsicCatalog.cs — E:\COBOL\src\Cobol.Net.Compiler\Binding\IntrinsicCatalog.cs (201 lines)

### Enums
- `IntrinsicType` (line 10): `{ Alphanumeric, Boolean, National, Numeric, Integer, Index }` — §15.2 function-type column. **Boolean IS present** (doc comment: Boolean → bool conceptually, but see ResultCategory below).
- `IntrinsicArity` (line 14): `{ Fixed, OptionalTrailing, Variadic }`.
- `IntrinsicBind` (line 21): `{ Runtime, Fold, Deferred }`. Runtime = a `CobolIntrinsics`/`CobolDate` call; Fold = compile-time resolution (LENGTH §15.50, WHEN-COMPILED §15.99.3 r2, the *-ALGEBRAIC folds); Deferred = catalogued (D8 edition gating + arity apply) but renders a LOUD not-implemented guard. There is NO `Unsupported` case yet (P11 Step 1 adds it).

### Row shape — `readonly record struct IntrinsicSig` (lines 33–36), ctor params IN ORDER:
1. `string Name` — COBOL function name (case-insensitive dictionary key).
2. `IntrinsicType Type` — §15.2 classification; drives `ResultCategory`.
3. `IntrinsicArity Arity`.
4. `int MinArgs`.
5. `int MaxArgs` (`int.MaxValue` = unbounded; `const int inf` at line 73).
6. `string ArgKinds` — per-position §15.3 codes: `'n'` numeric, `'i'` integer, `'s'` alphanumeric/string, `'p'` category-polymorphic (MAX/MIN family); LAST char repeats for the variadic tail. Accessor `ArgKind(int i)` lines 39–40.
7. `string RuntimeMethod` — the literal C# method name interpolated by RuntimeApi (empty for Deferred and for the name-routed folds HIGHEST/LOWEST/SMALLEST-ALGEBRAIC).
8. `IntrinsicBind Bind`.
9. `bool Float` — §15.4.1 floating-math family (compute in double, quantize via the ONE `FromDouble`).
10. `int IntroducedIn` — D8 window start (85 = the 1989 IF-module amendment).
11. `int? RemovedIn = null` — window end; only CONCATENATE sets it (2023).

### `ResultCategory` (lines 46–51) — THE result-category seam
```csharp
public PicCategory ResultCategory => Type switch {
    IntrinsicType.National => PicCategory.National,
    IntrinsicType.Alphanumeric => PicCategory.Alphanumeric,
    _ => PicCategory.Numeric,
};
```
XML doc explicitly says: "(Boolean-type rows are all Deferred; they fall to Numeric unchanged.)" — **promoting BOOLEAN-OF-INTEGER requires adding a `IntrinsicType.Boolean => PicCategory.Boolean` arm here** (PicCategory.Boolean exists, PicInfo.cs:29).

### Provisional-window comment (class doc, lines 58–61)
"Edition windows for post-85 rows beyond the 2023 seven-function delta (docs/VERSION_CHANGE_REFERENCE.md rows 65–73) are PROVISIONAL pending the version-test-matrix wave — D8 notes the 85↔2002 gating derives from the 2002 standard, which the reference doc does not yet tabulate."

### API: `IntrinsicCatalog.TryGet(string name, out IntrinsicSig sig)` line 65; table built once (`Build()` line 69, `StringComparer.OrdinalIgnoreCase`).

### Current `IntrinsicBind.Deferred` rows — exactly 17 (re-enumerated, current line numbers):
BOOLEAN-OF-INTEGER :133 (Type Boolean, "ii", 2,2) · BYTE-LENGTH :134 ("s") · DATE-TO-YYYYMMDD :139 (OptionalTrailing 1,3 "iii") · DAY-TO-YYYYDDD :140 · YEAR-TO-YYYY :141 · INTEGER-OF-BOOLEAN :161 ("s") · LOCALE-COMPARE :162 · LOCALE-DATE :163 · LOCALE-TIME :164 · LOCALE-TIME-FROM-SECONDS :165 · SECONDS-PAST-MIDNIGHT :166 · STANDARD-COMPARE :167 · TEST-DATE-YYYYMMDD :168 · TEST-DAY-YYYYDDD :169 · TEST-NUMVAL :170 · TEST-NUMVAL-C :171 (OptionalTrailing 1,2 "ss") · CONCATENATE :174 (window 2002→RemovedIn 2023).
Fold rows with empty RuntimeMethod (NOT Deferred, name-routed in the binder): HIGHEST-ALGEBRAIC :159, LOWEST-ALGEBRAIC :160, SMALLEST-ALGEBRAIC :196. LENGTH :113 carries RuntimeMethod "Length" (reused for the runtime-residue arm); WHEN-COMPILED :120 carries "WhenCompiled" (rendered as a baked constant).

## 2. IntrinsicBinder.cs — E:\COBOL\src\Cobol.Net.Compiler\Binding\Procedure\Verbs\IntrinsicBinder.cs (844 lines)

`internal sealed class IntrinsicBinder(BinderContext ctx, StatementBinder host)` :36. `internal static Func<DateTimeOffset> CompileClock` :41.

### BindIntrinsicCore flow (`private BoundExpr BindIntrinsicCore(string name, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)` :112) — exact order:
1. **UDF dispatch precedes catalog** (:119–121, §12.3.8.2 GR12) → `host.Udf.UdfBindCall`.
2. Catalog miss → **COBOLNET1501** (:126) + `BoundExprError`.
3. **Window gate** (:139–144): `sig.IntroducedIn > ctx.Edition.DialectLevel` → **COBOLNET1502**; `else if (sig.RemovedIn is { } gone && ctx.Edition.DialectLevel >= gone)` → **COBOLNET1503**. Both are non-fatal — bind continues (so arity/category checks still run).
4. Standard-arithmetic staging (:154–160): ANNUITY/PRESENT-VALUE/VARIANCE/STANDARD-DEVIATION under ARITHMETIC STANDARD/STANDARD-DECIMAL → `DiagnosticCatalog.ArithmeticStandardIntrinsic` = **COBOLNET0899** slug "arithmetic-standard-intrinsic" (DiagnosticCatalog.cs:127–135).
5. **Special-bind dispatch by NAME** (before generic arg binding): `TRIM`→`BindTrim` :164 · `FIND-STRING`→`BindFindString` :168 · `SUBSTITUTE`→`BindSubstitute` :172 · `CONVERT`→`BindConvert` :176 · `MODULE-NAME`→`BindModuleName` :180.
6. Generic `BindIntrinsicArgs(argCtxs)` :182 (body :684–693: `TryExpandAll` table(ALL) expansion :762, then `BindArgOperand` per arg) → **arity check COBOLNET1504** :185.
7. FORMATTED-*/date-format-must-be-literal check **COBOLNET1517** :196.
8. **Fold dispatch:** `sig.Bind == Fold && sig.Name == "LENGTH"` → `BindLengthFold(sig, args)` :201–202; `Fold && Name is "SMALLEST-ALGEBRAIC" or "HIGHEST-ALGEBRAIC" or "LOWEST-ALGEBRAIC"` → `BindAlgebraicFold` :206–208.
9. **NUMVAL-C default-currency injection** :213–214 (exact): `if (sig.Name == "NUMVAL-C" && args.Count == 1) args.Add(new BoundStringLiteral(ctx.Data.CurrencyString));` — §15.68.3 r3, injected at bind so SPECIAL-NAMES never reaches the backend. (Step 6's TEST-NUMVAL-C must extend this condition.)
10. MAX/MIN category-polymorphic resolution :219–232: `sig.ArgKinds == "p" && args.All(IsStringOperand)` → `resolved = sig with { RuntimeMethod = MaxString/MinString/OrdMaxString/OrdMinString }`; category → Alphanumeric for MAX/MIN only.
11. Collating flags :242–245: `collate` = CHAR/ORD ∧ `ctx.Data.Collating != null` ∧ no national arg; `collateNat` = `ctx.Data.NationalCollating != null` ∧ (CHAR-NATIONAL ∨ ORD-with-national-arg).
12. **COBOLNET0844 guard CURRENT state** :250–252: fires ONLY for `sig.Name is "CHAR" && nationalArg` — "FUNCTION CHAR takes an integer operand (§15.15.3 rule 1) — FUNCTION CHAR-NATIONAL (§15.16) is the national program-collating-sequence form". The old "CHAR-NATIONAL/ORD-over-national not yet implemented" guard is GONE (both landed in P10).
13. DISPLAY-OF/NATIONAL-OF class/category checks → `CheckRepertoireArgs` :256 (body :468–501, **COBOLNET1546**).
14. **EC gate** :260 (exact): `if (resolved.RuntimeMethod.StartsWith("Ec", StringComparison.Ordinal)) host.Ec.EcNoteFunction();` → EcBinder.cs:39 `public void EcNoteFunction() => ctx.EcState.Functions = true;` — sets the group EC gate so the generated source carries the Exceptions using. Any new Ec*-named RuntimeMethod is covered automatically.
15. `return new BoundIntrinsicCall(resolved, args, category, collate) { CollateNat = collateNat };` :262.

### Result-category decision per row
`var category = sig.ResultCategory` :220 (the catalog seam) unless overridden: all-string MAX/MIN → Alphanumeric :231; special binds pass explicit categories — BindTrim → Alphanumeric :296, BindFindString → Numeric :327, BindSubstitute → Alphanumeric :369, BindConvert → `dst == 3 ? National : Alphanumeric` per §15.19.1 :428–430, BindModuleName → Alphanumeric :537, LENGTH runtime residue → Numeric :574/:579.

### BindLengthFold (:562–584) — exact mechanism
Switch on `args[0]`:
- `BoundStringLiteral s` → `new BoundNumLiteral(Math.Max(1, s.Value.Length))` (compile-time fold).
- `BoundFieldOperand { Place: RefModPlace }` → `BoundExprError` (runtime length, staged loud).
- Group under an ODO table (`OdoModel.TableUnder(...).OccursSpec.Depending != null`) → `BoundExprError`.
- `Place.Item.IsAnyLength` → `new BoundIntrinsicCall(sig, args, PicCategory.Numeric)` — falls through to the renderer's `"Length"` arm (IntrinsicRenderer.cs:157–158) = runtime `.Length` over the carrier.
- `BoundFieldOperand f` (fixed) → `new BoundNumLiteral(Math.Max(1, f.Place.Item.ImageWidth))` :575 — **ImageWidth is CHARACTER positions** (DataItem.cs:321–325: elementary = digits + separate-sign position; group = Σ non-REDEFINES children × Occurs; P positions occupy none).
- Nested string-result intrinsic → runtime `.Length` BoundIntrinsicCall.
- Anything else (numeric/figurative literal) → `BoundExprError` citing §15.50.3.

**Where byte geometry for BYTE-LENGTH would come from:** there is NO existing byte-width authority. `PicInfo.StorageWidth` (PicInfo.cs:301–312) gives bytes ONLY for Packed (Digits/2+1), Binary/Comp5 (1/2/4/8 by digits), BinaryChar/Short/Long/Double (1/2/4/8) — and returns **0 for DISPLAY/national/everything else** ("else 0 — unused"). A `BindByteLengthFold` must compose: DISPLAY = ImageWidth×1, national = ImageWidth×2 (D-N1), binary/packed = StorageWidth, groups = element-wise sum. Note DiagnosticCatalog.cs:120–124: `CONSTANT … AS BYTE-LENGTH OF` (COBOLNET0899 "constant-byte-length") is staged loud pending exactly this — "the byte-width authority lands ONCE, with it (the singular-pattern rule)" — so P11 Step 7 should also unblock that constant leg or at least be designed as the ONE authority.

### KeywordWordOf (:734–739) — how TRIM detects LEADING/TRAILING
`private static string? KeywordWordOf(Core.FunctionArgumentContext a)` — returns the UPPERCASE text of a `fnArgPhraseWord()` (reserved phrase words in the arg grammar) OR of a bare, unqualified, unsubscripted sole data reference (`SoleDataReference` :743–750 unwraps arithmeticExpression→additive→multiplicative→power→unary→primary→dataReference with no operators); null otherwise. `BindTrim` (:271–297) loops args, consumes "LEADING"/"TRAILING" into `TrimMode` (0 both/1 LEADING/2 TRAILING → `BoundIntrinsicCall.TrimMode`), everything else → `BindArgOperand`; TRIM arg-2 below --std 2023 → COBOLNET1502 :292–295. Same pattern used by FIND-STRING (LAST/ANYCASE/START/AFTER :312–318), SUBSTITUTE (ANYCASE/FIRST/LAST :349–353), CONVERT (`IsConvertFormatWord` :504–505), MODULE-NAME (:517–518). An unmatched word falls back to the operand bind so data-names are never swallowed. **This is the mechanism Step 8's LOCALE-phrase detection reuses.**

### BindArgOperand (:701–713, internal — also reached back from UdfBinder)
OMITTED → COBOLNET1544 error operand; unconsumed `fnArgPhraseWord` → `BoundOperandError`; nonNumericLiteral → `NonNumericOperand` (:717–727: concat fold via `host.Expr.ConcatOperand`, figurative, STRINGLIT → `BoundStringLiteral`, NATLIT → `host.Expr.NationalLiteralOperand`, **BOOLLIT → `host.Expr.BooleanLiteralOperand`**); else `OperandOf(host.Expr.BindExpr(a.arithmeticExpression()))`.

## 3. Renderer — E:\COBOL\src\Cobol.Net.Compiler\CodeGen\Emit\IntrinsicRenderer.cs
`internal sealed class IntrinsicRenderer(EmitContext ctx, NumericRenderer num)` :33.
- `public NumX RenderNum(BoundIntrinsicCall ic)` :49. Deferred/empty-RuntimeMethod loud guard :52–53 (`EmitText.LoudValue("long", $"FUNCTION {sig.Name} (catalogued, not yet implemented)")`); string-class-in-numeric-context guard :54–55; Float family → `RenderFloat` :57 (:197–215 — doubles + `FromDouble` at ws=max(Receiver.Scale,9); Real receiver stays binary64). Then `switch (sig.RuntimeMethod)` :59–192 with a loud default :190–191. MEAN is special: catalog says "MeanScaled" but the renderer computes SumScaled + one division (SDIDI branch under StandardDecimal :116–118, else NumDivide :121–126) — **there is NO CobolIntrinsics.MeanScaled runtime body; "MeanScaled" is a dispatch key only**.
- `public string RenderString(BoundIntrinsicCall ic)` :269. Deferred guard :272–273; `switch (sig.RuntimeMethod)` :274–329, loud default :328. EC arms map EcStatus→EcFn("Status") :318 … EcFileN :325–327 (arg forms loud, VCR 68/69). WHEN-COMPILED renders the baked `WhenCompiledStamp` constant :287 (Lazy, :43–44).
- Arg helpers: `Arg`/`Dbl`/`IntArg` (:219–230, AsInt truncates via NumRescale), `AlignedArgs(Ex)` (:234–248), `StrArgList` :250, `CommaFlag` :252 (DECIMAL-POINT IS COMMA), `Collate(ic)` :259–260 (appends `, __COLLATE` / `, __COLLATE_NAT`).
- String-channel arg helpers: `ArgNum` under `ReceiverContext.None` :375, `ArgInt` :379, `Str` :384 via exhaustive `StrArgVisitor` :390–403 — note `Visit(BoundBoolOperand n) => Loud(n)` :402 (a boolean operand in a string-arg position is currently loud).
- RuntimeApi seams (E:\COBOL\src\Cobol.Net.Compiler\CodeGen\Roslyn\RuntimeApi.cs): `Intrinsic(method, args)` :555–556 → `CobolIntrinsics.{method}({args})`; `DateFn` :559–560 → `CobolDate.{method}`; `EcFn(method, args="")` :563–564 → `EcFunctions.{method}`; `ModuleNameFn(int kind)` :567 → `CobolModule.Name(kind)` (runtime at Runtime\Control\CobolModule.cs:25); boolean: `BoolNot` :24, `BoolOp` :29, `BoolOpAll` :32, `BoolOpName(char)` :37 (nameof-anchored to CobolBool.And/Or/Xor).

### Seams a NEW intrinsic must touch (checklist)
1. Catalog row flip/add — IntrinsicCatalog.cs (Bind, RuntimeMethod, window). 2. If Runtime: a runtime body (CobolIntrinsics partial / CobolDate / EcFunctions) + a `RenderNum` or `RenderString` case keyed on the exact RuntimeMethod string. 3. If Fold: a `BindXxxFold` beside BindLengthFold (:562) + a name-routed dispatch line in BindIntrinsicCore (:201–208 pattern). 4. If phrase-keyword shaped: a `BindXxx` special-bind + a `BoundIntrinsicCall` init-property (BoundTree.cs:150–190 — TrimMode/FindLast/FindAnycase/SubstituteModes/Convert*/ModuleNameKind precedent) + grammar phrase words already arrive via `fnArgPhraseWord`/bare-word (no grammar change needed for IDENTIFIER-shaped words like LOCALE). 5. If result category ≠ Numeric: catalog `Type` (+ ResultCategory arm for Boolean). 6. If Ec*-named: EC gate is automatic (:260). 7. Conformance golden + window negative in the same commit.

## 4. BoundIntrinsicCall — E:\COBOL\src\Cobol.Net.Compiler\Binding\Bound\BoundTree.cs:150–190
`public sealed record BoundIntrinsicCall(IntrinsicSig Sig, IReadOnlyList<BoundOperand> Args, PicCategory ResultCategory, bool Collate = false) : BoundExpr` with init props: `CollateNat` :157, `TrimMode` :161, `FindLast` :165, `FindAnycase` :169, `SubstituteModes` :174, `ConvertSource` :178, `ConvertDest` :182, `ConvertDestHex` :185, `ModuleNameKind` :189. Args are BoundOperands, NOT BoundExprs.

## 5. Boolean representation in the greenfield (the P10 base)
- **D-B1:** a boolean value IS a '0'/'1' character string; `PicCategory.Boolean` (PicInfo.cs:29 — PIC 1 [USAGE BIT], one alphanumeric char per boolean position; bit-packing is named future residue).
- **Runtime:** `CobolBool` (E:\COBOL\src\Cobol.Net.Runtime\Values\Text\CobolBool.cs) — `And/Or/Xor(string?, string?)` (§8.8.2 r9 right-zero-extension, r10 max-length result), `Not` :39, `Equal` :50 (equality-only), `IsTrue` :62 (§8.8.4.3.4 GR1), `Resize(v, width)` :67 (the §14.9.8 GR3 boolean-store discipline — left-align, right-zero-fill/truncate), `AndAll/OrAll/XorAll/EqualAll` :79–86 (figurative ALL forms). All string-in/string-out.
- **Bound tree:** `BoundBoolExpr` abstract (BoundTree.cs:249) → `BoundBoolLiteral(string Bits)` :252, `BoundBoolRef(Place)` :255, `BoundBoolAll(string Bits)` :260, `BoundBoolBinary(BoundBoolExpr, char Op, BoundBoolExpr)` :264, `BoundBoolNot` :267, `BoundBoolError` :270. Bridges: `BoundBoolOperand(BoundBoolExpr)` : BoundOperand :275 (relation operand); `BoundBooleanCondition(BoundBoolExpr)` : BoundCondition :294. A boolean LITERAL as an intrinsic ARG arrives as `BoundStringLiteral { Category = PicCategory.Boolean }` (ExpressionBinder.BooleanLiteralOperand, ExpressionBinder.cs:92–101, 8191-cap COBOLNET0814).
- **Binder:** ConditionBinder owns §8.8.2 — `MakeBoolBinary` :78–84 (rule-4 COBOLNET1511), `BindBoolOperandValue` :89–121 admits ONLY boolean literals / figurative ZERO / ALL B"…" / category-boolean items — **a FUNCTION call is NOT currently an admissible §8.8.2 boolean operand** (it would fail the sole-data-ref arm → COBOLNET1511). `Gr3Width` :144 computes the COMPUTE-store width.
- **Renderer:** `BooleanRenderer` (CodeGen\Emit\BooleanRenderer.cs, static) renders BoundBoolExpr → C# string exprs over CobolBool via the generated `IBoundBoolExprVisitor` (Render :20; a category-boolean item reads as its string via `PlaceRenderer.Read` :25).
- **How a Boolean-result intrinsic behaves TODAY:** BOOLEAN-OF-INTEGER is the only `IntrinsicType.Boolean` row and is Deferred; its ResultCategory degrades to Numeric (catalog :45–51), so it hits RenderNum's loud guard. **To promote (P11 Step 2):** the natural carrier is a '0'/'1' string — (a) add `Boolean => PicCategory.Boolean` to ResultCategory; (b) render through the STRING channel (`RenderString` arm — the value is a string, same as national rides the string channel); (c) consumers classify by `ic.ResultCategory`: MoveBinder.cs:162 (`BoundComputedOperand { Expr: BoundIntrinsicCall ic } => ic.ResultCategory` — MOVE Table-16 legality will see Boolean), IntrinsicBinder.OperandCategory :441–449 and IsStringOperand :542–553 (already treats category-Boolean fields as string operands); (d) if a FUNCTION result must participate in a §8.8.2 boolean expression or a boolean condition, ConditionBinder.BindBoolOperandValue (:89–121) and IsBooleanValueOperand (:127–140) need a new intrinsic arm — none exists; (e) IntrinsicRenderer.StrArgVisitor currently renders `BoundBoolOperand` loud (:402) — INTEGER-OF-BOOLEAN taking a boolean ITEM works via BoundFieldOperand (string read), but a nested Boolean-result intrinsic as an argument flows as `BoundComputedOperand{BoundIntrinsicCall}` whose Visit arm (:395–397) only accepts Alphanumeric/National result categories — extend to Boolean.

## 6. Full list of currently-implemented RuntimeMethod names (catalog key → runtime body)
**CobolIntrinsics.Float.cs** (all `double`): Acos, Asin, Atan, Cos, Sin, Tan, Sqrt, Log, Log10, Exp, Exp10, E, Pi, Annuity, PresentValue, Variance, StandardDeviation, Random (0-arg :83 + seeded :89).
**CobolIntrinsics.cs**: FromDouble :30 (quantizer — not a row).
**CobolIntrinsics.Exact.cs**: Factorial :32, SignOf :43, Floor :47, Truncate :56, AbsScaled :59, FractionPart :63, ModScaled :70, RemScaled :81, MaxScaled :88, MinScaled :96, SumScaled :104, RangeScaled :112, MedianScaled :117, MidrangeScaled :126, OrdMax :130, OrdMin :139, MaxString :151, MinString :159, OrdMaxString :167, OrdMinString :176, Numval :194, NumvalF :246, TestNumvalF :289, NumvalC :334. ("MeanScaled" has NO body — renderer-composed from SumScaled + divide.)
**CobolIntrinsics.Text.cs**: Char :18/:34 (plain/weights), CharNational :52/:66 (plain/NationalCollation), Ord :84/:90/:97 (plain/national/weights), UpperCase :107, LowerCase :110, Reverse :113, Length :124, Concat :130, BaseConvert :137, Convert :174, DisplayOf :223, NationalOf :230, Substitute :293, FindString :346, Trim :364.
**CobolDate.cs**: Format21 :24 (helper), CurrentDate :32, DateOfInteger :36, DayOfInteger :45, IntegerOfDate :55, IntegerOfDay :66, FormattedDate :222, FormattedTime :232, FormattedDatetime :237, FormattedCurrentDate :247, IntegerOfFormattedDate :342, SecondsFromFormattedTime :352, TestFormattedDatetime :362, CombinedDatetime :365 (returns Int128).
**EcFunctions.cs** (Runtime\Exceptions; catalog names EcStatus/EcLocation/EcLocationN/EcStatement/EcFile/EcFileN map via renderer to): Status :20, Location :27, Statement :32, File :44, FileN :63 (= `CobolIntrinsics.NationalOf(File())`), LocationN :70.
**CobolModule.cs**: Name(int kind) :25 (MODULE-NAME).

## 7. Diagnostics inventory touched by this subsystem
COBOLNET1501 not-an-intrinsic :126 · **COBOLNET1502 introduced-later** :140 (also TRIM-arg2 :293) · **COBOLNET1503 removed** :143 · COBOLNET1504 arity :185 (+ special binds) · COBOLNET1514 CONVERT SRs :402–420 · COBOLNET1515 MODULE-NAME NESTED :535 · COBOLNET1516 algebraic-fold arg/float :611/:659 · COBOLNET1517 literal-format :196 · COBOLNET1543 malformed keyword-omitted args :104 · COBOLNET1544 OMITTED :705 · COBOLNET1546 repertoire args :473–500 · COBOLNET0844 CHAR-with-national-arg :251 · COBOLNET0899 (slugs "arithmetic-standard-intrinsic" DiagnosticCatalog.cs:127, "constant-byte-length" :117–124). COBOLNET1518 (A.4.9) does NOT exist yet — P11 Step 8 allocates it.

### Key signatures / anchors

- `src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs:33 — public readonly record struct IntrinsicSig(string Name, IntrinsicType Type, IntrinsicArity Arity, int MinArgs, int MaxArgs, string ArgKinds, string RuntimeMethod, IntrinsicBind Bind, bool Float, int IntroducedIn, int? RemovedIn = null)`
- `src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs:10 — public enum IntrinsicType { Alphanumeric, Boolean, National, Numeric, Integer, Index }`
- `src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs:21 — public enum IntrinsicBind { Runtime, Fold, Deferred }  (no Unsupported yet)`
- `src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs:46-51 — public PicCategory ResultCategory (National/Alphanumeric else Numeric; Boolean falls to Numeric — THE seam for BOOLEAN-OF-INTEGER)`
- `src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs:65 — public static bool TryGet(string name, out IntrinsicSig sig)`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs:112 — private BoundExpr BindIntrinsicCore(string name, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs:139-144 — the D8 window gate: COBOLNET1502 (IntroducedIn > DialectLevel) / COBOLNET1503 (RemovedIn), after catalog lookup, before special binds`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs:213-214 — NUMVAL-C injection: if (sig.Name == "NUMVAL-C" && args.Count == 1) args.Add(new BoundStringLiteral(ctx.Data.CurrencyString));`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs:250-252 — COBOLNET0844 current state: fires only for CHAR with a national argument (§15.15.3 r1)`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs:260 — EC gate: if (resolved.RuntimeMethod.StartsWith("Ec", StringComparison.Ordinal)) host.Ec.EcNoteFunction();`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs:562 — private BoundExpr BindLengthFold(IntrinsicSig sig, List<BoundOperand> args)  (folds f.Place.Item.ImageWidth = CHARACTER positions; ref-mod/ODO loud; ANY LENGTH + nested-string → runtime .Length)`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs:594 — private BoundExpr BindAlgebraicFold(IntrinsicSig sig, List<BoundOperand> args)  (the Fold precedent for BYTE-LENGTH; uses pic.StorageWidth at :634)`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs:734-739 — private static string? KeywordWordOf(Core.FunctionArgumentContext a)  (phrase-word detection: fnArgPhraseWord OR bare sole dataReference, uppercased; LOCALE detection reuses this)`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs:701 — internal BoundOperand BindArgOperand(Core.FunctionArgumentContext a)`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs:271 — private BoundExpr BindTrim(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)  (TrimMode 0/1/2; arg-2 <2023 → COBOLNET1502)`
- `src/Cobol.Net.Compiler/Binding/Bound/BoundTree.cs:150-151 — public sealed record BoundIntrinsicCall(IntrinsicSig Sig, IReadOnlyList<BoundOperand> Args, PicCategory ResultCategory, bool Collate = false) : BoundExpr  (+init props TrimMode/FindLast/FindAnycase/SubstituteModes/Convert*/ModuleNameKind/CollateNat)`
- `src/Cobol.Net.Compiler/CodeGen/Emit/IntrinsicRenderer.cs:49 — public NumX RenderNum(BoundIntrinsicCall ic)  (Deferred loud guard :52-53; switch on sig.RuntimeMethod :59-192)`
- `src/Cobol.Net.Compiler/CodeGen/Emit/IntrinsicRenderer.cs:269 — public string RenderString(BoundIntrinsicCall ic)  (Deferred loud guard :272-273; switch :274-329)`
- `src/Cobol.Net.Compiler/CodeGen/Roslyn/RuntimeApi.cs:555-556 — public static string Intrinsic(string method, string args) => $"{nameof(CobolIntrinsics)}.{method}({args})"  (DateFn :559, EcFn :563, ModuleNameFn :567)`
- `src/Cobol.Net.Compiler/Binding/Model/PicInfo.cs:301-312 — public int StorageWidth (bytes: Packed Digits/2+1; Binary/Comp5 1/2/4/8; BinaryChar/Short/Long/Double 1/2/4/8; ELSE 0 — no display/national byte authority exists)`
- `src/Cobol.Net.Compiler/Binding/Model/DataItem.cs:321-325 — public int ImageWidth (character positions; the LENGTH-fold source)`
- `src/Cobol.Net.Compiler/Binding/Model/PicInfo.cs:29 — PicCategory.Boolean (PIC 1, '0'/'1' char per position, D-B1)`
- `src/Cobol.Net.Runtime/Values/Text/CobolBool.cs:29-86 — And/Or/Xor/Not/Equal/IsTrue/Resize/AndAll/OrAll/XorAll/EqualAll — all (string?,string?)->string/bool over the '0'/'1' substrate`
- `src/Cobol.Net.Compiler/Binding/Bound/BoundTree.cs:249-275 — BoundBoolExpr hierarchy (BoundBoolLiteral/Ref/All/Binary/Not/Error) + BoundBoolOperand(BoundBoolExpr) : BoundOperand`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/ConditionBinder.cs:89-121 — BindBoolOperandValue: the §8.8.2 boolean-operand whitelist (NO intrinsic arm today — COBOLNET1511 otherwise)`
- `src/Cobol.Net.Compiler/Binding/Procedure/ExpressionBinder.cs:92-101 — public BoundStringLiteral BooleanLiteralOperand(string raw) → BoundStringLiteral { Category = PicCategory.Boolean }`
- `src/Cobol.Net.Compiler/CodeGen/Emit/BooleanRenderer.cs:20 — public static string Render(BoundBoolExpr e)  (BoundBoolExpr → C# string expr over CobolBool)`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/EcBinder.cs:39 — public void EcNoteFunction() => ctx.EcState.Functions = true;`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/MoveBinder.cs:162 — BoundComputedOperand { Expr: BoundIntrinsicCall ic } => ic.ResultCategory  (MOVE legality reads the intrinsic result category)`
- `src/Cobol.Net.Compiler/CodeGen/Emit/IntrinsicRenderer.cs:395-402 — StrArgVisitor: nested-intrinsic arm accepts only Alphanumeric/National result categories; Visit(BoundBoolOperand) is Loud — both need extension for boolean-result nesting`
- `src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs:127 — DiagnosticCatalog.ArithmeticStandardIntrinsic = COBOLNET0899 slug "arithmetic-standard-intrinsic"`
- `src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs:117-124 — COBOLNET0899 "constant-byte-length": CONSTANT … AS BYTE-LENGTH OF staged loud pending the §15.14 byte-width authority ("lands ONCE, with it")`

### ⚠ Discrepancies vs the P11 phase doc

- Every catalog line reference in the P11 doc §3.1 has drifted (the doc itself warns to re-scout): BOOLEAN-OF-INTEGER is :133 (doc :124), INTEGER-OF-BOOLEAN :161 (doc :143), BYTE-LENGTH :134 (:125), DATE-TO-YYYYMMDD/DAY-TO-YYYYDDD/YEAR-TO-YYYY :139-141 (:127-129), SECONDS-PAST-MIDNIGHT :166 (:148), TEST-DATE/DAY :168-169 (:150-151), TEST-NUMVAL/-C :170-171 (:152-153), CONCATENATE :174 (:156), LOCALE rows :162-165 + STANDARD-COMPARE :167 (:144-147/:149), SMALLEST-ALGEBRAIC :196 (:178).
- The live Deferred set is 17 rows, not the doc's '22 at time of writing' — the 5 P10-landed rows (CHAR-NATIONAL, DISPLAY-OF, NATIONAL-OF, EXCEPTION-FILE-N, EXCEPTION-LOCATION-N) are already Runtime, exactly as the doc's strike-through notes predicted. grep 'IntrinsicBind.Deferred' confirms 17.
- IntrinsicBinder line refs in the doc are stale: the window gate is at IntrinsicBinder.cs:139-144 (doc says :122-125); the NUMVAL-C currency injection at :213-214 (doc :179); the Ec-gate StartsWith check at :260 (doc :220); BindLengthFold at :562 (doc :465); the COBOLNET0844 guard at :250-252 (doc :214).
- The COBOLNET0844 guard is no longer 'CHAR-NATIONAL/ORD-over-national not yet implemented' — it was NARROWED in P10 to reject only FUNCTION CHAR given a national argument (§15.15.3 r1 category violation, pointing the user at CHAR-NATIONAL). The doc's Step 3 'remove the 0844 guard' instruction is fully superseded (its own P10 banner already says so).
- IntrinsicRenderer refs drifted: the Deferred loud guards are at :52-53 (RenderNum, doc says :45) and :272-273 (RenderString, doc :245); the NumvalC render arm is at :144-149 (doc :124-128).
- The provisional-window comment is at IntrinsicCatalog.cs:58-61 (doc cites :50-52).
- SMALLEST-ALGEBRAIC's value golden ALREADY EXISTS: tests/conformance/2023/smallest_algebraic.cob/.out — the doc's Step 7 'add its golden' is partially satisfied; what is genuinely missing is the window-enforcement negative row (no smallest-algebraic-at-2014 file exists; tests/conformance/negative has no *algebraic* or *locale* rows).
- Step 8's sketch uses 'data.Edition.Error(...)' — the actual binder API is ctx.Edition.Error(code, message) (BinderContext, see IntrinsicBinder throughout).
- Step 2's renderer note ('render BooleanOfInteger through the string/boolean channel') under-states the work: IntrinsicSig.ResultCategory currently maps Boolean → PicCategory.Numeric by explicit design comment ('Boolean-type rows are all Deferred; they fall to Numeric unchanged'), StrArgVisitor's nested-intrinsic arm only accepts Alphanumeric/National results, Visit(BoundBoolOperand) is loud, and ConditionBinder's §8.8.2 boolean-operand whitelist has no intrinsic arm — a Boolean-result promotion touches all four seams, not just a renderer arm.
- Doc §3.2/Step 7: 'RecordLayout/PicInfo.StorageWidth byte geometry' — PicInfo.StorageWidth returns 0 for DISPLAY/national usages (it exists only for packed/binary/COMP-5/BINARY-CHAR..DOUBLE), so a BYTE-LENGTH fold must compose bytes itself (ImageWidth×1 display, ×2 national per D-N1, StorageWidth for binary/packed); also note the related staged CONSTANT…AS BYTE-LENGTH OF leg (DiagnosticCatalog 'constant-byte-length') that expects the §15.14 authority to land ONCE.
- Catalog row 'MeanScaled' (MEAN) names a RuntimeMethod that has no runtime body — the renderer composes SumScaled + one division (SDIDI-aware). Any tooling that assumes RuntimeMethod == an existing CobolIntrinsics member has this one exception.
- Doc exit criteria say '213 unit' tests and '3275 conformance' era counts; the current battery is 3467 conformance / 292 unit / 33 characterization / legacy 353 MATCH (per the memory index and P10 close) — record fresh baselines at Step 0, don't pin the doc's numbers.
- Minor: the doc's Step 4 note says the EC gate check is 'at IntrinsicBinder.cs:220' and covers EcFileN/EcLocationN — true in substance (the StartsWith("Ec") check at :260 covers them), only the line is stale. EcFunctions.FileN/LocationN already exist (EcFunctions.cs:63/:70), confirming Steps 3-4 are landed and are re-verify-only.

## `code:renderer-runtime` — Code: IntrinsicRenderer + Runtime (CobolIntrinsics/CobolDate/EcFunctions) seams

### Findings

# IntrinsicRenderer + runtime scout report (read-only; all paths absolute under E:\COBOL)

## 1. Channel dispatch — who chooses RenderNum vs RenderString

The chooser is NOT `IntrinsicType` at render time — it is the bound node's `ResultCategory` (a `PicCategory`), stamped at bind time from the catalog row (`IntrinsicSig.ResultCategory`, src\Cobol.Net.Compiler\Binding\IntrinsicCatalog.cs:46-51: National→National, Alphanumeric→Alphanumeric, EVERYTHING ELSE (Numeric/Integer/Boolean/Index)→Numeric — the Boolean fallthrough is explicitly documented "Boolean-type rows are all Deferred; they fall to Numeric unchanged").

Two consumption sites pick the channel:
- **Numeric contexts** (COMPUTE/arithmetic/MOVE-to-numeric/IF comparisons): the ONE expression renderer's generated visitor arm — src\Cobol.Net.Compiler\CodeGen\Emit\NumericRenderer.cs:83 `public NumX Visit(BoundIntrinsicCall n) => Intrinsics.RenderNum(n);`. The `IntrinsicRenderer` is a lazy per-unit instance: NumericRenderer.cs:22 `internal IntrinsicRenderer Intrinsics => _intrinsics ??= new IntrinsicRenderer(ctx, this);`.
- **String contexts** (DISPLAY / MOVE-to-alphanumeric / string comparisons): src\Cobol.Net.Compiler\CodeGen\Emit\OperandText.cs:31-34 — `AsString` intercepts at ENTRY: `op is BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National } ic } ? num.Intrinsics.RenderString(ic) : op.Accept(...)`. The `IsString` comparison predicate mirrors it at OperandText.cs:147-148.

Cross-channel guards: RenderNum rejects a string-class function in a numeric context loud (IntrinsicRenderer.cs:54-55); RenderString's `_ =>` default (:328) is the loud "in a string context" arm for functions with no string recipe.

**VERIFIED BY EXECUTION**: DISPLAY of a RUNTIME NUMERIC-result intrinsic is loud today. `DISPLAY FUNCTION ORD("A")` compiled+ran through the CLI throws `NotImplementedCobolFeatureException: computed expression in a string context` (the AsStringVisitor computed arm, OperandText.cs:128-129 — a deliberate loud channel). DISPLAY FUNCTION LENGTH(...) works only because Fold produces a `BoundNumericLiteral`. So every P11 value golden for a numeric-result function must MOVE/COMPUTE into a PIC receiver and DISPLAY the receiver (the NIST IF pattern) — or the implementer must first add a numeric-computed DISPLAY arm.

## 2. The catalog row (`IntrinsicSig`) — src\Cobol.Net.Compiler\Binding\IntrinsicCatalog.cs

- Enums: `IntrinsicType { Alphanumeric, Boolean, National, Numeric, Integer, Index }` (:10); `IntrinsicArity { Fixed, OptionalTrailing, Variadic }` (:14); `IntrinsicBind { Runtime, Fold, Deferred }` (:21).
- Row shape (:33-36): `IntrinsicSig(string Name, IntrinsicType Type, IntrinsicArity Arity, int MinArgs, int MaxArgs, string ArgKinds, string RuntimeMethod, IntrinsicBind Bind, bool Float, int IntroducedIn, int? RemovedIn = null)`. ArgKinds codes: 'n' numeric, 'i' integer, 's' string, 'p' polymorphic; last char repeats for the variadic tail (:39-40).
- 17 rows are currently `Deferred` (grep-verified): BOOLEAN-OF-INTEGER :133, BYTE-LENGTH :134, DATE-TO-YYYYMMDD :139, DAY-TO-YYYYDDD :140, YEAR-TO-YYYY :141, INTEGER-OF-BOOLEAN :161, LOCALE-COMPARE/-DATE/-TIME/-TIME-FROM-SECONDS :162-165, SECONDS-PAST-MIDNIGHT :166, STANDARD-COMPARE :167, TEST-DATE-YYYYMMDD :168, TEST-DAY-YYYYDDD :169, TEST-NUMVAL :170, TEST-NUMVAL-C :171, CONCATENATE :174 (window [2002,2023)). Matches the P11 doc's 22-minus-5-landed arithmetic.

## 3. The binder — src\Cobol.Net.Compiler\Binding\Procedure\Verbs\IntrinsicBinder.cs

`BindIntrinsicCore` (:112) is the one funnel: UDF dispatch precedes the catalog (:119-121, §12.3.8.2 GR12) → catalog lookup + COBOLNET1501 (:123-134) → D8 edition window COBOLNET1502/1503 (:139-144) → standard-arithmetic staging (:154-160) → bespoke phrase-keyword binds (TRIM :164, FIND-STRING :168, SUBSTITUTE :172, CONVERT :176, MODULE-NAME :180) → generic `BindIntrinsicArgs` + arity COBOLNET1504 (:182-189) → literal-format rule COBOLNET1517 (:193-198) → Fold short-circuits (LENGTH :201-202 → BindLengthFold :562-584; ALGEBRAIC :206-208 → BindAlgebraicFold :594-653) → **NUMVAL-C default-currency injection** (:213-214: `if (sig.Name == "NUMVAL-C" && args.Count == 1) args.Add(new BoundStringLiteral(ctx.Data.CurrencyString));`) → MAX/MIN category-polymorphic resolution (:219-232, `sig with { RuntimeMethod = ... }`) → collate flags (:242-245) → EC-gate flag for `RuntimeMethod.StartsWith("Ec")` (:260 `host.Ec.EcNoteFunction()`) → `new BoundIntrinsicCall(resolved, args, category, collate) { CollateNat = collateNat }` (:262).

Bound node: src\Cobol.Net.Compiler\Binding\Bound\BoundTree.cs:150-151 `BoundIntrinsicCall(IntrinsicSig Sig, IReadOnlyList<BoundOperand> Args, PicCategory ResultCategory, bool Collate = false)` + init-props `CollateNat/TrimMode/FindLast/FindAnycase/SubstituteModes/ConvertSource/ConvertDest/ConvertDestHex/ModuleNameKind`. Args are BoundOperands, not BoundExprs. `OperandOf` (:55-61) wraps for operand positions: folded BoundNumLiteral→BoundNumericLiteral, else BoundComputedOperand.

## 4. Deferred loud fallback

- RenderNum, IntrinsicRenderer.cs:52-53: `if (sig.Bind == IntrinsicBind.Deferred || sig.RuntimeMethod.Length == 0) return new NumX(EmitText.LoudValue("long", $"FUNCTION {sig.Name} (catalogued, not yet implemented)"), 0);`
- RenderString twin :272-273 (`LoudValue("string", ...)`).
- `EmitText.LoudValue` (src\Cobol.Net.Compiler\CodeGen\Emit\EmitCore.cs:75-76) emits `NotImplemented.Value<T>("...")` which throws `NotImplementedCobolFeatureException` at run time (verified above). Note the `RuntimeMethod.Length == 0` half also catches Fold rows with empty RuntimeMethod that leak past the binder folds.

## 5. Argument rendering

**Numeric channel** (IntrinsicRenderer.cs): `Arg(ic,i) => num.AsNum(ic.Args[i], num.Receiver)` :219 (the ONE NumericRenderer, receiver-aware); `Dbl` :222 (double for the Float family); `IntArg`/`AsInt` :226-230 (`(long)` truncation to scale 0, with `CobolNum.Rescale` when scaled); `AlignedArgs(Ex)` :234-248 (§8.8.1 common-scale alignment for variadics, + AnyReal for MEAN's SDIDI branch); `StrArgList` :250; `CommaFlag` :252 (DECIMAL-POINT IS COMMA → `", commaMode: true"`); `Collate(ic)` :259-260 (`", __COLLATE"` / `", __COLLATE_NAT"` only when the binder flagged it — the emitted field otherwise does not exist, hazard H5).

**String channel**: `ArgNum` :375 renders numeric args under `ReceiverContext.None`; `ArgInt` :379; `Str(op)` :384 dispatches through the exhaustive `StrArgVisitor` :390-403 — string literal → `EmitText.CsLiteral`, field → `OperandText.AsString(n, owner.Num)`, nested string-class intrinsic (Alphanumeric OR National) re-enters `owner.RenderString` :395-397, everything else (numeric literal, figurative, ALL, bool operand) is loud.

**Scale discipline** (RenderNum returns `NumX(string Expr, int Scale, bool Dec, bool Real)`, EmitCore.cs:65): integer results scale 0; ABS/FRACTION-PART keep argument scale; MOD/REM the common scale; MEDIAN/MIDRANGE common+1; NUMVAL ws = max(Receiver.Scale, 6); NUMVAL-F and Float family ws = max(Receiver.Scale, 9); SECONDS-FROM-FORMATTED-TIME the format's fraction count computed at COMPILE time via `RuntimeApi.DateFormatFractionDigits` :177 (requires a literal format — COBOLNET1517).

## 6. TEST-NUMVAL-F end-to-end (the template's live exemplar)

1. **Catalog row** IntrinsicCatalog.cs:187: `Add(new("TEST-NUMVAL-F", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "s", "TestNumvalF", IntrinsicBind.Runtime, false, 2014));`
2. **Binder**: nothing bespoke — the generic funnel (arity 1/1, the 's' arg binds as a string literal/field operand), ResultCategory Numeric.
3. **Renderer arm** IntrinsicRenderer.cs:187-188: `case "TestNumvalF": return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}{CommaFlag}"), 0);`
4. **Fragment façade** src\Cobol.Net.Compiler\CodeGen\Roslyn\RuntimeApi.cs:555-556: `Intrinsic(method, args) => $"{nameof(CobolIntrinsics)}.{method}({args})"` (type name nameof-anchored; the METHOD name is the catalog string — a mismatch breaks the generated program's Roslyn compile, loud). Siblings: `DateFn` :559-560 (`CobolDate.`), `EcFn` :563-564 (`EcFunctions.`), `ModuleNameFn` :567-568.
5. **Runtime body** src\Cobol.Net.Runtime\Intrinsics\CobolIntrinsics.Exact.cs:289-325 `public static long TestNumvalF(string text, bool commaMode = false)` — 0 / first-error 1-based position / LENGTH+1, per §15.95 r b.1/b.2/c. NOTE: it does NOT call Numval — it is a dedicated position-reporting scanner beside the value parsers.
6. **Consumption**: `MOVE FUNCTION TEST-NUMVAL-F(X) TO N` / COMPUTE / IF → NumericRenderer.AsNum → Visit(BoundIntrinsicCall) → RenderNum. (Direct DISPLAY of it would be loud — see §1.)

The NUMVAL family all lives in **Exact.cs** (not Text.cs): `Numval` :194-239 (parse to (unscaled Int128, actual frac) then rescale to the emitter's compile-time working scale — the H2 discipline; malformed → `ExceptionState.ArgumentError` → §15.3 default 0), `NumvalF` :246-283 (mantissa + mandatory-signed 1..4-digit exponent), `NumvalC` :334-344 (strip currency + grouping separators, then delegate to `Numval`). TEST-NUMVAL/TEST-NUMVAL-C (P11 Step 6) should be authored beside these in Exact.cs, mirroring TestNumvalF's scanner style, with TEST-NUMVAL-C getting the same bind-time currency injection as NUMVAL-C (extend IntrinsicBinder.cs:213).

## 7. Optional trailing args — four proven patterns

1. **Bind-time injection** (config-dependent default): NUMVAL-C's omitted arg-2 → binder appends the SPECIAL-NAMES currency literal (IntrinsicBinder.cs:213-214); the renderer always renders 2 args (IntrinsicRenderer.cs:144-149).
2. **C# optional parameter**: DISPLAY-OF/NATIONAL-OF render `StrArgList(ic)` with 1 or 2 args (IntrinsicRenderer.cs:294-295); the runtime declares `DisplayOf(string arg, string? sub = null)` (CobolIntrinsics.Text.cs:223, :230) so absence takes the C# default.
3. **Renderer-supplied explicit default + presence flag**: FORMATTED-TIME/DATETIME pass `hasOff ? ArgInt(...) : "0"` plus a `hasOff` bool (IntrinsicRenderer.cs:334-350); FIND-STRING's skip `ic.Args.Count > 2 ? IntArg(ic, 2) : "0"` (:151-154); RANDOM's zero-arg vs seeded switch (:204-205).
4. **Loud for a not-yet-implemented optional form**: EXCEPTION-FILE(-N) with an argument → `LoudValue` naming VCR rows 68/69 (:322-327).

## 8. Fold rows

- **LENGTH** (row :113, Bind=Fold): binder short-circuit `BindLengthFold` (IntrinsicBinder.cs:562-584) → `BoundNumLiteral` from `DataItem.ImageWidth`/literal length at COMPILE time; the runtime-length shapes (ANY LENGTH item, nested string-intrinsic argument) instead return a `BoundIntrinsicCall`, which RenderNum's `"Length"` case (:157-158) renders as `CobolIntrinsics.Length(<image>)` (Text.cs:124) — the runtime residue. Ref-mod/ODO args stay BoundExprError (loud by name).
- **WHEN-COMPILED** (row :120): RenderString's `"WhenCompiled"` arm (:287) emits `EmitText.CsLiteral(WhenCompiledStamp.Value)` — a process-wide `Lazy<string>` baked from `RuntimeApi.DateFormat21(IntrinsicBinder.CompileClock())` (IntrinsicRenderer.cs:43-44); `CompileClock` is the injectable compile-time clock (IntrinsicBinder.cs:41).
- **HIGHEST/LOWEST/SMALLEST-ALGEBRAIC** (rows :159-160, :196): `BindAlgebraicFold` (:594-653) → `BoundNumLiteral` from PICTURE metadata; float args rejected COBOLNET1516.
- BYTE-LENGTH (P11 Step 7) should follow the LENGTH pattern: keep Bind=Fold, add `BindByteLengthFold` beside `BindLengthFold`, route by name at the :201 fold dispatch.

## 9. Runtime files

- **CobolIntrinsics.Text.cs** (F3 char family): Char/CharNational + weights/NationalCollation overloads :18-75, Ord ×3 :84-103, Upper/Lower/Reverse :107-118, Length :124, Concat :130, BaseConvert :137-162, Convert :174-215, DisplayOf :223-224, NationalOf :230-231, the ONE `Repertoire` translator :239-245 (+ `Sub()` '?'+EC-DATA-CONVERSION :247-252), Substitute :293-336, FindString :346-358, Trim :364-374. Error discipline everywhere: `Exceptions.ExceptionState.ArgumentError(...)` returns the §15.3 default (0 / " " / "").
- **CobolIntrinsics.Exact.cs** (F2): Factorial :32, SignOf :43, Floor :47, Truncate :56, Abs :59, FractionPart :63, Mod/Rem :70-83, Max/Min/Sum/Range/Median/Midrange/OrdMax/OrdMin :88-145, the string MAX/MIN family :151-182, the NUMVAL family :194-344 (see §6).
- **CobolDate.cs** (F4): `Format21` :24-28 (the ONE 21-char formatter, shared with WHEN-COMPILED's bake), **`CurrentDate` :32 = `Format21(DateTimeOffset.Now)` — NOT the IClock seam**; DateOfInteger/DayOfInteger/IntegerOfDate/IntegerOfDay :36-72 (Epoch 1601-01-01 :17, range 1..3,067,671); the §15.3 format engine — Tokenize :91-142, FormatFractionDigits :146 (compile-time consumer), EmitFormatted :160-218, FormattedDate :222, FormattedTime :232, FormattedDatetime :237, **FormattedCurrentDate :247-255 (also DateTimeOffset.Now direct)**, Analyze :261-338 (per-digit range narrowing), IntegerOfFormattedDate :342, SecondsFromFormattedTime :352, TestFormattedDatetime :362, CombinedDatetime :365. P11's date additions (DATE-TO-YYYYMMDD etc., TEST-DATE/DAY) belong here.
- **EcFunctions.cs**: Status :20 (31-char padded), Location :27, Statement :32 (63-char), File :44-52 (r1a "00" / r1b two spaces / r1c status+SELECT-name with the `::` key prefix stripped), FileN :63 `=> CobolIntrinsics.NationalOf(File())`, LocationN :70 — the -N twins are the SAME renderings through the ONE NationalOf translator; the compiler-side difference is only the catalog row's National category.

## 10. Clock seam + how tests fix the clock

- Seam: `IClock { DateTime Now(); }` + `SystemClock` (src\Cobol.Net.Runtime\IO\Clock.cs:11-31). `SystemClock.Now()` consults the **COBOLNET_CLOCK** environment variable (invariant-culture DateTime, e.g. `2026-06-10T14:30:45.67`) — the cross-process deterministic pin — else `DateTime.Now`.
- Owner: `RunUnit.Clock { get; set; } = SystemClock.Instance` (src\Cobol.Net.Runtime\Control\RunUnit.cs:55). Consumer: ACCEPT temporal only — `AcceptSource.Now() => RunUnit.Current.Clock.Now()` (src\Cobol.Net.Runtime\IO\AcceptSource.cs:26).
- Tests: tests\Cobol.Net.Tests.Conformance\AcceptDifferentialTests.cs:43 sets `psi.Environment["COBOLNET_CLOCK"] = clock` on the spawned run. In-process: assign `RunUnit.Current.Clock` (no `FixedClock` class actually exists despite the doc comment — supply your own IClock).
- **CURRENT-DATE and FORMATTED-CURRENT-DATE BYPASS the seam** (DateTimeOffset.Now direct; IClock.Now() returns a DateTime with no UTC offset, which the 21-char layout needs). SECONDS-PAST-MIDNIGHT (P11 Step 5) must route through `RunUnit.Current.Clock.Now()` (the AcceptSource pattern) to be deterministic under COBOLNET_CLOCK — or the seam must be extended to offsets and CURRENT-DATE re-homed onto it.

## 11. Boolean-result and national-result value flow to DISPLAY

- **National (works today, proven)**: catalog Type=National → ResultCategory National (IntrinsicCatalog.cs:48) → OperandText.AsString entry intercept (:32) → RenderString → the runtime returns a plain UTF-16 .NET string (D-N1: one char per national position) → AcceptDisplayEmitter.EmitDisplay concatenates parts and emits `System.Console.WriteLine(...)` (src\Cobol.Net.Compiler\CodeGen\Verbs\AcceptDisplayEmitter.cs:19-24). Exercised by `DISPLAY FUNCTION EXCEPTION-FILE-N` in the version matrix + the `exception_file_n` golden.
- **Boolean (does NOT flow today)**: the boolean substrate is a '0'/'1' .NET string (D-B1; CobolBool, src\Cobol.Net.Runtime\Values\Text\CobolBool.cs:23 — And/Or/Xor/Not/Equal; stores pad '0' via `RuntimeApi.StrStoreBoolean` RuntimeApi.cs:151-153). A boolean DATA ITEM displays fine (category Boolean is string-stored — OperandText.cs:95-97 reads the value directly). But a boolean-RESULT intrinsic currently cannot reach DISPLAY: `IntrinsicSig.ResultCategory` folds Boolean→Numeric (:46-51), so the AsString entry intercept (:32) does not fire and the operand falls to the loud computed arm (:128-129). Landing BOOLEAN-OF-INTEGER (P11 Step 2) therefore needs FOUR compiler seams besides the runtime body: (a) `IntrinsicType.Boolean => PicCategory.Boolean` in ResultCategory; (b) the OperandText.AsString entry intercept widened to include PicCategory.Boolean; (c) IsStringVisitor's computed arm (:147-148) and StrArgVisitor's nested arm (IntrinsicRenderer.cs:395-397) widened likewise; (d) a RenderString arm returning the '0'/'1' string (and MOVE-to-boolean-receiver classification checked in MoveClassifier). The result then flows to DISPLAY exactly like national — a plain string through Console.

## 12. The end-to-end template for adding ONE Runtime intrinsic

1. **Catalog row** (IntrinsicCatalog.cs, one line in Build()): flip `IntrinsicBind.Deferred`→`Runtime`, set `RuntimeMethod`, confirm Type/Arity/Min/Max/ArgKinds/IntroducedIn(/RemovedIn) — the D8 window gate and arity check are already generic.
2. **Binder**: usually NOTHING. Touch IntrinsicBinder.cs only for: phrase keywords (BindTrim/BindFindString pattern), bind-time default injection (the NUMVAL-C :213 pattern — extend for TEST-NUMVAL-C), a Fold (new Bind*Fold beside :562), or result-category logic (the Boolean seams above).
3. **Renderer arm**: RenderNum switch (IntrinsicRenderer.cs:59-192) for Numeric/Integer results — e.g. copy TestNumvalF :187-188: `case "TestNumval": return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}{CommaFlag}"), 0);` — or RenderString switch (:274-329) for Alphanumeric/National. Route the fragment through `RuntimeApi.Intrinsic`/`DateFn`/`EcFn` (never bare `CobolIntrinsics.` text — the ratchet guard pins bare counts). Pick the scale per §5 above.
4. **Runtime body**: a static method named EXACTLY the RuntimeMethod string, on `CobolIntrinsics` (Text.cs for char/string analysis, Exact.cs for numeric/NUMVAL-family) or `CobolDate` (date/time) or `EcFunctions` (EC reads). Edge cases via `Exceptions.ExceptionState.ArgumentError(...)` returning the §15.3 default; cite the §15.n.4 rule per branch.
5. **Same commit**: a value-exercising conformance golden (MOVE the FUNCTION into a PIC receiver, then DISPLAY the receiver — NOT `DISPLAY FUNCTION <numeric-fn>`, which is loud) + a `--std <earlier>` negative window row asserting COBOLNET1502/1503, + the version-matrix row where applicable. Battery + guard before commit.

### Key signatures / anchors

- `src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs:33-36 — readonly record struct IntrinsicSig(string Name, IntrinsicType Type, IntrinsicArity Arity, int MinArgs, int MaxArgs, string ArgKinds, string RuntimeMethod, IntrinsicBind Bind, bool Float, int IntroducedIn, int? RemovedIn = null)`
- `src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs:46-51 — IntrinsicSig.ResultCategory: National→PicCategory.National, Alphanumeric→Alphanumeric, else Numeric (Boolean currently falls to Numeric)`
- `src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs:187 — the TEST-NUMVAL-F exemplar row: Add(new("TEST-NUMVAL-F", IntrinsicType.Integer, IntrinsicArity.Fixed, 1, 1, "s", "TestNumvalF", IntrinsicBind.Runtime, false, 2014))`
- `src/Cobol.Net.Compiler/Binding/Bound/BoundTree.cs:150-151 — BoundIntrinsicCall(IntrinsicSig Sig, IReadOnlyList<BoundOperand> Args, PicCategory ResultCategory, bool Collate = false) + init props CollateNat/TrimMode/FindLast/FindAnycase/SubstituteModes/Convert*/ModuleNameKind`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs:112 — BindIntrinsicCore(string name, IReadOnlyList<Core.FunctionArgumentContext> argCtxs): the one bind funnel (window gate :139-144, arity :182-189, NUMVAL-C currency injection :213-214, EcNoteFunction :260, node ctor :262)`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs:562 — BindLengthFold: the Fold pattern to copy for BYTE-LENGTH (BoundNumLiteral at compile time; runtime-length shapes → BoundIntrinsicCall)`
- `src/Cobol.Net.Compiler/CodeGen/Emit/IntrinsicRenderer.cs:49 — public NumX RenderNum(BoundIntrinsicCall ic); Deferred loud guard :52-53; TestNumvalF arm :187-188: new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{Str(ic.Args[0])}{CommaFlag}"), 0)`
- `src/Cobol.Net.Compiler/CodeGen/Emit/IntrinsicRenderer.cs:269 — public string RenderString(BoundIntrinsicCall ic); Deferred loud guard :272-273; DisplayOf/NationalOf via StrArgList :294-295; EcFn arms :318-327`
- `src/Cobol.Net.Compiler/CodeGen/Emit/IntrinsicRenderer.cs:252 — CommaFlag => ctx.Data.DecimalPointIsComma ? ", commaMode: true" : "" (the DECIMAL-POINT IS COMMA channel)`
- `src/Cobol.Net.Compiler/CodeGen/Emit/NumericRenderer.cs:83 — public NumX Visit(BoundIntrinsicCall n) => Intrinsics.RenderNum(n) (the numeric-context dispatch); per-unit instance :22`
- `src/Cobol.Net.Compiler/CodeGen/Emit/OperandText.cs:31-34 — AsString entry intercept: BoundComputedOperand{Expr: BoundIntrinsicCall{ResultCategory: Alphanumeric or National}} → num.Intrinsics.RenderString(ic) (the string-context dispatch; widen for Boolean)`
- `src/Cobol.Net.Compiler/CodeGen/Emit/EmitCore.cs:65 — internal readonly record struct NumX(string Expr, int Scale, bool Dec = false, bool Real = false); LoudValue :75-76 → NotImplemented.Value<T>`
- `src/Cobol.Net.Compiler/CodeGen/Roslyn/RuntimeApi.cs:555-556 — public static string Intrinsic(string method, string args) => $"{nameof(CobolIntrinsics)}.{method}({args})"; DateFn :559-560; EcFn :563-564`
- `src/Cobol.Net.Runtime/Intrinsics/CobolIntrinsics.Exact.cs:194 — public static long Numval(string text, int scale, bool commaMode = false); NumvalF :246; TestNumvalF :289 (position-reporting scanner, does NOT call Numval); NumvalC :334 (currency-strip then Numval)`
- `src/Cobol.Net.Runtime/Intrinsics/CobolIntrinsics.Text.cs:223/230 — public static string DisplayOf(string arg, string? sub = null) / NationalOf(...): C#-optional-parameter pattern for optional trailing args; the ONE Repertoire translator :239-245; CharNational :52/:66`
- `src/Cobol.Net.Runtime/Intrinsics/CobolDate.cs:32 — public static string CurrentDate() => Format21(DateTimeOffset.Now) — bypasses IClock; Format21 :24; FormattedCurrentDate :247 (also DateTimeOffset.Now)`
- `src/Cobol.Net.Runtime/Exceptions/EcFunctions.cs:44-63 — File() r1a/r1b/r1c rendering; FileN() => CobolIntrinsics.NationalOf(File()) — the -N twin pattern; LocationN :70`
- `src/Cobol.Net.Runtime/Control/RunUnit.cs:55 — public IClock Clock { get; set; } = SystemClock.Instance; consumed only by AcceptSource.cs:26 Now() => RunUnit.Current.Clock.Now()`
- `src/Cobol.Net.Runtime/IO/Clock.cs:20-31 — SystemClock.Now() consults the COBOLNET_CLOCK env var (invariant-culture DateTime) else DateTime.Now; tests pin it via psi.Environment["COBOLNET_CLOCK"] (AcceptDifferentialTests.cs:43)`
- `src/Cobol.Net.Runtime/Values/Text/CobolBool.cs:23 — public static class CobolBool ('0'/'1' string substrate): And/Or/Xor :29-35, Not :39, Equal :50; boolean stores pad '0' via RuntimeApi.StrStoreBoolean (RuntimeApi.cs:151-153)`

### ⚠ Discrepancies vs the P11 phase doc

- DISPLAY of a runtime NUMERIC-result intrinsic is LOUD today (verified by compiling+running DISPLAY FUNCTION ORD("A") → NotImplementedCobolFeatureException 'computed expression in a string context', OperandText.cs:128-129). The P11 doc's Step-2 golden pattern 'DISPLAY FUNCTION INTEGER-OF-BOOLEAN(B01)' would fail — value goldens for numeric-result promotions must MOVE/COMPUTE into a PIC receiver and DISPLAY the receiver, or a numeric-computed DISPLAY arm must be added first.
- P11 doc Step 5 claims SECONDS-PAST-MIDNIGHT 'reads the same clock seam as CURRENT-DATE (the injectable clock)'. Reality: CobolDate.CurrentDate (CobolDate.cs:32) and FormattedCurrentDate (:247-255) call DateTimeOffset.Now DIRECTLY, bypassing the injectable IClock/RunUnit.Clock/COBOLNET_CLOCK seam (which only ACCEPT temporal uses, AcceptSource.cs:26; IClock.Now() returns a DateTime with no UTC offset). SECONDS-PAST-MIDNIGHT must route through RunUnit.Current.Clock.Now() to be deterministic under the test clock — CURRENT-DATE is not a valid model for that.
- P11 doc §3.2 places TestNumval/TestNumvalC 'in CobolIntrinsics.Text.cs beside TestNumvalF' — but TestNumvalF and the whole NUMVAL family actually live in CobolIntrinsics.Exact.cs (:194-344), not Text.cs. New TEST twins belong in Exact.cs.
- The boolean-result channel is thinner than the doc implies ('render through the string/boolean channel'): IntrinsicSig.ResultCategory folds Boolean→Numeric (IntrinsicCatalog.cs:46-51, explicitly documented as the Deferred-era shortcut), and the string-channel intercepts (OperandText.AsString:32, IsStringVisitor:147-148, StrArgVisitor IntrinsicRenderer.cs:395-397) admit only Alphanumeric/National. BOOLEAN-OF-INTEGER needs a ResultCategory Boolean arm + those three intercepts widened + a boolean RenderString arm + MOVE-to-boolean-receiver classification — four compiler seams the doc does not enumerate.
- P11 doc line anchors have drifted: the Deferred loud guards are at IntrinsicRenderer.cs:52-53 and :272-273 (doc says :45/:245); the window gate at IntrinsicBinder.cs:139-144 (doc :122-125); the NUMVAL-C currency injection at :213-214 (doc :179); the Ec-gate check at :260 (doc :220); BindLengthFold at :562 (doc :465); all §3.1 catalog row line refs shifted ~+9..+18 (BOOLEAN-OF-INTEGER :133 not :124, INTEGER-OF-BOOLEAN :161 not :143, CONCATENATE :174 not :156, SMALLEST-ALGEBRAIC :196 not :178, LOCALE rows :162-167 not :144-149, SECONDS-PAST-MIDNIGHT :166 not :148, TEST-* :168-171 not :150-153).
- The live Deferred set is 17 rows (grep-verified), consistent with the doc's 22-minus-5-LANDED arithmetic — the '43' figure the doc itself flags as stale is indeed stale; work the 17.
- Clock.cs's doc comment names 'new FixedClock(...)' for in-process tests, but no FixedClock class exists anywhere — an in-process test must supply its own IClock implementation (or use the COBOLNET_CLOCK env pin as AcceptDifferentialTests does).
- Doc §5.3 exit criterion says '213+ unit' — the current battery is 292 unit / 3467 conformance (post-P10); stale count only, no code impact.
- Minor: the doc cites the provisional-window catalog comment at IntrinsicCatalog.cs:50-52 — it is at :59-61; the COBOLNET0844 CHAR guard the (already-superseded) Step 3 references is at IntrinsicBinder.cs:250-252 and was already narrowed to CHAR-only in P10 as the doc's banner notes.

## `code:tests-harness` — Code: conformance/matrix/negative test wiring (P10 worked example)

### Findings

## How conformance tests are wired in E:/COBOL

### 1. Corpus layout + discovery (tests/conformance/)

Four dirs: `tests/conformance/2002/` (247 files), `2014/` (68), `2023/` (32), `negative/` (195 files ≈ ~97 .cob/.err pairs). Each edition dir AND negative/ carries a `manifest.json` with two arrays: `"enabled"` and `"pending"`. TWO runners consume the corpus:

**A. Greenfield runner (the one that gates new work): `E:\COBOL\tests\Cobol.Net.Tests.Conformance\CorpusRunnerTests.cs`**
- Discovery is manifest-driven, NOT pure filesystem-glob. `Load(dir)` (CorpusRunnerTests.cs:25-31) parses `manifest.json`; `EnabledPositive()` (:52-59) yields (edition, name) for every enabled entry in 2002/2014/2023.
- Positive golden runner: `EnabledProgram_CompilesStrict_AndMatchesOutIfPresent(string edition, string name)` (CorpusRunnerTests.cs:61-88). Compiles `<dir>/<name>.cob` via `CobolNet.CompilerDriver.Compile(new CompilerDriver.Options(src, dll, DialectLevel: int.Parse(edition)))` STRICT; if a sibling `<name>.out` exists it runs via `CutRunner.Run(dll, tmp)` and asserts `Assert.Equal(CutRunner.Normalize(File.ReadAllText(outFile)), CutRunner.Normalize(stdout))` (:85) — LF normalization + per-line trailing-space trim, no trailing newline. No `.out` = compile-only entry (:80).
- Integrity fact: `Manifest_CoversEveryProgram_NoOverlap` (CorpusRunnerTests.cs:34-50) — every on-disk `*.cob` must be listed (enabled ⊕ pending), no phantoms, no overlap. So a new `.cob` dropped WITHOUT a manifest entry FAILS THE BUILD; a new pair is asserted only once added to `"enabled"`. `"pending"` = catalogued but not asserted (the mass-red guard).

**B. Legacy runner (auto-discovery): `E:\COBOL\tests\CobolSharp.Tests.Integration\ConformanceTests.cs`**
- `Cases()` (:30-42) filesystem-globs `tests/conformance/<ver>/*.cob` with a sibling `.out` — a new pair IS auto-picked-up here with no code change, which is why legacy exclusions matter (see §4).

### 2. Negative tests (how P10's exception_file_n_below_2002 ran)

`CorpusRunnerTests.EnabledNegativeCase_RejectsWithItsDiagnostic(string name)` (CorpusRunnerTests.cs:97-115):
- `EnabledNegative()` (:90-95) yields every `"enabled"` name from `tests/conformance/negative/manifest.json`.
- Expected diagnostic = `File.ReadAllText(dir/name + ".err").Trim()` (:103) — the `.err` file holds a SUBSTRING (typically just a code, e.g. `COBOLNET1502`).
- Reject editions come from the `.cob`'s FIRST LINE: `*> reject-at: 85` (or `85 2002 2014 2023`) — parsed at :106-108; a missing header throws `InvalidOperationException`.
- For each listed edition: `EditionHarness.CompileFull(src, ed)` must fail (`Assert.False(ok, ...)` :112) and `EditionHarness.AssertHasDiagnostic(errors, expected)` (:113) asserts SOME diagnostic contains the `.err` substring case-insensitively (EditionHarness.cs:95-100).

### 3. EditionHarness (`tests\Cobol.Net.Tests.Conformance\EditionHarness.cs`)

THE per-edition compile path. `Editions = [85, 2002, 2014, 2023]` (:17). `CompileFull(source, edition, permissive=false)` (:22-36) writes source to temp, runs `CompilerDriver.Compile(... DialectLevel: edition, Permissive: permissive)`, returns (Ok, Errors, Warnings). Also `Compile` (:40), `CompileNist` (:49, supports `checkOnly` — parse+validate+bind, no backend), `CompileAndRun` (:71), `AssertHasDiagnostic` (:95), `AssertNoDiagnostic` (:104), `RepoRoot()` (:111 — walks up to the dir containing tests/nist).

### 4. Legacy-suite exclusion (GreenfieldOnly) — YES, required for greenfield-only goldens

Because the legacy `ConformanceTests` auto-discovers every `.cob`+`.out`, a golden the frozen legacy engine cannot compile/run needs a `GreenfieldOnly` entry in `tests\CobolSharp.Tests.Integration\ConformanceTests.cs` in the SAME commit (memory `feedback_legacy_suite_on_shared_corpus`). `GreenfieldOnly` is a `HashSet<(string,string)>` of (versionDir, name) (:66-298); the theory returns early at :304 (`if (GreenfieldOnly.Contains((version, name))) return;`). There is also a smaller `LegacyDivergent` set (:51-59) where legacy compiles+runs but its output is skipped (ISO-adjudicated divergence). P10 added its entries with an explanatory comment, e.g. :108-111: the EC-N wave comment + `("2002", "exception_file_n")` and `("2002", "char_national")`.

### 5. VERSION TEST MATRIX registry

- **Canonical file: `E:\COBOL\tests\version-matrix\constructs.json`** — `{"constructs": [ {row}, ... ]}`. Row fields: `id` (kebab, e.g. "exception-file-n-2002"), optional `status` ("active" default | "pending" = catalogued, compile assertions skipped), `description`, `display`, `diagnosticCode`, `citation`, `introducedIn` (int), `removedIn` (int|null), optional `obsoleteIn`, `expectDiagnostic`, optional `expectDiagnosticBelow`, `vcr` (citation of VERSION_CHANGE_REFERENCE row or introducing standard), `source` (inline \n-joined minimal COBOL program). Vendor constructs live in `vendor-constructs.json`, never here.
- **Executor: `tests\Cobol.Net.Tests.Conformance\VersionMatrixTests.cs`** — `Construct_MatchesEditionExpectation(string constructId, int edition)` (:76-101); expected outcome `ExpectCompiles` = `edition >= IntroducedIn && (RemovedIn is null || edition < RemovedIn)` (:50-51); a reject cell asserts the diagnostic code: `edition < IntroducedIn ? ExpectDiagnosticBelow ?? ExpectDiagnostic : ExpectDiagnostic` (:97-99). Also `RemovedConstruct_CompilesPermissive_WithWarning` (:115), `ObsoleteConstruct_CompilesEverywhere_WarnsFromObsoleteEdition` (:141, fixed 0903 band).
- **Generated renderings (committed):** `scripts/gen-constructs.ps1` emits `src\Cobol.Net.Editions\ConstructRegistry.g.cs` (Entries list) and `src\Cobol.Net.Editions\Constructs.g.cs` (one `public const string PascalId = "kebab-id";` per row); `ConstructRegistryDriftTests` asserts they equal constructs.json. So adding a matrix row = edit constructs.json + re-run gen-constructs.ps1 + commit both .g.cs files.

### 6. Other test classes asked about

- **`IntrinsicFunctionDifferentialTests.cs`** (tests\Cobol.Net.Tests.Conformance): two idioms — `AssertSameAsLegacy(source)` → `DifferentialGolden.Assert(source)` (:24) and `AssertSpec(source, expected, dialect = 85)` (:26-31) which does `new CobolNetCompiler(dialect).CompileAndRun(source)` + `Assert.Equal(CutRunner.Normalize(expected), cout)`. `DifferentialGolden.Assert` (DifferentialGolden.cs:42) compares against a COMMITTED golden `tests/differential/<TestClass>/<hash(edition,source)>.out` by default (`COBOLNET_DIFF_MODE=golden`); `bake`/`verify` modes cross-check the live legacy oracle. New P11 spec-pinned intrinsic facts belong here via `AssertSpec` (spec value + § citation) — NOT differential — when legacy diverges or predates the function.
- **`EditionGateDiagnosticTests.cs`**: message-QUALITY facts for the COBOLNET0900 band — `AssertNames(errors, requiredEdition, targeting)` (:20-25) asserts "COBOLNET0900" + `requires COBOL-{X}` + `targeting COBOL-{Y}`. One representative per gate CLASS (statement keyword, clause, SPECIAL-NAMES, unit, CALL-arg). Note: intrinsic-function windows do NOT use 0900 — they use COBOLNET1502/1503 from `src\Cobol.Net.Compiler\Binding\Procedure\Verbs\IntrinsicBinder.cs:139-144` (the D8 window: `sig.IntroducedIn > ctx.Edition.DialectLevel` → 1502 "FUNCTION {name} was introduced by ISO/IEC 1989:{year} (§15) — it requires --std {year} or later (targeting COBOL-{level})"; `RemovedIn` → 1503). Unknown name → 1501 (:126).

### 7. Complete worked example — the P10 exception_file_n set, end to end

**(a) Positive golden**: `tests\conformance\2002\exception_file_n.cob` (header comment cites ISO 15.29/15.31; `>>TURN EC-I-O CHECKING ON`; a DECLARATIVES `USE AFTER EXCEPTION CONDITION EC-I-O-AT-END` displaying `FUNCTION EXCEPTION-FILE-N`, LENGTH probes, a `MOVE ... TO PIC N` + `N"10TF"` compare proving category-national) + `exception_file_n.out` (8 lines: `PRE=00`, `LEN-PRE=02`, `S: EC-I-O-AT-END`, `FN: 10TF`, `LEN-FN=04`, `NAT=YES`, `LEN-LN=01`, `AFTER`).
**(b) Enabled**: `tests\conformance\2002\manifest.json:20` — `"exception_file_n"` in `"enabled"`. Runs via CorpusRunnerTests.cs:61-88 at strict `--std 2002`, byte-compares stdout to the .out under CutRunner.Normalize.
**(c) Legacy exclusion**: `tests\CobolSharp.Tests.Integration\ConformanceTests.cs:111` — `("2002", "exception_file_n")` in `GreenfieldOnly` (comment :108-110: frozen legacy has no EC model and no national result category); the legacy theory returns early at :304.
**(d) Negative**: `tests\conformance\negative\exception_file_n_below_2002.cob` — line 1 `*> reject-at: 85`, lines 2-5 a spec-citation comment, body displays both -N twins. `exception_file_n_below_2002.err` = the single line `COBOLNET1502`. Enabled at `tests\conformance\negative\manifest.json:42`. Runs via CorpusRunnerTests.cs:97-115: CompileFull at 85 must fail; AssertHasDiagnostic(errors, "COBOLNET1502"). The diagnostic is produced by IntrinsicBinder.cs:139-141.
**(e) Matrix row**: `tests\version-matrix\constructs.json:1731-1743` — id `"exception-file-n-2002"`, status "active", introducedIn 2002, removedIn null, expectDiagnostic "COBOLNET1502", diagnosticCode "COBOLNET1502", citation (§15.29/§15.31 + the 2023 optional-arg form staged loud → PHASE-13 Step 9), vcr, source = inline VMEFN1 program displaying both twins. Executed by VersionMatrixTests.cs:76-101 as 4 cells: compile at 2002/2014/2023, reject at 85 asserting COBOLNET1502. Rendered to `src\Cobol.Net.Editions\Constructs.g.cs:151` (`public const string ExceptionFileN2002 = "exception-file-n-2002";`) and `ConstructRegistry.g.cs:153` by `scripts/gen-constructs.ps1`.

### Checklist for a new P11 golden+negative set (the P10-proven pattern)
1. `tests/conformance/<ed>/<name>.cob` + `.out` (LF; header comment citing the ISO §).
2. Add `<name>` to that dir's `manifest.json` `"enabled"` array (else the integrity fact fails).
3. If the frozen legacy cannot reproduce it: add `("<ed>", "<name>")` + comment to `GreenfieldOnly` in `tests\CobolSharp.Tests.Integration\ConformanceTests.cs` — SAME commit.
4. Negative window row: `tests/conformance/negative/<name>.cob` with `*> reject-at: <editions>` line 1 + `<name>.err` containing the code substring (COBOLNET1502 for introduced-later intrinsics, 1503 for removed) + manifest `"enabled"` entry.
5. Matrix row in `tests/version-matrix/constructs.json` (if a new construct id is warranted) + re-run `scripts/gen-constructs.ps1` and commit both .g.cs files.
6. Optional spec-pinned xUnit facts in `IntrinsicFunctionDifferentialTests.cs` via `AssertSpec`.

### Key signatures / anchors

- `tests/Cobol.Net.Tests.Conformance/CorpusRunnerTests.cs:63 — public void EnabledProgram_CompilesStrict_AndMatchesOutIfPresent(string edition, string name)  [positive golden runner; MemberData EnabledPositive :52]`
- `tests/Cobol.Net.Tests.Conformance/CorpusRunnerTests.cs:99 — public void EnabledNegativeCase_RejectsWithItsDiagnostic(string name)  [negative runner; '*> reject-at:' header parse :104-108; .err substring assert :113]`
- `tests/Cobol.Net.Tests.Conformance/CorpusRunnerTests.cs:39 — Manifest_CoversEveryProgram_NoOverlap(string edition)  [integrity: every on-disk .cob must be manifest-listed]`
- `tests/Cobol.Net.Tests.Conformance/EditionHarness.cs:22 — public static (bool Ok, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings) CompileFull(string source, int edition, bool permissive = false)`
- `tests/Cobol.Net.Tests.Conformance/EditionHarness.cs:95 — public static void AssertHasDiagnostic(IEnumerable<string> diagnostics, string expectedSubstring)  [case-insensitive substring]`
- `tests/Cobol.Net.Tests.Conformance/VersionMatrixTests.cs:78 — public void Construct_MatchesEditionExpectation(string constructId, int edition)  [f(case,V) :50-51; reject-cell code pick :97]`
- `tests/version-matrix/constructs.json:1731-1743 — the 'exception-file-n-2002' row (id/status/description/display/diagnosticCode/citation/introducedIn/removedIn/expectDiagnostic/vcr/source)`
- `tests/conformance/2002/manifest.json:20 — "exception_file_n" in "enabled"`
- `tests/conformance/negative/manifest.json:42 — "exception_file_n_below_2002" in "enabled"`
- `tests/conformance/negative/exception_file_n_below_2002.cob:1 — *> reject-at: 85   (.err = the single line COBOLNET1502)`
- `tests/CobolSharp.Tests.Integration/ConformanceTests.cs:66 — private static readonly HashSet<(string, string)> GreenfieldOnly  [exception_file_n entry :111; skip :304]`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs:139-144 — the D8 window gate: COBOLNET1502 (IntroducedIn > DialectLevel) / COBOLNET1503 (RemovedIn)`
- `src/Cobol.Net.Editions/Constructs.g.cs:151 + src/Cobol.Net.Editions/ConstructRegistry.g.cs:153 — generated by scripts/gen-constructs.ps1 from constructs.json (drift-tested)`
- `tests/Cobol.Net.Tests.Conformance/IntrinsicFunctionDifferentialTests.cs:26 — private static void AssertSpec(string source, string expected, int dialect = 85)  [spec-pinned; :24 AssertSameAsLegacy → DifferentialGolden.Assert]`
- `tests/Cobol.Net.Tests.Conformance/DifferentialGolden.cs:42 — public static void Assert(string source, int edition = 85, [CallerFilePath] string file = "", string? goldenName = null)  [committed goldens tests/differential/<class>/<hash>.out; COBOLNET_DIFF_MODE golden|bake|verify]`
- `tests/Cobol.Net.Tests.Conformance/EditionGateDiagnosticTests.cs:20-25 — AssertNames(errors, requiredEdition, targeting)  [0900-band message quality: 'requires COBOL-X' + 'targeting COBOL-Y']`

### ⚠ Discrepancies vs the P11 phase doc

- tests/conformance/README.md (lines 17-27) claims a new .cob+.out pair is 'auto-discovered (no test code to write)' — that is true only of the LEGACY ConformanceTests glob (ConformanceTests.cs:30-42). The greenfield CorpusRunnerTests requires a manifest.json 'enabled' entry, and its integrity fact FAILS the build for any unlisted .cob. The README also never mentions manifest.json, the negative/ dir, or GreenfieldOnly.
- tests/conformance/negative/manifest.json _comment (line 2) says each entry carries 'an "editions" map naming the --std values that must reject' — no such map exists anywhere in the file (entries are bare name strings, 'pending' is []). The reject editions actually come from the .cob's first-line '*> reject-at:' comment header (CorpusRunnerTests.cs:104-108).
- PHASE-11 doc line 38 exit criterion says '213 unit' tests — the current battery is 292 unit (per MEMORY.md; CLAUDE.md snapshot says 281). Stale count, harmless.
- PHASE-11 doc's proposed negative-row names are hyphenated (boolean-of-integer-at-85.cob, concatenate-at-2023.cob) while the P10 EC-N precedent is underscored (exception_file_n_below_2002). The corpus already mixes both styles and the runner is name-agnostic — pick either, but the P10 intrinsic-window precedent is underscores with a _below_<edition> suffix.
- PHASE-11 doc lines 353-354, 371-374 (Step 4, EXCEPTION-FILE-N/-LOCATION-N) are correctly marked already-delivered by P10 — verified: golden 2002/exception_file_n (not the doc's earlier intrinsics_ec_national name), matrix row exception-file-n-2002, negative exception_file_n_below_2002 @85 all exist and are enabled/active. Step 4 needs no new corpus work.
- Housekeeping: stray build artifacts sit inside the corpus dirs — tests/conformance/2002/ contains address_of_qualified.dll/.g.cs/.runtimeconfig.json + Cobol.Net.Runtime.dll, and negative/ contains Cobol.Net.Runtime.dll. Harmless to discovery (only *.cob is enumerated) but they are CLI-run leftovers polluting the tree.
- Terminology: intrinsic-function edition windows assert COBOLNET1502/1503 from IntrinsicBinder (the D8 catalog window), NOT the COBOLNET0900 band that EditionGateDiagnosticTests covers — a P11 negative row's .err should say COBOLNET1502 (introduced-later) or COBOLNET1503 (removed, e.g. CONCATENATE at 2023), matching the P11 doc's own §1 note (lines 92-96).

## `code:tier-c-code` — Code: Tier-C guard-site inventory + StorageForm/GroupImageCodec

### Findings

# Tier-C REDEFINES Surface — Complete Map (E:/COBOL, branch main @ 45fe74dd)

## 1. RedefinesModel.cs (src/Cobol.Net.Compiler/Binding/Model/RedefinesModel.cs) — complete

- **RenamesInfo** (lines 11–30): level-66 RENAMES descriptor (§13.18.45); `IsAlias => ThruName is null` (Tier-A forward GR1 vs Tier-B THRU composition GR2).
- **RedefinesTier enum** (lines 37–53), priority cascade D > C > B > A, lattice A ⊑ B ⊑ C ⊑ D:
  - `Alias` (A) — identical storage type; one typed field, others pass-throughs.
  - `StringCanonical` (B) — whole class USAGE DISPLAY; canonical is ONE string of class-max width; each view a typed (offset,width) accessor. "The dominant real case."
  - `ByteCanonical` (C) — "a genuine mixed-USAGE pun (a COMP/COMP-1/2/3/5/INDEX leaf observed cross-view): the canonical is ONE class-scoped byte[]; each leaf is a typed codec accessor over a (offset,length,usage) window." **DEAD-BY-CONSTRUCTION today: ComputeTier never returns it; nothing in src/ ever assigns or matches `RedefinesTier.ByteCanonical` except the enum decl, a doc-comment in RedefinesModel.cs:74, and comments in StorageFormPass.cs:177.**
  - `Rejected` (D) — spec-forbidden / unmodelable, loud diagnostic.
- **RedefinesClass** (lines 61–107): `Canonical` (required init), `Members` (List, source order), `Tier` (private set), `Width` (private set; chars for B, bytes for C; class-max incl. SR8 larger redefiner), `BackingCsName => "_redef_" + Canonical.CsName` (line 79, "the single stored backing field for a Tier-B/Tier-C class"), `BasedPointerField` (get/set — BASED deref bridge, §13.18.5), `RejectReason` (private set, non-null only when Rejected).
- **`internal void Classify(RedefinesTier tier, int width, string? rejectReason)`** (line 101) — THE one verdict-application site (P5.11d). Doc-comment names exactly two callers: `DataBinder.ClassifyRedefinesClasses` (ComputeTier reason table carries the ISO citations) and `DataBinder.ForceStringCanonical` (the ONE cell-backing forcer, may deliberately RE-classify per §13.18.22.4 GR5). "No other writer exists."

## 2. ComputeTier — DataBinder.cs:2376 (NOT ~1752; see discrepancies)

`private static RedefinesTier ComputeTier(RedefinesClass cls, out string? reject)` — flattens all members to leaves, then:

1. **Tier C → Rejected (interim)** (lines 2390–2396): any leaf `IsFloat || Usage is Comp5 or Index or BinaryChar or BinaryShort or BinaryLong or BinaryDouble` →
   reject = `"float/COMP-5/BINARY-*/INDEX REDEFINES of '{cls.Canonical.CobolName}' (Tier-C byte path) not yet implemented"`, returns `Rejected`.
   Citations (comment block 2381–2389): COMP-1/2 no fixed decimal-digit width; COMP-5 BinaryCapacity exceeds PICTURE digits; INDEX no character image §13.18.60. A DISPLAY+BINARY/PACKED mix is deliberately **Tier B** under the digit-image representation — ISO §13.18.60 USAGE GR4 (implementor-defined representation incl. sign) + §12.4.6.4.4 SAME RECORD AREA GR2 ("implicit redefinition of the area"). "No pointer/object/strongly-typed items exist in the bound model yet → no Tier-D check."
2. **National → Rejected** (lines 2402–2407): any leaf `Category: National` →
   reject = `"REDEFINES over national data in '{cls.Canonical.CobolName}' (the 2-byte national character has no single-byte char-window overlay) not yet implemented (Phase 4a residue)"`. Citation: §13.18.44 lays the shared area in BYTES + the documented D-N1/D-N2 2-byte national. Boolean falls through (one '0'/'1' char = one byte, D-B1).
3. **Tier A** (2411–2414): canonical elementary AND every member elementary with same `ElementType` + same `ImageWidth` → `Alias`.
4. **Tier B** (2417): everything else → `StringCanonical`.

**Caller** `ClassifyRedefinesClasses()` (DataBinder.cs:2263, BindPipeline pass row BindPipeline.cs:40) adds a third reject AFTER ComputeTier (lines 2304–2311): any member is/contains an OCCURS DYNAMIC table → `Edition.Error("COBOLNET1525", "REDEFINES involving the dynamic-capacity table in '{X}': ... shall be neither the subject nor the object of a REDEFINES (ISO §13.18.44 SR5)")`, tier forced `Rejected`, reason ??= `"REDEFINES of/over a dynamic-capacity table (§13.18.44 SR5, D9)"`. Then ONE `cls.Classify(tier, maxWidth, reject)` at 2313, offsets via `AssignClassOffsets` (SR9/SR10/GR1), and the Tier-B numeric-DISPLAY/BINARY/PACKED leaf image-promotion loop (2321–2343, `MarkImageForced` + SignKind rewrite per §13.18.60 GR4).

**The second sanctioned writer** — `ForceStringCanonical(DataItem, string what)` (DataBinder.Linkage.cs:381): cell gate at 392 `leaves.Any(l => l.Pic is not { IsFloat: false, Usage: Usage.Display })` → `cls.Classify(Rejected, cls.Width, "{what} '{item.CobolName}' has a COMP/float/index/national/bit leaf — the shared single-byte character image cannot carry it (Tier-C byte island / the RESIDUE-11 2-byte national layout, deferred)")` and returns null. Note this gate is STRICTER than ComputeTier (refuses plain BINARY/PACKED, NATIONAL, BIT too — byte-addressed cell safety). Three callers: `CallMakeExternal` Linkage.cs:360 ("EXTERNAL record"; on null → `Edition.Error(DiagnosticCatalog.ExternalRecordNotCellBacked /* COBOLNET0899 family, DiagnosticCatalog.cs:383 */, "EXTERNAL record '{X}' cannot be cell-backed — {Class?.RejectReason ?? "unsupported leaf"} — recognized but not yet implemented")`), DataBinder.Ptr.cs:69 ("BASED item"), DataBinder.Ptr.cs:92 ("ADDRESS OF target record").

## 3. Guard-site inventory (every "Tier-C" / "IsImageCapable" site)

**Three distinct predicates are in play:**
- **P1 = `DataItem.IsImageCapable`** (DataItem.cs:303–313): leaf ∈ {Alphanumeric, NumericEdited, National, Boolean} ∪ {Numeric fixed-point, Usage Display/Binary/Packed}; group = all children; false for dynamic tables. Excludes float, COMP-5, INDEX, BINARY-CHAR/SHORT/LONG/DOUBLE. THE Tier-C-island predicate.
- **P2 = `DataItem.IsCharacterImage`** (DataItem.cs:278–288): stricter — numeric leaf qualifies only when `StoreAsImage`-promoted. At emit sites P2 ≈ P1 because whole-group usage promotes the leaves, but they are different predicates (drift hazard).
- **P3 = ForceStringCanonical's cell gate** (Display-only; refuses national/bit as well) — deliberately stricter.

### A. Bind-time verdict writers (single-sourced already)
| # | Site | Context guarded | Predicate | Message | ISO cite |
|---|------|-----------------|-----------|---------|----------|
| A1 | DataBinder.cs:2390–2396 | ComputeTier reject 1 | float/COMP-5/BINARY-*/INDEX leaf | "float/COMP-5/BINARY-*/INDEX REDEFINES of '{X}' (Tier-C byte path) not yet implemented" | §13.18.60 (comment) |
| A2 | DataBinder.cs:2402–2407 | ComputeTier reject 2 (national) | National leaf | "REDEFINES over national data in '{X}' … not yet implemented (Phase 4a residue)" | §13.18.44 + D-N1/D-N2 |
| A3 | DataBinder.cs:2304–2311 | dynamic-table REDEFINES | ContainsDynamicTable | COBOLNET1525 + reason "(§13.18.44 SR5, D9)" | §13.18.44 SR5 |
| A4 | DataBinder.Linkage.cs:392–402 | ForceStringCanonical (EXTERNAL/BASED/ADDRESS-OF cell re-base) | P3 | "{what} '{X}' has a COMP/float/index/national/bit leaf — … (Tier-C byte island / the RESIDUE-11 2-byte national layout, deferred)" | §13.18.22.4 GR5 (accept path) |
| A5 | DataBinder.Linkage.cs:360–366 | EXTERNAL record consumer of A4 | null class | COBOLNET0899 ExternalRecordNotCellBacked "EXTERNAL record '{X}' cannot be cell-backed — {RejectReason}" | §13.18.24 (catalog) |

### B. Bind-time loud-null / BoundUnsupported consumers of the class verdict
| # | Site | Context | Predicate | Message | ISO cite |
|---|------|---------|-----------|---------|----------|
| B1 | ReferenceResolver.cs:283–284 (PlaceForItem) | any reference to a non-canonical view of a Tier-C/Rejected class | `cls.Tier != Alias && != StringCanonical` | returns null → caller fails loud | — |
| B2 | ExpressionBinder.cs:149–157 (RefFailure) | loud-text builder for unresolvable refs | `Tier: Rejected, RejectReason: not null` | appends "— {RejectReason}" to "reference '{X}'" | rides A1/A2/A4 citations |
| B3 | InitializeBinder.cs:304 (InitializeMemberCursor.Child) | INITIALIZE descending into a class | `Tier != Alias` (non-B canonical) | null → loud | — |
| B4 | SortBinder.cs:51–53 (SortBindFile) | SORT SD record usability | !P1 via SortRecordOf (line 279–281) | "SORT '{X}' without a usable SD record (a COMP/binary-leaf record is the Tier-C byte island, deferred — COBOLNET_DESIGN §4.2)" | §14.9.40.3 SR4 context |
| B5 | SortBinder.cs:175–176 (BindMerge) | MERGE SD record | !P1 via SortRecordOf | "MERGE '{X}' without a usable SD record (Tier-C byte island, deferred)" | §14.9.24.3 context |
| B6 | SortBinder.cs:254–256 (BindReturn) | RETURN SD record area | SortRecordOf null | "RETURN '{name}' without a usable SD record area" | §14.9.34.3 SR1 context |
| B7 | UdfBinder.cs:206–213 | UDF group-RETURNING leaf | leaf not character-category and not Numeric+Display (stricter than P1 — Binary/Packed also rejected) | "a group RETURNING item with a non-character leaf ('{X}') is not yet carried — binary/packed/COMP-5/float/index/pointer/object leaves have no shared character image across the activation boundary (the Tier-C island, COBOLNET_DESIGN §4.2; a named residue)" | §14.8.3.2/§14.8.3.3 |
| B8 | OoConformance.cs:162 (ConformanceDescriptor) | universal-INVOKE descriptor for a group | !P1 | "T:!" sentinel (matches nothing) | §9.3.8.2.1 |
| B9 | OoConformance.cs:217–220 (DescriptionMismatch) | BY REFERENCE / override signature group pairs | !P1 (arg and formal separately) | "the argument/formal group has a float/COMP-5/INDEX leaf (no character image — Tier-C)" | §14.8.2.3.2 / §9.3.8.2 |
| B10 | OoConformance.cs:239 | group argument vs alphanumeric formal | !P1 | "the argument group has no character image (Tier-C)" | §14.8.2 |
| B11 | OoBinder.cs:585 (OoContentMismatch) | INVOKE BY CONTENT group argument | !P1 | "the argument group has no character image (Tier-C)" | §14.8.2.3.3 |
| B12 | OoBinder.cs:664–670 | universal INVOKE argument | descriptor == "T:!" | COBOLNET0866 "INVOKE: the argument '{X}' has no crossing form (a Tier-C group or a not-yet-carried category — mirrors the typed path's rejection)" | §14.9.23 |
| B13 | OoBinder.cs:686–691 | universal INVOKE RETURNING | descriptor == "T:!" | COBOLNET0866 "INVOKE RETURNING '{X}': no crossing form (Tier-C / not-carried)" | §14.9.23 |

### C. Emit-time loud guards (runtime `NotImplemented.Run/Value` via EmitCore.cs:72/75 LoudStmt/LoudValue)
| # | Site | Verb/context | Predicate | Exact message | ISO cite |
|---|------|--------------|-----------|---------------|----------|
| C1 | OperandText.cs:74–77 (AsString) | whole-group SENDER (WRITE/RELEASE/DISPLAY/compare) | !P1 | LoudValue string "whole-group image of '{X}' with a float/COMP-5/INDEX leaf (Tier-C byte island, deferred — COBOLNET_DESIGN §4.2)" | §8.5.2.1 + §13.18.60.4 GR4 (accept path) |
| C2 | NumericRenderer.cs:135–137 (FieldNumCore) | group operand in numeric context | !P2 | LoudValue long "numeric use of group item '{X}'" (Tier-C only in comment) | §8.5.2.1 / §14.6.13.2 |
| C3 | StringEmitter.cs:151–153 (WriteImage) | STRING INTO group | !P2 | LoudStmt "STRING INTO mixed-usage group '{X}' with a COMP/binary leaf (Tier-C byte path, deferred)" | §14.9.43.4 GR7 context |
| C4 | StringEmitter.cs:193–195 (MoveString) | UNSTRING INTO group | !P2 | LoudStmt "UNSTRING INTO mixed-usage group '{X}' with a COMP/binary leaf (Tier-C byte path, deferred)" | §14.9.48.4 GR11 context |
| C5 | SortEmitter.cs:224–226 (TableCompare) | table-SORT group key compare | !P2 | LoudValue int "table-sort key '{X}' over a mixed-usage group (Tier-C byte island, deferred)" | §8.8.4.2.7 context |
| C6 | CallEmitter.cs:184–187 (ArgText) | CALL USING group argument | !P2 (and not RedefViewPlace) | LoudValue string "CALL USING mixed-usage group '{X}' with a COMP/binary leaf (Tier-C byte island, deferred)" wrapped in a CobolArg cell | §14.2.3 GR8 context |
| C7 | SequentialIoEmitter.cs:378–382 (EmitImageInto) | READ/RETURN record-area distribution | !P1 | LoudStmt "record area '{X}' contains float/COMP-5/INDEX leaves — the Tier-C byte island (COBOLNET_DESIGN §4.2), deferred" | §13.18.60 GR4 / §14.4 (accept path) |
| C8 | SequentialIoEmitter.cs:414–417 (EmitStoreFileStatus) | FILE STATUS into group item | !P1 | LoudStmt "FILE STATUS into group '{X}' with a float/COMP-5/INDEX leaf (Tier-C byte island, deferred)" | §14.9.25.4 GR4 context |
| C9 | AcceptDisplayEmitter.cs:63–65 | ACCEPT into group | !P2 | LoudStmt "ACCEPT into mixed-usage group '{X}' with a COMP/binary leaf (Tier-C byte path, deferred)" | §14.9.1 GR3/GR4 context |
| C10 | AcceptDisplayEmitter.cs:124–126 | ACCEPT temporal into group | !P2 | LoudStmt "ACCEPT temporal into mixed-usage group '{X}' with a COMP/binary leaf (Tier-C byte path, deferred)" | §14.9.1.4 GR6–12 context |
| C11 | InspectEmitter.cs:89–91 (EmitStore) | INSPECT REPLACING/CONVERTING into group | !P2 | LoudStmt "INSPECT REPLACING/CONVERTING into mixed-usage group '{X}' with a COMP/binary leaf (Tier-C byte path, deferred)" | §14.9.22.4 GR4 context |
| C12 | MoveEmitter.cs:114–116 (EmitGroupToElementaryMove) | group→elementary MOVE receiver | !P1 | LoudStmt "group MOVE into '{X}' (a float/COMP-5/INDEX receiver has no character image — Tier-C, COBOLNET_DESIGN §4.2)" | §14.9.25.4 GR4 |
| C13 | MoveEmitter.cs:168–170 (EmitGroupMove) | MOVE into whole group (after AlignedLeafPairs memberwise fast path) | !P1 | LoudStmt "MOVE to group '{X}' with a float/COMP-5/INDEX leaf (Tier-C byte island, deferred — COBOLNET_DESIGN §4.2)" | §14.9.25.4 GR4 |

### D. Positive-side consumers / comment-only mentions (no loud, but the predicate's other half)
- RecordStructEmitter.cs:123 — `if (item.IsImageCapable) codec.EmitImageMethods(item, w)` — decides which groups GET AsImage/FromImage.
- PhysicalModel.cs:83–86 — numLeaf filter (Display/Binary/Packed only; COMP-5/INDEX excluded, comment cites IsImageCapable).
- GroupValueSlicer.cs:38–48 — DistributableSubtree: binary/packed/float leaves undistributable ("their character image is the Tier-C byte boundary") → member-wise default, no loud.
- SortBinder.cs:279–281 — `SortRecordOf` = the B4/B5/B6 fence predicate (`IsElementary || IsImageCapable`).
- StorageFormPass.cs:177 (comment: ByteCanonical/Rejected fall through to base classify — a Tier-C/Rejected VIEW member gets its plain base form) and :239 (`StorageForm.TierCWindow => "string", // unreachable today`).
- ReferenceResolver.cs:413–421 — `BuildBackingPath` doc says "Tier-B/Tier-C class's single stored backing" (would serve a byte backing identically).
- Comment-only: DataBinder.cs:392, MoveEmitter.cs:203, SequentialIoEmitter.cs:376, SortBinder.cs:253/277, OperandText.cs:72, NumericRenderer.cs:134, DataItem.cs:275/280, StorageForm.cs:64–66, RedefinesModel.cs:78/95, InitializeBinder.cs:294, ReferenceResolver.cs:240/282, ExpressionBinder.cs:146, DataBinder.Linkage.cs:356/364/400, DataBinder.Ptr.cs:16.

## 4. StorageForm.cs — TierCWindow state

`StorageForm.TierCWindow(RedefinesClass Class, int Offset, int Length, Usage Usage)` **EXISTS** (StorageForm.cs:67–71): `IsCharacterImage => false`, `ImageWidth => Length`. Doc: "QUARANTINED — unreachable corpus-wide today (the classifier rejects float/COMP-5/BINARY-*/INDEX/National Tier-C classes), so its parity obligation is that StorageFormPass assigns it to ZERO leaves." **There are NO Read/Write members** — StorageForm is a pure value classification (no emit logic); the design's "TierCWindow.Read/Write throw the internal-error backstop" has no as-built counterpart. StorageFormPass.Classify (:165–178) handles only StringCanonical (→ CharImage(Numeric) if promoted, else TierBWindow) and Alias (→ forward to canonical); ByteCanonical/Rejected fall through to base classification. StorageElementType maps `TierCWindow => "string" // unreachable today` (:239). There is no `TierCPlace`; RedefViewPlace is the only class-window Place.

## 5. GroupImageCodec.cs — the Tier-B string codec a byte path would parallel

- `ImageInitOf(DataItem)` (:21) — compile-time INITIAL-image composer seeding Tier-B backings (`PhysicalModel.PhysicalChildrenOf` yields ONE `Physical(cls.BackingCsName, "string", cls.Width, …, StrStore(ImageInitOf(canonical)))` per contained class, PhysicalModel.cs:66–68). Group = child-image concat skipping redefining children; leaf = VALUE formatted (CobolNum.FormatDisplay for numeric, StrStore for alnum/national, StrStoreBoolean, EditCompose for edited); OCCURS repeats via StrRepeat.
- `EmitImageMethods(DataItem group, CodeWriter w)` (:67–81) — emits on every IsImageCapable group:
  - `public readonly string AsImage() => <member1> + <member2> + …;` where each member is: nested group → `X.AsImage()`; string leaf → the field; native fixed-point leaf → `CobolNum.FormatDisplay(field, ImageProfileOf(leaf))`; fixed-OCCURS → `string.Concat(Array.ConvertAll(...))` per element (:88–95).
  - `public void FromImage(string __s)` — `__s = StrStore(__s, totalWidth)` pad/truncate, then per member at running offset: nested group → `FromImage(__s.Substring(off,w))`; native numeric → `(ClrType)CobolNum.ParseDisplay(__s.Substring(off,w), ImageProfileOf(leaf))`; string leaf → Substring assign; OCCURS → for-loop with per-occurrence width (:103–121).
  - `ImageProfileOf` (:131–137): signed BINARY/PACKED leaves image with `SignKind = ImageSignKind` (trailing overpunch — the §13.18.60 USAGE GR4 implementor license); DISPLAY leaves use their own `_P_` profile.
- **A Tier-C byte codec would parallel this exactly**: `AsBytes()/FromBytes(byte[])` over `PhysicalChildrenOf` with running BYTE offsets and per-`(Offset, Length, Usage)` leaf codecs (COMP big-endian two's-complement, COMP-3 packed nibbles, COMP-1/2 IEEE bits, DISPLAY Latin-1 chars) — the `TierCWindow(Class, Offset, Length, Usage)` shape already carries precisely those coordinates.

## 6. DESIGN-data-model.md §2.3 (docs/rearchitecture/DESIGN-data-model.md:188–200)

The 4-tier lattice "correct and kept". Prescriptions: (a) tier classification produces StorageForm per member (Tier-C view → TierCWindow); (b) Tier/Width become init-only, set once by "RedefinesClassifier"; (c) **Tier-C decision (resolves the ~10 scattered guards): implement the confined byte[] codec OR single-source the rejection. Recommends single-source-now-implement-later: one `RedefinesClassifier.RejectTierC(class, reason)` emitting one diagnostic code, `TierCWindow.Read/Write` throw the internal-error backstop, delete the ~10 inline guards; each guard's ISO citation kept in the single RejectTierC reason table (risk §5 item 3, golden per collapsed guard); full byte[] codec is a separately-scheduled increment — the sanctioned single byte boundary of invariant #1.** The doc's STATUS banner (line 3–11) marks the model IMPLEMENTED and records the as-built §2.3 deviation: facts written once through the ONE named `RedefinesClass.Classify` mutator; the cell forcer's re-classification is a real second write by design; "the Tier-C guards collapse into ComputeTier + IsImageCapable".

## 7. How a single-sourced RejectTierC + ONE predicate would route each site

The CLASS-side rejection is ALREADY single-sourced (ComputeTier + ForceStringCanonical → the one `Classify` mutator; `RejectReason` threaded to references by ExpressionBinder.RefFailure and CallMakeExternal). What remains scattered is the **classless mixed-usage-GROUP island** — 13 emit guards (C1–C13) + 7 bind conformance guards (B4–B13), each re-testing P1 or P2 with hand-rolled message text.

Routing under P11 Step C:
- **A1–A3 → RejectTierC(cls, reason)**: fold ComputeTier's two reject arms + the SR5 dynamic-table arm into one reason-table method on the classifier emitting ONE diagnostic code; `Classify` keeps the single write. A4 (cell gate) stays a deliberate second, stricter gate but calls the same RejectTierC to format/emit, keeping its national/bit legs.
- **B1/B2/B3 (class-verdict consumers)**: unchanged — they already consume the single verdict; B2 keeps riding RejectReason.
- **B4–B6 (SORT/MERGE/RETURN), B8–B13 (OO conformance), B7 (UDF)**: route the group test through the ONE predicate (`IsImageCapable`) and the ONE message formatter (e.g. `TierC.Reason(item, context)`); B7's extra Binary/Packed strictness and A4's Display-only strictness must either be preserved as named predicate variants or consciously widened (each needs a golden — the doc §5 risk-3 mitigation).
- **C1–C13**: replace each ad-hoc LoudStmt/LoudValue with one helper (LoudStmt(TierC.Reason(item, "STRING INTO"))-style), predicate unified on `IsImageCapable`. The P2 (`IsCharacterImage`) sites C2–C6/C9–C11 need a one-time proof that promotion always fires where P1 holds (whole-group operand ⇒ MarkImageForced) before swapping predicates — that is the only behavioral risk in the collapse.
- **TierCWindow backstop**: give StorageFormPass a ByteCanonical arm producing TierCWindow, and make any Place/emit encounter of it throw internal-error — today it simply never exists, and B1's null-return is the de-facto backstop.
- When Step D (the confined byte[] codec) lands, ONLY ComputeTier's arm 1 flips from Rejected to ByteCanonical; every guard above that routes through the one predicate/reason table then admits the class automatically — that is the payoff of the collapse.

### Key signatures / anchors

- `src/Cobol.Net.Compiler/Binding/Model/RedefinesModel.cs:37 — public enum RedefinesTier { Alias, StringCanonical, ByteCanonical, Rejected }`
- `src/Cobol.Net.Compiler/Binding/Model/RedefinesModel.cs:101 — internal void Classify(RedefinesTier tier, int width, string? rejectReason) — THE one verdict-application site, exactly two callers`
- `src/Cobol.Net.Compiler/Binding/Model/RedefinesModel.cs:79 — public string BackingCsName => "_redef_" + Canonical.CsName;`
- `src/Cobol.Net.Compiler/Binding/DataBinder.cs:2376 — private static RedefinesTier ComputeTier(RedefinesClass cls, out string? reject)`
- `src/Cobol.Net.Compiler/Binding/DataBinder.cs:2263 — internal void ClassifyRedefinesClasses() (BindPipeline row at Binding/Passes/BindPipeline.cs:40)`
- `src/Cobol.Net.Compiler/Binding/DataBinder.cs:2353 — private static void AssignClassOffsets(DataItem item, int off, RedefinesClass cls)`
- `src/Cobol.Net.Compiler/Binding/DataBinder.Linkage.cs:381 — internal RedefinesClass? ForceStringCanonical(DataItem item, string what) — the ONE cell-backing forcer (gate at :392, reject at :398-401)`
- `src/Cobol.Net.Compiler/Binding/Model/DataItem.cs:303 — public bool IsImageCapable (P1: the Tier-C island predicate; excludes float/COMP-5/INDEX/BINARY-CHAR..DOUBLE)`
- `src/Cobol.Net.Compiler/Binding/Model/DataItem.cs:278 — public bool IsCharacterImage (P2: stricter, StoreAsImage-dependent)`
- `src/Cobol.Net.Compiler/Binding/Model/StorageForm.cs:67 — public sealed record TierCWindow(RedefinesClass Class, int Offset, int Length, Usage Usage) : StorageForm — QUARANTINED, zero leaves, NO Read/Write members`
- `src/Cobol.Net.Compiler/Binding/Model/StorageForm.cs:58 — public sealed record TierBWindow(RedefinesClass Class, int Offset, int Width) : StorageForm — the Tier-B twin TierCWindow parallels`
- `src/Cobol.Net.Compiler/Binding/Passes/StorageFormPass.cs:239 — StorageForm.TierCWindow => "string", // unreachable today (and :177 fall-through comment)`
- `src/Cobol.Net.Compiler/CodeGen/DataDivision/GroupImageCodec.cs:67 — public void EmitImageMethods(DataItem group, CodeWriter w) — emits AsImage()/FromImage(string) (AsImageOf :88, EmitMemberFromImage :103, ImageProfileOf :131)`
- `src/Cobol.Net.Compiler/CodeGen/DataDivision/GroupImageCodec.cs:21 — public string ImageInitOf(DataItem item) — Tier-B backing seed`
- `src/Cobol.Net.Compiler/CodeGen/DataDivision/PhysicalModel.cs:66-68 — one Physical(cls.BackingCsName, "string", cls.Width, …) per contained class`
- `src/Cobol.Net.Compiler/CodeGen/Emit/EmitCore.cs:72,75 — LoudStmt => NotImplemented.Run(...); LoudValue<T> => NotImplemented.Value<T>(...)`
- `src/Cobol.Net.Compiler/Binding/Procedure/ExpressionBinder.cs:149 — private string RefFailure(DataReferenceContext) — threads Class.RejectReason into the loud text`
- `src/Cobol.Net.Compiler/Binding/ReferenceResolver.cs:283-284 — the loud-null for a Tier-C/Rejected view; :418 BuildBackingPath(RedefinesClass) for the backing field`
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/SortBinder.cs:279-281 — private static DataItem? SortRecordOf(FileModel) — the SD Tier-C fence (IsElementary || IsImageCapable)`
- `src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs:383 — ExternalRecordNotCellBacked descriptor (ISO §13.18.24)`

### ⚠ Discrepancies vs the P11 phase doc

- ComputeTier is at DataBinder.cs:2376 (called from ClassifyRedefinesClasses at :2300), NOT ~line 1752 as the task/phase doc states.
- The design doc's '~10 scattered guards' undercounts: today there are 13 emit-time loud guards (C1-C13) + ~13 bind-time guard/consumer sites (A/B tables) — roughly 26 active sites referencing the Tier-C island, plus ~20 comment-only mentions.
- DESIGN-data-model.md §3 (line 314) cites stale anchors 'DataBinder.cs:1606,1631,1642' and 'CSharpEmitter.cs:565' — CSharpEmitter.cs still exists but contains ZERO Tier-C guards today; the guards live in CodeGen/Verbs/* and CodeGen/Emit/OperandText.cs.
- No 'RedefinesClassifier' type exists (§2.3/§2.5 name): classification lives in DataBinder.ClassifyRedefinesClasses + ComputeTier, applied through RedefinesClass.Classify. RedefinesClass.Tier/Width are NOT init-only as §2.3 prescribes — they are private-set via the internal Classify mutator with two sanctioned callers (the doc's own STATUS banner records this deviation, including the cell forcer's deliberate second write).
- The prescribed 'RedefinesClassifier.RejectTierC(class, reason)' single-source does NOT exist; the STATUS banner claims 'the Tier-C guards collapse into ComputeTier + IsImageCapable', which is only half-true: the REDEFINES-CLASS rejection is single-sourced, but the classless mixed-usage-group guards remain scattered across ~20 sites with per-site message text.
- TierCWindow has NO Read/Write members (StorageForm is a pure classification record) — the §2.3 'TierCWindow.Read/Write throw the internal-error backstop' has no as-built counterpart; the de-facto backstop is ReferenceResolver.PlaceForItem returning null (caller fails loud) and StorageFormPass falling through to base classification for ByteCanonical/Rejected members.
- §2.3 says 'Tier-C view → TierCWindow' is produced by classification — in reality StorageFormPass NEVER constructs TierCWindow (its documented parity obligation is assigning it to ZERO leaves); RedefinesTier.ByteCanonical is never returned by ComputeTier (dead enum case by construction).
- NOT one predicate but three: IsImageCapable (float/COMP-5/INDEX/BINARY-* island), IsCharacterImage (stricter, promotion-dependent — used by STRING/UNSTRING/ACCEPT/INSPECT/CALL/table-SORT emit guards), and ForceStringCanonical's Display-only cell gate (also refuses national/bit); UdfBinder's group-RETURNING gate is a fourth variant (rejects Binary/Packed too). A single-predicate collapse must reconcile these deliberately.


---

# APPENDIX — P11 Step 2 — boolean conversions: the exact edit list (pre-verified against live code)

All anchors re-verified this session (see also PHASE-11-scout-notes.md `spec:boolean` + `code:renderer-runtime` §11).
Apply ONLY after the Step-1 commit lands and no battery is running.

## 1. Catalog (`src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs`)
- Row :133 → `"BooleanOfInteger"`, `IntrinsicBind.Runtime` (keep Type Boolean, "ii", 2002).
- Row :161 → `"IntegerOfBoolean"`, `IntrinsicBind.Runtime` (keep Type Integer, "s", 2002).
- `ResultCategory` (:46-51): add `IntrinsicType.Boolean => PicCategory.Boolean,` and REWRITE the doc
  comment's parenthetical ("Boolean-type rows are all Deferred; they fall to Numeric unchanged" is now false —
  a boolean function result IS category boolean, §15.2 item 2 / §8.5.2.5 item 4).

## 2. The four channel seams (scout `code:renderer-runtime` §11 — Boolean flows like National)
- `CodeGen/Emit/OperandText.cs:32` AsString entry intercept: `Alphanumeric or National` → add `or PicCategory.Boolean`.
- `OperandText.cs:147-148` IsStringVisitor computed arm: same widening (boolean-result intrinsic compares as text).
- `CodeGen/Emit/IntrinsicRenderer.cs:396` StrArgVisitor nested arm: same widening (nested boolean-result argument).
- `IntrinsicRenderer.cs:54-55` RenderNum string-class guard: add Boolean (a boolean-result function in a numeric
  context is loud BY NAME, never a wrong value).

## 3. Renderer arms (`IntrinsicRenderer.cs`)
- RenderNum: `case "IntegerOfBoolean": return new NumX(RuntimeApi.Intrinsic(sig.RuntimeMethod, Str(ic.Args[0])), 0);   // §15.45.4 r1 — unsigned MSB-first value, scale 0`
- RenderString: `"BooleanOfInteger" => RuntimeApi.Intrinsic(sig.RuntimeMethod, $"{ArgInt(ic.Args[0])}, {ArgInt(ic.Args[1])}"),   // §15.13.4 r1`

## 4. Runtime bodies (`src/Cobol.Net.Runtime/Intrinsics/CobolIntrinsics.Text.cs`, new region at the end)
```csharp
// ── FUNCTION BOOLEAN-OF-INTEGER / INTEGER-OF-BOOLEAN (§15.13 / §15.45) — boolean conversions ─────────────

/// <summary>FUNCTION BOOLEAN-OF-INTEGER (ISO §15.13.4 r1): the boolean value whose bit configuration is the
/// binary representation of argument-1 — rightmost boolean position = low-order binary digit — zero-filled
/// or TRUNCATED ON THE LEFT to exactly argument-2 boolean positions (left truncation is NORMAL, not an
/// error: the result is argument-1 mod 2^argument-2 — Annex D.10's 544→low-6-bits worked example).
/// §15.13.3: argument-2 shall be a positive nonzero integer (r2); argument-1 shall be positive (r1) —
/// WiseOwl COBOL accepts 0 (all-zero bits; the r1-vs-r2 "positive"/"positive nonzero" drafting contrast, scout-
/// resolved) and rejects negatives via EC-ARGUMENT-FUNCTION (§15.3). The documented WiseOwl COBOL maximum
/// returned-value length (§15.4) is the §8.3.3.4.3 SR1 boolean maximum, 8 191 positions.</summary>
public static string BooleanOfInteger(long value, long length)
{
    if (length < 1 || length > 8191)
    {
        Exceptions.ExceptionState.ArgumentError(
            $"FUNCTION BOOLEAN-OF-INTEGER argument-2 {length} is not in 1..8191 (§15.13.3 r2; §15.4)");
        return "0";
    }
    if (value < 0)
    {
        Exceptions.ExceptionState.ArgumentError(
            $"FUNCTION BOOLEAN-OF-INTEGER argument-1 {value} is negative (§15.13.3 r1)");
        return new string('0', (int)length);
    }
    var chars = new char[length];
    for (int i = 0; i < length; i++)   // rightmost position = the low-order digit (§15.13.4 r1)
        chars[length - 1 - i] = i < 63 && ((value >> i) & 1) != 0 ? '1' : '0';
    return new string(chars);
}

/// <summary>FUNCTION INTEGER-OF-BOOLEAN (ISO §15.45.4 r1): the unsigned binary value of argument-1's bit
/// configuration, MSB first, over a temporary boolean item sized to argument-1 (r1a/r1b). WiseOwl COBOL's
/// integer channel is a signed 64-bit long, so a configuration above 63 significant bits takes
/// EC-ARGUMENT-FUNCTION via the §15.4 maximum-returned-value hook (documented). A zero-length argument
/// (a zero-length BX literal, §8.3.3.4.3) is value 0 — the natural reading of an empty configuration.</summary>
public static long IntegerOfBoolean(string boolean)
{
    long v = 0;
    foreach (char c in boolean)
    {
        if (c is not ('0' or '1'))
            return Exceptions.ExceptionState.ArgumentError(
                "FUNCTION INTEGER-OF-BOOLEAN argument-1 is not of class boolean (§15.45.3 r1)");
        if (v > (long.MaxValue >> 1) - (c - '0'))   // the next shift would exceed 63 significant bits
            return Exceptions.ExceptionState.ArgumentError(
                "FUNCTION INTEGER-OF-BOOLEAN value exceeds the 63-bit WiseOwl COBOL integer maximum (§15.4)");
        v = (v << 1) | (uint)(c - '0');
    }
    return v;
}
```
(Overflow guard: `v > (long.MaxValue >> 1) - bit` ⇔ `2v + bit > long.MaxValue` without overflowing. Verify with a
unit-style golden if in doubt; the corpus golden stays within 8 bits.)

## 5. Corpus (files pre-authored in this scratchpad dir)
- `tests/conformance/2002/intrinsics_boolean_conv.cob` + `.out` (copy from here) + `"intrinsics_boolean_conv"`
  into `tests/conformance/2002/manifest.json` "enabled".
- Legacy exclusion SAME COMMIT: `("2002", "intrinsics_boolean_conv")` + comment into `GreenfieldOnly`,
  `tests/CobolSharp.Tests.Integration/ConformanceTests.cs` (frozen legacy has no boolean functions).
- `tests/conformance/negative/boolean_conv_below_2002.cob` + `.err` (copy from here) + manifest "enabled" entry.
- Matrix row in `tests/version-matrix/constructs.json` (id `boolean-of-integer-2002`, introducedIn 2002,
  expectDiagnostic COBOLNET1502, citation §15.13/§15.45, source = the nested COMPUTE program from the negative
  row without the reject header) → re-run `scripts/gen-constructs.ps1` → commit BOTH regenerated .g.cs files
  (`src/Cobol.Net.Editions/Constructs.g.cs` + `ConstructRegistry.g.cs`).
- Optional: one `AssertSpec` fact in `IntrinsicFunctionDifferentialTests.cs` (dialect 2002) pinning T3/T5 values.

## 6. Verify (before the battery)
- CLI spot-run: `dotnet build src/Cobol.Net.Cli && dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll
  <scratch>/intrinsics_boolean_conv.cob --std 2002 -o E:/Temp/claude/p11boolconv.dll --run` → expect the .out
  content EXACTLY (T3=128 proves MSB-first; T2 proves left-truncate + right-fill composition).
- Negative spot: compile at --std 85 → COBOLNET1502 naming both functions.
- Then: build sln → stage → guard-fast → conformance → unit → commit (msg pattern:
  `feat(cobolnet): P11 Step 2 — FUNCTION BOOLEAN-OF-INTEGER/INTEGER-OF-BOOLEAN (§15.13/§15.45) on the P10
  boolean base + golden + 85-window row (DEVLOG NNN)`).

## Known hazards (checked, do not re-derive)
- MOVE legality: `MoveCategoryLegality` already maps `BoundComputedOperand{BoundIntrinsicCall}` →
  `ic.ResultCategory` (MoveBinder.cs:162) — boolean→boolean MOVE is legal once ResultCategory says Boolean.
- MOVE store path: MoveEmitter.cs:343 `StrStoreBoolean(OperandText.AsString(source…))` — flows through the
  widened AsString intercept automatically (§14.6.8.6 left-justify right-zero-fill).
- Boolean literals bind as `BoundStringLiteral{Category=Boolean}` (ExpressionBinder.cs:92-101) — StrArgVisitor
  renders them via CsLiteral with NO change.
- BX"…" literals are NOT lexed yet (boolean_data.cob header: deferred) — keep them out of goldens.
- Ref-mod over bit items: unproven on the greenfield — kept OUT of the Step-2 golden (T3 uses the whole item).
- DISPLAY FUNCTION <numeric-result> is LOUD by design — goldens COMPUTE/MOVE into a PIC receiver first.

### Pre-authored golden `tests/conformance/2002/intrinsics_boolean_conv.cob`

```cobol
      *> ISO §15.13 BOOLEAN-OF-INTEGER / §15.45 INTEGER-OF-BOOLEAN — the boolean-conversion pair (P11 Step 2).
      *> Values hand-derived in docs/rearchitecture/PHASE-11-scout-notes.md (spec:boolean): §15.13.4 r1 —
      *> rightmost boolean position = low-order binary digit, zero-filled or TRUNCATED ON THE LEFT to
      *> argument-2 positions (truncation is NORMAL: Annex D.10's 544 → low-6-bits worked example); the MOVE
      *> into a longer PIC 1(n) receiver then right-zero-fills (§14.6.8.6 — the OPPOSITE end). §15.45.4 r1 —
      *> the unsigned binary value of the whole bit configuration, MSB first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11BOOLCONV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIT-8      PIC 1(8) USAGE BIT.
       01 INT-ITEM   PIC 9(5) VALUE 544.
       01 N-3        PIC 9(3).
       01 N-5        PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
      *> §15.13.4 r1 exact fit: 5 = 101 over 8 positions, left zero-fill.
           MOVE FUNCTION BOOLEAN-OF-INTEGER(5, 8) TO BIT-8
           IF BIT-8 = B"00000101" DISPLAY "T1=OK"
              ELSE DISPLAY "T1=" BIT-8 END-IF
      *> Annex D.10: 544 = 0b1000100000 → the low 6 bits B"100000"; the MOVE into PIC 1(8)
      *> left-justifies and right-zero-fills (§14.6.8.6) → B"10000000".
           MOVE FUNCTION BOOLEAN-OF-INTEGER(INT-ITEM, 6) TO BIT-8
           IF BIT-8 = B"10000000" DISPLAY "T2=OK"
              ELSE DISPLAY "T2=" BIT-8 END-IF
      *> §15.45.4 r1 MSB-first: B"10000000" = 128 (an LSB-first bug would yield 1).
           COMPUTE N-3 = FUNCTION INTEGER-OF-BOOLEAN(BIT-8)
           DISPLAY "T3=" N-3
      *> §15.45.4 r1 over a boolean literal argument (§15.3 item 3).
           COMPUTE N-3 = FUNCTION INTEGER-OF-BOOLEAN(B"00000101")
           DISPLAY "T4=" N-3
      *> Round-trip identity for 0 <= n < 2^k (§15.13.4 r1 ∘ §15.45.4 r1, both unsigned MSB-first).
           COMPUTE N-5 = FUNCTION INTEGER-OF-BOOLEAN(
               FUNCTION BOOLEAN-OF-INTEGER(200, 8))
           DISPLAY "T5=" N-5
      *> §15.13.4 r1 left truncation: 256 mod 2^8 = 0 → all-zero bits.
           MOVE FUNCTION BOOLEAN-OF-INTEGER(256, 8) TO BIT-8
           IF BIT-8 = B"00000000" DISPLAY "T6=OK"
              ELSE DISPLAY "T6=" BIT-8 END-IF
           STOP RUN.
```

### Pre-authored `.out`

```
T1=OK
T2=OK
T3=128
T4=005
T5=00200
T6=OK
```

### Pre-authored negative `tests/conformance/negative/boolean_conv_below_2002.cob` (`.err` = `COBOLNET1502`)

```cobol
      *> reject-at: 85
      *> ISO §15.13 BOOLEAN-OF-INTEGER / §15.45 INTEGER-OF-BOOLEAN are COBOL-2002 introductions (the
      *> boolean-data amendment; PHASE-11-scout-notes.md spec:boolean). Below 2002 the D8 catalog window
      *> rejects each reference BY NAME — COBOLNET1502 (IntrinsicBinder window gate).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11BOOLW85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-5 PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N-5 = FUNCTION INTEGER-OF-BOOLEAN(
               FUNCTION BOOLEAN-OF-INTEGER(5, 8))
           STOP RUN.
```
