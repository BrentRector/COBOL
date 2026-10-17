*>--- conformance-case
*> id: pb1001-next-page-in-page-heading
*> rules: [SR-13.18.35.3-8]
*> source-format: free
*> portable: unverified
*>---
*> A NEXT PAGE PHRASE IN A PAGE HEADING (kb/Work PB1001). ISO/IEC 1989:2023 §13.18.35.3 SR8: "The NEXT PAGE
*> phrase may appear only in the description of a body group or a report footing." A page heading is printed
*> BY a page advance (§13.18.57.4 GR6b), so it cannot ask for one. A rule of every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1001PH.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1001-next-page-in-page-heading.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20 LINES.
       01  PH-1 TYPE PAGE HEADING LINE 1 ON NEXT PAGE.
           05  COLUMN 1 PIC X(4) VALUE "HEAD".
       01  DE-1 TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "BODY".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DE-1.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
