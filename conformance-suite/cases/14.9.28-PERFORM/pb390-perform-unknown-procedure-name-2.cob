*>--- conformance-case
*> id: pb390-perform-unknown-procedure-name-2
*> rules: [SR-14.9.28.3-13]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB390 - ISO 14.9.28.3 SR13, the procedure-name-2 twin of SR12: "Procedure-name-2 shall be the
*> name of either a paragraph or a section in the same source element as that in which the PERFORM
*> statement is specified." Two rules, two if blocks, ONE shape - the two-arm defect this project keeps
*> finding, so the THRU arm gets its own case. Note what must NOT be rejected with it: an INVERTED range
*> (procedure-name-2 physically preceding procedure-name-1) is legal, 14.9.28.4 GR6, and the positive
*> golden tests/conformance/85/pb390_procedure_and_condition_operands.cob keeps the THRU composition green.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB390PERFSR13.
PROCEDURE DIVISION.
MAIN.
    PERFORM P1 THRU NO-SUCH-PARA.
    STOP RUN.
P1.
    DISPLAY "P1".
P2.
    DISPLAY "P2".
