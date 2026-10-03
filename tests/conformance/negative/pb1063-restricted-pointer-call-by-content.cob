      *> reject-at: 2002 2014 2023
      *> kb/Work PB1063 -- the CALL lane of the same rule. ISO 14.8.2.3.3
      *> 2) makes a BY CONTENT argument into a pointer formal "the same as
      *> if a SET statement were performed" (14.9.4.3 SR25 imports it into
      *> a CALL ... AS NESTED). DP is restricted to REC-T and LP is not:
      *> 14.9.39.3 SR19 refuses the SET, so the CALL is COBOLNET1688.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1063C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC-T IS TYPEDEF STRONG.
          05 A PIC X(2).
       01 DPT IS TYPEDEF USAGE POINTER TO REC-T.
       01 W TYPE REC-T.
       01 DP TYPE DPT.
       PROCEDURE DIVISION.
           SET DP TO ADDRESS OF W
           CALL "N1063S" AS NESTED USING BY CONTENT DP
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1063S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE POINTER.
       PROCEDURE DIVISION USING LP.
           DISPLAY "IN-S"
           GOBACK.
       END PROGRAM N1063S.
       END PROGRAM N1063C.
