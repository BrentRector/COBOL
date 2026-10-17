      *>--- conformance-case
      *> id: pb3154_global_sum_counter_tier
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3154 review R3-1 - a GLOBAL report's sum counter is
      *>   a global name of the program that DECLARES the report, so a
      *>   same-spelled name a NEARER container declares is the item a
      *>   contained program references.
      *> ISO 13.18.27.4 GR1: "All data-names subordinate to a global
      *>   name are global names" -> OK 13.18.27.4 1).
      *> ISO 8.4.6.2.1 3) b) 2.: "The item in the containing source
      *>   element if the name is not declared in source element A and
      *>   is declared in the source element containing source element
      *>   A" -> OK 8.4.6.2.1 3) b) 2.
      *> NESTING: PB3154GO declares the GLOBAL report RR, whose FINAL
      *>   control footing names the sum counters S1, S2 and S3. It
      *>   contains PB3154GM, which declares S1 as a GLOBAL constant
      *>   ("KKK") and S2 as a GLOBAL data item ("MMM"). PB3154GM
      *>   contains PB3154GI, which declares none of the three.
      *> EXPECTED OUTPUT, DERIVED:
      *>   MID KKK MMM 005  PB3154GM's own S1 and S2; S3 is declared
      *>                    only by PB3154GO, so it is the counter:
      *>                    one GENERATE with WS-AMT 5 adds 5.
      *>   INNER KKK MMM 005  PB3154GI declares none: for S1 and S2 the
      *>                    nearest declaring element is PB3154GM
      *>                    (depth 1), not PB3154GO (depth 2); S3 is
      *>                    still the counter.
      *>   INNER Q 005      S2 OF CFF: the qualifier reaches only the
      *>                    counter, so the qualified reference is it.
      *> Before the fix INNER showed 005 for S1 (the farther counter
      *>   removed the nearer constant) and 005 for S2 (the counter was
      *>   resolved before the nearer data item was asked).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3154GO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PR ASSIGN TO "pb3154g.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PR GLOBAL REPORT IS RR.
       WORKING-STORAGE SECTION.
       01  WS-AMT  PIC 9     VALUE 0 GLOBAL.
       REPORT SECTION.
       RD  RR IS GLOBAL CONTROL IS FINAL.
       01  DL TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 9 SOURCE WS-AMT.
       01  CFF TYPE CF FINAL LINE PLUS 1.
           03  S1 COLUMN 1 PIC 999 SUM WS-AMT.
           03  S2 COLUMN 5 PIC 999 SUM WS-AMT.
           03  S3 COLUMN 9 PIC 999 SUM WS-AMT.
       PROCEDURE DIVISION.
       OUTER-PARA.
           OPEN OUTPUT PR.
           INITIATE RR.
           MOVE 5 TO WS-AMT.
           GENERATE DL.
           CALL "PB3154GM".
           TERMINATE RR.
           CLOSE PR.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3154GM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S1 CONSTANT IS GLOBAL AS "KKK".
       01  S2 PIC XXX VALUE "MMM" GLOBAL.
       01  Y1 PIC XXX.
       01  Y2 PIC XXX.
       01  Y3 PIC 999.
       PROCEDURE DIVISION.
       MID-PARA.
           MOVE S1 TO Y1.
           MOVE S2 TO Y2.
           MOVE S3 TO Y3.
           DISPLAY "MID " Y1 " " Y2 " " Y3.
           CALL "PB3154GI".
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3154GI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  Y1 PIC XXX.
       01  Y2 PIC XXX.
       01  Y3 PIC 999.
       PROCEDURE DIVISION.
       INNER-PARA.
           MOVE S1 TO Y1.
           MOVE S2 TO Y2.
           MOVE S3 TO Y3.
           DISPLAY "INNER " Y1 " " Y2 " " Y3.
           MOVE S2 OF CFF TO Y3.
           DISPLAY "INNER Q " Y3.
           GOBACK.
       END PROGRAM PB3154GI.
       END PROGRAM PB3154GM.
       END PROGRAM PB3154GO.
