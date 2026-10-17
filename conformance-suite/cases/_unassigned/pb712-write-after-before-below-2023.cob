*>--- conformance-case
*> id: pb712-write-after-before-below-2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> THE SECOND ARM of the introduction gate: §5.2.6.4 says the alternatives inside choice indicators "may be
*> specified in any order", so AFTER BEFORE ADVANCING n is the same COBOL-2023 combination as
*> BEFORE AFTER ADVANCING n and must be refused by the same edition gate, with the same COBOLNET0900 naming
*> write-before-and-after-advancing-2023. A gate that keyed on the first word, or on a phrase count, would
*> pass one order and not the other — which is the defect shape kb/Work PB712 was: the gate counted PHRASES.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB712NEG4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb712neg4.prt".
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS 4 LINES.
       01 P-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LPF.
           MOVE "AAAA" TO P-REC.
           WRITE P-REC AFTER BEFORE ADVANCING 2 LINES.
           CLOSE LPF.
           STOP RUN.
