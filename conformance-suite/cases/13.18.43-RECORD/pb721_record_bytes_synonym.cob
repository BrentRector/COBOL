*>--- conformance-case
*> id: pb721_record_bytes_synonym
*> rules: [SR-13.18.43.3-2, SR-13.18.43.3-3, SR-13.18.43.3-9, FMT-13.18.43.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.43.3 SR2: "The words BYTES and CHARACTERS are synonymous and may be
      *> used interchangeably." All three general formats of 13.18.43.2 print the same
      *> { BYTES | CHARACTERS } brace group, so the permission covers the fixed clause,
      *> the VARYING clause and the CONTAINS m TO n clause alike - and this program
      *> writes BYTES in all three.
      *>
      *> Until kb/Work PB721 the word had no lexer token at all and the grammar admitted
      *> only CHARACTERS, so every one of these lines was refused COBOLNET1970 ("'BYTES'
      *> is not a clause of the file description entry") - legal COBOL-2023, rejected.
      *>
      *> !! THIS GOLDEN IS 2023-ONLY BY RULE, NOT BY HABIT. The BYTES spelling is a
      *> COBOL-2023 ADDITION: Annex E.3.3 item 13 lists BYTES among the words "added to
      *> the list of context-sensitive words or the context in which they are reserved
      *> has been expanded", and 8.10 gives BYTES exactly ONE construct - the RECORD
      *> clause - so there is no earlier context for the second arm to name. Below 2023
      *> it is refused COBOLNET0900; that is
      *> tests/conformance/negative/pb721-record-bytes-below-2023.cob.
      *>
      *> THE COMPLEMENT IS THE OTHER HALF OF THE RULE, and it is why WS-BYTES is here.
      *> 8.10 lists BYTES as a CONTEXT-SENSITIVE word whose construct is the "RECORD
      *> clause", and 8.10's own sentence says what that means: "If a context-sensitive
      *> word is used where the context-sensitive word is permitted in the general
      *> format, the word is treated as a keyword; otherwise it is treated as a
      *> user-defined word." It is absent from 8.9's reserved-word list at every
      *> supported edition, so `01 WS-BYTES` and a table named BYTES are legal source
      *> that a hard keyword would have broken. That half is edition-INVARIANT, and its
      *> COBOL-85 witness is tests/conformance/85/pb721_bytes_user_word_85.cob: the
      *> clause is gated, the word is not.
      *>
      *> EXPECTED VALUES: the clauses are declarations, so the observable is that the
      *> three files compile and round-trip their records exactly as the CHARACTERS
      *> spelling does - F1 writes and reads back a 10-byte record ('00'/'00'), F2 a
      *> 4-byte variable-length one whose length register reads back 0004 per 13.18.43.4
      *> GR15, and the user word named BYTES still holds and subscripts its own data.
      *> kb/Work PB721.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB721-BYTES.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb721by1.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS WS-ST.
           SELECT F2 ASSIGN TO "pb721by2.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS WS-ST.
           SELECT F3 ASSIGN TO "pb721by3.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  F1 RECORD CONTAINS 10 BYTES.
       01  F1-REC PIC X(10).
       FD  F2 RECORD IS VARYING IN SIZE FROM 1 TO 8 BYTES
               DEPENDING ON WS-LEN.
       01  F2-REC PIC X(8).
       FD  F3 RECORD CONTAINS 4 TO 12 BYTES.
       01  F3-REC PIC X(12).
       WORKING-STORAGE SECTION.
       01  WS-ST    PIC XX.
       01  WS-LEN   PIC 9(4) VALUE 0.
       01  WS-BYTES PIC X(6) VALUE "USERWD".
       01  WS-TAB.
           05  BYTES OCCURS 3 TIMES PIC X.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F1
           MOVE "ABCDEFGHIJ" TO F1-REC
           WRITE F1-REC
           DISPLAY "F1 W=" WS-ST
           CLOSE F1
           MOVE SPACES TO F1-REC
           OPEN INPUT F1
           READ F1 AT END CONTINUE END-READ
           DISPLAY "F1 R=" WS-ST " [" F1-REC "]"
           CLOSE F1

           MOVE 4 TO WS-LEN
           MOVE "WXYZ" TO F2-REC
           OPEN OUTPUT F2
           WRITE F2-REC
           DISPLAY "F2 W=" WS-ST
           CLOSE F2
           MOVE 99 TO WS-LEN
           OPEN INPUT F2
           READ F2 AT END CONTINUE END-READ
           DISPLAY "F2 R=" WS-ST " LEN=" WS-LEN
           CLOSE F2

           OPEN OUTPUT F3
           MOVE "HELLO-WORLD!" TO F3-REC
           WRITE F3-REC
           DISPLAY "F3 W=" WS-ST
           CLOSE F3

           MOVE "PQR" TO WS-TAB
           DISPLAY "USER=" WS-BYTES " " BYTES(1) BYTES(2) BYTES(3)
           STOP RUN.
