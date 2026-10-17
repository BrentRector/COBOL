*>--- conformance-case
*> id: pb482-sum-addend-ref-modified
*> rules: [SR-13.18.54.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.54.3 SR5 - "If the addend is identifier-1, it shall specify a
      *> numeric data item not defined in the report section" - and ISO
      *> 8.4.3.3.4 GR6 c) - "the categories numeric and numeric-edited are
      *> considered class and category national if the usage is national;
      *> otherwise they are considered class and category alphanumeric".  So the
      *> unique data item reference modification creates over a USAGE DISPLAY
      *> numeric item is ALPHANUMERIC, and no reference-modified spelling can be
      *> the numeric data item SR5 requires.  The ref-mod itself is well formed
      *> (8.4.3.3.3 SR1 admits "a numeric data item of usage display"); it is the
      *> SUM clause that cannot take its result.
      *> MEASURED BEFORE kb/Work PB482: the capture kept only the base word and
      *> its qualifiers, so the modifier was DROPPED and the WHOLE six-digit item
      *> was summed - a silent wrong answer, with no diagnostic at any edition.
      *> The SUBSCRIPTED spelling is legal and is witnessed by
      *> 85/pb482_sum_addend_written_reference_85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB482N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb482n2.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-NUM PIC 9(6) VALUE 123456.
       REPORT SECTION.
       RD R-1 CONTROL IS FINAL PAGE LIMIT IS 20 LINES.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
       01 CFT TYPE CF FINAL LINE PLUS 1.
          02 COLUMN 7 PIC 9999 SUM WS-NUM(1:2).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
