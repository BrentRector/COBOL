      *> reject-at: 85
      *> kb/Work PB2650 -- RELEASE record-name-1 FROM literal-1 below the
      *> edition that introduced the literal operand. ISO 1989:2023
      *> 14.9.32.2 prints FROM { identifier-1 | literal-1 }; X3.23-1985
      *> prints FROM identifier-1 only, so at COBOL-85 the literal sender
      *> is refused by the FROM-phrase edition gate (construct
      *> from-literal-2002, COBOLNET0871), the one gate WRITE and REWRITE
      *> now share. The positive is 2002/pb2650_from_literal. The same
      *> program with FROM W-REC (an identifier) compiles at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2650S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S-F ASSIGN TO "pb2650s.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD  S-F.
       01  S-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  W-REC PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN.
           SORT S-F ON ASCENDING KEY S-REC
               INPUT PROCEDURE IS IP
               OUTPUT PROCEDURE IS OP.
           STOP RUN.
       IP.
           RELEASE S-REC FROM "HELLO".
       OP.
           RETURN S-F INTO W-REC AT END CONTINUE END-RETURN.
