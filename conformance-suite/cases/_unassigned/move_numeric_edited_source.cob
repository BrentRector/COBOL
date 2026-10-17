*>--- conformance-case
*> id: move_numeric_edited_source
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> CA27 (CONFORMANCE-FIX-QUEUE): MOVE of a numeric-edited source to a numeric-edited (or numeric) receiver
      *> DE-EDITS the sender to its numeric value (ISO 14.9.25.4 GR5 + GR6d1, sign preserved) then re-edits it into
      *> the receiver mask. Pre-fix, the edited image was digit-extracted (' 12.34' -> 1234 at scale 0), giving
      *> '234.00' instead of '012.34'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA27.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NUM PIC 99V99 VALUE 12.34.
       01 SRC PIC ZZ9.99.
       01 DST PIC 999.99.
       PROCEDURE DIVISION.
           MOVE NUM TO SRC.
           MOVE SRC TO DST.
           DISPLAY DST.
           STOP RUN.
