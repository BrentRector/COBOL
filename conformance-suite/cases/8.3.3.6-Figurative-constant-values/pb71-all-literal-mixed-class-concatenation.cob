*>--- conformance-case
*> id: pb71-all-literal-mixed-class-concatenation
*> rules: [SR-8.3.3.6.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.3.3.6.3 SR2 lets literal-1 be a concatenation expression, and 8.8.3.2 SR1 requires the operands of
      *> a concatenation to be of the same class - an alphanumeric "AB" and a national N"Q" are not. kb/Work
      *> PB71: COBOLNET1540 (the concatenation class-mismatch code) at bind.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB71NMIXED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AR PIC X(4).
       PROCEDURE DIVISION.
           MOVE ALL "AB" & N"Q" TO AR.
           STOP RUN.
