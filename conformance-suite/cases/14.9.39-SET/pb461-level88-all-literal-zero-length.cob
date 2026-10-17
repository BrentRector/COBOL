*>--- conformance-case
*> id: pb461-level88-all-literal-zero-length
*> rules: [GR-14.9.39.4-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.3.3.6.3 SR2: literal-1 of the figurative ALL literal-1 "shall be neither a figurative constant nor
      *> a zero-length literal". kb/Work PB461: the sibling arm of pb71-all-literal-zero-length, which pins the
      *> MOVE sender. The level-88 VALUE operand is now parted by the SAME one classifier
      *> (FigurativeConstants.Classify) as every other reader of the form, so the arm that recognizes Format 6
      *> and the arm that refuses an illegal literal-1 have to stay in agreement HERE too: recognizing the form
      *> is not accepting it. Rejected at every edition - SR2 has no edition gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB461NZERO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AR PIC X(4) VALUE "ZZZZ".
          88 AR-EMPTY VALUE ALL "".
       PROCEDURE DIVISION.
           SET AR-EMPTY TO TRUE.
           STOP RUN.
