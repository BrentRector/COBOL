      *> kb/Work PB1466 - the NUMERIC class condition over a BINARY / PACKED-DECIMAL item whose
      *> content arrived through a CHARACTER channel. ISO 1989:2023 8.8.4.4.4 GR3 n) 1. c.: "the
      *> condition is true if the content of the data item referenced by identifier-1 consists
      *> entirely of a valid representation for the usage and, if a PICTURE clause is specified,
      *> the numeric value is within the range of values implied by the PICTURE clause." A group
      *> MOVE fills the group "without consideration for the individual elementary or group items"
      *> (14.9.25.4 GR4), and a READ and a BY REFERENCE crossing deliver bytes the same way, so the
      *> leaf can hold anything - which is the case this rule exists for (validating a packed
      *> field after a READ). Before the fix each leaf's native carrier held a DECODED value and the
      *> test was folded to the constant TRUE, so every leg below printed T.
      *> Expected values, derived (packed: every digit nibble 0-9 and a trailing SIGN nibble A-F;
      *> binary: the value within 0 .. 10**digits - 1):
      *>  GRP-PK-BAD  X"ABCD" - digit nibbles A,B,C are not digits           F
      *>  GRP-PK-OK   X"123F" - digits 1,2,3 and sign nibble F               T
      *>  GRP-BN-BAD  X"2710" = 10000, outside PIC 9(4)'s 0..9999            F
      *>  GRP-BN-OK   X"0063" = 99                                           T
      *>  READ-PK-BAD / READ-PK-OK - the same two packed images, READ from a record   F / T
      *>  REF-PK-BAD  X"ABCD" passed BY REFERENCE to a PACKED-DECIMAL formal    F
      *>  ARITH       the image-stored leaves still compute: 12 + 5 = 17 and NUMERIC
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1466SIC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb1466-packed.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 FR.
          05 FP PIC 9(3) USAGE PACKED-DECIMAL.
       WORKING-STORAGE SECTION.
       01 GP. 05 GPK PIC 9(3) USAGE PACKED-DECIMAL.
       01 GB. 05 GBN PIC 9(4) USAGE BINARY.
       01 BAD-PK PIC X(2) VALUE X"ABCD".
       01 OK-PK PIC X(2) VALUE X"123F".
       PROCEDURE DIVISION.
           MOVE X"ABCD" TO GP
           IF GPK IS NUMERIC
               DISPLAY "GRP-PK-BAD T" ELSE DISPLAY "GRP-PK-BAD F" END-IF
           MOVE X"123F" TO GP
           IF GPK IS NUMERIC
               DISPLAY "GRP-PK-OK T" ELSE DISPLAY "GRP-PK-OK F" END-IF
           MOVE X"2710" TO GB
           IF GBN IS NUMERIC
               DISPLAY "GRP-BN-BAD T" ELSE DISPLAY "GRP-BN-BAD F" END-IF
           MOVE X"0063" TO GB
           IF GBN IS NUMERIC
               DISPLAY "GRP-BN-OK T" ELSE DISPLAY "GRP-BN-OK F" END-IF
           OPEN OUTPUT F1
           WRITE FR FROM BAD-PK
           WRITE FR FROM OK-PK
           CLOSE F1
           OPEN INPUT F1
           READ F1
           IF FP IS NUMERIC
               DISPLAY "READ-PK-BAD T" ELSE DISPLAY "READ-PK-BAD F" END-IF
           READ F1
           IF FP IS NUMERIC
               DISPLAY "READ-PK-OK T" ELSE DISPLAY "READ-PK-OK F" END-IF
           CLOSE F1
           CALL "PB1466SUB" USING BAD-PK
           MOVE 12 TO GBN
           ADD 5 TO GBN
           IF GBN IS NUMERIC
               DISPLAY "ARITH " GBN " T" ELSE DISPLAY "ARITH " GBN " F"
           END-IF
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1466SUB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP PIC 9(3) USAGE PACKED-DECIMAL.
       PROCEDURE DIVISION USING LP.
           IF LP IS NUMERIC
               DISPLAY "REF-PK-BAD T" ELSE DISPLAY "REF-PK-BAD F" END-IF
           GOBACK.
       END PROGRAM PB1466SUB.
       END PROGRAM PB1466SIC.
