*>--- conformance-case
*> id: pb1631-call-by-value-all-zero
*> rules: [SR-8.3.3.6.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1631 - ISO 14.9.4.3 SR23: a BY VALUE literal-2 "shall be
      *> a numeric literal", and 8.3.3.6.3 SR1 a) admits only "ZERO (ZEROS,
      *> ZEROES) without the ALL phrase" where a literal is restricted to a
      *> numeric literal. ALL ZERO is therefore refused: COBOLNET1762. The
      *> former test asked only zeroWord(), which ALL ZERO also carries.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1631C.
       PROCEDURE DIVISION.
           CALL "NEG1631D" AS NESTED USING BY VALUE ALL ZERO
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1631D.
       DATA DIVISION.
       LINKAGE SECTION.
       01 N PIC 9(4) BINARY.
       PROCEDURE DIVISION USING BY VALUE N.
           GOBACK.
       END PROGRAM NEG1631D.
       END PROGRAM NEG1631C.
