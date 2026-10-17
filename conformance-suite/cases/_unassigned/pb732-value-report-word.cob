*>--- conformance-case
*> id: pb732-value-report-word
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB732 — arm: Format 4 (report-section) VALUE. ISO 13.18.63.2 Format 4 writes `{literal-1}...`
*> for a report printable item, the same literal position as Format 1 -> COBOLNET1639 for an undefined
*> word (8.4.2.1). Pre-fix the report printed the word's own spelling into the report file and the
*> program exited 0 with no diagnostic.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB732F.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RPTF ASSIGN TO "pb732-report-negative.txt".
DATA DIVISION.
FILE SECTION.
FD RPTF REPORT IS R1.
WORKING-STORAGE SECTION.
01 W PIC 9 VALUE 1.
REPORT SECTION.
RD R1.
01 DET TYPE DETAIL.
   05 LINE 1.
      10 COLUMN 1 PIC X(7) VALUE NOSUCHW.
PROCEDURE DIVISION.
    OPEN OUTPUT RPTF.
    INITIATE R1.
    GENERATE DET.
    TERMINATE R1.
    CLOSE RPTF.
    STOP RUN.
