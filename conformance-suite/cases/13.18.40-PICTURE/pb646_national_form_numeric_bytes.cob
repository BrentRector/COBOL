*>--- conformance-case
*> id: pb646_national_form_numeric_bytes
*> rules: [GR-13.18.40.4-1, GR-13.18.40.4-2, SR-13.18.60.3-12, DOC-A.1-213]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB646 - the BYTE IMAGE of national-form numeric data (ISO 1989:2023 13.18.60.4 GR8).
      *>
      *> GR8: "National characters shall be represented in the storage of the computer as characters of
      *> a uniform size equal to or a multiple of the size of characters in the computer's alphanumeric
      *> character set. Each implementor shall specify the size and representation of characters stored
      *> for usage NATIONAL." COBOL.NET pins TWO bytes per national character position, UTF-16 big-endian
      *> (data-model design D-N1); this golden is where that determination is OBSERVED rather than
      *> asserted, through the byte surfaces 13.18.44 REDEFINES and 14.9.3 the record area.
      *>
      *> 13.18.40.4 GR1 makes each symbol of a usage-national PICTURE a NATIONAL character position, so
      *>   NB1 PIC 9(3) USAGE NATIONAL is 3 positions = 6 bytes
      *>   NB2 PIC 9(2) USAGE NATIONAL is 2 positions = 4 bytes
      *> and the group is 10 bytes, which FUNCTION BYTE-LENGTH (15.14.4) reports and over which
      *> 13.18.44.4 GR1 lays the redefining PIC X(10).
      *>
      *> The redefining item's ten alphanumeric positions hold, in order, the UTF-16BE serialization of
      *> "123" and "45": 00 31 00 32 00 33 00 34 00 35. Reading it back through an ordinary alphanumeric
      *> REDEFINES would print those NUL bytes, so the golden measures the ODD positions instead - the
      *> low byte of each pair - which are exactly the digit characters. The EVEN positions are each the
      *> high byte 00, and the golden asserts that too: it is the half of the determination a
      *> single-byte reading would not distinguish.
      *>
      *> WRITING THROUGH THE OTHER DESCRIPTION IS THE SAME AREA (13.18.44.4 GR1 - "the storage area ...
      *> begins at the first bit of the data item referenced by data-name-2 and continues over an area
      *> sufficient to contain the number of bits required"): storing the ten characters of the
      *> serialization of 987 / 65 through RB makes NB1 read 987 and NB2 read 65.
      *>
      *> THE INSERTION CHARACTERS OF A NATIONAL-FORM NUMERIC-EDITED ITEM (13.18.40.4 GR2): "The value of
      *> insertion and replacement characters in a resultant edited item is the value of those characters
      *> in the computer's runtime coded character set. When the usage of the item being edited is
      *> national, the value is the national character representation; otherwise, the value is the
      *> alphanumeric character representation." NEDIT PIC ZZ9.99 USAGE NATIONAL is SIX national
      *> character positions (GR1 + GR14's Z / 9 / '.'), so 12 bytes, and after MOVE 42.5 its image is
      *> " 42.50" - where the '.' is an INSERTION position (GR14: "the character '.' represents the
      *> decimal point for alignment purposes and ... represents a character position into which the
      *> character '.' is inserted"). The golden reads the 6 LOW bytes back as the image and counts the
      *> 6 HIGH bytes as zero, which is what "the national character representation" of a period is under
      *> the D-N4 total UTF-16 identity: U+002E, serialized 00 2E - not one byte, and not a national
      *> character that differs from its alphanumeric correspondent.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB646NB3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NGRP.
          05 NB1 PIC 9(3) USAGE NATIONAL VALUE 123.
          05 NB2 PIC 9(2) USAGE NATIONAL VALUE 45.
       01 RB REDEFINES NGRP PIC X(10).
       01 EGRP.
          05 NEDIT PIC ZZ9.99 USAGE NATIONAL.
       01 RE REDEFINES EGRP PIC X(12).
       01 LOWS PIC X(5).
       01 ELOW PIC X(6).
       01 HIGHZ PIC 9 VALUE 0.
       01 I PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "BYTES=" FUNCTION BYTE-LENGTH(NGRP)
           MOVE SPACE TO LOWS
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 5
               MOVE RB(2 * I:1) TO LOWS(I:1)
           END-PERFORM
           DISPLAY "LOW=[" LOWS "]"
           MOVE 0 TO HIGHZ
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 5
               IF RB(2 * I - 1:1) = LOW-VALUE THEN ADD 1 TO HIGHZ END-IF
           END-PERFORM
           DISPLAY "HIGHZEROS=" HIGHZ
      *> Write the other description of the same area and read the national items back.
           MOVE LOW-VALUE TO RB(1:1)
           MOVE "9" TO RB(2:1)
           MOVE LOW-VALUE TO RB(3:1)
           MOVE "8" TO RB(4:1)
           MOVE LOW-VALUE TO RB(5:1)
           MOVE "7" TO RB(6:1)
           MOVE LOW-VALUE TO RB(7:1)
           MOVE "6" TO RB(8:1)
           MOVE LOW-VALUE TO RB(9:1)
           MOVE "5" TO RB(10:1)
           DISPLAY "NB1=[" NB1 "] NB2=[" NB2 "]"
      *> 13.18.40.4 GR1/GR2/GR14 over a national-form NUMERIC-EDITED item.
           MOVE 42.5 TO NEDIT
           DISPLAY "EDIT=[" NEDIT "]"
           DISPLAY "EBYTES=" FUNCTION BYTE-LENGTH(EGRP)
           MOVE SPACE TO ELOW
           MOVE 0 TO HIGHZ
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 6
               MOVE RE(2 * I:1) TO ELOW(I:1)
               IF RE(2 * I - 1:1) = LOW-VALUE THEN ADD 1 TO HIGHZ END-IF
           END-PERFORM
           DISPLAY "ELOW=[" ELOW "]"
           DISPLAY "EHIGHZEROS=" HIGHZ
           STOP RUN.
