*>--- conformance-case
*> id: pb19-numeric-arg-alphanumeric
*> rules: [AR-15.49.3-1]
*> source-format: free
*> portable: unverified
*>---
*> PB19 - INTEGER-PART's argument rule (15.49.3 r1) requires class numeric, and was enforced NOWHERE: the
*> function was absent from IntrinsicArgumentRules.Verified, so CheckArgumentClasses returned at its
*> TryGetValue guard and an operand of the wrong class was silently accepted and reinterpreted.
*> The legal shapes are pinned by conformance:2023/pb19_argument_class_batch5.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB19.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC X(4) VALUE "12.5".
01 R PIC S9(4)V99.
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FUNCTION INTEGER-PART(X)
    STOP RUN.
