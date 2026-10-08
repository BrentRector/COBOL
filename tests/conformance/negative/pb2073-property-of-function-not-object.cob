      *> reject-at: 2002 2014 2023
      *> kb/Work PB2073 - 8.4.3.1.2 Format 7 names an object property as property-name-1 OF identifier-3, and
      *>   8.4.3.9.3 SR2 makes identifier-3 an object reference ("Identifier-1 shall be an object reference"). The
      *>   function PB2073NF returns PIC 9, so its temporary (8.4.3.2.4 GR1: the description of the RETURNING item) is
      *>   no object reference and BAL OF FUNCTION PB2073NF (1) is no object property: the resolver's probe, which now
      *>   reads a function's RETURNING description, must not take it for one, and the reference is refused under SR2
      *>   (COBOLNET2918) however 14.9.4.3 SR20's omission of BY CONTENT would have routed it.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB2073NF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-SEED PIC 9.
       01 L-RES PIC 9.
       PROCEDURE DIVISION USING L-SEED RETURNING L-RES.
           MOVE L-SEED TO L-RES
           GOBACK.
       END FUNCTION PB2073NF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2073NP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB2073NF
           PROPERTY BAL.
       PROCEDURE DIVISION.
           CALL "PB2073NS" AS NESTED USING BAL OF FUNCTION PB2073NF (1)
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2073NS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB PIC 9(5).
       PROCEDURE DIVISION USING BY REFERENCE LB.
           GOBACK.
       END PROGRAM PB2073NS.
       END PROGRAM PB2073NP.
