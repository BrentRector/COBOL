*>--- conformance-case
*> id: l1_char_national_also_positions
*> rules: [GR-12.3.7.4-7, RV-15.16.4-2, DOC-A.1-22, DOC-A.1-90]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.16.4 r2 FUNCTION CHAR-NATIONAL, quoted WHOLE because its three parts are not independent:
      *> "If more than one character has the same position in the national program collating sequence, the
      *> character returned is the first character defined for that character position. If the order of multiple
      *> characters having the same position is undefined, the implementor shall define which of those multiple
      *> characters is returned; for a given implementation, collating sequence, and ordinal position, every
      *> invocation of the CHAR-NATIONAL function shall return the same character."
      *> ⛔ THE THIRD PART IS SEMICOLON-JOINED TO THE SECOND, i.e. it rides the UNDEFINED-ORDER branch (and that
      *> branch is Annex A.1 item 22, already determined at CONFORMANCE.md §7). An ALSO phrase does NOT enter
      *> that branch: §12.3.7.4 GR7 k)6 DEFINES the order ("Literal-1 is the first character in the sequence of
      *> multiple characters defined at that ordinal position"), so sentence ONE governs and its answer is
      *> forced, not chosen. This golden therefore closes r2 on sentence one, and carries the determinism
      *> property as a check rather than a claim (see SAME below).
      *> Also §15.16.3 r2 - "The value of argument-1 shall be greater than zero and less than or equal to the
      *> number of positions in the national program collating sequence" - measured against the sequence THIS
      *> alphabet actually builds, which is NOT 65 536 because the ALSO members share one position.
      *>
      *> THE SIBLING IT MIRRORS. conformance:2002/pb59_char_representative already pins the ALPHANUMERIC twin of
      *> this rule (§15.15.4 r2) with the same highest-coded-literal-1 discriminator and the same bound pattern
      *> (`ALPHABET AL IS "C" ALSO "A" ALSO "B"` -> CH1 =[C], ORMAX=065534, OVER =DEFAULT). What was untested is
      *> the NATIONAL branch: no golden carries an ALSO phrase on a FOR NATIONAL alphabet, and the two branches
      *> are DIFFERENT runtime bodies - AlphanumericCollation over there, NationalCollation.CharAt here - so
      *> neither is evidence about the other. That two-arm shape is exactly what this file exists to close.
      *>
      *> THE SEQUENCE. §12.3.7.4 GR7 k)2: "The order in which the literals appear in the ALPHABET clause
      *> specifies, in ascending sequence, the ordinal number of the character within the collating sequence
      *> being specified" - so the ALSO group is ordinal position 1 and N"M" is ordinal position 2.
      *> §12.3.7.4 GR7 k)6, quoted whole so nothing is elided: "If the ALSO phrase is specified, the characters
      *> of the native character set specified by the value of literal-1 and literal-3 are assigned to the same
      *> ordinal position in the collating sequence being specified or in the character code set that is used to
      *> represent the data. Literal-1 is the first character in the sequence of multiple characters defined at
      *> that ordinal position." So C, A and B all sit at
      *> position 1 and C is the FIRST character defined there. LITERAL-1 IS THE HIGHEST-CODED MEMBER on
      *> purpose: an implementation returning the lowest code answers A, one returning the last-written member
      *> answers B, and both are ruled out by P1 below.
      *> §12.3.7.4 GR7 k)3: "Any characters of the native collating sequence that are not specified in the
      *> literal phrase shall assume a position in the collating sequence that is greater than that of the
      *> highest character specified in this literal phrase. The relative order within the set of these
      *> unspecified characters is unchanged from the native collating sequence." The native national character
      *> set is the 65 536 UTF-16 code units, one per character position (§8.5.1.4; CONFORMANCE.md item 188), so
      *> the 65 532 unspecified units take positions 3 .. 65 534 in code order, U+FFFF last.
      *> TOTAL POSITIONS = 2 + 65 532 = 65 534, and §15.16.3 r2's window is 1 .. 65 534.
      *>
      *> WHY ORD OVER DISPLAY-OF IS THE ORACLE AND A RELATION IS NOT. C, A and B share a collating position, so
      *> the national relation condition (§8.8.4.2.9) reports all three EQUAL - `N-X = N"A"` is true whichever
      *> one was returned, and FUNCTION ORD over a national argument reads the same weights (§15.70.4 r2).
      *> FUNCTION DISPLAY-OF is the character correspondence, not a collating sequence (§15.26.4 r1;
      *> CONFORMANCE.md item 33 - the total UTF-16 identity), and the ALPHANUMERIC program collating sequence
      *> here is NATIVE, so by §15.70.4 r1 ("If the class of argument-1 is alphabetic or alphanumeric, the
      *> returned value is the ordinal position of argument-1 in the current alphanumeric program collating
      *> sequence") read with §15.70.1 ("The lowest ordinal position is 1"), ORD of the converted character is
      *> its code unit + 1: an encoding-immune identity oracle that no shared collating position can blur.
      *> C = U+0043 -> 68; M = U+004D -> 78; U+FFFF -> 65536; the national space U+0020 -> 33.
      *>
      *> P1     - r2 SENTENCE ONE, the discriminating leg: position 1 returns C (68), not A (66), not B (67).
      *> P2     - r1/k)2 control: position 2 is the second literal-phrase element, M (78).
      *> SAME   - r2's determinism tail, as a PRESENCE CHECK and NOT a discriminator, stated honestly: both call
      *>          sites pass the same literal and NationalCollation.CharAt is a pure array read, so this leg
      *>          cannot fail while P1 passes - and its clause rides the undefined-order branch this alphabet
      *>          never enters. It is carried because "every invocation of the CHAR-NATIONAL function shall
      *>          return the same character" is a rule of the standard that a golden closing r2 should be seen
      *>          to satisfy, and it becomes a
      *>          real discriminator the day the choice among a shared position's members stops being one
      *>          deterministic table read.
      *> LAST   - §15.16.3 r2 upper bound, INSIDE: 65 534 is the last position and holds U+FFFF (65536).
      *> OVER   - §15.16.3 r2 upper bound, OUTSIDE: 65 535 exceeds the 65 534 positions this sequence has, so
      *>          §15.3 sets EC-ARGUMENT-FUNCTION and, checking being disabled, "the implementor defines the
      *>          result of the function reference" (Annex A.1 item 90). COBOL.NET's determination is the ZERO
      *>          VALUE OF THE RETURNED TYPE - one national space for CHAR-NATIONAL - recorded at
      *>          CONFORMANCE.md §7 item 90, so 33. The load-bearing half is that it is NOT 65535: the bound
      *>          tracks the sequence, and a hardcoded 65 536 ceiling would answer 65535 here.
      *> ZERO   - §15.16.3 r2 lower bound: 0 is not greater than zero, same item-90 default, 33.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CNALSO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. GENERIC-BOX
           PROGRAM COLLATING SEQUENCE
               FOR ALPHANUMERIC IS STD-SEQ
               FOR NATIONAL IS DUP-NAT.
       SPECIAL-NAMES.
           ALPHABET STD-SEQ IS NATIVE
           ALPHABET DUP-NAT FOR NATIONAL IS N"C" ALSO N"A" ALSO N"B", N"M".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-X    PIC N.
       01 N-Y    PIC N.
       01 A-X    PIC X.
       01 A-Y    PIC X.
       01 ORD-R  PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION CHAR-NATIONAL(1) TO N-X.
           MOVE FUNCTION DISPLAY-OF(N-X) TO A-X.
           COMPUTE ORD-R = FUNCTION ORD(A-X).
           DISPLAY "P1=" ORD-R.
           MOVE FUNCTION CHAR-NATIONAL(2) TO N-Y.
           MOVE FUNCTION DISPLAY-OF(N-Y) TO A-Y.
           COMPUTE ORD-R = FUNCTION ORD(A-Y).
           DISPLAY "P2=" ORD-R.
           MOVE FUNCTION CHAR-NATIONAL(1) TO N-Y.
           MOVE FUNCTION DISPLAY-OF(N-Y) TO A-Y.
           IF A-X = A-Y DISPLAY "SAME=YES" ELSE DISPLAY "SAME=NO" END-IF.
           MOVE FUNCTION CHAR-NATIONAL(65534) TO N-X.
           MOVE FUNCTION DISPLAY-OF(N-X) TO A-X.
           COMPUTE ORD-R = FUNCTION ORD(A-X).
           DISPLAY "LAST=" ORD-R.
           MOVE FUNCTION CHAR-NATIONAL(65535) TO N-X.
           MOVE FUNCTION DISPLAY-OF(N-X) TO A-X.
           COMPUTE ORD-R = FUNCTION ORD(A-X).
           DISPLAY "OVER=" ORD-R.
           MOVE FUNCTION CHAR-NATIONAL(0) TO N-X.
           MOVE FUNCTION DISPLAY-OF(N-X) TO A-X.
           COMPUTE ORD-R = FUNCTION ORD(A-X).
           DISPLAY "ZERO=" ORD-R.
           STOP RUN.
