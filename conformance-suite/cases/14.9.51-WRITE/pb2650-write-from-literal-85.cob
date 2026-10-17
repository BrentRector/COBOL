*>--- conformance-case
*> id: pb2650-write-from-literal-85
*> rules: [FMT-14.9.51.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2650 -- WRITE record-name-1 FROM literal-1 below the
      *> edition that introduced the literal operand. ISO 1989:2023
      *> 14.9.51.2 prints FROM { identifier-1 | literal-1 }; X3.23-1985
      *> prints FROM identifier-1 only, so at COBOL-85 the literal sender
      *> is refused by the FROM-phrase edition gate (construct
      *> from-literal-2002, COBOLNET0871) - the gate RELEASE already had
      *> and WRITE lacked. The positive is 2002/pb2650_from_literal. The
      *> same program with FROM W-REC (an identifier) compiles at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2650W.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb2650w.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  W-REC PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           WRITE F-REC FROM "HELLO".
           CLOSE F.
           STOP RUN.
