*>--- conformance-case
*> id: pb721_zero_length_record
*> rules: [GR-8.5.4-5, SR-13.18.43.3-4, SR-13.18.43.3-5,
*>    SR-13.18.43.3-7, SR-13.18.43.3-8, GR-13.18.43.4-11,
*>    GR-13.18.43.4-13, GR-13.18.43.4-15]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 Annex A.3 item 43): "The capabilities of specifying a
      *> zero-length record for relative and sequential files, and of reading and
      *> writing such records, are dependent on the capabilities of the processor."
      *> COBOL.NET PROVIDES the capability, on BOTH organizations the item names
      *> (docs/CONFORMANCE.md section 2 row 43) - so this golden exercises a sequential
      *> file AND a relative one, because a sequential-only witness would claim more
      *> than it proves.
      *>
      *> The clause that makes a zero MINIMUM legal is a SYNTAX rule, not a general
      *> rule: 13.18.43.3 SR7, "Integer-2 shall be greater than or equal to zero." That
      *> is the express override 5.5 rule 1 contemplates - "When the term 'integer-n'
      *> ... is used in a general format and associated rules, it refers to a
      *> fixed-point integer literal that shall be unsigned and nonzero unless
      *> OTHERWISE SPECIFIED IN THE ASSOCIATED RULES" - so RECORD IS VARYING IN SIZE
      *> FROM 0 is legal COBOL and shall draw no diagnostic. 8.5.4 item 5 confirms the
      *> zero is a real zero-length item rather than a degenerate spelling: a logical
      *> record "specified using the variable-length ... format of the RECORD clause in
      *> which the number of characters positions is zero" is one of the enumerated
      *> zero-length items.
      *>
      *> EXPECTED VALUES, derived from the standard:
      *>  - W=00 / WR=00: 13.18.43.4 GR13 a) makes the written record length the content
      *>    of data-name-1 (here 0), and GR14 a) sets EC-I-O-LOGIC-ERROR only when that
      *>    length is LESS THAN integer-2 or greater than integer-3. 0 is not less than
      *>    0, so the WRITE is successful and 9.1.13.2's '00' is the status.
      *>  - LEN=0000: GR15 - "after the successful execution of a READ ... the contents
      *>    of the data item referenced by data-name-1 will indicate the number of bytes
      *>    in the record just read." The record just read has zero bytes.
      *>  - Z=[          ] (ten SPACES), NOT the ten hyphens WS-Z held before the READ:
      *>    GR16's closing sentence makes the sending operand of the implicit MOVE a
      *>    zero-length item - "If the number of bytes determined as above is zero, the
      *>    record is a zero-length item" - and 14.9.25.4 GR1 then says "If identifier-1
      *>    is a zero-length item, it is as if literal-1 were specified as a zero-length
      *>    literal." GR2 finishes it: "If literal-1 is an alphanumeric or national
      *>    zero-length literal and the receiving operand is other than a dynamic-length
      *>    elementary item, literal-1 is treated as if it were the figurative constant
      *>    SPACE." WS-Z is a fixed PIC X(10), so it becomes ten spaces - the standard
      *>    names this result outright, it is not a fill-rule inference.
      *>    The hyphens are there to prove the move happened -
      *>    a receiver left untouched would be the other, wrong answer, and a receiver
      *>    pre-set to spaces could not tell the two apart.
      *>
      *> THE FORMAT 3 ARM IS SR8's, AND IT IS A DIFFERENT FACT. 13.18.43.3 SR8 -
      *> "Integer-4 shall be greater than or equal to zero" - is SR7's twin over the
      *> RECORD CONTAINS integer-4 TO integer-5 clause, and it lifts the same 5.5
      *> rule 1 nonzero default. What it buys is the ACCEPTANCE of a zero integer-4,
      *> not a zero-length record: Format 3 has no DEPENDING register, so 13.18.43.4
      *> GR13 b) fixes the written length at "the number of bytes in the record" and
      *> GR18 says "the size of each record is completely defined in the record
      *> description entry". F3 therefore writes and reads its full 20 bytes; the
      *> observable under test is that the clause COMPILES and the file round-trips.
      *> kb/Work PB721.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB721-ZERO-LEN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "pb721zlr-sq.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-ST.
           SELECT RL ASSIGN TO "pb721zlr-rl.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS WS-RK
               FILE STATUS IS WS-ST.
           SELECT F3 ASSIGN TO "pb721zlr-f3.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ RECORD IS VARYING IN SIZE FROM 0 TO 20 CHARACTERS
               DEPENDING ON WS-LEN.
       01  SQ-REC PIC X(20).
       FD  RL RECORD IS VARYING IN SIZE FROM 0 TO 20 CHARACTERS
               DEPENDING ON WS-LEN.
       01  RL-REC PIC X(20).
       FD  F3 RECORD CONTAINS 0 TO 20 CHARACTERS.
       01  F3-REC PIC X(20).
       WORKING-STORAGE SECTION.
       01  WS-ST  PIC XX.
       01  WS-RK  PIC 9(4) VALUE 0.
       01  WS-LEN PIC 9(4) VALUE 0.
       01  WS-Z   PIC X(10) VALUE ALL "-".
       PROCEDURE DIVISION.
       MAIN.
           MOVE ALL "-" TO WS-Z
           MOVE 0 TO WS-LEN
           MOVE SPACES TO SQ-REC
           OPEN OUTPUT SQ
           WRITE SQ-REC
           DISPLAY "SQ W=" WS-ST
           CLOSE SQ
           MOVE 99 TO WS-LEN
           OPEN INPUT SQ
           READ SQ INTO WS-Z
               AT END DISPLAY "SQ ATEND"
           END-READ
           DISPLAY "SQ R=" WS-ST " LEN=" WS-LEN
           DISPLAY "SQ Z=[" WS-Z "]"
           CLOSE SQ

           MOVE ALL "-" TO WS-Z
           MOVE 0 TO WS-LEN
           MOVE SPACES TO RL-REC
           OPEN OUTPUT RL
           WRITE RL-REC
           DISPLAY "RL W=" WS-ST
           CLOSE RL
           MOVE 99 TO WS-LEN
           OPEN INPUT RL
           READ RL INTO WS-Z
               AT END DISPLAY "RL ATEND"
           END-READ
           DISPLAY "RL R=" WS-ST " LEN=" WS-LEN
           DISPLAY "RL Z=[" WS-Z "]"
           CLOSE RL

           MOVE "HELLO" TO F3-REC
           OPEN OUTPUT F3
           WRITE F3-REC
           DISPLAY "F3 W=" WS-ST
           CLOSE F3
           MOVE SPACES TO F3-REC
           OPEN INPUT F3
           READ F3 AT END DISPLAY "F3 ATEND" END-READ
           DISPLAY "F3 R=" WS-ST " [" F3-REC "]"
           CLOSE F3
           STOP RUN.
