*>--- conformance-case
*> id: pb2586-unstring-alphabetic-slice-sender
*> rules: [SR-14.9.48.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.48.3 SR2 (kb/Work PB2586) with 8.4.3.3.4 GR6: the unique data item of a reference modification
      *> has the same category as identifier-1 except for the lettered rewrites, none of which names alphabetic,
      *> so a slice of a PIC A item is still category alphabetic and is not a legal sender.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2586N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC A(5) VALUE "AB CD".
       01 B PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           UNSTRING A(1:3) DELIMITED BY SPACE INTO B
           STOP RUN.
