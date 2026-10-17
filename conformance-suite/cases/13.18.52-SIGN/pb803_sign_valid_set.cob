*>--- conformance-case
*> id: pb803_sign_valid_set
*> rules: [GR-13.18.52.4-5, DOC-A.1-178]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.52.4 GR5 — a signed numeric item whose SIGN clause omits SEPARATE CHARACTER
      *> (Annex A.1 item 178; kb/Work PB803, owner decision 2026-09-09).
      *>
      *> THE RULE, both halves.  §13.18.52.4 GR5: "If the SEPARATE CHARACTER phrase is not
      *> specified, then:  a) The operational sign is presumed to be associated with the leading
      *> (or, respectively, trailing) digit position of the data item to which it applies.
      *> b) The implementor defines what constitutes valid signs for data items."
      *> a) is NORMATIVE and carries no latitude: the sign is associated with a DIGIT position, so
      *> it adds no character position.  b) is the whole of A.1 item 178, and §8.8.4.4.4 GR3 n)1.a
      *> delegates the NUMERIC class condition to it: "the condition is true if the presence or
      *> absence of an operational sign in the content of the data item ... is in agreement with the
      *> data description ... and if the content, except for the operational sign, consists entirely
      *> of the characters 0, 1, 2, 3, ..., 9. Valid operational signs are defined in 13.18.52,
      *> SIGN clause."
      *>
      *> ⛔ GR5 AND GR6 ARE MUTUALLY EXCLUSIVE BY THEIR OWN ANTECEDENTS, so every leg below writes a
      *> SIGN clause WITHOUT SEPARATE.  A `SIGN IS TRAILING SEPARATE` leg would pin §13.18.52.4 GR6,
      *> a different rule with no implementor latitude at all ("The operational signs for positive
      *> and negative are the basic special characters '+' and '-'").
      *>
      *> THE DETERMINATION PINNED HERE (docs/CONFORMANCE.md DOC-A.1-178), under the default
      *> --sign-encoding=ibm: the valid characters at the SIGN POSITION are the plain digits 0-9
      *> (an item whose sign was never punched), "{ABCDEFGHI" (+0..+9) and "}JKLMNOPQR" (-0..-9);
      *> every other character position must be a plain digit.  Any other character at the sign
      *> position makes the item NOT NUMERIC.
      *>
      *> WHY EACH LEG CAN FAIL — expected values derived from the determination, not observed.
      *> WIDTH is GR5 a): a LEADING item and a TRAILING item are each 3 character positions on
      *>   PIC S9(3), and the enclosing group of 3 + 1 + 3 + 1 is 8.  An implementation that gave a
      *>   fused sign a character position of its own answers 4 4 10 here.
      *> The class legs load RAW CHARACTERS into a PIC X(3) and read them through a REDEFINES
      *>   window, so nothing the compiler stored can mask what the class condition accepts.  The
      *>   accepted set is exercised at BOTH ends of each table (the position-0 characters `{` and
      *>   `}`, which are the ones a "letters only" reading would drop) and the REFUSED set contains
      *>   the four near-misses that a sloppy predicate lets through: the SEPARATE signs `+`/`-`
      *>   (GR6's characters, not GR5's), a space, the LOWER-CASE `l` (the table is upper-case), and
      *>   `Z` (a letter just past the end of the `}J-R` run).
      *> Every ACCEPTED leg also prints the algebraic value, so the class test and the value are
      *>   pinned to agree: `12{` is +120 and `12}` is -120 -- table position 0 is the digit 0, not
      *>   a sign-only character.  A REFUSED leg prints no value: what a numeric operation makes of
      *>   incompatible content is §14.6.13.2's question, not this rule's, and pinning it here would
      *>   file one determination under another's number.
      *> The L legs repeat the set at the LEADING position, which is GR5 a)'s "or, respectively":
      *>   the same table, read at the other end of the item.  ⛔ THEIR VALUES ARE 323 / -323 / 023,
      *>   NOT 123 / -123 / 023, and that is the point: a punched character carries a DIGIT as well
      *>   as a sign, so `C` at the LEADING position contributes the digit 3 (positive table position
      *>   3) exactly as it contributes the digit 3 at the trailing position of `12C`.  An
      *>   implementation that treated the punch as a sign-only marker and left the underlying digit
      *>   alone would answer 123 / -123 here and pass every other leg in this file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB803B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GW.
          05 GWL   PIC S9(3) SIGN IS LEADING.
          05 GWM   PIC X VALUE "|".
          05 GWT   PIC S9(3) SIGN IS TRAILING.
          05 GWE   PIC X VALUE "|".
       01 RAW.
          05 R3    PIC X(3).
          05 RT    REDEFINES R3 PIC S9(3) SIGN IS TRAILING.
          05 RL    REDEFINES R3 PIC S9(3) SIGN IS LEADING.
       01 SHOW PIC -999.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "WIDTH=" FUNCTION BYTE-LENGTH(GWL)
               " " FUNCTION BYTE-LENGTH(GWT)
               " " FUNCTION BYTE-LENGTH(GW)
           MOVE "123" TO R3
           PERFORM SHOW-T
           MOVE "12C" TO R3
           PERFORM SHOW-T
           MOVE "12L" TO R3
           PERFORM SHOW-T
           MOVE "12{" TO R3
           PERFORM SHOW-T
           MOVE "12}" TO R3
           PERFORM SHOW-T
           MOVE "12-" TO R3
           PERFORM SHOW-T
           MOVE "12+" TO R3
           PERFORM SHOW-T
           MOVE "12 " TO R3
           PERFORM SHOW-T
           MOVE "12l" TO R3
           PERFORM SHOW-T
           MOVE "12Z" TO R3
           PERFORM SHOW-T
           MOVE "C23" TO R3
           PERFORM SHOW-L
           MOVE "L23" TO R3
           PERFORM SHOW-L
           MOVE "{23" TO R3
           PERFORM SHOW-L
           MOVE "Z23" TO R3
           PERFORM SHOW-L
           STOP RUN.
       SHOW-T.
           IF RT IS NUMERIC
              MOVE RT TO SHOW
              DISPLAY "T[" R3 "]=Y " SHOW
           ELSE
              DISPLAY "T[" R3 "]=N"
           END-IF.
       SHOW-L.
           IF RL IS NUMERIC
              MOVE RL TO SHOW
              DISPLAY "L[" R3 "]=Y " SHOW
           ELSE
              DISPLAY "L[" R3 "]=N"
           END-IF.
