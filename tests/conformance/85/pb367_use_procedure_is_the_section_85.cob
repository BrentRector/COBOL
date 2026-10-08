      *> kb/Work PB367 - a USE procedure is the whole remainder of its declarative section, at
      *> --std 85. The full derivation is in tests/conformance/2023/pb367_use_procedure_is_the_
      *> section.cob; this is the 85 WITNESS, because the rule is edition-INDEPENDENT: 14.9.49.3
      *> SR1, 14.4.2, 14.6.3 rule 1 and 14.9.14.4 GR1/GR2/GR7 carry the same text in every edition
      *> COBOL.NET targets, and the truncation had no edition arm. Only USE format 1 is exercised
      *> here - the EXCEPTION CONDITION format and >>TURN are 2002-and-later, so this file gates
      *> clean at 85 while asserting the same rule.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>  . The second READ hits end-of-file with no AT END phrase => 9.1.13 I-O status 10, and
      *>    14.9.49.4 GR6 a) selects the file-name declarative D1.
      *>  . 14.9.49.3 SR1 makes the use procedure the REMAINDER OF THE SECTION and 14.4.2 ends that
      *>    section at END DECLARATIVES, so D1-P1, D1-EX, D1-P2 and D1-LAST all execute in the
      *>    14.6.3 statement-to-statement order => F1-A FS=10 / F1-B / F1-C.
      *>  . 14.9.14.4 GR1 - a format-1 EXIT "has no other effect", so D1-EX ends nothing.
      *>  . 14.9.14.4 GR2 - EXIT PROGRAM in a program that is not under a calling runtime element
      *>    "is treated as if it were a CONTINUE statement", so D1-LAST completes and control
      *>    reaches D1's return mechanism, which 14.9.14.4 GR7's NOTE places AFTER the section's
      *>    last paragraph.
      *>  . 14.9.49.4 GR7 b) - status 10 is not a fatal EC-I-O, so control returns to an implicit
      *>    CONTINUE following the READ => AFTER-READ FS=10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB367USE85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb367-use85.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS1.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 F1-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01 FS1 PIC XX VALUE "00".
       01 W-AAA PIC X(3) VALUE "AAA".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       D1-P1.
           DISPLAY "F1-A FS=" FS1.
       D1-EX.
           EXIT.
       D1-P2.
           DISPLAY "F1-B".
       D1-LAST.
           DISPLAY "F1-C".
           EXIT PROGRAM.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT F1.
           WRITE F1-REC FROM W-AAA.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1.
           READ F1.
           DISPLAY "AFTER-READ FS=" FS1.
           CLOSE F1.
           STOP RUN.
