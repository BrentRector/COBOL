      *>--- conformance-case
      *> id: pb693_enter_after_periodless_move
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB693, THE 85 SIDE OF THE SAME GATE - a word ISO 8.9
      *> reserves at COBOL-85 and FREES later.  ENTER is 85-reserved
      *> and was deleted by ISO 2002 (VCR Table 7 row 7.16; the 8.9
      *> list of the 2023 standard runs END-WRITE -> ENVIRONMENT with
      *> no ENTER, and reserved-words.json carries r85=true with
      *> r2002/r2014/r2023 false).  8.3.2.1 rule 1 - "Reserved words
      *> shall not be used as user-defined words or system-names" -
      *> therefore bars ENTER from the cobolWord slot AT 85 ONLY.
      *>
      *> Before the fix cobolWord admitted ENTER at every edition, so
      *> this ONE sentence - `MOVE 2 TO N ENTER COBOL.`, no period
      *> after the MOVE - parsed as a MOVE with the receivers N and
      *> ENTER and answered COBOLNET1639: a conforming COBOL-85
      *> program rejected.  With the gate the enterStatement arm wins
      *> at 85 and the statement is the comment-equivalent no-op VCR
      *> 7.16 documents, so N keeps the 2 the MOVE put there.
      *>
      *> This is the direction pb693_unlock_after_periodless_move
      *> (2002 dir) cannot show: there the word is free at 85 and
      *> reserved above, here it is reserved at 85 and free above.
      *> One derived gate covers both, which is the point.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB693ENTERNOP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 2 TO N
           ENTER COBOL.
           DISPLAY "N=" N.
           STOP RUN.
