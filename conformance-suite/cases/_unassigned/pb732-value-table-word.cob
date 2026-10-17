*>--- conformance-case
*> id: pb732-value-table-word
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB732 — arm: Format 2 (table) VALUE. ISO 13.18.63.2 Format 2 writes
*> `{{literal-1}... FROM ({subscript-1}...) [TO ({subscript-2}...)]}...`, so each occurrence literal is
*> the same literal position -> COBOLNET1639 for an undefined word (8.4.2.1). Pre-fix the table's
*> occurrences were initialized to the word's own spelling with no diagnostic. Rejected at COBOL-85 too,
*> where the introduction gate (COBOLNET0900, Format 2 is COBOL-2002) reports IN ADDITION.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB732E.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 TBL.
   05 T PIC X(7) OCCURS 3 TIMES VALUE NOSUCHW FROM (1).
PROCEDURE DIVISION.
    DISPLAY T(1).
    STOP RUN.
