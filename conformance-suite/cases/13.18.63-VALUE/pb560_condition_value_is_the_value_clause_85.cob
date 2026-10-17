      *>--- conformance-case
      *> id: pb560_condition_value_is_the_value_clause_85
      *> rules: [GR-13.18.63.4-19]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB560 - ONE recipe places a level-88 literal, and the ROUND TRIP proves it. ISO 14.9.39.4 GR6:
      *> "the literal in the VALUE clause associated with condition-name-1 is placed in the conditional variable
      *> according to the rules for the VALUE clause"; 13.18.63.4 GR19: "The characteristics of a condition-name
      *> are implicitly those of its conditional variable"; 8.8.4.5.3 GR3: "the result of the test is true if one
      *> of the values corresponding to condition-name-1 equals the value of its associated conditional variable".
      *> Together those make SET <cond> TO TRUE followed by IF <cond> an IDENTITY, whatever the item's storage
      *> form - which is exactly what the SET emitter's own private literal recipe could not deliver, because it
      *> was a THIRD spelling of rules the VALUE initializer and the condition test already carried.
      *>
      *> This is PB461's golden's 85-legal sibling; the numeric-edited legs live in the 2023 copy, because a
      *> numeric-literal VALUE for a numeric-edited item is a COBOL-2023 introduction (Annex E.3.3 item 43).
      *>
      *> The expected values are COMPUTED FROM THE RULES, not measured:
      *>   A  13.18.63.3 SR2 - a numeric subject takes numeric literals "permissible values within the range
      *>      indicated by the PICTURE clause", so 7 in PIC 9(4) COMP-3 is the value seven and DISPLAYs 0007.
      *>      A-PK is WHOLE-GROUP-ALIASED (WS-A is redefined), so the leaf is stored as its packed BYTE IMAGE
      *>      rather than as a native integer - the shape that used to fail the whole compilation, because the
      *>      SET emitter handed a long to a string field (CS1503) while the VALUE clause on the same item was
      *>      composing bytes. GR19 says the condition-name has the conditional variable's characteristics, and
      *>      the storage form is one of them.
      *>   B  SR4 - an alphanumeric subject takes an alphanumeric literal, stored as written (SR11 NOTE 3).
      *>   C  8.3.3.6.4 GR2 - ALL "AB" is repeated to the variable's 5 character positions then truncated from
      *>      the right: ABABA. The odd width is the leg that tells a repeat-then-truncate from a whole-copy fill.
      *> (The FALSE-phrase arm of the same recipe - 13.18.63.4 GR20 - rides the 2002 copy: the WHEN SET TO
      *> FALSE phrase and SET condition-name TO FALSE are both COBOL-2002 additions.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB560-COND-VALUE-85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A.
          05 A-PK PIC 9(4) COMP-3.
             88 A-SEVEN VALUE 7.
          05 A-TAIL PIC X(2).
       01 WS-A-VIEW REDEFINES WS-A PIC X(5).
       01 WS-B PIC X(4).
          88 B-TEXT VALUE "WXYZ".
       01 WS-C PIC X(5).
          88 C-AB VALUE ALL "AB".
       PROCEDURE DIVISION.
           SET A-SEVEN TO TRUE
           DISPLAY "A=[" A-PK "]"
           IF A-SEVEN DISPLAY "A-TRUE" ELSE DISPLAY "A-FALSE" END-IF
           SET B-TEXT TO TRUE
           DISPLAY "B=[" WS-B "]"
           IF B-TEXT DISPLAY "B-TRUE" ELSE DISPLAY "B-FALSE" END-IF
           SET C-AB TO TRUE
           DISPLAY "C=[" WS-C "]"
           IF C-AB DISPLAY "C-TRUE" ELSE DISPLAY "C-FALSE" END-IF
           STOP RUN.
