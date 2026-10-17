*>--- conformance-case
*> id: pb452_set_content_numeric
*> rules: [SR-14.9.39.3-31, GR-14.9.39.4-32, GR-14.9.39.4-36]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 14.9.39.2 Format 15 (numeric-content), general rules 32 and 36 - the SET statement that
      *> sets a numeric item to an extreme of its OWN data description. kb/Work PB452.
      *>
      *> GR32 a) "the content is set to the value farthest away from zero permitted by the specifications of
      *> identifier-14"; GR36 a) "the content is set to the nonzero value nearest to zero permitted by the
      *> specifications of identifier-14"; GR32 c) / GR36 c) "If the SIGN phrase is specified, the sign of the
      *> content of identifier-14 is set according to the SIGN specification; otherwise, the sign ... is set to
      *> indicate that the content is positive."
      *>
      *> Every expected value below is COMPUTED FROM THE DESCRIPTION, not from a run:
      *>   S9(4)V99  - a DigitCount item, 6 digit positions with scale 2, so the permitted magnitudes run
      *>               0.00 .. 9999.99 in steps of 0.01: farthest = 9999.99, nearest nonzero = 0.01.
      *>   9(4)V99   - the same digits, unsigned, so it holds no sign to set (GR32 c has nothing to apply).
      *>   S9(4) COMP-5 - a BinaryCapacity item (13.18.60.4 GR12): the item owns its 2-byte container's whole
      *>               two's-complement range -32768 .. 32767, so the two farthest-from-zero values differ in
      *>               MAGNITUDE and 14.9.39.3 SR31 a) makes the SIGN phrase mandatory (see the negative case
      *>               negative/pb452-set-content-sign-required).
      *>   S9(3)     - 3 digit positions, scale 0: farthest = 999, nearest nonzero = 1.
      *> Annex D.32 states the same values from the other side: SET CONTENT OF n TO FARTHEST-FROM-ZERO "is the
      *> same as ... MOVE HIGHEST-ALGEBRAIC (numeric-item) TO numeric-item", and with SIGN NEGATIVE, LOWEST-
      *> ALGEBRAIC; SET ... TO NEAREST-TO-ZERO IN-ARITHMETIC-RANGE, SMALLEST-ALGEBRAIC. Lines M1/M2/M3 assert
      *> that equivalence in the program itself, so the two surfaces cannot drift apart silently.
      *>
      *> The receivers carry SIGN IS LEADING SEPARATE so the printed sign is 13.18.52.4 GR6 b)'s '+'/'-' with no
      *> implementor latitude at all - this golden then does not depend on the --sign-encoding option's default.
      *> OF is written on some statements and omitted on others: 14.9.39.2 Format 15 underlines SET, CONTENT and
      *> TO and leaves OF bare, so it is an optional word (5.2.3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB452SETCONTENTNUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-SIGNED   PIC S9(4)V99 SIGN IS LEADING SEPARATE.
       01 WS-UNSIGNED PIC 9(4)V99.
       01 WS-SMALL    PIC S9(3)    SIGN IS LEADING SEPARATE.
       01 WS-BINARY   PIC S9(4) COMP-5.
       01 WS-SHOW     PIC S9(9)    SIGN IS LEADING SEPARATE.
       01 WS-FOLD     PIC S9(4)V99 SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION.
       MAIN-PARA.
      *> GR32 a) + GR32 c) with no SIGN phrase - the positive extreme.
           SET CONTENT OF WS-SIGNED TO FARTHEST-FROM-ZERO
           DISPLAY "A:" WS-SIGNED
      *> GR32 c) with SIGN NEGATIVE - the same magnitude, negative.
           SET CONTENT OF WS-SIGNED TO FARTHEST-FROM-ZERO SIGN NEGATIVE
           DISPLAY "B:" WS-SIGNED
      *> GR32 c) with SIGN POSITIVE written out - identical to A.
           SET CONTENT OF WS-SIGNED TO FARTHEST-FROM-ZERO SIGN POSITIVE
           DISPLAY "C:" WS-SIGNED
      *> GR36 a) + c) - the nonzero value nearest to zero, 10 to the minus scale.
           SET CONTENT OF WS-SIGNED TO NEAREST-TO-ZERO
           DISPLAY "D:" WS-SIGNED
           SET CONTENT OF WS-SIGNED TO NEAREST-TO-ZERO SIGN NEGATIVE
           DISPLAY "E:" WS-SIGNED
      *> An UNSIGNED receiver: GR32 a) still names its farthest-from-zero value.
           SET CONTENT OF WS-UNSIGNED TO FARTHEST-FROM-ZERO
           DISPLAY "F:" WS-UNSIGNED
           SET CONTENT OF WS-UNSIGNED TO NEAREST-TO-ZERO
           DISPLAY "G:" WS-UNSIGNED
      *> OF OMITTED (5.2.3 - the word is not underlined in the printed general format).
           SET CONTENT WS-SMALL TO FARTHEST-FROM-ZERO
           DISPLAY "H:" WS-SMALL
           SET CONTENT WS-SMALL TO NEAREST-TO-ZERO SIGN NEGATIVE
           DISPLAY "I:" WS-SMALL
      *> SEVERAL RECEIVERS in one statement - the ellipsis on { identifier-14 }. Each receives the extreme of
      *> ITS OWN description (GR32 says "the content of identifier-14", per occurrence), so these two differ.
           SET CONTENT OF WS-SIGNED WS-SMALL TO FARTHEST-FROM-ZERO
           DISPLAY "J:" WS-SIGNED "/" WS-SMALL
      *> A two's-complement container reaches one further down than up (13.18.60.4 GR12), which is why SR31 a)
      *> demands the SIGN phrase here. Shown through a wide separate-sign item so the printed value is the whole
      *> magnitude and not the 4 digit positions the receiver's own PICTURE would display.
           SET CONTENT OF WS-BINARY TO FARTHEST-FROM-ZERO SIGN POSITIVE
           MOVE WS-BINARY TO WS-SHOW
           DISPLAY "K:" WS-SHOW
           SET CONTENT OF WS-BINARY TO FARTHEST-FROM-ZERO SIGN NEGATIVE
           MOVE WS-BINARY TO WS-SHOW
           DISPLAY "L:" WS-SHOW
      *> IN-ARITHMETIC-RANGE (GR32 b / GR36 b): the content is set to whichever of the item's own extreme and
      *> the arithmetic mode's is closer to zero (farthest) or farther from zero (nearest). Under the default
      *> NATIVE mode the intermediate is binary64 - +-1.797E+308 down to 4.94E-324 - so a 6-digit fixed-point
      *> item's own bounds are the closer/farther ones and the phrase changes nothing here.
           SET CONTENT OF WS-SIGNED TO FARTHEST-FROM-ZERO IN-ARITHMETIC-RANGE
           DISPLAY "M:" WS-SIGNED
           SET CONTENT OF WS-SIGNED TO NEAREST-TO-ZERO IN-ARITHMETIC-RANGE SIGN NEGATIVE
           DISPLAY "N:" WS-SIGNED
      *> THE ANNEX D.32 EQUIVALENCE, asserted rather than trusted.
           SET CONTENT OF WS-SIGNED TO FARTHEST-FROM-ZERO
           MOVE FUNCTION HIGHEST-ALGEBRAIC(WS-FOLD) TO WS-FOLD
           IF WS-SIGNED = WS-FOLD
               DISPLAY "M1:SAME"
           ELSE
               DISPLAY "M1:DIFF"
           END-IF
           SET CONTENT OF WS-SIGNED TO FARTHEST-FROM-ZERO SIGN NEGATIVE
           MOVE FUNCTION LOWEST-ALGEBRAIC(WS-FOLD) TO WS-FOLD
           IF WS-SIGNED = WS-FOLD
               DISPLAY "M2:SAME"
           ELSE
               DISPLAY "M2:DIFF"
           END-IF
      *> (D.32's third equivalence, SET ... TO NEAREST-TO-ZERO IN-ARITHMETIC-RANGE against MOVE
      *> SMALLEST-ALGEBRAIC, is asserted in 2023/pb452_set_content_smallest_2023: the SMALLEST-ALGEBRAIC
      *> function is a COBOL-2023 addition while SET format 15 is a 2014 one, so it cannot be written here.)
           STOP RUN.
