      *>--- conformance-case
      *> id: l1_set_format1_once_and_itemid
      *> rules: [GR-14.9.39.4-2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.39.4 GR2 (SET statement, format 1, the rule head) —
      *> "The value of the sending operand is determined once at the
      *> beginning of the execution of the statement. However, item
      *> identification of the data item referenced by identifier-1 is
      *> done immediately before the value of that data item is
      *> changed."  Both sentences are pinned, one per output line.
      *>
      *> LINE 1 (ONCE) pins the FIRST sentence.  `SET IX IY TO T(IX)+1`
      *> is a legal format 1: every receiver is an index-name-1, so
      *> §14.9.39.3 SR3 (no arithmetic-expression-1 when identifier-1
      *> is of class index) and SR4 (a numeric identifier-1 requires
      *> index-name-2) cannot bite.  IX refers to occurrence 1 and
      *> T(1) = 3, so arithmetic-expression-1 evaluates to 4 — ONCE.
      *> GR2 a)1.c then sets EACH index-name to the element whose
      *> occurrence number is that expression, so IX = IY = 4; GR2 c)
      *> copies an index-name-2's occurrence number into a numeric
      *> identifier-1, so N = 4 and M = 4.
      *> DISCRIMINATOR: a per-receiver re-evaluation of the sender
      *> would read T(IX) again AFTER IX had become 4, and T(4) is
      *> seeded to 8 for exactly that reason — it would print M=9.
      *> Every value stays inside OCCURS 9, so §14.9.39.4 GR2 a)1.b's
      *> EC-RANGE-INDEX leg is not reached on either reading and the
      *> two readings differ only in the printed number.
      *>
      *> LINE 2 (ITEMID) pins the SECOND sentence.  `SET N T(N) TO IX`
      *> has index-name-2 as the sending operand (SR4 satisfied) and
      *> both receivers are integer data items (SR1).  IX refers to
      *> occurrence 2, so GR2 c) stores 2 into each receiver.  Item
      *> identification of the SECOND receiver, T(N), is done
      *> immediately before ITS value is changed — by which time the
      *> first receiver has already made N = 2 — so the element
      *> written is T(2).  Expected: N=2, T(1)=0, T(2)=2, T(3)=0.
      *> DISCRIMINATOR: hoisting the receivers' subscripts to the
      *> start of the statement would have written T(1) and printed
      *> T=200.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SETG2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 T PIC 9 OCCURS 9 INDEXED BY IX IY.
       01 N PIC 9 VALUE 1.
       01 M PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 3 TO T(1)
           MOVE 8 TO T(4)
           SET IX TO 1
           SET IX IY TO T(IX) + 1
           SET N TO IX
           SET M TO IY
           DISPLAY "ONCE N=" N " M=" M
           MOVE 0 TO T(1)
           MOVE 0 TO T(2)
           MOVE 0 TO T(3)
           MOVE 1 TO N
           SET IX TO 2
           SET N T(N) TO IX
           DISPLAY "ITEMID N=" N " T=" T(1) T(2) T(3)
           STOP RUN.
