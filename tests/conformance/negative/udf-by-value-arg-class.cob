      *> reject-at: 2002 2014 2023
      *> ISO 8.4.3.2.3 SR10 - when the formal parameter corresponding to argument-1 is specified
      *> with a BY VALUE phrase, argument-1 shall be of class numeric, object, or pointer: an
      *> alphanumeric literal argument is rejected (COBOLNET1554).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. BVF-P10UV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-V PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE L-V RETURNING L-R.
       P.
           MOVE L-V TO L-R.
           GOBACK.
       END FUNCTION BVF-P10UV.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BVAC-P10UV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION BVF-P10UV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-R = FUNCTION BVF-P10UV("AB").
           STOP RUN.
       END PROGRAM BVAC-P10UV.
