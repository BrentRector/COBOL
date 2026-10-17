      *>--- conformance-case
      *> id: pb501_condition_name_entry_format
      *> rules: [GR-13.16.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.16.2 format 3 - `88 condition-name-1 value-clause .` - the OVER-REJECTION guard for the entry-format
      *> screen that enforces it (kb/Work PB501). Format 3 requires BOTH constituents, and the screen has to accept
      *> every legal spelling of the pair while refusing an entry that is missing either one. The shapes below are
      *> the ones the screen could have mis-read:
      *>   N-SEVEN  - the plain single-literal form.
      *>   N-RANGE  - the THROUGH range form (13.18.63.2 format 3's second brace pair with the THRU bracket).
      *>   N-LIST   - several literal items, which the ellipsis in that figure admits.
      *>   G-88     - a condition-name over a GROUP conditional variable (8.8.4.2.1 treats an alphanumeric group
      *>              item as an elementary alphanumeric data item), which carries no PICTURE of its own.
      *> Expected values are derived from 13.16.4 GR3 - a condition-name is true when the conditional variable
      *> holds one of the values associated with it. (The `WHEN SET TO FALSE` bracket is a COBOL-2002 addition and
      *> rides its own introduction gate, so it is not written here: the ENTRY-format rule this case guards does
      *> not differ by edition, and one golden at the introducing edition is what it needs.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB501FORMAT3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(2) VALUE 07.
          88 N-SEVEN  VALUE 07.
          88 N-RANGE  VALUE 1 THRU 9.
          88 N-LIST   VALUE 01 03 07.
       01 G.
          05 GA PIC X(2) VALUE "AB".
          88 G-AB  VALUE "AB".
       PROCEDURE DIVISION.
       MAIN.
           IF N-SEVEN DISPLAY "SEVEN=T" ELSE DISPLAY "SEVEN=F" END-IF
           IF N-RANGE DISPLAY "RANGE=T" ELSE DISPLAY "RANGE=F" END-IF
           IF N-LIST  DISPLAY "LIST=T"  ELSE DISPLAY "LIST=F"  END-IF
           IF G-AB    DISPLAY "GAB=T"   ELSE DISPLAY "GAB=F"   END-IF
           DISPLAY "N=" WS-N
           STOP RUN.
