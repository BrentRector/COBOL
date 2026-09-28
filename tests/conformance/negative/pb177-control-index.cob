      *> reject-at: 85 2002 2014 2023
      *> ISO 13.18.60.3 SR10: "An index data item may be referenced explicitly only in a SEARCH or SET
      *> statement, a relation condition, an intrinsic function argument, an inline method invocation argument,
      *> the USING phrase of a procedure division header, or the USING phrase of a CALL or INVOKE statement."
      *> A CONTROL clause naming the item IS an explicit reference: 8.4.5 says "a specification in the
      *> environment or data division may specify the name of a data item as an explicit reference in order to
      *> identify those data items that are to be referenced implicitly in procedure division statements
      *> related to such specifications." The CONTROL clause is not on SR10's list, so this is illegal source.
      *> Until kb/Work PB177 arm C's follow-up it compiled clean and staged a RUNTIME loud - the mis-tier a
      *> SYNTAX rule never permits. (The FLOAT operand of that same runtime guard is NOT this case: no syntax
      *> rule bars it, and since kb/Work PB1234 it is saved and restored through its own float channel.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB177N8.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb177n8.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 CIX USAGE INDEX.
       01 WS-SRC PIC 99 VALUE 7.
       REPORT SECTION.
       RD R-1
           CONTROL IS CIX
           PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 2.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
