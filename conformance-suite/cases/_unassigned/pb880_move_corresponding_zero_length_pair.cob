*>--- conformance-case
*> id: pb880_move_corresponding_zero_length_pair
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 14.9.25.4 GR11: MOVE CORRESPONDING's results "are the same as if the user had referred to
      *> each pair of corresponding identifiers in separate MOVE statements". So the pair N OF S -> N OF T is
      *> MOVE N OF S TO N OF T, and with N OF S a dynamic-length item of length zero, 14.9.25.4 GR1 applies: "If
      *> identifier-1 is a zero-length item, it is as if literal-1 were specified as a zero-length literal", and
      *> GR2: "If literal-1 is an alphanumeric or national zero-length literal and the receiving operand is other
      *> than a dynamic-length elementary item, literal-1 is treated as if it were the figurative constant SPACE"
      *> - so the PIC 9(3) receiver holds three spaces. The C pair is the control: "AB" -> PIC X(2) is "AB".
      *> kb/Work PB880: the per-pair moves were built by the EMITTER, after the bind-time pass that gives N OF T
      *> the image-backed storage GR2's SPACE fill needs, so this ABORTED the run unit where the written MOVE of
      *> the same pair stored the spaces. Nothing else in this program moves into N OF T, so the storage fact
      *> can only come from the CORRESPONDING statement's own (now bound) moves.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB880C14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S.
          05 N PIC X DYNAMIC LENGTH LIMIT IS 3.
          05 C PIC X(2) VALUE "AB".
       01 T.
          05 N PIC 9(3) VALUE 123.
          05 C PIC X(2) VALUE "ZZ".
       PROCEDURE DIVISION.
           MOVE CORRESPONDING S TO T
           DISPLAY "N=[" N OF T "] C=[" C OF T "]"
           STOP RUN.
