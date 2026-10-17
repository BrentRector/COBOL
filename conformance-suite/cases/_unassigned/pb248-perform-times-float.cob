*>--- conformance-case
*> id: pb248-perform-times-float
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB248 - THE SIBLING OUTSIDE THE INTRINSIC SCREEN, found by the rule-4 sweep. The compiler's
*> "ONE is-this-operand-an-integer classifier" (IntrinsicResultType.IsIntegerOperand) carried the SAME
*> missing conjunct, and it is what enforces every statement rule that says "shall be an integer".
*> 14.9.28.3 SR2: "Identifier-1 shall be an integer" (--check verified). ⛔ NOTE THE ASYMMETRY WITH THE
*> INTRINSIC HALF: 14.9.28.3 is a SYNTAX RULES subclause, so here 5.5 2) - "When the term 'integer' is used
*> as a constraint for an operand in a syntax rule ... a FIXED-POINT numeric data item ... whose description
*> does not include any digit positions to the right of the radix point" - GOVERNS directly, where for an
*> intrinsic ARGUMENT rule it only corroborates (5.3.1). The classifier's data-item arm read
*> the SCALE alone - category numeric, scale <= 0, usage not INDEX - and a floating-point item is
*> PICTURE-less, so its synthesized profile carries Scale 0 and it answered TRUE. `PERFORM ... WS-F TIMES`
*> over a COMP-2 holding 3.7 therefore compiled clean and iterated three times, with no diagnostic at all.
*> The same classifier picks the INTEGER-vs-NUMERIC result row for every all-integer-arguments function
*> (15.2 type 5), so a floating-point argument also selected the wrong returned-value type there.
*> BOTH SCREENS NOW READ ONE PREDICATE - PicInfo.IsIntegerDescription, 5.5 2)b)2.'s conjunction written
*> down once - and they are complements over the scale (one asks > 0, the other <= 0), so neither can grow
*> an arm the other lacks.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB248PERFTIMES.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-F USAGE COMP-2.
01 N PIC 9(4) VALUE 0.
PROCEDURE DIVISION.
MAIN.
    MOVE 3.7 TO WS-F.
    PERFORM ADD-ONE WS-F TIMES.
    STOP RUN.
ADD-ONE.
    ADD 1 TO N.
