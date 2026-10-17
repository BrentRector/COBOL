*>--- conformance-case
*> id: pb183_usage_index_in_group_ok
*> rules: [SR-13.18.60.3-4, SR-13.18.60.3-14]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB183 - THE ANTI-OVER-REJECTION PIN. NON-OPTIONAL.
      *>
      *> ISO 13.18.60.3 SR14 restricts FIVE usage phrases - "MESSAGE-TAG, OBJECT REFERENCE, POINTER,
      *> FUNCTION-POINTER, or PROGRAM-POINTER" - to level 1 or a STRONG type declaration. The
      *> NEIGHBOURING rule of the same clause, SR4, names SIX: "The INDEX, MESSAGE-TAG, OBJECT
      *> REFERENCE, POINTER, FUNCTION-POINTER, and PROGRAM-POINTER phrases shall not be specified in a
      *> data item described with the CONSTANT RECORD clause ...". INDEX is in SR4's list and NOT in
      *> SR14's, and the omission is deliberate drafting.
      *>
      *> So `05 IX USAGE INDEX.` inside an ordinary group is LEGAL COBOL and shall compile and run.
      *> This golden is the only thing standing between a future "the two lists should be unified"
      *> tidy-up and a compiler that rejects legal source - the risk that actually matters for a
      *> screen like PB183's, where over-rejection costs a user a working program and under-rejection
      *> costs only a missing diagnostic. A unit drift test asserts the same difference at the
      *> predicate level; this asserts it end to end.
      *>
      *> The index item is also USED (SET / SEARCH-free arithmetic through SET TO) so the golden
      *> cannot pass by declaring something the binder never resolved.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB183IXGRP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 TE PIC X OCCURS 9 INDEXED BY TIX.
       01 G.
          05 GA PIC X(3) VALUE "abc".
          05 IX USAGE INDEX.
          05 GB PIC X(3) VALUE "xyz".
       01 W-N PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           SET TIX TO 4.
           SET IX TO TIX.
           SET TIX TO 1.
           SET TIX TO IX.
           MOVE 0 TO W-N.
           IF TIX = 4 MOVE 1 TO W-N END-IF.
           DISPLAY "IX-ROUNDTRIP=" W-N.
           DISPLAY "GA=" GA " GB=" GB.
           STOP RUN.
