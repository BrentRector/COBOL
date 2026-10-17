*>--- conformance-case
*> id: typedef_same_as
*> rules: [GR-13.18.49.4-1, FMT-13.18.49.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> SAME AS (ISO 13.18.49, COBOL-2002; P10 Step 16). The subject takes the SAME data description as
      *> data-name-1's entry, subordinates included (GR1/GR2), via the ONE ExpandTypes/CloneItem machinery:
      *> an ELEMENTARY copy (the VALUE clause rides the copy - GR1 excludes only level/name/CONSTANT RECORD/
      *> EXTERNAL/GLOBAL/REDEFINES/SELECT WHEN), a GROUP copy whose subordinates renumber relative to the
      *> subject (GR2b - qualified references prove the copied hierarchy), SAME AS + OCCURS (13.16.3 SR12 -
      *> a table of the copied description), and a copied STRONG type identity (P2 SAME AS P1 stays the same
      *> strong type, so the whole-record MOVE + relation are legal - 14.9.25.3 SR2 / 8.8.4.2.3 SR1; the
      *> unsigned-leaf ordering is the 8.8.4.2.12 element order).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SAME-AS-P10TS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PROTO-AMT PIC 9(3)V99 VALUE 1.5.
       01 W-COPY SAME AS PROTO-AMT.
       01 CUST-REC.
          05 CUST-ID   PIC 9(4).
          05 CUST-NAME PIC X(6).
       01 REC-A SAME AS CUST-REC.
       01 OUTER-REC.
          05 INNER-REC SAME AS CUST-REC.
          05 TAG PIC X.
       01 TBL.
          05 ROW-REC SAME AS CUST-REC OCCURS 2.
       01 POINT-T TYPEDEF STRONG.
          05 PX PIC 9(3).
          05 PY PIC 9(3).
       01 P1 TYPE POINT-T.
       01 P2 SAME AS P1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "W0=[" W-COPY "]".
           MOVE 2.25 TO W-COPY.
           DISPLAY "W1=[" W-COPY "]".
           MOVE 42 TO CUST-ID OF REC-A.
           MOVE "ALICE" TO CUST-NAME OF REC-A.
           DISPLAY "A=[" CUST-ID OF REC-A "][" CUST-NAME OF REC-A "]".
           MOVE 7 TO CUST-ID OF INNER-REC.
           MOVE "DEEP" TO CUST-NAME OF INNER-REC.
           MOVE "Z" TO TAG.
           DISPLAY "N=[" CUST-ID OF INNER-REC "][" CUST-NAME OF INNER-REC
               "][" TAG "]".
           MOVE 8 TO CUST-ID OF ROW-REC (1).
           MOVE "BOB" TO CUST-NAME OF ROW-REC (1).
           MOVE 9 TO CUST-ID OF ROW-REC (2).
           MOVE "CAROL" TO CUST-NAME OF ROW-REC (2).
           DISPLAY "T1=[" CUST-ID OF ROW-REC (1) "]["
               CUST-NAME OF ROW-REC (1) "]".
           DISPLAY "T2=[" CUST-ID OF ROW-REC (2) "]["
               CUST-NAME OF ROW-REC (2) "]".
           MOVE 10 TO PX OF P1.
           MOVE 20 TO PY OF P1.
           MOVE P1 TO P2.
           IF P1 = P2
               DISPLAY "SAME-EQ"
           END-IF.
           MOVE 99 TO PY OF P2.
           IF P1 < P2
               DISPLAY "SAME-LT"
           END-IF.
           STOP RUN.
