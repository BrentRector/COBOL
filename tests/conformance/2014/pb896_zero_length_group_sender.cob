      *> kb/Work PB896 - A ZERO-LENGTH GROUP SENDER TAKES GR1's ROUTE, WHICH CHANGES THE MOVE'S KIND.
      *> Every expected line below is derived from the rule text, never from a run.
      *>
      *> ISO 8.5.4 - "A zero-length item is a data item or a literal whose minimum length is zero and whose
      *>   length at runtime is zero", and item 1 of its enumeration is "A group data item containing only an
      *>   occurs-depending table in which the number of occurrences is zero". ZG below is exactly that shape:
      *>   its one member is an OCCURS 0 TO 5 DEPENDING table, and 13.18.38 SR16 permits integer-1 to be zero.
      *> ISO 14.9.25.4 GR1 - "If identifier-1 is a zero-length item, it is as if literal-1 were specified as a
      *>   zero-length literal."
      *> ISO 14.9.25.4 GR2 - "If literal-1 is an alphanumeric or national zero-length literal and the receiving
      *>   operand is other than a dynamic-length elementary item, literal-1 is treated as if it were the
      *>   figurative constant SPACE."
      *> ISO 14.9.25.4 GR4 - "Any move in which the sending operand is either a literal or an elementary item and
      *>   the receiving item is an elementary item is an elementary move." THE SUBSTITUTED SENDER IS A LITERAL,
      *>   so the statement is no longer GR4's group move - which is why the group path could not carry the rule.
      *> ISO 8.3.3.6.4 GR2 - a figurative constant is repeated to the length of the associated item.
      *> ISO 13.18.38.4 GR8 a) - with data-name-1 outside the group, only the part of the table area the
      *>   DEPENDING item specifies is used, which is what makes L3/L4's non-zero extent "125".
      *>
      *> EDITION: --std 2014. Neither 8.5.4 nor 13.18.38 SR16's zero integer-1 carries an edition marker and
      *> Annex E lists no change to either, but the zero-length literal the program also moves is a COBOL-2014
      *> introduction (kb/Work PB895, VCR row 7.30), so 2014 is the oldest edition that can compile it.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC:
      *>
      *> L1NUM=[   ]   N = 0, so ZG is a zero-length item; GR1 substitutes a zero-length literal, GR2 the
      *>               figurative SPACE, GR4 makes the move ELEMENTARY, and 8.3.3.6.4 GR2 repeats the space over
      *>               all three receiving positions. (The group route, which GR1 replaces, stored 000.)
      *> L1EDIT=[   ]  The same substitution into a numeric-edited receiver: three space positions, not an
      *>               edited zero.
      *> L1ALPH=[    ] And into an alphanumeric one: four spaces.
      *> L2* = L1*     THE DISCRIMINATOR: the SAME three receivers under a written zero-length literal. GR1's
      *>               substitution makes the two statements the same statement, so the two answers are owed to
      *>               be equal - CONTROL=SAME - and a compiler that routes the group sender through the group
      *>               move disagrees on the numeric receiver alone.
      *> L3NUM=125     The complement: with N = 3 the sending operand is NOT zero-length, GR1's antecedent fails
      *>               and the group move applies over GR8 a)'s three-character current extent.
      *> L4ALPH=[125 ] The same non-zero extent into a four-character alphanumeric receiver - GR4's
      *>               "alphanumeric to alphanumeric elementary move", left-justified and space-filled
      *>               (14.6.8.3). Without L3/L4 an implementation that always substituted SPACE would pass.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB896ZLGRP85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N               PIC 9 VALUE 0.
       01 ZG.
          05 ZE           PIC 9 OCCURS 0 TO 5 TIMES DEPENDING ON N.
       01 R-NUM           PIC 9(3) VALUE 987.
       01 R-EDIT          PIC ZZ9.
       01 R-ALPH          PIC X(4) VALUE "ABCD".
       01 C-NUM           PIC 9(3) VALUE 987.
       01 C-EDIT          PIC ZZ9.
       01 C-ALPH          PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
           MOVE 3 TO N
           MOVE 1 TO ZE(1)
           MOVE 2 TO ZE(2)
           MOVE 5 TO ZE(3)

      *> ---- L1: the group is a zero-length item (8.5.4 item 1)
           MOVE 0 TO N
           MOVE ZG TO R-NUM
           MOVE ZG TO R-EDIT
           MOVE ZG TO R-ALPH
           DISPLAY "L1NUM=[" R-NUM "]"
           DISPLAY "L1EDIT=[" R-EDIT "]"
           DISPLAY "L1ALPH=[" R-ALPH "]"

      *> ---- L2: the written zero-length literal GR1 says it is AS IF
           MOVE "" TO C-NUM
           MOVE "" TO C-EDIT
           MOVE "" TO C-ALPH
           IF R-NUM = C-NUM AND R-EDIT = C-EDIT AND R-ALPH = C-ALPH
              DISPLAY "L2CONTROL=SAME"
           ELSE
              DISPLAY "L2CONTROL=DIFFERENT"
           END-IF

      *> ---- L3/L4: the complement - a non-zero extent is still a group move
           MOVE 3 TO N
           MOVE ZG TO R-NUM
           MOVE ZG TO R-ALPH
           DISPLAY "L3NUM=" R-NUM
           DISPLAY "L4ALPH=[" R-ALPH "]"
           STOP RUN.
