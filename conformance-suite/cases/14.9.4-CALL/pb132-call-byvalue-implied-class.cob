*>--- conformance-case
*> id: pb132-call-byvalue-implied-class
*> rules: [SR-14.9.4.3-22]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR22's OTHER arm: "identifier-4 OR ITS CORRESPONDING FORMAL PARAMETER is specified
      *> with a BY VALUE phrase" - the keyword-less argument's formal is BY VALUE (GR9 b), so the class
      *> screen applies to the alphanumeric argument even though no BY VALUE is written (kb/Work PB132).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB132N9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AX PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           CALL "S1" AS NESTED USING AX
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LV PIC 9(4) USAGE BINARY.
       PROCEDURE DIVISION USING BY VALUE LV.
       P.
           GOBACK.
       END PROGRAM S1.
       END PROGRAM PB132N9.
