*>--- conformance-case
*> id: pb165_gr12_forward
*> rules: [GR-14.9.4.4-12]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB165 - ISO 8.8.4.8.4 GR1c with 14.9.4.4 GR12: forwarding an OMITTED formal parameter as an
      *> argument. GR12 - "If a parameter for which the omitted-argument condition is true is referenced in a
      *> called program, EXCEPT AS AN ARGUMENT or in the omitted-argument condition, the EC-PROGRAM-ARG-OMITTED
      *> exception condition is set to exist" - so this reference form neither raises nor reads. GR1c -
      *> "the result of the OMITTED test is true ... if the argument corresponding to data-name-1 is itself a
      *> formal parameter for which the omitted-argument condition is true" - so the omission is TRANSITIVE,
      *> through any number of forwardings, and independently of the passing mode written on the forward.
      *> Two shapes, both measured WRONG before PB165 (each printed INNER-SEES-PRESENT):
      *>   C  the forward is BY CONTENT - the snapshot read the omitted formal's accessor;
      *>   G  the formal is a GROUP, i.e. NOT carrier-resident - the copy-in field was forwarded, and a fresh
      *>      carrier over a field always answers IsNull false.
      *> The already-landed shape (an elementary formal forwarded BY REFERENCE) is pb133_omitted_transitive.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165F.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB165FC" AS NESTED USING OMITTED
           CALL "PB165FG" AS NESTED USING OMITTED
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165FC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LC PIC 9(4).
       PROCEDURE DIVISION USING OPTIONAL LC.
       M1.
           CALL "PB165FC2" AS NESTED USING BY CONTENT LC
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165FC2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LC2 PIC 9(4).
       PROCEDURE DIVISION USING OPTIONAL LC2.
       M2.
           IF LC2 IS OMITTED
             DISPLAY "C-TRANSITIVE-OMITTED"
           ELSE
             DISPLAY "C-PRESENT"
           END-IF
           GOBACK.
       END PROGRAM PB165FC2.
       END PROGRAM PB165FC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165FG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 LG-A PIC X(3).
          05 LG-B PIC X(3).
       PROCEDURE DIVISION USING OPTIONAL LG.
       M3.
           CALL "PB165FG2" AS NESTED USING LG
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165FG2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LH.
          05 LH-A PIC X(3).
          05 LH-B PIC X(3).
       PROCEDURE DIVISION USING OPTIONAL LH.
       M4.
           IF LH IS OMITTED
             DISPLAY "G-TRANSITIVE-OMITTED"
           ELSE
             DISPLAY "G-PRESENT"
           END-IF
           GOBACK.
       END PROGRAM PB165FG2.
       END PROGRAM PB165FG.
       END PROGRAM PB165F.
