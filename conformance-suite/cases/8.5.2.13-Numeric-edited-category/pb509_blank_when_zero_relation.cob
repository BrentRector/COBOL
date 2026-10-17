      *>--- conformance-case
      *> id: pb509_blank_when_zero_relation
      *> rules: [GR-8.5.2.13-2, GR-13.18.8.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB509 - BLANK WHEN ZERO's General rule 3 is a rule about OPERATIONS, and a relation condition
      *> is one of them. ISO/IEC 1989:2023 13.18.8.4 GR3: "If the subject of the entry is a sending data item,
      *> the object of an operation is a numeric or numeric-edited data item, and the content of the sending
      *> data item is all spaces, the value of the sending data item is considered to be zero."
      *> GR1, one rule earlier, says "receiving operand" where GR3 says "the object of an OPERATION", and
      *> 8.8.4.2.1 calls a relation's two operands its subject and its object. The de-editing MOVE is already
      *> legislated by 14.9.25.4 GR6 d) 1, so the relation condition is what GR3 adds - and GR3 has to name a
      *> numeric-EDITED object for a reason: giving the blanked item "the value zero" changes nothing in a
      *> character comparison, so the comparison there is by VALUE (8.8.4.2.4, algebraic).
      *> Until 2026-09-20 every numeric-edited operand took the alphanumeric arm and nothing asked whether the
      *> sender was blanked, so a blanked item compared as five spaces against "00000" and was NOT equal.
      *>
      *> The rule is written about the CONTENT, so each expectation below turns on the content, not the clause:
      *>   EQ-NUMITEM   blanked BWZ vs a numeric data item holding zero -> GR3 -> 0 = 0 -> EQ.
      *>   EQ-EDITED    blanked BWZ vs a numeric-EDITED data item holding zero ("   00") -> by value -> EQ.
      *>   EQ-BOTH-BWZ  two blanked BWZ items - each is the other's object -> EQ.
      *>   GT-NEG       blanked BWZ vs PIC S9(5) VALUE -3 -> algebraically 0 > -3 -> TRUE. (As characters it
      *>                would be FALSE: 8.8.4.2.5 moves the unsigned integer to "00003" and space < '0'.)
      *>   NE-FIVE      blanked BWZ vs a numeric data item holding 5 -> 0 not = 5 -> NE.
      *>   NE-DIGITS    a BWZ item HOLDING "   12" is not all spaces, so GR3 does not apply and 8.8.4.2.5/.7
      *>                compare characters: "   12" vs "00012" -> NE. The clause changes nothing here.
      *>   EQ-DIGITS    a BWZ item holding "00012" (an unsuppressed PIC 9(5)) vs the same characters -> EQ by
      *>                the same ordinary character rule.
      *>   NE-LITERAL   blanked BWZ vs the numeric LITERAL 0 -> GR3 requires the object to be a "data item",
      *>                which a literal is not, so 8.8.4.2.5 applies unchanged: "0" space-extended vs spaces -> NE.
      *>   NE-REFMOD    a reference-modified slice is a DIFFERENT data item: 8.4.3.3.4 GR5 "creates a unique data
      *>                item" and GR6 c) makes a slice of a numeric or numeric-edited item "class and category
      *>                alphanumeric", so neither side of WS-PLAIN(1:2) = WS-ZERONUM(1:2) is what GR3 names and
      *>                the ordinary character comparison stands: "  " vs "00" -> NE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB509REL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-PLAIN   PIC 9(5)  BLANK WHEN ZERO.
       01 WS-EDIT    PIC ZZZ99 BLANK WHEN ZERO.
       01 WS-OTHER   PIC 9(5)  BLANK WHEN ZERO.
       01 WS-ED2     PIC ZZZ99.
       01 WS-ZERONUM PIC 9(5)  VALUE 0.
       01 WS-FIVE    PIC 9(5)  VALUE 5.
       01 WS-NEG3    PIC S9(5) VALUE -3.
       01 WS-TWELVE  PIC 9(5)  VALUE 12.
       PROCEDURE DIVISION.
           MOVE 0 TO WS-PLAIN
           MOVE 0 TO WS-EDIT
           MOVE 0 TO WS-OTHER
           MOVE 0 TO WS-ED2
           IF WS-PLAIN = WS-ZERONUM
               DISPLAY "EQ-NUMITEM-TRUE"
           ELSE
               DISPLAY "EQ-NUMITEM-FALSE"
           END-IF
           IF WS-EDIT = WS-ED2
               DISPLAY "EQ-EDITED-TRUE"
           ELSE
               DISPLAY "EQ-EDITED-FALSE"
           END-IF
           IF WS-PLAIN = WS-OTHER
               DISPLAY "EQ-BOTH-BWZ-TRUE"
           ELSE
               DISPLAY "EQ-BOTH-BWZ-FALSE"
           END-IF
           IF WS-PLAIN > WS-NEG3
               DISPLAY "GT-NEG-TRUE"
           ELSE
               DISPLAY "GT-NEG-FALSE"
           END-IF
           IF WS-PLAIN = WS-FIVE
               DISPLAY "NE-FIVE-FALSE"
           ELSE
               DISPLAY "NE-FIVE-TRUE"
           END-IF
           MOVE 12 TO WS-EDIT
           IF WS-EDIT = WS-TWELVE
               DISPLAY "NE-DIGITS-FALSE"
           ELSE
               DISPLAY "NE-DIGITS-TRUE"
           END-IF
           MOVE 12 TO WS-PLAIN
           IF WS-PLAIN = WS-TWELVE
               DISPLAY "EQ-DIGITS-TRUE"
           ELSE
               DISPLAY "EQ-DIGITS-FALSE"
           END-IF
           MOVE 0 TO WS-PLAIN
           IF WS-PLAIN = 0
               DISPLAY "NE-LITERAL-FALSE"
           ELSE
               DISPLAY "NE-LITERAL-TRUE"
           END-IF
           IF WS-PLAIN(1:2) = WS-ZERONUM(1:2)
               DISPLAY "NE-REFMOD-FALSE"
           ELSE
               DISPLAY "NE-REFMOD-TRUE"
           END-IF
           STOP RUN.
