      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1171 - a REPORT file's name referenced by statements
      *> 13.4.5.3 SR9 does not list: READ, START, UNLOCK, DELETE FILE.
      *> RULE (13.4.5.3 SR9): "The subject of a file description entry
      *> that specifies a REPORT clause may be referenced in the
      *> procedure division only by the USE statement, the WHEN phrase
      *> of a PERFORM statement, the CLOSE statement, or the OPEN
      *> statement with the OUTPUT or EXTEND phrase."
      *> cite.py --check 13.4.5.3 "The subject of a file description
      *>   entry that specifies a REPORT clause may be referenced in the
      *>   procedure division only by the USE statement" -> OK 9)
      *> Before the fix only OPEN INPUT / I-O was refused (COBOLNET2371);
      *> each statement below compiled and ran. Each is COBOLNET2577.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1171N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1171N1.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD RPF REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01 W PIC X(5).
       REPORT SECTION.
       RD RPT PAGE LIMIT 60.
       01 DL TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
           OPEN OUTPUT RPF
           READ RPF INTO W
           START RPF FIRST
           UNLOCK RPF
           CLOSE RPF
           DELETE FILE RPF
           STOP RUN.
