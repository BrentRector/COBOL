*>--- conformance-case
*> id: pb920_false_value_cross_class_distinct
*> rules: [SR-13.18.63.3-27]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> !! SR27 IS A DISTINCTNESS SCREEN, NOT A CROSS-CLASS BAN. kb/Work PB920 — the POSITIVE half of
      *> negative/pb920-false-value-crosses-edited-image and its mirrored twin.
      *>
      *> ISO 13.18.63.3 SR27, first sentence: "The value of literal-4 shall not be equal to the value of
      *> any occurrence of literal-2."  The rule forbids an EQUAL pair; it says nothing about the two
      *> literals being written in the same class.  On a numeric-edited subject SR6 gives a numeric
      *> literal the edited image it would receive from a MOVE ("...literals in formats 1, 2, and 4 of the
      *> VALUE clause may be numeric when they shall be converted to their numeric-edited forms according
      *> to the rules for the MOVE statement"), and 8.8.4.2.1's NOTE makes the comparison that follows an
      *> alphanumeric one: "All comparisons involving numeric-edited data items are alphanumeric or
      *> national comparisons, including when the associated VALUE clause is a numeric literal".  So a
      *> cross-spelling pair whose IMAGES DIFFER is conforming source and shall compile — this program is
      *> what stops the screen from being made unconditionally loud over every mixed pair.
      *>
      *> Expected values, COMPUTED FROM THE STANDARD (not measured):
      *>   X (PIC ZZ9.99): 14.9.39.4 GR6 places literal-2 " 10.00" as written (SR7 takes an alphanumeric
      *>     literal on a numeric-edited item as the edited form the programmer wrote) -> X = " 10.00" and
      *>     8.8.4.5.3 GR2 makes X-TEN TRUE.  14.9.39.4 GR7 then places literal-4 = 11 "according to the rules for
      *>     the VALUE clause", i.e. through SR6: 11 over ZZ9.99 edits to " 11.00" -> X = " 11.00", and
      *>     the relation " 11.00" = " 10.00" is false, so X-TEN is FALSE.  The two images differ, which
      *>     is exactly why SR27 does not reach this entry.
      *>   Y (PIC ZZ9.99): the mirrored spelling.  literal-2 = 10 edits to " 10.00"; literal-4 is the
      *>     alphanumeric "  0.00" (ZZ9.99 over the value zero suppresses both Z positions), and
      *>     " 10.00" <> "  0.00", so the pair is distinct in the other direction too.
      *>   N (PIC 9(3)): SR27 a)'s algebraic arm on a NUMERIC subject — 5 and 7 are unequal numbers, so
      *>     the entry stands; GR6 stores 005 and GR7 stores 007 (13.18.63.3 SR2, no truncation).
      *>   A (PIC X(3)): SR27 b)'s character arm under the native runtime collating sequence — "ABC" and
      *>     "ZZZ" are unequal, so the entry stands; 8.8.4.2.7's equal-length comparison decides the IFs.
      *>
      *> The FALSE phrase and SET condition-name TO FALSE are a COBOL-2002 introduction; this program is
      *> in the 2023 corpus because its first two items rest on SR6's numeric literal over a
      *> numeric-edited item, which Annex E.3.3 item 43 dates to COBOL-2023
      *> (negative/pb560-numeric-edited-value-below-2023 pins that edge).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB920XDIST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X               PIC ZZ9.99.
           88  X-TEN       VALUE " 10.00" WHEN SET TO FALSE IS 11.
       01  Y               PIC ZZ9.99.
           88  Y-TEN       VALUE 10 WHEN SET TO FALSE IS "  0.00".
       01  N               PIC 9(3).
           88  N-FIVE      VALUE 5 WHEN SET TO FALSE IS 7.
       01  A               PIC X(3).
           88  A-ABC       VALUE "ABC" WHEN SET TO FALSE IS "ZZZ".
       PROCEDURE DIVISION.
       MAIN-P.
           SET X-TEN TO TRUE
           DISPLAY "XT=[" X "]"
           IF X-TEN DISPLAY "XT-ON=yes" ELSE DISPLAY "XT-ON=no" END-IF
           SET X-TEN TO FALSE
           DISPLAY "XF=[" X "]"
           IF X-TEN DISPLAY "XF-ON=yes" ELSE DISPLAY "XF-ON=no" END-IF
           SET Y-TEN TO TRUE
           DISPLAY "YT=[" Y "]"
           SET Y-TEN TO FALSE
           DISPLAY "YF=[" Y "]"
           IF Y-TEN DISPLAY "YF-ON=yes" ELSE DISPLAY "YF-ON=no" END-IF
           SET N-FIVE TO TRUE
           DISPLAY "NT=[" N "]"
           SET N-FIVE TO FALSE
           DISPLAY "NF=[" N "]"
           IF N-FIVE DISPLAY "NF-ON=yes" ELSE DISPLAY "NF-ON=no" END-IF
           SET A-ABC TO TRUE
           DISPLAY "AT=[" A "]"
           SET A-ABC TO FALSE
           DISPLAY "AF=[" A "]"
           IF A-ABC DISPLAY "AF-ON=yes" ELSE DISPLAY "AF-ON=no" END-IF
           DISPLAY "DONE"
           STOP RUN.
