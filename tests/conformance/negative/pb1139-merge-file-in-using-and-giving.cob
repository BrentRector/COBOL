      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1139 - A MERGE FILE NAMED IN BOTH USING AND GIVING IS REFUSED.
      *>   cite.py --check 14.9.24.3 "File-names shall not be repeated within the MERGE statement." ->
      *>     OK §14.9.24.3 7)
      *> F2 is file-name-3 and file-name-4 at once: the rule is about the statement's file-names as a whole.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1139F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB1139F.tmp".
           SELECT SW2 ASSIGN TO "PB1139Fs2.tmp".
           SELECT F1 ASSIGN TO "PB1139F1.dat".
           SELECT F2 ASSIGN TO "PB1139F2.dat".
           SELECT F3 ASSIGN TO "PB1139F3.dat".
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
           MERGE SW ASCENDING KEY SK USING F1 F2 GIVING F2
           STOP RUN.
