*>--- conformance-case
*> id: l1_move_numeric_sign_rules
*> rules: [GR-14.9.25.4-6, GR-14.9.25.4-9]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.25.4 GR6 d) 2. a. and b. MOVE statement — the SIGN rules for a numeric sending
      *> operand ("When the sending operand is numeric, or is the numeric value produced by
      *> de-editing").
      *>   python scripts/spec/cite.py --check 14.9.25.4 "When a signed numeric item is the
      *>   receiving item, the sign of the numeric value shall be represented in the receiving
      *>   operand."  ->  OK  §14.9.25.4 6) 1.  (General rules)
      *>   python scripts/spec/cite.py --check 14.9.25.4 "When an unsigned numeric item is the
      *>   receiving item, the absolute value of the sending value is used, and no operational sign
      *>   is generated for the receiving item."  ->  OK  §14.9.25.4 6) 1.  (General rules)
      *> (cite.py prints an APPROXIMATE rule path below two levels of nesting — the printed rule is
      *> 6) d) 2. a. / b., which is what the quoted text above pins.)
      *>
      *> ⛔ THE SIGN IS READ THROUGH AN EDITED ITEM, NEVER THROUGH A DISPLAY OF THE SIGNED ITEM.
      *> The representation of an operational sign on a USAGE DISPLAY item is an implementor
      *> determination (§13.18.52, SIGN clause — the overpunch that move_group_overpunch pins), so a
      *> DISPLAY of a signed item would measure that choice and not this rule. PIC -999 is the
      *> standard's own instrument: §13.18.40.5 rule 5, fixed insertion editing, Table 8 "Results of
      *> fixed insertion editing" gives the editing symbol '-' the result "space" for a positive or
      *> zero value and the minus character for a negative value.
      *>   python scripts/spec/cite.py --check 13.18.40.5 "Table 8, Results of fixed insertion
      *>   editing, shows the character(s) produced by an editing sign control symbol, depending on
      *>   the value of the data item."  ->  OK  §13.18.40.5 5)  (Editing rules)
      *> The §8.8.4.7 simple sign condition is the second, independent instrument: it "determines
      *> whether or not the algebraic value of an arithmetic expression is less than, greater than,
      *> or equal to zero", so it reads the stored VALUE rather than any rendering. Both are
      *> reported for each arm; a stored sign that failed to survive would break both.
      *>
      *> EXPECTED OUTPUT, derived line by line:
      *>   A-UNSIGNED-SENDER  rule a, second sentence: "If the sending operand is unsigned, the sign
      *>                      shall be positive." PIC 9(3) VALUE 456 -> PIC S9(3) is +456, and
      *>                      Table 8 renders a positive value's '-' position as a SPACE: [ 456].
      *>   A-POS             the same fact through the sign condition: T.
      *>   A-NEG-SENDER      rule a, first sentence: the sign of the numeric value (-123) shall be
      *>                      represented in the receiving operand, so PIC -999 shows [-123].
      *>   A-NEG             the same fact through the sign condition: T.
      *>   A-POS-SENDER      a SIGNED sender that is positive keeps its sign: +789 -> [ 789].
      *>   B-UNSIGNED-RCVR   rule b: PIC S9(3) VALUE -123 into PIC 9(3) uses the ABSOLUTE VALUE,
      *>                      so the unsigned receiver holds 123 and DISPLAYs [123].
      *>   B-EDITED          rule b's second half, "no operational sign is generated for the
      *>                      receiving item": re-edited through PIC -999 the value is positive, so
      *>                      Table 8 gives a space: [ 123]. A generated negative sign would show.
      *>   B-NEG             the sign condition on the unsigned receiver: F.
      *>   F-SIGNED /        the rule's own FLOAT arm. Its opening sentence is implementor latitude
      *>   F-UNSIGNED        ("the implementor specifies ... alignment of the data by decimal
      *>                      point"), so this fixture uses an INTEGRAL float value, -125, which is
      *>                      exactly representable in FLOAT-SHORT and whose alignment by decimal
      *>                      point is the same under any choice the implementor could make. What is
      *>                      pinned is only what rules a and b fix and latitude does not: the
      *>                      signed receiver shows [-125] and the unsigned receiver, by rule b's
      *>                      absolute value, shows [125].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1MVSIGN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-US   PIC 9(3)  VALUE 456.
       01 W-SN   PIC S9(3) VALUE -123.
       01 W-SP   PIC S9(3) VALUE +789.
       01 W-FL   USAGE FLOAT-SHORT.
       01 R-S1   PIC S9(3).
       01 R-S2   PIC S9(3).
       01 R-S3   PIC S9(3).
       01 R-SF   PIC S9(3).
       01 R-U1   PIC 9(3).
       01 R-UF   PIC 9(3).
       01 W-ED   PIC -999.
       PROCEDURE DIVISION.
       MAIN.
           MOVE W-US TO R-S1
           MOVE R-S1 TO W-ED
           DISPLAY "A-UNSIGNED-SENDER=[" W-ED "]"
           IF R-S1 IS POSITIVE
               DISPLAY "A-POS=T"
           ELSE
               DISPLAY "A-POS=F"
           END-IF
           MOVE W-SN TO R-S2
           MOVE R-S2 TO W-ED
           DISPLAY "A-NEG-SENDER=[" W-ED "]"
           IF R-S2 IS NEGATIVE
               DISPLAY "A-NEG=T"
           ELSE
               DISPLAY "A-NEG=F"
           END-IF
           MOVE W-SP TO R-S3
           MOVE R-S3 TO W-ED
           DISPLAY "A-POS-SENDER=[" W-ED "]"
           MOVE W-SN TO R-U1
           DISPLAY "B-UNSIGNED-RCVR=[" R-U1 "]"
           MOVE R-U1 TO W-ED
           DISPLAY "B-EDITED=[" W-ED "]"
           IF R-U1 IS NEGATIVE
               DISPLAY "B-NEG=T"
           ELSE
               DISPLAY "B-NEG=F"
           END-IF
           MOVE -125 TO W-FL
           MOVE W-FL TO R-SF
           MOVE R-SF TO W-ED
           DISPLAY "F-SIGNED=[" W-ED "]"
           MOVE W-FL TO R-UF
           DISPLAY "F-UNSIGNED=[" R-UF "]"
           STOP RUN.
