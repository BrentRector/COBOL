*>--- conformance-case
*> id: pb186_sort_unsigned_wide_binary_key
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB186SRTUW.
      *> kb/Work PB186 - A SORT key that is an UNSIGNED 16-byte binary
      *> item orders by its ALGEBRAIC value across its whole container.
      *>
      *> ISO 14.9.40.4 GR8: "the contents of corresponding key data items
      *> are compared according to the rules for comparison of operands in
      *> a relation condition"; for numeric operands 8.8.4.2.4 compares
      *> "with respect to the algebraic value of the operands regardless of
      *> the manner in which their usage is described".  A PIC 9(31) COMP-5
      *> item owns its 16-byte container range [0, 2^128) (kb/Work R10), so
      *> ALL X"FF" is 2^128 - 1 and ALL X"80" is 0x8080...80, both at or
      *> above 2^127 - the values a SIGNED 128-bit decode of the key window
      *> turns negative, which sorted them BEFORE 7.
      *>
      *> EXPECTED (by value, not by measurement):
      *>   ASCENDING  : SEV (7), HI8 (0x80..80), ALL (2^128 - 1)
      *>   DESCENDING : ALL, HI8, SEV
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SWK ASSIGN TO "pb186-sort-wk.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SWK.
       01 SW-REC.
          05 SW-KEY PIC 9(31) COMP-5.
          05 SW-TAG PIC X(3).
       WORKING-STORAGE SECTION.
       01 DONE-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "ASCENDING".
           SORT SWK ON ASCENDING KEY SW-KEY
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
           DISPLAY "DESCENDING".
           SORT SWK ON DESCENDING KEY SW-KEY
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
           STOP RUN.
       FEED.
           MOVE ALL X"FF" TO SW-REC.
           MOVE "ALL" TO SW-TAG.
           RELEASE SW-REC.
           MOVE 7 TO SW-KEY.
           MOVE "SEV" TO SW-TAG.
           RELEASE SW-REC.
           MOVE ALL X"80" TO SW-REC.
           MOVE "HI8" TO SW-TAG.
           RELEASE SW-REC.
       DRAIN.
           MOVE "N" TO DONE-FLAG.
           PERFORM UNTIL DONE-FLAG = "Y"
               RETURN SWK RECORD
                   AT END MOVE "Y" TO DONE-FLAG
                   NOT AT END DISPLAY SW-TAG
               END-RETURN
           END-PERFORM.
