      *> kb/Work PB1171 - the procedure-division references a REPORT
      *> file admits: the USE statement, OPEN OUTPUT and CLOSE (and the
      *> report writer's own INITIATE / GENERATE / TERMINATE, which name
      *> the report, not the file).
      *> RULE (13.4.5.3 SR9): "The subject of a file description entry
      *> that specifies a REPORT clause may be referenced in the
      *> procedure division only by the USE statement, the WHEN phrase
      *> of a PERFORM statement, the CLOSE statement, or the OPEN
      *> statement with the OUTPUT or EXTEND phrase."
      *> RULE (13.4.5.3 SR8): no record description entries under the
      *> report FD - its lines come from the REPORT SECTION.
      *> The refused references are the negatives pb1171-*.
      *> cite.py --check 13.4.5.3 "The subject of a file description
      *>   entry that specifies a REPORT clause may be referenced in the
      *>   procedure division only by the USE statement" -> OK 9)
      *> Expected: the program compiles at every edition and runs to
      *> STOP RUN with no declarative entered (no I-O error occurs).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1171M.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1171M.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD RPF REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01 W-TEXT PIC X(5) VALUE "HELLO".
       REPORT SECTION.
       RD RPT PAGE LIMIT 60.
       01 DL TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X(5) SOURCE W-TEXT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       RPF-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RPF.
       RPF-ERR-P.
           DISPLAY "RPF ERROR".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT RPF
           DISPLAY "OPENED"
           INITIATE RPT
           GENERATE DL
           MOVE "WORLD" TO W-TEXT
           GENERATE DL
           TERMINATE RPT
           CLOSE RPF
           DISPLAY "CLOSED " W-TEXT
           STOP RUN.
