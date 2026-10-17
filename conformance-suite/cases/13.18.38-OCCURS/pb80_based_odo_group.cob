*>--- conformance-case
*> id: pb80_based_odo_group
*> rules: [SR-13.18.38.3-17, GR-13.18.38.4-8, GR-14.9.3.4-3, SR-14.9.39.3-19, RV-15.14.4-2, RV-15.50.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB80. ISO §13.18.38.4 GR8: when a group containing an
      *> occurs-depending table is a sending or receiving operand and
      *> data-name-1 is OUTSIDE the group, "only that part of the table area
      *> that is specified by the value of the data item referenced by
      *> data-name-1 at the start of the operation will be used" (GR8a); the
      *> rule does not care how the group is stored. A BASED record
      *> (§13.18.5) is a string-canonical class here, and its ODO group was
      *> never wrapped as an ODO operand: `MOVE BODO TO OUT` copied the MAXIMUM
      *> image (ABcccdddeee), a MOVE into it wrote past data-name-1's extent,
      *> and FUNCTION LENGTH(BODO) was a run-time stage. §15.50.4 r4a: LENGTH of
      *> "a based entry not associated with actual data" is the receiving-item
      *> (maximum) length — 2 + 5 × 3 = 17; r4b: an associated one is
      *> data-name-1's current extent — 2 + 2 × 3 = 8. The WORKING-STORAGE twin
      *> WODO is the control on every row.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB80BASEDODO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NN PIC 9 VALUE 2.
       01 WODO.
          05 WF1 PIC X(2) VALUE "AB".
          05 WT OCCURS 1 TO 5 DEPENDING ON NN PIC X(3).
       01 BODO BASED.
          05 BF1 PIC X(2).
          05 BT OCCURS 1 TO 5 DEPENDING ON NN PIC X(3).
       01 OUT PIC X(20).
       01 L PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE L = FUNCTION LENGTH(BODO).
           DISPLAY "T0 LENGTH unassociated=" L.
           ALLOCATE BODO.
           MOVE "AB" TO BF1.
           MOVE "ccc" TO WT(1) BT(1).
           MOVE "ddd" TO WT(2) BT(2).
           MOVE "eee" TO WT(3) BT(3).
           MOVE ALL "-" TO OUT.
           MOVE WODO TO OUT.
           DISPLAY "T1 W sends [" OUT "]".
           MOVE ALL "-" TO OUT.
           MOVE BODO TO OUT.
           DISPLAY "T2 B sends [" OUT "]".
           COMPUTE L = FUNCTION LENGTH(BODO).
           DISPLAY "T3 LENGTH associated=" L.
           COMPUTE L = FUNCTION LENGTH(WODO).
           DISPLAY "T4 LENGTH W=" L.
           MOVE "XYZ12345678" TO BODO.
           DISPLAY "T5 B receives [" BF1 BT(1) BT(2) BT(3) "]".
           IF BODO = "XYZ12345" DISPLAY "T6 B compares its current part"
              ELSE DISPLAY "T6 WRONG" END-IF.
           DISPLAY "T7 B(2:4)=[" BODO(2:4) "]".
           MOVE 3 TO NN.
           MOVE ALL "-" TO OUT.
           MOVE BODO TO OUT.
           DISPLAY "T8 B sends 3 [" OUT "]".
           SET ADDRESS OF BODO TO NULL.
           COMPUTE L = FUNCTION LENGTH(BODO).
           DISPLAY "T9 LENGTH after SET NULL=" L.
           STOP RUN.
