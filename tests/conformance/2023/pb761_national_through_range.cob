      *> kb/Work PB761 — a condition-name THROUGH range over a NATIONAL
      *> conditional variable is CONFORMING SOURCE and is implemented.
      *> It used to be refused by a COBOLNET0899 "recognized but not yet
      *> implemented (Phase 4a residue)" stage citing 13.18.63 SR31 —
      *> but SR31 reads "Alphabet-name-1 may be specified only when the
      *> literals specified in the THROUGH phrase are of class
      *> alphanumeric or national", which governs when the
      *> IN alphabet-name-1 phrase may be written and says nothing about
      *> implementability.  SR29 bans THROUGH for a BOOLEAN subject and
      *> for no other category.
      *>
      *> What actually governs the range is 14.7.8 rule 2 — "When the
      *> range of values is defined by alphanumeric or national
      *> literals, the range of values depends on the collating sequence
      *> used for evaluation of the range" — reached from 8.8.4.5.3 GR2,
      *> "The rules for comparing a conditional variable with a
      *> condition-name value are the same as those specified for
      *> relation conditions", which sends a national subject to
      *> 8.8.4.2.9's NATIONAL program collating sequence.
      *>
      *> The two alphabets are declared with OPPOSITE orders so no leg
      *> can pass for the wrong reason (12.3.6.4 GR9/GR10 install both;
      *> 12.3.7.4 GR7 k)3 puts every unlisted character above both).
      *>   REV-AN  "CBA"   -> alphanumeric  C(0) < B(1) < A(2)
      *>   REV-NAT N"ABC"  -> national      A(0) < B(1) < C(2)
      *> Every expected value is DERIVED, not measured.
      *>
      *>  V01 THE DISCRIMINATOR, ELEMENTARY.  EN is PIC N(3) holding
      *>      N"CCC"; its 88 range is N"AAA" THRU N"CCC".  Under REV-NAT
      *>      that is ascending and EN equals its upper bound -> Y.
      *>      Under REV-AN the same two literals are INVERTED (A is 2,
      *>      C is 0), so the range is empty and the answer would be N.
      *>      Before PB761 this entry did not run at all: it was
      *>      REFUSED at COBOLNET0899.
      *>  V02 THE SECOND DISCRIMINATOR, in the other direction.  E3 is
      *>      N"AAA" and its range is N"AAA" THRU N"BBB": under REV-NAT
      *>      ascending with E3 at the LOWER bound -> Y; under REV-AN
      *>      inverted and therefore empty -> N.
      *>  V03 THE AGREEMENT 8.8.4.2.1 REQUIRES: the identical range over
      *>      a GROUP-USAGE NATIONAL subject, which "shall be treated as
      *>      an elementary national data item" (13.18.29.4 GR2b gives
      *>      it the as-if PICTURE N(3)) -> Y, the same as V01.
      *>  V04 THE OUT-OF-RANGE CONTROL.  E2 holds N"CCC" against the
      *>      range N"AAA" THRU N"BBB"; under REV-NAT C(2) is above the
      *>      upper bound B(1) -> N.  Without it every national leg
      *>      could be vacuously true.
      *>  V05 THE OVER-FIX GUARD.  An ALPHANUMERIC elementary item must
      *>      stay on the ALPHANUMERIC sequence (8.8.4.2.7).  AE holds
      *>      "CCC" and its range is "BBB" THRU "AAA", which ASCENDS
      *>      under REV-AN (C < B < A) and excludes C -> N.  Under
      *>      REV-NAT ("ABC") the same pair is inverted, which
      *>      13.18.63.3 SR26 b) refuses at compile time for a known
      *>      sequence (COBOLNET2961, kb/Work PB552), so a change that
      *>      moved the alphanumeric side to REV-NAT would not compile.
      *>      (The range was "AAA" THRU "CCC", inverted under REV-AN,
      *>      before SR26 was enforced.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB761NTR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XCOMP PROGRAM COLLATING SEQUENCE IS REV-AN
           REV-NAT.
       SPECIAL-NAMES.
           ALPHABET REV-AN IS "CBA"
           ALPHABET REV-NAT FOR NATIONAL IS N"ABC".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EN PIC N(3).
          88 EN-IN-RANGE VALUE N"AAA" THRU N"CCC".
       01 E3 PIC N(3).
          88 E3-IN-RANGE VALUE N"AAA" THRU N"BBB".
       01 GN GROUP-USAGE NATIONAL.
          88 GN-IN-RANGE VALUE N"AAA" THRU N"CCC".
          05 GN-A PIC N(3).
       01 E2 PIC N(3).
          88 E2-IN-RANGE VALUE N"AAA" THRU N"BBB".
       01 AE PIC X(3).
          88 AE-IN-RANGE VALUE "BBB" THRU "AAA".
       PROCEDURE DIVISION.
       MAIN.
           MOVE N"CCC" TO EN
           MOVE N"AAA" TO E3
           MOVE N"CCC" TO GN-A
           MOVE N"CCC" TO E2
           MOVE "CCC" TO AE
           IF EN-IN-RANGE
              DISPLAY "V01=Y" ELSE DISPLAY "V01=N" END-IF
           IF E3-IN-RANGE
              DISPLAY "V02=Y" ELSE DISPLAY "V02=N" END-IF
           IF GN-IN-RANGE
              DISPLAY "V03=Y" ELSE DISPLAY "V03=N" END-IF
           IF E2-IN-RANGE
              DISPLAY "V04=Y" ELSE DISPLAY "V04=N" END-IF
           IF AE-IN-RANGE
              DISPLAY "V05=Y" ELSE DISPLAY "V05=N" END-IF
           STOP RUN.
