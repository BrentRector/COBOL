*>--- conformance-case
*> id: pb506_report_operand_lists_2014
*> rules: [SR-13.18.63.3-34, SR-13.18.63.3-35, GR-13.18.63.4-23]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> THE MIDDLE EDITION'S ARM of the operand-list rules its 2002 and 2023 twins prove
      *> (tests/conformance/2002/pb506_report_operand_lists_2002,
      *>  tests/conformance/2023/pb506_report_value_operand_list; kb/Work PB506).
      *> ISO §13.18.63.3 SR35 (VALUE) and §13.18.53.3 SR6 (SOURCE) carry no version proviso, Annex E lists no
      *> 2014→2023 change to either, and the repetition vehicle they require — §13.15.4 GR3's "a LINE or COLUMN
      *> clause with more than one operand" — has existed since COBOL-2002 along with the COLUMNS/ARE and SOURCES
      *> spellings.  So the whole construct shall compile and run at COBOL-2014 exactly as it does at 2002 and
      *> 2023, and there is no gating diagnostic to check here: nothing this program writes is edition-gated at
      *> 2014.  The binder's operand list, the SR35/SR6 screen and the emitter's cycling all carry no version
      *> predicate, so a divergence between the editions could only come from a gate — which would show up here
      *> as a COBOLNET0900 rejection.
      *>
      *> WHY THIS PROGRAM DOES NOT READ THE REPORT FILE BACK, and the 2023 twin does.  The read-back needs
      *> ORGANIZATION IS LINE SEQUENTIAL, which is itself a COBOL-2023 introduction (§12.4.5.10.3 GR2 — the
      *> Foreword lists it among the main changes over ISO/IEC 1989:2014), so it cannot appear in a COBOL-2014
      *> program at all (the kb/Work PB688 precedent, which removed exactly this read-back from an 85 fixture).
      *> The CONTENT assertions therefore live in the 2023 twin.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  One INITIATE, two GENERATEs of the same TYPE DE group, one TERMINATE;
      *> nothing is control-broken and nothing is suppressed, so the program simply reaches its final DISPLAY.
      *> Expected: GEN-1, GEN-2, DONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB506OL14.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb506ol14.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-OL.
       WORKING-STORAGE SECTION.
       01  WS-P    PIC X(3)  VALUE "PPP".
       01  WS-Q    PIC X(3)  VALUE "QQQ".
       REPORT SECTION.
       RD  R-OL PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMNS ARE 1 6 11 PIC X(3) VALUES ARE "AAA" "BBB" "CCC".
           03  COLUMNS ARE 16 21  PIC X(3) SOURCES ARE WS-P WS-Q.
           03  COLUMN 26 PIC XXBXX VALUE "AB CD".
           03  COLUMN 34 PIC X(5) JUSTIFIED RIGHT VALUE "AB".
           03  COLUMN 40 PIC ZZZ9 BLANK WHEN ZERO VALUE "0000".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-OL.
           DISPLAY "GEN-1".
           GENERATE DET.
           DISPLAY "GEN-2".
           GENERATE DET.
           TERMINATE R-OL.
           CLOSE PRT.
           DISPLAY "DONE".
           STOP RUN.
