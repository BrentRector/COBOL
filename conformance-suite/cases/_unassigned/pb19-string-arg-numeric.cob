*>--- conformance-case
*> id: pb19-string-arg-numeric
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> PB19 - LOWER-CASE's argument rule (15.57.3 r1) requires class alphabetic, alphanumeric, or national, and was enforced NOWHERE: the
*> function was absent from IntrinsicArgumentRules.Verified, so CheckArgumentClasses returned at its
*> TryGetValue guard and an operand of the wrong class was silently accepted and reinterpreted.
*> The legal shapes are pinned by conformance:cases/15.49.3-Argument-rule/pb19_argument_class_batch5.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB19.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 N PIC 9(4) VALUE 1234.
01 S PIC X(8).
PROCEDURE DIVISION.
MAIN.
    MOVE FUNCTION LOWER-CASE(N) TO S
    STOP RUN.
