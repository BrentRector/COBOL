      *> kb/Work PB1077 - ISO 12.4.5.2 SR2 and SR3 are scoped to ONE
      *> program: "A given file-name may be specified in only one SELECT
      *> clause within a factory, function, object, or program", and each
      *> SELECT needs an FD or SD "in the file section of the ... program
      *> in which the SELECT clause is specified". The containing program
      *> and the contained program below each select their own F with
      *> their own FD, and the containing program pairs a sort file with
      *> its SD. Every pairing is legal, so the program compiles and runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73BSCOPE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "w73b_scope_outer.dat".
           SELECT S ASSIGN TO "w73b_scope_sort.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FR PIC X(8).
       SD S.
       01 SR PIC X(8).
       PROCEDURE DIVISION.
           OPEN OUTPUT F.
           MOVE "OUTER" TO FR.
           WRITE FR.
           CLOSE F.
           CALL "W73BSCOPEIN".
           OPEN INPUT F.
           READ F.
           DISPLAY "OUTER-READ=" FR.
           CLOSE F.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73BSCOPEIN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "w73b_scope_inner.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FR PIC X(8).
       PROCEDURE DIVISION.
           OPEN OUTPUT F.
           MOVE "INNER" TO FR.
           WRITE FR.
           CLOSE F.
           OPEN INPUT F.
           READ F.
           DISPLAY "INNER-READ=" FR.
           CLOSE F.
           EXIT PROGRAM.
       END PROGRAM W73BSCOPEIN.
       END PROGRAM W73BSCOPE.
