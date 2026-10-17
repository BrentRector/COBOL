*>--- conformance-case
*> id: pb70_group_reference_modification
*> rules: [SR-8.4.3.3.3-1, GR-8.4.3.3.4-2, GR-8.4.3.3.4-6, GR-13.18.38.4-8]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB70. ISO §8.4.3.3.3 SR1 admits as identifier-1 of a reference
      *> modification "an alphanumeric group item" and "a group item that is
      *> neither a strongly-typed group nor a variable-length group";
      *> §8.4.3.3.4 GR6 makes the unique data item an ELEMENTARY item without
      *> the JUSTIFIED clause, of class and category alphanumeric for a group.
      *> Before this the resolver's category gate returned null for every group:
      *> a SENDING group ref-mod (MOVE source, relation, DISPLAY, ORD/LENGTH
      *> argument) died at run time with NotImplemented, and a RECEIVING one
      *> (`MOVE "Z" TO TB(2:1)`) was a SILENT no-op — the receiver was dropped
      *> from the list. Expected values are the positional character semantics:
      *> the group's positions are its leaves' images in declaration order
      *> (USAGE §13.18.60.4 GR4 / MOVE §14.9.25.4 GR4 — no conversion), a slice is a
      *> substring of that image, and a store splices the slice back.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB70GRPREFMOD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 EL PIC X OCCURS 3.
       01 GP.
          05 GA PIC X(2) VALUE "AB".
          05 GB PIC 9(3) VALUE 123.
          05 GC PIC X(2) VALUE "YZ".
       01 OG.
          05 OC PIC 9 VALUE 2.
          05 OE PIC X OCCURS 1 TO 5 DEPENDING ON OC.
       01 OK1 PIC X.
       01 OK2 PIC X.
       01 R  PIC X(4).
       01 N  PIC 9(4).
       01 CNT PIC 99.
       PROCEDURE DIVISION.
      *> 1 — the note's repro: a group ref-mod as a MOVE source (§8.4.3.3.3 SR1 bullet 3).
           MOVE "ABC" TO TB.
           MOVE TB(1:1) TO EL(3).
           DISPLAY "T1 [" TB "]".
      *> 2 — the receiving side, in a multi-receiver MOVE: every receiver is written.
           MOVE "Z" TO OK1 TB(2:1) OK2.
           DISPLAY "T2 [" OK1 "][" TB "][" OK2 "]".
      *> 3-4 — a relation operand and a DISPLAY operand over a group with a numeric leaf
      *> (the leaf's image is its zoned digits — §8.4.3.3.4 GR2 posture over the group).
           IF GP(2:2) = "B1" DISPLAY "T3 EQ" ELSE DISPLAY "T3 NE [" GP(2:2) "]" END-IF.
           DISPLAY "T4 [" GP(3:3) "]".
      *> 5-6 — function arguments: ORD (§15.70 — 'A' is ordinal 66) and LENGTH (§15.50 — the slice's positions).
           COMPUTE N = FUNCTION ORD(GP(1:1)) - 1.
           DISPLAY "T5 ORD-1=" N.
           COMPUTE N = FUNCTION LENGTH(GP(2:4)).
           DISPLAY "T6 LEN=" N.
      *> 7-8 — receiving stores that cross leaf boundaries: the splice lands positionally, no conversion (GR4).
           MOVE "PQ" TO GP(6:2).
           DISPLAY "T7 [" GP "] GC=[" GC "]".
           MOVE "12" TO GP(3:3).
           DISPLAY "T8 [" GP "] GB=[" GB "]".
      *> 9-10 — INSPECT identifier-1 as a group slice (§14.9.22.3 SR1; the ref-mod is an alphanumeric item).
           INSPECT GP(1:2) REPLACING ALL "A" BY "a".
           DISPLAY "T9 [" GP "]".
           INSPECT GP(1:7) TALLYING CNT FOR ALL "2".
           DISPLAY "T10 CNT=" CNT.
      *> 11-12 — a MOVE source into a shorter/longer receiver; an UNSTRING sending operand (§14.9.48.3 SR2 —
      *> category alphanumeric, which the GR6 unique item is).
           MOVE ALL "-" TO R.
           MOVE GP(2:3) TO R.
           DISPLAY "T11 R=[" R "]".
           UNSTRING GP(1:5) DELIMITED BY "1" INTO R.
           DISPLAY "T12 R=[" R "]".
      *> 13-14 — a figurative into a group slice; an UNSTRING receiver (§14.9.48.4 GR11 — the MOVE rules).
           MOVE SPACES TO GP(1:2).
           DISPLAY "T13 [" GP "]".
           MOVE "uv" TO R.
           UNSTRING R DELIMITED BY SPACE INTO GP(6:2).
           DISPLAY "T14 [" GP "]".
      *> 15-18 — a group slice as a function argument's operand and a MOVE receiver, a class condition
      *> (§8.8.4.4 — "8V" is not alphabetic), and an EVALUATE subject.
           MOVE "260818" TO GP(1:6).
           MOVE FUNCTION UPPER-CASE(GP(6:2)) TO GP(1:2).
           DISPLAY "T16 [" GP "]".
           IF GP(1:2) IS ALPHABETIC DISPLAY "T17 ALPHABETIC"
              ELSE DISPLAY "T17 NOT ALPHABETIC" END-IF.
           EVALUATE GP(1:2)
              WHEN "UV" DISPLAY "T18 WHEN UV"
              WHEN OTHER DISPLAY "T18 OTHER"
           END-EVALUATE.
      *> 19-20 — an occurs-depending group (a FIXED-length group per §8.5.1.12 — SR1 admits it): the
      *> operand is its CURRENT-count part (§13.18.38 GR8, three positions here), so a store past it
      *> lands only in the current positions and a slice inside it reads/writes normally.
           MOVE "12345" TO OG(2:5).
           DISPLAY "T19 [" OG "]".
           MOVE "1" TO OG(3:1).
           DISPLAY "T20 [" OG(1:3) "]".
           STOP RUN.
