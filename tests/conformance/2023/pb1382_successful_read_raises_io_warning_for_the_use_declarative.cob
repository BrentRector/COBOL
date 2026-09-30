      *> PB1382 - ISO 9.1.13.1: a successful completion with an I-O status other than 00 is
      *>   EC-I-O-WARNING; with it turned on explicitly (7.3.25.4 GR4) a USE declarative for it
      *>   runs after a SUCCESSFUL sequential READ (status 06: the 20-character line is longer
      *>   than the 5-character record), and 14.6.13.1.4 3) then does NOT execute the NOT AT END
      *>   imperative. The READ emitter used to call the hook only on its failure branch.
      *>   cite.py: OK 14.6.13.1.4 3), OK 9.1.13.1
       >>TURN EC-I-O-WARNING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1382P.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb1382.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT F2 ASSIGN TO "pb1382.dat"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(20).
       FD F2.
       01 R2 PIC X(5).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-WARNING.
       H-P.
           DISPLAY "DECL " FS " " FUNCTION EXCEPTION-STATUS.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT F1
           MOVE "ABCDEFGHIJKLMNOPQRST" TO R1
           WRITE R1
           CLOSE F1
           OPEN INPUT F2
           READ F2
               AT END DISPLAY "END"
               NOT AT END DISPLAY "NOTEND " FS
           END-READ
           DISPLAY "AFTER " FS
           CLOSE F2
           STOP RUN.
