      *> ISO 13.18.63.2 format 3's trailing `[ IN alphabet-name-1 ]` over a level-88 condition-name VALUE
      *> range. 13.18.63.4 GR18 defers the whole question: "The manner of determining the range of values
      *> specified by the THROUGH phrase is described in 14.7.8, THROUGH phrase." 14.7.8 rule 2: with no
      *> phrase "the collating sequence is defined by the implementor" (this compiler's default is the
      *> PROGRAM COLLATING SEQUENCE); "when the IN alphabet-name phrase is specified, the collating sequence
      *> used for range evaluation is the collating sequence defined by that alphabet"; "the range of values
      *> includes all values collating at the starting value and all successive ascending values in the
      *> applicable collating sequence up to and including all values collating at the ending value"; and
      *> "when the value of literal-1 is greater than the value of literal-2 in the collating sequence in
      *> effect at runtime, the EC-RANGE-INVALID exception condition is set to exist, and, upon completion of
      *> any exception processing, execution proceeds as if the range of values were empty."
      *>
      *> ⛔ NO PROGRAM COLLATING SEQUENCE IS DECLARED, AND THAT IS THE POINT. kb/Work PB502's arm of the
      *> phrase (the VALUE clause; the EVALUATE twin is PB398) was PARSED AND DROPPED, a silent wrong answer,
      *> and the sibling golden 2002/pb398_range_in_alphabet cannot catch its return: that program names the
      *> SAME alphabet in PROGRAM COLLATING SEQUENCE, so its level-88 legs answer identically whether the IN
      *> phrase is read or discarded. Here rule 2's no-phrase arm resolves to the NATIVE sequence and every
      *> IN leg below is the OPPOSITE of its no-phrase twin, so dropping the phrase again flips a line.
      *>
      *> POSITIONS. `ALPHABET ALPHA-REV IS "Z" THRU "A"` - 12.3.7.4 GR7 k) 5.: "if the THROUGH phrase is
      *> specified, the set of consecutive characters in the native character set beginning with the
      *> character specified by the value of literal-1, and ending with the character specified by the value
      *> of literal-2, is assigned a successive ascending position in the collating sequence being
      *> specified", and that set "may specify characters of the native character set in either ascending or
      *> descending sequence"; GR7 k) 2. is the same ascending-order rule for a literal list. So Z=1, Y=2 ...
      *> A=26, giving Q=10, P=11, O=12, M=14, D=23. GR7 k) 3. places every unlisted character above them.
      *> WS-A holds "M" - position 14 under ALPHA-REV, x4D natively.
      *>
      *>   A  `VALUE "P" THRU "D" IN ALPHA-REV`        -> 11 <= 14 <= 23, ascending under ALPHA-REV and M is
      *>      inside                                                                    -> TRUE   -> A=T
      *>      (NATIVE would give "P" x50 > "D" x44, an inverted and therefore EMPTY range -> A=F.)
      *>   C  `VALUE "D" THRU "P"`, NO phrase -> rule 2's implementor arm, i.e. the PROGRAM COLLATING
      *>      SEQUENCE, which is the native one here: x44 <= x4D <= x50                  -> TRUE   -> C=T
      *>      (This is the control: it proves A and B are the PHRASE and not a global change of sequence.)
      *>   D  `VALUE "Q" THRU "O", "P" THRU "D" IN ALPHA-REV` - the bracket stands OUTSIDE the repeated
      *>      literal group in the printed format, so ONE alphabet governs EVERY range in the set. Under
      *>      ALPHA-REV the first range is 10..12 (14 is outside -> false) and the second is 11..23
      *>      (14 inside -> true); the disjunction of 13.18.63.4 GR18's value set  -> TRUE   -> D=T
      *>      (NATIVE would make BOTH ranges inverted and empty                            -> D=F.)
      *>   E  `VALUE "P" THRU "D" IN ALPHA-REV WHEN SET TO FALSE IS "Q"` - the PRINTED bracket ORDER. Format
      *>      3 puts `[ IN alphabet-name-1 ]` at the end of the repeated-group line and
      *>      `[ WHEN SET TO FALSE IS literal-4 ]` on the LINE AFTER it, so IN comes FIRST, and 5.2.6.2
      *>      makes that order part of the format. Same range as A, and literal-4 does not disturb it
      *>                                                                                -> TRUE   -> E=T
      *>   B  `VALUE "D" THRU "P" IN ALPHA-REV` - 23 > 11: literal-1 collates AFTER literal-2 under
      *>      ALPHA-REV. That leg moved to conformance:negative/pb552-value-through-not-ascending: ALPHA-REV
      *>      is a sequence the compiler KNOWS, so 13.18.63.3 SR26 b) refuses the inverted range at compile
      *>      time (COBOLNET2961, kb/Work PB552) - in the NAMED sequence, which is what B pinned here (NATIVE
      *>      would give x44 <= x4D <= x50, a perfectly ordered range). A and E are its mirror: inverted
      *>      NATIVELY, they compile only because the phrase is read.
      *>
      *> EXC-STATUS is taken after A, C, D and E - every one of which is ascending in the sequence rule 2
      *> puts it under - so no EC-RANGE-INVALID exists and FUNCTION EXCEPTION-STATUS returns spaces; a
      *> compiler that weighed the ranges natively would raise it for A, D and E.
       >>TURN EC-RANGE-INVALID CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB502VALRNGALPH.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ALPHA-REV IS "Z" THRU "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X VALUE "M".
           88 A-ASC   VALUE "P" THRU "D" IN ALPHA-REV.
           88 A-NONE  VALUE "D" THRU "P".
           88 A-MULTI VALUE "Q" THRU "O", "P" THRU "D" IN ALPHA-REV.
           88 A-ORDER VALUE "P" THRU "D" IN ALPHA-REV WHEN SET TO FALSE IS "Q".
       PROCEDURE DIVISION.
       MAIN-P.
           IF A-ASC   DISPLAY "A=T" ELSE DISPLAY "A=F" END-IF
           IF A-NONE  DISPLAY "C=T" ELSE DISPLAY "C=F" END-IF
           IF A-MULTI DISPLAY "D=T" ELSE DISPLAY "D=F" END-IF
           IF A-ORDER DISPLAY "E=T" ELSE DISPLAY "E=F" END-IF
           DISPLAY "EXC-STATUS=" FUNCTION EXCEPTION-STATUS
           STOP RUN.
