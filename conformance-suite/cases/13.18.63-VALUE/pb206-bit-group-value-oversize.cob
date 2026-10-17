*>--- conformance-case
*> id: pb206-bit-group-value-oversize
*> rules: [SR-13.18.63.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR10, sentence 3: "Boolean literals in the VALUE clause of a bit group item shall not
      *> exceed the size of the group item."  GI carries GROUP-USAGE BIT, so 8.5.2.1 gives it "class and
      *> category boolean" and 13.18.29.4 GR1b treats it as though it were PICTURE 1(m) with m its bit extent -
      *> I1's two boolean positions plus I2's two, laid out by the 8.5.1.6.3 walk.  B"101010" is six.
      *> ⛔ THE THIRD ARM, AND IT WAS UNREACHABLE.  kb/Work PB207 stages a bit-packed group's VALUE loud
      *> (COBOLNET0899 - the 13.18.63.4 GR5 area deposit over boolean positions is not implemented), and that
      *> refusal used to run FIRST and skip the syntax rules, so this program was told the AREA RULE is
      *> unimplemented rather than that its literal is illegal.  A refusal is what is left when the source is
      *> conforming and we cannot compile it, never the first thing said about source that is not - so the
      *> refusal now runs LAST, over entries the syntax rules accepted.  THIS FIXTURE IS THE PROOF: it must
      *> report COBOLNET0898 and not COBOLNET0899.  (The 0899 stage itself is still live - a CONFORMING bit
      *> group VALUE reaches it; that stays PB207's to lift.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB206N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GI GROUP-USAGE BIT VALUE B"101010".
          05 I1 PIC 1(2).
          05 I2 PIC 1(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X"
           STOP RUN.
