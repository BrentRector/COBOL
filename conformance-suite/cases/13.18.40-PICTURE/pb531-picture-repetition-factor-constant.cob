*>--- conformance-case
*> id: pb531-picture-repetition-factor-constant
*> rules: [SR-13.18.40.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.40.3 SR6, second sentence - "The integer may be specified by a constant-name, in which
      *> case the length of the integer, not the length of the constant-name, is counted toward the maximum
      *> number of characters in character-string-1."  The constant-name spelling substitutes an INTEGER, so
      *> it is bound by the same first sentence: unsigned and nonzero.  reject-at names 2002+ only because
      *> the CONSTANT entry is itself a COBOL-2002 introduction.
      *> THE TWO-ARM SHAPE (kb/Work PB531): DataBinder.Constants.ExpandPicConstants rewrites
      *> (constant-name) to (integer) in the source string and hands it to the SAME expander, so before the
      *> fix this crashed identically to the literal spelling - the constant arm had been added without
      *> re-deriving the repetition factor's own syntax rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB531PICTUREREPETITIONFACTOR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CONSTANT-N CONSTANT AS -3.
       01 W-1 PIC 9(CONSTANT-N).
       PROCEDURE DIVISION.
           DISPLAY "X".
           STOP RUN.
