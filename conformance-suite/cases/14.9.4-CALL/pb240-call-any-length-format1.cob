*>--- conformance-case
*> id: pb240-call-any-length-format1
*> rules: [SR-14.9.4.3-11]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB240 - ISO 14.9.4.3 SR11 (FORMAT 1): "Identifier-2 and
      *> identifier-3 shall not be described with the ANY LENGTH clause."
      *> The contained program forwards its own ANY LENGTH formal through a
      *> Format-1 CALL (no AS phrase) - COBOLNET1542, whatever the mode.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB240NF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "ABCDEF".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB240NFI" AS NESTED USING X
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB240NFI.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       PROCEDURE DIVISION USING BY REFERENCE L.
       MAIN.
           CALL "PB240NFX" USING BY REFERENCE L
           GOBACK.
       END PROGRAM PB240NFI.
       END PROGRAM PB240NF.
