      *> reject-at: 2023
      *> A RESERVE CLAUSE ON A LINE SEQUENTIAL FILE (kb/Work PB781).
      *> ISO/IEC 1989:2023 §12.4.5.2 SR12: "If the LINE SEQUENTIAL phrase of the
      *> ORGANIZATION clause is specified, the RESERVE clause shall not be specified."
      *> The RESERVE clause is written BEFORE the ORGANIZATION clause here, so the screen
      *> must read the whole entry first. LINE SEQUENTIAL is new in ISO/IEC 1989:2023, so
      *> the rule is reachable only there (below it the organization itself is refused).
      *> Refused, COBOLNET2921.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB781RL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb781rl.txt"
               RESERVE 2 AREAS
               ORGANIZATION LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
