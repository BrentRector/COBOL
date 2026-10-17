*>--- conformance-case
*> id: pb19-boolean-arg-alphanumeric
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> PB19 - INTEGER-OF-BOOLEAN's argument rule (15.45.3 r1) requires class boolean, and was enforced NOWHERE: the
*> function was absent from IntrinsicArgumentRules.Verified, so CheckArgumentClasses returned at its
*> TryGetValue guard and an operand of the wrong class was silently accepted and reinterpreted.
*> The legal shapes are pinned by conformance:cases/15.49.3-Argument-rule/pb19_argument_class_batch5.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB19.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC X(8) VALUE "10101010".
01 R PIC 9(4).
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN(X)
    STOP RUN.
