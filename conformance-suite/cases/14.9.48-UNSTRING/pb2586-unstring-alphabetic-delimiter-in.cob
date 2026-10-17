*>--- conformance-case
*> id: pb2586-unstring-alphabetic-delimiter-in
*> rules: [SR-14.9.48.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.48.3 SR2 (kb/Work PB2586): identifier-5 (the DELIMITER IN receiver) shall reference a data item
      *> of category alphanumeric or national; a PIC A receiver is category alphabetic. (SR4 admits alphabetic for
      *> identifier-4, the INTO receiver, which is a different rule.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2586N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(5) VALUE "AB CD".
       01 DI PIC A.
       01 B PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           UNSTRING A DELIMITED BY SPACE INTO B DELIMITER IN DI
           STOP RUN.
