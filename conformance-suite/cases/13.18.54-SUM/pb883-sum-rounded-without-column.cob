*>--- conformance-case
*> id: pb883-sum-rounded-without-column
*> rules: [SR-13.18.54.3-3, GR-13.18.54.4-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.54.3 SR3 - "The ROUNDED phrase may be specified in the SUM
      *> clause only if the COLUMN clause is specified for the subject of the
      *> entry."  The entry below carries a SUM clause with a ROUNDED phrase and
      *> no COLUMN clause, so it defines no printable item (13.18.14).  The rule
      *> exists because 13.18.54.4 GR4 is the phrase's only general rule and it
      *> opens "If the entry also contains a COLUMN clause, the sum counter acts
      *> as a source data item" - with nothing to deliver the counter to, the
      *> phrase would have nothing to round.  13.18.54.4 GR1 still establishes
      *> the counter (an unprintable SUM entry is legal), which is why only the
      *> ROUNDED phrase is refused and not the entry.  Before kb/Work PB883 the
      *> phrase had no grammar surface, so SR3 had nothing to screen.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB883N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb883n1.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9V99 VALUE 1.55.
       REPORT SECTION.
       RD R-1 CONTROL IS FINAL PAGE LIMIT IS 20 LINES.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
       01 CFT TYPE CF FINAL LINE PLUS 1.
          02 CF-S PIC 9999 SUM WS-A ROUNDED.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
