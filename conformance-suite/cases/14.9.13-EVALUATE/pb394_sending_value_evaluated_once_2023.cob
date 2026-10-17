*>--- conformance-case
*> id: pb394_sending_value_evaluated_once_2023
*> rules: [GR-14.9.13.4-3, GR-14.9.13.4-4, GR-14.9.25.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB394 - THE SENDING VALUE IS EVALUATED ONCE. Two verbs, one rule, and every expected line
      *> below is derived from the rule text, never from a run.
      *>
      *> ISO 14.9.25.4 GR1 (MOVE) - "If identifier-1 is reference-modified, subscripted, or is a
      *>   function-identifier, the reference modifier, subscript, or function-identifier is evaluated only once,
      *>   immediately before data is moved to the first of the receiving operands", and the rule writes the
      *>   required shape out as an equivalence: "The result of the statement / MOVE a (b) TO b, c (b) / is
      *>   equivalent to: / MOVE a (b) TO temp / MOVE temp TO b / MOVE temp to c (b) / where 'temp' is an
      *>   intermediate result item provided by the implementor."
      *> ISO 14.9.25.4 GR1 also - "Item identification for identifier-2 is performed immediately before the data
      *>   is moved to the respective data item" (the RECEIVER half, which must NOT be frozen).
      *> ISO 14.9.13.4 GR3 (EVALUATE) - "At the beginning of the execution of the EVALUATE statement, each
      *>   selection subject is evaluated and assigned a value, a range of values, or a truth value."
      *> ISO 14.9.13.4 GR4 a) 5. - a range object pairs the ONE subject value against BOTH bounds:
      *>   "selection-subject >= left-part AND selection-subject <= right-part".
      *> ISO 15.75.3 r3 / 15.75.4 r2 - "If a subsequent reference specifies argument-1, a new sequence of
      *>   pseudo-random numbers is started" and "For a given seed value on a given implementation, the sequence
      *>   of pseudo-random numbers will always be the same". The function cases below therefore compare a
      *>   RE-SEEDED run against a control instead of asserting any particular pseudo-random value: the golden
      *>   pins the RULE, not this implementation's sequence.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC:
      *>
      *> GR1-EX-B / GR1-EX-C5   The standard's own example with A(1..5) = 1,2,5,4,9 and B = 3.
      *>                        temp = A(3) = 5; MOVE temp TO B makes B = 5; MOVE temp TO C(B) identifies C(5)
      *>                        with the NEW B and stores the TEMP, so C(5) = 5 => B=5, C5=00005.
      *>                        (Re-reading A(B) after the first store gives A(5) = 9 - the defect.)
      *> GR1-RECV-K / -T3       The receiver half, which stays as it is: MOVE 3 TO K, T(K) with K = 1 stores
      *>                        K = 3 and then identifies T(K) with the NEW K, so T(3) = 3.
      *> GR1-OVER-G / -R        W-G = "ABCDEF"; MOVE W-G TO G2, W-R1. temp holds "ABCDEF"; the first store puts
      *>                        the temp's leading three characters in G2, making W-G "ABCABC"; the second store
      *>                        moves the TEMP, so W-R1 = "ABCDEF". The sending operand overlaps a receiver -
      *>                        the equivalence is what makes the second receiver's value independent of the
      *>                        first store.
      *> GR1-REFMOD-P / -R      W-S = "123456", W-P = 1; MOVE W-S(W-P:2) TO W-P, W-RM2. The reference modifier
      *>                        is evaluated once, so temp = "12"; MOVE temp TO W-P treats the alphanumeric
      *>                        sender as an unsigned integer (14.9.25.3 Table 16) and the one-digit receiver
      *>                        keeps the low-order digit => W-P = 2; MOVE temp TO W-RM2 = "12". Re-evaluating
      *>                        the modifier would read W-S(2:2) = "23".
      *> GR1-FN                 MOVE FUNCTION RANDOM TO F1, F2 - "the function-identifier is evaluated only
      *>                        once", so the two receivers hold the SAME value: SAME.
      *> GR1-ODO-J1 / -J2       THE INTERMEDIATE MUST NOT CHANGE THE SENDER'S LENGTH. ODO-E(1..5) = 1,2,5,4,9
      *>                        with N = 3, so 13.18.38.4 GR8 a) - data-name-1 is outside the group, so "only
      *>                        that part of the table area that is specified by the value of the data item
      *>                        referenced by data-name-1 at the start of the operation will be used" - makes the
      *>                        sending operand the three character positions "125". 13.18.34.4 GR1 right-aligns
      *>                        a shorter sender in a JUSTIFIED receiver with space fill on the left, so a PIC
      *>                        X(5) JUSTIFIED receiver holds "  125" - and 14.9.25.4 GR1's equivalence is a
      *>                        result equivalence, so the ONE-receiver and the TWO-receiver statements must
      *>                        agree: J1 = J2 = "  125". An intermediate result item of the group's MAXIMUM
      *>                        extent would give the second one "125  ".
      *> GR1-ODO-N / -J3        THE DISCRIMINATING CASE: data-name-1 IS a receiving operand. With N = 3 the
      *>                        sending operand is "125" (three positions, 13.18.38.4 GR8 a). The receivers are
      *>                        stored "in the order specified" (14.9.25.4 GR1): N first - a group sender makes
      *>                        this GR4's "alphanumeric to alphanumeric elementary move", so the one-character
      *>                        receiving area takes the leading character and N = 1 - and J3 second. "The
      *>                        length of the data item referenced by identifier-1 is evaluated only once,
      *>                        immediately before the data is moved to the first of the receiving operands",
      *>                        and "the evaluation of the length of identifier-1 or identifier-2 may be
      *>                        affected by the DEPENDING ON phrase of the OCCURS clause" - so the second store
      *>                        still sends three positions, right-aligned by 13.18.34.4 GR1: J3 = "  125".
      *>                        Re-reading the length after the first store sends ONE position, "    1".
      *> GR3-BRANCH             The same seeded sequence drives a DATA-ITEM subject and a FUNCTION subject over
      *>                        identical WHEN phrases. GR3 assigns the subject one value at the beginning of
      *>                        the statement, so both must select the same WHEN phrase: SAME.
      *> GR3-COUNT              After each EVALUATE the next number of the sequence is read. The function
      *>                        subject is evaluated ONCE, so exactly one number is consumed by the statement
      *>                        and the two reads are equal: ONE. (Re-evaluating the subject per relational use
      *>                        consumes one per WHEN and two per THRU arm.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB394SEND23.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A.
          05 A            PIC 9 OCCURS 5 TIMES.
       01 W-C.
          05 C            PIC 9(5) OCCURS 5 TIMES.
       01 B              PIC 9.
       01 W-T.
          05 T            PIC 9(3) OCCURS 5 TIMES.
       01 K              PIC 9.
       01 W-G.
          05 G1           PIC X(3).
          05 G2           PIC X(3).
       01 W-R1           PIC X(6).
       01 W-S            PIC X(6) VALUE "123456".
       01 W-P            PIC 9 VALUE 1.
       01 W-RM2          PIC X(2).
       01 F0             PIC 9V9(6).
       01 F1             PIC 9V9(6).
       01 F2             PIC 9V9(6).
       01 E-SUBJ         PIC 9V9(6).
       01 E-CTRL-BR      PIC X(5).
       01 E-TEST-BR      PIC X(5).
       01 E-CTRL-NEXT    PIC 9V9(6).
       01 E-TEST-NEXT    PIC 9V9(6).
       01 N              PIC 9 VALUE 3.
       01 ODO-G.
          05 ODO-E        PIC 9 OCCURS 1 TO 5 TIMES DEPENDING ON N.
       01 Z              PIC X(5) VALUE SPACES.
       01 J1             PIC X(5) JUSTIFIED RIGHT.
       01 J2             PIC X(5) JUSTIFIED RIGHT.
       01 J3             PIC X(5) JUSTIFIED RIGHT.
       PROCEDURE DIVISION.
           MOVE 1 TO A(1)
           MOVE 2 TO A(2)
           MOVE 5 TO A(3)
           MOVE 4 TO A(4)
           MOVE 9 TO A(5)
           MOVE 3 TO B
           MOVE A(B) TO B, C(B)
           DISPLAY "GR1-EX-B=" B
           DISPLAY "GR1-EX-C5=" C(5)

           MOVE 1 TO K
           MOVE 3 TO K, T(K)
           DISPLAY "GR1-RECV-K=" K
           DISPLAY "GR1-RECV-T3=" T(3)

           MOVE "ABCDEF" TO W-G
           MOVE W-G TO G2, W-R1
           DISPLAY "GR1-OVER-G=" W-G
           DISPLAY "GR1-OVER-R=" W-R1

           MOVE W-S(W-P:2) TO W-P, W-RM2
           DISPLAY "GR1-REFMOD-P=" W-P
           DISPLAY "GR1-REFMOD-R=" W-RM2

           MOVE FUNCTION RANDOM(1) TO F0
           MOVE FUNCTION RANDOM TO F1, F2
           IF F1 = F2
              DISPLAY "GR1-FN=SAME"
           ELSE
              DISPLAY "GR1-FN=DIFFERENT"
           END-IF

           MOVE 5 TO N
           MOVE 1 TO ODO-E(1)
           MOVE 2 TO ODO-E(2)
           MOVE 5 TO ODO-E(3)
           MOVE 4 TO ODO-E(4)
           MOVE 9 TO ODO-E(5)
           MOVE 3 TO N
           MOVE ODO-G TO J1
           MOVE ODO-G TO J2, Z
           DISPLAY "GR1-ODO-J1=[" J1 "]"
           DISPLAY "GR1-ODO-J2=[" J2 "]"

           MOVE 3 TO N
           MOVE ODO-G TO N, J3
           DISPLAY "GR1-ODO-N=" N
           DISPLAY "GR1-ODO-J3=[" J3 "]"

           MOVE FUNCTION RANDOM(1) TO F0
           MOVE FUNCTION RANDOM TO E-SUBJ
           EVALUATE E-SUBJ
              WHEN 0 THRU 0.25
                 MOVE "BAND1" TO E-CTRL-BR
              WHEN 0.25 THRU 0.5
                 MOVE "BAND2" TO E-CTRL-BR
              WHEN OTHER
                 MOVE "BAND3" TO E-CTRL-BR
           END-EVALUATE
           MOVE FUNCTION RANDOM TO E-CTRL-NEXT

           MOVE FUNCTION RANDOM(1) TO F0
           EVALUATE FUNCTION RANDOM
              WHEN 0 THRU 0.25
                 MOVE "BAND1" TO E-TEST-BR
              WHEN 0.25 THRU 0.5
                 MOVE "BAND2" TO E-TEST-BR
              WHEN OTHER
                 MOVE "BAND3" TO E-TEST-BR
           END-EVALUATE
           MOVE FUNCTION RANDOM TO E-TEST-NEXT

           IF E-TEST-BR = E-CTRL-BR
              DISPLAY "GR3-BRANCH=SAME"
           ELSE
              DISPLAY "GR3-BRANCH=DIFFERENT"
           END-IF
           IF E-TEST-NEXT = E-CTRL-NEXT
              DISPLAY "GR3-COUNT=ONE"
           ELSE
              DISPLAY "GR3-COUNT=MANY"
           END-IF
           STOP RUN.
