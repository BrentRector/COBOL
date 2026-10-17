      *>--- conformance-case
      *> id: pb370-start-reverse-operator-85
      *> rules: [GR-14.9.41.4-9, GR-14.9.41.4-17]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.41.4 GR9 b) / GR17 d): LESS, NOT GREATER and LESS OR
      *> EQUAL in the START KEY phrase select a REVERSE search; §A.3
      *> item 39 binds them to READ ... PREVIOUS as one reverse-access
      *> capability, a COBOL-2002 introduction
      *> (start-reverse-operators-2002, kb/Work PB370). COBOL-85's START
      *> searches forward only, so at --std 85 each reverse spelling
      *> draws COBOLNET0900. The constructs.json row's own source writes
      *> LESS THAN; this case writes the two spellings whose fold is not
      *> visible in their first word: NOT GREATER (a NOT that turns a
      *> forward word into a reverse operator) and the symbol <=. The
      *> forward spellings (NOT LESS, NOT <) stay legal at 85:
      *> conformance:cases/14.9.41-START/pb370_start_forward_operators.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB370NR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FR ASSIGN TO "pb370nr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD FR.
       01 R-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT FR
           MOVE 5 TO RK
           START FR KEY IS NOT GREATER THAN RK
               INVALID KEY DISPLAY "NG INVALID"
           END-START
           START FR KEY <= RK
               INVALID KEY DISPLAY "LE INVALID"
           END-START
           CLOSE FR
           STOP RUN.
