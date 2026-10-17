*>--- conformance-case
*> id: l1-cos-class-numeric
*> rules: [AR-15.20.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.20.3 r1 — "Argument-1 shall be of class numeric." COS's OWN argument rule, which
      *> the corpus had never asserted for COS.
      *>
      *> --check validated:
      *>   cite.py --check 15.20.3 "Argument-1 shall be of class numeric." -> OK §15.20.3 1)
      *> §8.5.2.1 Table 2 puts an alphanumeric PICTURE item in class ALPHANUMERIC, so PIC X(3) is
      *> not of class numeric and this reference is illegal source; §4.2.2 obliges the indication.
      *> The diagnostic is COBOLNET1627 ("intrinsic-argument-class", docs/DIAGNOSTICS.md).
      *>
      *> ⛔ IT LOOKED COVERED AND WAS NOT - twice over, and each neighbouring fixture says so in
      *> its own header. negative/pb1-numeric-arg-trig-family is NAMED for the trig family but its
      *> program calls only the three INVERSE functions ACOS, ASIN and ATAN. And
      *> negative/pb52-intrinsic-argument-stage's header records that COS, SIN, TAN and SIGN
      *> joined IntrinsicArgumentRules.Verified in the same change as SQRT - while its PROGRAM
      *> calls SQRT alone. COS's rule therefore had a table row and no witness: the exact shape
      *> pb52's own comment warns about, one function further along.
      *>
      *> ⚠ EDITION 85 IS IN THE REJECT SET, DELIBERATELY. COS carries IntroducedIn 85 in
      *> IntrinsicCatalog - IntrinsicBinder's D8 comment records that the 42 rows of the 1989
      *> Intrinsic Function Module amendment are part of the 85 dialect here - so at --std 85 the
      *> reference reaches the class screen rather than the COBOLNET1502 edition window, and the
      *> same COBOLNET1627 must fire. (pb1-numeric-arg-trig-family's header asserts the opposite
      *> for ACOS/ASIN/ATAN, "post-85 intrinsics", and omits 85 from its own reject set; that
      *> claim contradicts the catalog and leaves those three unwitnessed at 85.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1COSCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC X(3) VALUE "abc".
       01 W-R PIC S9(6)V99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W-R = FUNCTION COS(W-A).
           STOP RUN.
