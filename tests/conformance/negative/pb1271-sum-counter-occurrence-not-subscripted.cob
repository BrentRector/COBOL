      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1271 - an unsubscripted reference to the sum counter of a REPEATING entry. CF-U's entry carries
      *> a multiple COLUMN clause, which 13.18.14.4 GR12 makes "functionally equivalent to a COLUMN clause with a
      *> single operand, together with a simple OCCURS clause" (cite.py: OK 13.18.14.4 12)), and 13.18.54.4
      *> GR8 a) gives such an entry's sum counter an occurrence per repetition ("each occurrence of the addend is
      *> added into the corresponding occurrence of the sum counter" - cite.py: OK 13.18.54.4 8) a)). CF-U is
      *> therefore a table element, and 8.4.2.3.3 SR5 - "Each table element reference shall be subscripted"
      *> (cite.py: OK 8.4.2.3.3 5)) - outside its seven listed contexts, none of which a MOVE receiver is.
      *> COBOLNET2270. Before the fix this compiled clean and altered ONE counter shared by all three printable
      *> items, printing 0503 in every column. (The multiple COLUMN clause itself is a COBOL-2002 form, so
      *> COBOL-85 also refuses the entry on its edition gate, COBOLNET0900.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1271N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1271n1.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-X PIC 9 VALUE 3.
       REPORT SECTION.
       RD  R1 CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03 COLUMN 1 PIC X(3) VALUE "DET".
       01  CFG TYPE IS CONTROL FOOTING FINAL LINE PLUS 1.
           03 CF-U COLUMN 1 13 25 PIC 9999 SUM WS-X.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R1.
           MOVE 500 TO CF-U.
           GENERATE DET.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
