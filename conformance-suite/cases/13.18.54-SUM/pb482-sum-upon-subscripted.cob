*>--- conformance-case
*> id: pb482-sum-upon-subscripted
*> rules: [SR-13.18.54.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.54.3 SR7 - "Data-name-2 shall be the name of a detail. It may
      *> be qualified only by a report-name."  A report group is NAMED, never
      *> indexed: ISO 8.4.2.3.3 SR2 permits a subscript only where the entry
      *> "contains an OCCURS clause or is subordinate to a data description entry
      *> that contains an OCCURS clause", which a report group description entry
      *> is not, and where a general format writes data-name-n the reference is a
      *> qualified-data-name, not an identifier - 8.4.3.3.3's NOTE: "Because the
      *> references to data items are restricted to identifiers, where
      *> data-name-n is used in a general format or syntax rule, then reference
      *> modification is not permitted."
      *> MEASURED BEFORE kb/Work PB482: the whole suffix was thrown away with the
      *> first-word reduction `up.cobolWord()?.GetText()`, so DET-A(2) bound as
      *> DET-A and this program ran as though the subscript had not been written.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB482N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb482n4.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 99 VALUE 11.
       REPORT SECTION.
       RD R-1 CONTROL IS FINAL PAGE LIMIT IS 20 LINES.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
       01 CFT TYPE CF FINAL LINE PLUS 1.
          02 COLUMN 7 PIC 9999 SUM WS-A UPON DET-A(2).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
