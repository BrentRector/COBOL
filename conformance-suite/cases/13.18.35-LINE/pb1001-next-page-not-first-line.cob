*>--- conformance-case
*> id: pb1001-next-page-not-first-line
*> rules: [SR-13.18.35.3-7]
*> source-format: free
*> portable: unverified
*>---
*> A NEXT PAGE PHRASE ON A LATER LINE CLAUSE OF THE GROUP (kb/Work PB1001). ISO/IEC 1989:2023 §13.18.35.3
*> SR7: "Within a given report group description, a NEXT PAGE phrase, if present, shall be specified only in
*> the first LINE clause." The phrase acts before any line is printed (§13.18.35.4 GR4a: the page fit is
*> declared unsuccessful), so on a second line it would split the group across two pages, which GR2 forbids.
*> A rule of every edition: `LINE integer-1 ON NEXT PAGE` is COBOL-85 syntax.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1001NF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1001-next-page-not-first-line.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20 LINES.
       01  DE-1 TYPE DETAIL.
           05  LINE 2.
               10  COLUMN 1 PIC X(5) VALUE "FIRST".
           05  LINE 4 ON NEXT PAGE.
               10  COLUMN 1 PIC X(6) VALUE "SECOND".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DE-1.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
