*>--- conformance-case
*> id: pb790-alphabet-through-with-also
*> rules: [FMT-12.3.7.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB790 - ONE ALPHABET literal-phrase entry may carry a THROUGH range OR ALSO operands, never
      *> both.  The literal-phrase figure (ISO 12.3.7.2, RENDERED - PDF p321 / folio 291) stacks
      *> `{THROUGH|THRU} literal-2` and `{ALSO literal-3}...` inside ONE pair of square brackets with no
      *> choice indicators, and 5.2.6.2 lets a bracket supply "one of the alternatives contained within the
      *> brackets".  This entry compiled clean at every edition and the ALSO operand "D" was silently dropped:
      *> it kept an UNSPECIFIED position above "A" (12.3.7.4 GR7 k3) instead of sharing "A"'s, so the IF below
      *> printed ABOVE.  There is no merged reading - GR7 k5 (THROUGH) and k6 (ALSO) place operands by
      *> incompatible rules - so the entry is refused by name: COBOLNET2242.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB790TA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE IS ALF.
       SPECIAL-NAMES.
           ALPHABET ALF IS "A" THRU "C" ALSO "D".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC X VALUE "D".
       01 A PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF D > A DISPLAY "ABOVE" ELSE DISPLAY "NOT ABOVE".
           STOP RUN.
