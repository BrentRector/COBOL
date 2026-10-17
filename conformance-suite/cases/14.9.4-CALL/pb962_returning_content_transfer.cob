*>--- conformance-case
*> id: pb962_returning_content_transfer
*> rules: [GR-14.9.4.4-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB962 - A RETURNING ITEM DELIVERS ITS CONTENT, NOT A RE-PARSED VALUE.
      *>
      *> 14.6.5: "The result of the execution of a program, function, or method that specifies a RETURNING
      *> phrase in its procedure division header, is the content of the data item referenced by that
      *> RETURNING phrase", and "the result is placed in the data item referenced by that RETURNING phrase of
      *> that activating statement". 14.8.3.3: a conforming receiver has "the same ALIGN, BLANK WHEN ZERO,
      *> DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses" - every pair below is identically
      *> described, so each delivery is a content transfer under ONE description.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   D1 - P962S's returning item holds three SPACES (written through its REDEFINES). They are its
      *>        content, and the redefined receiver RSP takes them as they stand. This used to ABORT the run
      *>        unit with EC-PROGRAM-ARG-MISMATCH: the character result was re-parsed as a number.
      *>   D2 - P962N returns -12.5 in PIC S9(3)V9 (native storage in the callee). The receiver RIM is held as
      *>        its character image (it is redefined), and its image is 012N: digits 0125 with the negative
      *>        sign over-punched into the last digit (5 -> N, DOC-A.1-177's default convention). It used to
      *>        arrive as 012E (+12.5): the delivery wrote the value's C# text "-125", which dropped the sign.
      *>   D3 - the same result into a natively held receiver of the same description: 012N (the control).
      *>   D4 - P962P returns 12345 in PIC 9(5) COMP-3 whose storage is its character image (redefined); the
      *>        receiver RP has the same description and receives the same value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P962M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RSP PIC 9(3) VALUE 999.
       01 RSPX REDEFINES RSP PIC X(3).
       01 RIM PIC S9(3)V9 VALUE 999.
       01 RIMX REDEFINES RIM PIC X(4).
       01 RN PIC S9(3)V9 VALUE 999.
       01 RP PIC 9(5) COMP-3 VALUE 1.
       PROCEDURE DIVISION.
           CALL "P962S" RETURNING RSP
           DISPLAY "D1 [" RSPX "]"
           CALL "P962N" RETURNING RIM
           DISPLAY "D2 [" RIMX "]"
           CALL "P962N" RETURNING RN
           DISPLAY "D3 [" RN "]"
           CALL "P962P" RETURNING RP
           DISPLAY "D4 [" RP "]"
           STOP RUN.
       END PROGRAM P962M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P962S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9(3).
       01 RX REDEFINES R PIC X(3).
       PROCEDURE DIVISION RETURNING R.
           MOVE SPACES TO RX
           GOBACK.
       END PROGRAM P962S.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P962N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC S9(3)V9.
       PROCEDURE DIVISION RETURNING R.
           MOVE -12.5 TO R
           GOBACK.
       END PROGRAM P962N.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P962P.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9(5) COMP-3.
       01 RX REDEFINES R PIC X(3).
       PROCEDURE DIVISION RETURNING R.
           MOVE 12345 TO R
           GOBACK.
       END PROGRAM P962P.
