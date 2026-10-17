*>--- conformance-case
*> id: pb1002-unpaged-next-page
*> rules: [SR-13.18.35.3-5]
*> source-format: free
*> portable: unverified
*>---
*> A BARE `ON NEXT PAGE` LINE OPERAND IN A REPORT THAT IS NOT DIVIDED INTO PAGES (kb/Work PB1001 + PB1002).
*> ISO/IEC 1989:2023 §13.18.35.3 SR5: "If the report is not divided into pages, all its LINE clauses shall be
*> relative." The relative FORM is `{PLUS|+} integer-2` (SR3: "Integer-2 specifies a relative line number"), and
*> the bare operand is not it - the reading §13.18.37.3 SR3 gives NEXT GROUP NEXT PAGE ("only the relative form of
*> the clause may be specified"); a report of one page of indefinite length (§13.18.39.4 GR2a) has no next page
*> to begin. A DETERMINATION (docs/CONFORMANCE.md, the LINE NEXT PAGE block). Rejected at every edition: the
*> report writer is not edition-gated (docs/VERSION_CHANGE_REFERENCE.md row 130d).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1002UN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1002-unpaged-next-page.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       REPORT SECTION.
       RD  R1.
       01  DE-1 TYPE DETAIL LINE NEXT PAGE.
           05  COLUMN 1 PIC X(5) VALUE "WORLD".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DE-1.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
