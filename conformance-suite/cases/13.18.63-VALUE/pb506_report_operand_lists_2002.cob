*>--- conformance-case
*> id: pb506_report_operand_lists_2002
*> rules: [SR-13.18.63.3-34, SR-13.18.63.3-35, GR-13.18.63.4-21, GR-13.18.63.4-23]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> THE EARLIEST EDITION AT WHICH THE OPERAND LIST CAN EXIST (kb/Work PB506).
      *> ISO §13.18.63.3 SR35 (VALUE) and §13.18.53.3 SR6 (SOURCE) carry no version proviso, and Annex E lists no
      *> 2014→2023 change to either; what IS edition-bound is the REPETITION VEHICLE they require — §13.15.4 GR3's
      *> "a LINE or COLUMN clause with more than one operand" — and the multiple COLUMN clause, the COLUMNS/ARE
      *> spellings and the SOURCES spelling are all COBOL-2002 introductions (COBOLNET0900 below 2002; the
      *> version-matrix rows report-multi-column-2002 and report-multi-source-2002 pin those gates).  So COBOL-2002
      *> is the oldest edition at which a conforming multi-operand VALUE or SOURCE clause can be WRITTEN at all,
      *> and this program is the proof that it compiles and runs there — the same source as the 2023 twin's
      *> report section, with the two lists and all five VALUE-initialization shapes.
      *>
      *> WHY THIS PROGRAM DOES NOT READ THE REPORT FILE BACK, and the 2023 twin does.  The read-back needs
      *> ORGANIZATION IS LINE SEQUENTIAL, which is itself a COBOL-2023 introduction (§12.4.5.10.3 GR2 — the
      *> Foreword lists it among the main changes over ISO/IEC 1989:2014), so it cannot appear in a COBOL-2002
      *> program at all (the kb/Work PB688 precedent, which removed exactly this read-back from an 85 fixture).
      *> The CONTENT assertions therefore live in tests/conformance/2023/pb506_report_value_operand_list, and what
      *> this program asserts at 2002 is that the whole construct is ACCEPTED and the report presents: the
      *> emitter's operand-cycling and VALUE-recipe paths carry no version predicate, so a divergence between the
      *> editions could only come from a gate, and a gate would show up here as a COBOLNET0900 rejection.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  One INITIATE, two GENERATEs of the same TYPE DE group, one TERMINATE;
      *> nothing is control-broken and nothing is suppressed, so the program simply reaches its final DISPLAY.
      *> Expected: GEN-1, GEN-2, DONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB506OL02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb506ol02.rpt".
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
