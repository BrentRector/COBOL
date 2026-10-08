      *> reject-at: 85
      *> kb/Work PB2650 -- REWRITE record-name-1 FROM literal-1 below the
      *> edition that introduced the literal operand, written as the
      *> figurative constant SPACE (a literal: 8.3.3.6). ISO 1989:2023
      *> 14.9.35.2 prints FROM { identifier-1 | literal-1 }; X3.23-1985
      *> prints FROM identifier-1 only, so at COBOL-85 the literal sender
      *> is refused by the FROM-phrase edition gate (construct
      *> from-literal-2002, COBOLNET0871) - the gate RELEASE already had
      *> and REWRITE lacked. The positive is 2002/pb2650_from_literal.
      *> The same program with FROM W-REC (an identifier) compiles at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2650R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb2650r.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  W-REC PIC X(5) VALUE SPACE.
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O F.
           READ F.
           REWRITE F-REC FROM SPACE.
           CLOSE F.
           STOP RUN.
