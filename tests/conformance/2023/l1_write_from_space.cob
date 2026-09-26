      *> ISO §14.9.51.4 6) — WRITE record-name-1 FROM SPACE: one alphanumeric space, moved to an alphanumeric and a national record
      *> THE RULE: "The figurative constant SPACE when specified in the
      *>   WRITE statement references one alphanumeric space character."
      *>   cite.py --check 14.9.51.4 "The figurative constant SPACE when
      *>     specified in the WRITE statement references one
      *>     alphanumeric space character" -> OK §14.9.51.4 6)
      *> The only place SPACE can appear in a WRITE is literal-1 of the
      *> FROM phrase, and with the FILE phrase a figurative constant is
      *> forbidden (cite.py --check 14.9.51.3 "literal-1 shall be an
      *> alphanumeric, boolean, or national literal and shall not be a
      *> figurative constant" -> OK §14.9.51.3 7) b)), so the rule's
      *> branch is exactly WRITE record-name-1 FROM SPACE, which GR5
      *> makes a MOVE:
      *>   cite.py --check 14.9.51.4 "The result of the execution of a
      *>     WRITE statement specifying record-name-1 and the FROM phrase
      *>     is equivalent to the execution of the following statements
      *>     in the order specified" -> OK §14.9.51.4 5) (a) MOVE
      *>     literal-1 TO record-name-1; b) the WRITE without FROM)
      *>   cite.py --check 14.9.51.3 "If record-name-1 is specified,
      *>     identifier-1 or literal-1 shall be valid as a sending
      *>     operand in a MOVE statement specifying record-name-1 as the
      *>     receiving operand" -> OK §14.9.51.3 6)
      *>   cite.py --check 8.3.3.6.4 "the string of characters is
      *>     repeated character by character until the size of the
      *>     resultant string is greater than or equal to the number of
      *>     character positions in the associated data item"
      *>     -> OK §8.3.3.6.4 2)
      *>   cite.py --check 9.1.13.2 "I-O status = 00. The input-output
      *>     statement is successfully executed and no further
      *>     information is available concerning the input-output
      *>     operation" -> OK §9.1.13.2 1)
      *>   cite.py --check 14.9.25.4 "The category of figurative
      *>     constants when used in the MOVE statement depends on the
      *>     category of the receiving operand" -> OK §14.9.25.4 7)
      *>   cite.py --check 14.9.25.4 "Any necessary conversion from
      *>     alphanumeric character to national character representation
      *>     shall be performed, before any alignment" -> OK 14.9.25.4 6)
      *>   cite.py --check 14.9.25.4 "alignment and any necessary space
      *>     filling shall take place as defined in 14.6.8"
      *>     -> OK §14.9.25.4 6) a)
      *> TWO RECORDS, TWO CATEGORIES. R1 is an alphanumeric group, so the
      *> one alphanumeric space fills all 8 positions (8.3.3.6.4 2)).
      *> NR is PIC N(4): GR6 overrides MOVE's Table 17 (which would make
      *> SPACE national against a national receiver) and the alphanumeric
      *> space is converted to national on the move (14.9.25.4 6)) and
      *> space-filled — both routes give four national spaces, and the
      *> WRITE is legal source under SR6 either way.
      *> The record area is loaded with Zs first, so a WRITE that skipped
      *> GR5 a)'s MOVE would write Zs, not spaces.
      *> DERIVATION of every .out line:
      *>   W1 00    WRITE R1 FROM W ("ABCDEFGH") succeeds
      *>   W2 00    WRITE R1 FROM SPACE succeeds
      *>   W3 00    WRITE NR FROM SPACE succeeds
      *>   R1 [ABCDEFGH] 00               first record read back
      *>   R2 [        ] [   ] [     ] 00 second record: 8 spaces, so
      *>                                  R1A (3) and R1B (5) are spaces
      *>   N1 [    ] 00                   national record: 4 spaces
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1W06SP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "L1W06SP.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT NF ASSIGN TO "L1W06SPN.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FSN.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R1.
          05 R1A PIC X(3).
          05 R1B PIC X(5).
       FD NF.
       01 NR PIC N(4).
       WORKING-STORAGE SECTION.
       01 FS  PIC XX.
       01 FSN PIC XX.
       01 W   PIC X(8) VALUE "ABCDEFGH".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           WRITE R1 FROM W
           DISPLAY "W1 " FS
           MOVE "ZZZZZZZZ" TO R1
           WRITE R1 FROM SPACE
           DISPLAY "W2 " FS
           CLOSE F
           OPEN OUTPUT NF
           MOVE N"ZZZZ" TO NR
           WRITE NR FROM SPACE
           DISPLAY "W3 " FSN
           CLOSE NF
           OPEN INPUT F
           READ F AT END DISPLAY "EOF1" END-READ
           DISPLAY "R1 [" R1 "] " FS
           READ F AT END DISPLAY "EOF2" END-READ
           DISPLAY "R2 [" R1 "] [" R1A "] [" R1B "] " FS
           CLOSE F
           OPEN INPUT NF
           READ NF AT END DISPLAY "EOF3" END-READ
           DISPLAY "N1 [" NR "] " FSN
           CLOSE NF
           STOP RUN.
