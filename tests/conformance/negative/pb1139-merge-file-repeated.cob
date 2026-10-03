      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1139 - A FILE-NAME REPEATED WITHIN A MERGE IS REFUSED.
      *>   cite.py --check 14.9.24.3 "File-names shall not be repeated within the MERGE statement." ->
      *>     OK §14.9.24.3 7)
      *> F1 is named twice in USING (the rule covers file-name-2 through file-name-4 together). No set-membership test
      *> existed, so the statement compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1139E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB1139E.tmp".
           SELECT SW2 ASSIGN TO "PB1139Es2.tmp".
           SELECT F1 ASSIGN TO "PB1139E1.dat".
           SELECT F2 ASSIGN TO "PB1139E2.dat".
           SELECT F3 ASSIGN TO "PB1139E3.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 SK PIC X(4).
       SD SW2.
       01 SR2.
          05 SK2 PIC X(4).
       FD F1.
       01 R1 PIC X(4).
       FD F2.
       01 R2 PIC X(4).
       FD F3.
       01 R3 PIC X(4).
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           MERGE SW ASCENDING KEY SK USING F1 F1 GIVING F3
           STOP RUN.
