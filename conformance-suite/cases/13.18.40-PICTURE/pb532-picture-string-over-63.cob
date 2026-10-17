*>--- conformance-case
*> id: pb532-picture-string-over-63
*> rules: [SR-13.18.40.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.40.3 SR4 - "The maximum number of characters allowed in character-string-1 is 63."
      *> 64 written X characters.  The count is over character-string-1 AS WRITTEN, not over the
      *> repeat-expanded symbol run: SR6's second sentence fixes that reading, and the expanded reading
      *> would outlaw PIC X(30000), which the positive witness tests/conformance/85/
      *> pb531_picture_repetition_factor.cob compiles.  Nothing in the PICTURE pipeline measured this
      *> length at any edition before the fix (kb/Work PB532).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB532PICTURESTRINGOVER63.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-1 PIC XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX.
       PROCEDURE DIVISION.
           DISPLAY "X".
           STOP RUN.
