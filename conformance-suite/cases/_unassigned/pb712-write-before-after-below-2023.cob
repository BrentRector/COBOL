*>--- conformance-case
*> id: pb712-write-before-after-below-2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> The PRINTED spelling of the combination — one ADVANCING, one operand, both words (ISO §14.9.51.2
*> Format 1, measured off PDF page 815 / printed folio 785). It is a COBOL-2023 INTRODUCTION: Annex §E.3.3
*> item 2, "BEFORE and AFTER phrases. Both BEFORE and AFTER are allowed together in WRITE ADVANCING", and
*> docs/VERSION_CHANGE_REFERENCE.md row 45 records the 2014->2023 delta (old: the two could not both be
*> specified). So COBOL-85, COBOL-2002 and COBOL-2014 must REFUSE it by edition, with COBOLNET0900 naming
*> the construct — not with a bare parse error, which is what the pre-PB712 grammar produced for this exact
*> source because it could not spell the printed format at all. The 2023 arm is the positive golden
*> tests/conformance/2023/write_before_and_after.cob. kb/Work PB712.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB712NEG3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPF ASSIGN TO "pb712neg3.prt".
       DATA DIVISION.
       FILE SECTION.
       FD LPF LINAGE IS 4 LINES.
       01 P-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT LPF.
           MOVE "AAAA" TO P-REC.
           WRITE P-REC BEFORE AFTER ADVANCING 2 LINES.
           CLOSE LPF.
           STOP RUN.
