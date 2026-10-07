      *> ISO 13.18.29.4 GR2b - "a national group is treated as though
      *> it were an elementary data item of usage national and class and
      *> category national" - and GR1b, its bit-group twin.  8.8.4.5.3
      *> GR2 makes a condition-name test compare "by the rules ...
      *> specified for relation conditions", so 8.8.4.2.9 orders a
      *> national conditional variable under the NATIONAL program
      *> collating sequence and 8.8.4.2.8 compares a boolean one by
      *> value with boolean-zero extension - never the alphanumeric one.
      *> 14.7.8 rule 2 - "When the range of values is defined by
      *> alphanumeric or national literals, the range of values depends
      *> on the collating sequence used for evaluation of the range" -
      *> makes the THROUGH leg decide on the sequence, not merely carry
      *> it.  kb/Work PB728: the level-88 renderer read the item's own
      *> PICTURE instead of its OPERAND picture, and a GROUP has none,
      *> so a GROUP-USAGE NATIONAL conditional variable was weighed by
      *> the ALPHANUMERIC sequence over a national image while the
      *> identical elementary 88 was weighed correctly.
      *>
      *> The two alphabets are declared with OPPOSITE orders so no leg
      *> can pass for the wrong reason (12.3.6.4 GR9/GR10 install both;
      *> 12.3.7.4 GR7 puts every unlisted character above both blocks).
      *>   REV-AN  "CBA"   -> alphanumeric  C(0) < B(1) < A(2)
      *>   REV-NAT N"ABC"  -> national      A(0) < B(1) < C(2)
      *>
      *> Every expected value is DERIVED, not measured.
      *>  V01 THE DISCRIMINATOR.  GN is a national GROUP holding N"CCC";
      *>      its 88 range is N"AAA" THRU N"CCC".  Under REV-NAT that is
      *>      an ascending range whose upper bound GN equals -> Y.  Under
      *>      REV-AN the same two literals are INVERTED (A is 2, C is 0),
      *>      so the range is empty and the answer would be N.  Before
      *>      PB728/PB741 the compiler emitted __COLLATE here and
      *>      answered N.
      *>  V02 THE AGREEMENT THIS PAIR EXISTS TO ASSERT: the identical
      *>      bounds written as a RELATION over a national ELEMENTARY
      *>      item holding N"CCC".  8.8.4.5.3 GR2 says the two are the
      *>      same comparison, so they must answer alike -> Y.
      *>  V03 THE OVER-FIX GUARD.  An ordinary ALPHANUMERIC group must
      *>      stay on the ALPHANUMERIC sequence (8.8.4.2.1 - "an
      *>      alphanumeric group item shall be treated as an elementary
      *>      alphanumeric data item").  AG holds "CCC" and its range is
      *>      "CCC" THRU "AAA", which ASCENDS under REV-AN (C < B < A)
      *>      -> Y.  Under the national REV-NAT ("ABC") it would be
      *>      inverted, which 13.18.63.3 SR26 b) refuses at compile
      *>      time for a known sequence (COBOLNET2961, kb/Work PB552) -
      *>      so a fix that moved this group to the national sequence
      *>      would not compile.  (The range was "AAA" THRU "CCC",
      *>      inverted under REV-AN, before SR26 was enforced.)
      *>  V04 the alphanumeric ELEMENTARY twin of V03 -> Y.
      *>  V05 the BOOLEAN group: 8.8.4.2.8 compares boolean values
      *>      "regardless of their usage" and rule 2 extends the shorter
      *>      operand "on the right by sufficient boolean zeros", so no
      *>      collating sequence applies at all and B"101" equals
      *>      B"101" -> Y.  A no-regression guard for the pad arm: it is
      *>      TRUE under any sequence, and its value is the EMITTED
      *>      form (pad '0', no collate argument).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB728G88.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XCOMP PROGRAM COLLATING SEQUENCE IS REV-AN
           REV-NAT.
       SPECIAL-NAMES.
           ALPHABET REV-AN IS "CBA"
           ALPHABET REV-NAT FOR NATIONAL IS N"ABC".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GN GROUP-USAGE NATIONAL.
          88 GN-IN-RANGE VALUE N"AAA" THRU N"CCC".
          05 GN-A PIC N(3).
       01 EN PIC N(3).
       01 AG.
          88 AG-IN-RANGE VALUE "CCC" THRU "AAA".
          05 AG-A PIC X(3).
       01 AE PIC X(3).
          88 AE-IN-RANGE VALUE "CCC" THRU "AAA".
       01 BG GROUP-USAGE BIT.
          88 BG-IS-101 VALUE B"101".
          05 BG-A PIC 1(3) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           MOVE N"CCC" TO GN-A
           MOVE N"CCC" TO EN
           MOVE "CCC" TO AG-A
           MOVE "CCC" TO AE
           MOVE B"101" TO BG-A
           IF GN-IN-RANGE
              DISPLAY "V01=Y" ELSE DISPLAY "V01=N" END-IF
           IF EN >= N"AAA" AND EN <= N"CCC"
              DISPLAY "V02=Y" ELSE DISPLAY "V02=N" END-IF
           IF AG-IN-RANGE
              DISPLAY "V03=Y" ELSE DISPLAY "V03=N" END-IF
           IF AE-IN-RANGE
              DISPLAY "V04=Y" ELSE DISPLAY "V04=N" END-IF
           IF BG-IS-101
              DISPLAY "V05=Y" ELSE DISPLAY "V05=N" END-IF
           STOP RUN.
