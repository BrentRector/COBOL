*>--- conformance-case
*> id: pb133_omitted_transitive
*> rules: [GR-14.9.4.4-11, GR-14.9.4.4-12]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB133 wave C - ISO 8.8.4.8 GR1c: the omitted-argument condition is true when "the
      *> argument corresponding to data-name-1 is itself a formal parameter for which the omitted-argument
      *> condition is true". S1 forwards its omitted OPTIONAL formal to S2 - the carrier-resident formal
      *> forwards its CARRIER, so presence rides with it (and 14.9.4.4 GR12's as-an-argument exception
      *> holds: the forwarding reference raises nothing). The present formal forwards its aliased value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OM7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC 9(4) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           CALL "S1" AS NESTED USING W-A OMITTED
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-A PIC 9(4).
       01 L-B PIC 9(4).
       PROCEDURE DIVISION USING L-A OPTIONAL L-B.
       P.
           CALL "S2" AS NESTED USING L-A L-B
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 M-A PIC 9(4).
       01 M-B PIC 9(4).
       PROCEDURE DIVISION USING M-A OPTIONAL M-B.
       Q.
           DISPLAY "A2=" M-A
           IF M-B IS OMITTED DISPLAY "B2-TRANSITIVE-OMITTED" END-IF
           GOBACK.
       END PROGRAM S2.
       END PROGRAM S1.
       END PROGRAM OM7.
