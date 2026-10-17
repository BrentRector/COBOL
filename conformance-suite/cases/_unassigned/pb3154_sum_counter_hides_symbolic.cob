*>--- conformance-case
*> id: pb3154_sum_counter_hides_symbolic
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3154 review R2-1 - a contained program's REPORT
      *>   SECTION sum counter HIDES its container's SYMBOLIC CHARACTERS
      *>   name of the same spelling.
      *> ISO 13.18.54.4 GR5: "If a data-name immediately follows the
      *>   level number in the entry containing the SUM clause, the
      *>   data-name is the name of the sum counter" -> OK 13.18.54.4 5).
      *> ISO 8.4.6.1: "even when it is a different type of user-defined
      *>   word, is to the item which that source element describes"
      *>   -> OK 8.4.6.1.
      *> NESTING: PB3154UO declares the symbolic-character S1 (67). It
      *>   contains PB3154UI, whose report's FINAL control footing names
      *>   its sum counter S1, summing WS-AMT.
      *> EXPECTED OUTPUT, DERIVED:
      *>   INNER 05   PB3154UI's own sum counter S1: one GENERATE of the
      *>              detail with WS-AMT 5 adds 5 to it.
      *>   OUTER B    the outer program's own symbolic: ordinal 67 =
      *>              X"42" = "B" (12.3.7.4 GR11).
      *> Before the fix the sum counter was not asked: MOVE S1 moved the
      *>   figurative ALL "B" in place of the counter.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3154UO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS S1 IS 67.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB3154UI".
           DISPLAY "OUTER " S1.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3154UI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PR ASSIGN TO "pb3154u.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PR REPORT IS RR.
       WORKING-STORAGE SECTION.
       01  WS-AMT  PIC 9     VALUE 0.
       01  N       PIC 99.
       REPORT SECTION.
       RD  RR CONTROL IS FINAL.
       01  DL TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 9 SOURCE WS-AMT.
       01  CFF TYPE CF FINAL LINE PLUS 1.
           03  S1 COLUMN 1 PIC 99 SUM WS-AMT.
       PROCEDURE DIVISION.
       INNER-PARA.
           OPEN OUTPUT PR.
           INITIATE RR.
           MOVE 5 TO WS-AMT.
           GENERATE DL.
           MOVE S1 TO N.
           DISPLAY "INNER " N.
           TERMINATE RR.
           CLOSE PR.
       END PROGRAM PB3154UI.
       END PROGRAM PB3154UO.
