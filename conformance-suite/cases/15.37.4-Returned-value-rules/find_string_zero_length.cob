*>--- conformance-case
*> id: find_string_zero_length
*> rules: [RV-15.37.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.37.4 rule 5 — "If argument-1 or argument-2 is of zero length, the function shall
      *> return zero." — plus the degenerate reach of rule 3 ("If no match is found, the function
      *> shall return zero") where argument-2 is LONGER than argument-1 and the scan has no position
      *> to start from.
      *>
      *> A zero-length operand is legal here and its result is DEFINED, not diagnosed: §8.3.3.1 makes
      *> `""` a zero-length literal ("If the opening and closing delimiters are contiguous, the length
      *> of the literal is zero, and it is known as a zero-length literal"), §15.37.3 states no
      *> prohibition on one — unlike §15.59.3 r3, §15.63.3 r3, §15.66.3 r3, §15.71.3 r2 and §15.72.3 r2,
      *> which each read "Argument-1 shall not be a zero-length literal.", and §15.85.3 r4, which reads
      *> "Neither argument-1 nor argument-2 shall be a zero-length literal." — and r5 gives the answer.
      *> So every Z line below must COMPILE and return zero.
      *>
      *> r5's subject is "argument-1 or argument-2", which §15.37.3 r1 defines as "a data item or
      *> literal", and §8.5.4 (Zero-length items) lists nine ways to BE one — eight of them DATA
      *> ITEMS, whose length is a RUN-TIME fact rather than a compile-time constant. A file of
      *> zero-length LITERALS alone would leave that whole half unreached, so three species are
      *> written: the zero-length literal (§8.5.4 item 8 — Z1..Z6), a group containing only an
      *> occurs-depending table whose current number of occurrences is zero (item 1 — ZG1/ZG2, whose
      *> extent is set at run time by MOVE 0 TO ZN), and a reference-modified item that has resolved
      *> to a length of zero under >>REF-MOD-ZERO-LENGTH (item 9 — ZR1/ZR2).
      *> ZN is declared OUTSIDE ZG on purpose: that is the arm of §13.18.38.4 GR8 a) which states the
      *> consequence outright — "If the data item referenced by data-name-1 is outside the group, only
      *> that part of the table area that is specified by the value of the data item referenced by
      *> data-name-1 at the start of the operation will be used. If there are no elementary data items
      *> defined between the data description entry of the group data item and the definition of the
      *> table ... and the value ... at the start of the operation is zero, the group data item is a
      *> zero-length item." ZT is ZG's only subordinate, so nothing stands between them.
      *>
      *> r5 is unconditional: it names no phrase and no exception, so Z4 (LAST, START AFTER 1 and
      *> ANYCASE all written over a zero-length argument-2) is zero for the same reason as Z2.
      *> POPULATION GUARDS — a file whose every expected value is zero can pass for the wrong reason,
      *> so each species is paired with a reach that is NOT zero-length and must answer non-zero:
      *> CTRL / NCTRL use the same alphanumeric and national items over non-zero-length operands, and
      *> ZGCTRL is the SAME ODO group read at its full three-occurrence extent, so a run that read a
      *> stale or fixed extent instead of a zero one would answer 2 at ZG1 and 1 at ZG2, not 0.
       >>REF-MOD-ZERO-LENGTH ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FSZERO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 H  PIC X(9) VALUE "ABCABCABC".
       01 ND PIC X(3) VALUE "ABC".
       01 NH PIC N(9) VALUE N"ABCABCABC".
       01 NN PIC N(3) VALUE N"ABC".
       01 ZN PIC 9 VALUE 0.
       01 ZG.
          05 ZT OCCURS 0 TO 3 TIMES DEPENDING ON ZN PIC X.
       01 P  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
      *> The population guard — these are NOT zero-length, so §15.37.4 r1 applies: "BC" first at 2,
      *> and the national "ABC" first at 1.
           MOVE FUNCTION FIND-STRING(H "BC") TO P.
           DISPLAY "CTRL=" P.
           MOVE FUNCTION FIND-STRING(NH NN) TO P.
           DISPLAY "NCTRL=" P.
      *> r5, argument-1 of zero length.
           MOVE FUNCTION FIND-STRING("" "A") TO P.
           DISPLAY "Z1=" P.
      *> r5, argument-2 of zero length.
           MOVE FUNCTION FIND-STRING(H "") TO P.
           DISPLAY "Z2=" P.
      *> r5, BOTH of zero length — the rule's "or" is satisfied twice over.
           MOVE FUNCTION FIND-STRING("" "") TO P.
           DISPLAY "Z3=" P.
      *> r5 with every optional element of §15.37.2 written: the rule states no exception for LAST,
      *> argument-3 or ANYCASE, so the result is still zero.
           MOVE FUNCTION FIND-STRING(H "" LAST START AFTER 1 ANYCASE)
               TO P.
           DISPLAY "Z4=" P.
      *> r5 in the NATIONAL class: a zero-length national literal (§8.3.3.1 covers national literals
      *> in the same sentence) on each side in turn.
           MOVE FUNCTION FIND-STRING(NH N"") TO P.
           DISPLAY "Z5=" P.
           MOVE FUNCTION FIND-STRING(N"" NN) TO P.
           DISPLAY "Z6=" P.
      *> r5 over a zero-length DATA ITEM (§8.5.4 item 1: a group containing only an occurs-depending
      *> table with zero occurrences). ZGCTRL first reads the SAME group at its full extent — the
      *> three characters "ABC", in which "BC" starts at 2 — so the two zero answers below are known
      *> to come from the extent going to zero and not from the group never being read at all.
           MOVE 3 TO ZN.
           MOVE "A" TO ZT(1).
           MOVE "B" TO ZT(2).
           MOVE "C" TO ZT(3).
           MOVE FUNCTION FIND-STRING(ZG "BC") TO P.
           DISPLAY "ZGCTRL=" P.
           MOVE 0 TO ZN.
           MOVE FUNCTION FIND-STRING(ZG "BC") TO P.
           DISPLAY "ZG1=" P.
           MOVE FUNCTION FIND-STRING(H ZG) TO P.
           DISPLAY "ZG2=" P.
      *> r5 over the other zero-length DATA ITEM shape (§8.5.4 item 9: a reference-modified item
      *> resolved to length zero, permitted by the >>REF-MOD-ZERO-LENGTH directive above). If the
      *> slice were read as the whole item these would answer 1, so both discriminate.
           MOVE FUNCTION FIND-STRING(H(1:0) "A") TO P.
           DISPLAY "ZR1=" P.
           MOVE FUNCTION FIND-STRING(H H(1:0)) TO P.
           DISPLAY "ZR2=" P.
      *> r3, the degenerate no-match: argument-2 is 9 characters, argument-1 is 3, so no substring of
      *> argument-1 can equal argument-2 and no match is found.
           MOVE FUNCTION FIND-STRING(ND H) TO P.
           DISPLAY "NDLONG=" P.
           STOP RUN.
       END PROGRAM L1FSZERO.
