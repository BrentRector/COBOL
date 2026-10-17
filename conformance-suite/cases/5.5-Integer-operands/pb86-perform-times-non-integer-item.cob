*>--- conformance-case
*> id: pb86-perform-times-non-integer-item
*> rules: [SR-5.5-2, SR-14.9.28.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.28.3 SR2: "Identifier-1 shall be an integer." A PIC 9V9 item has a digit position to the
      *> right of the decimal point, so it is not an integer count. kb/Work PB86: this was ACCEPTED, and the
      *> emitter read the item's UNSCALED digits - X = 1.2 iterated the body 12 times.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB86NEGITEM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9V9 VALUE 1.2.
       01 CNT PIC 99 VALUE 0.
       PROCEDURE DIVISION.
           PERFORM COUNT-IT X TIMES.
           STOP RUN.
       COUNT-IT.
           ADD 1 TO CNT.
