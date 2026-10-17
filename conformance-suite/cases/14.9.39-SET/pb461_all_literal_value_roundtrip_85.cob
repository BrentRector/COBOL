      *>--- conformance-case
      *> id: pb461_all_literal_value_roundtrip_85
      *> rules: [GR-14.9.39.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB461 - ONE reading of a level-88 VALUE operand, proved by the ROUND TRIP. ISO 14.9.39.4 GR6:
      *> "the literal in the VALUE clause associated with condition-name-1 is placed in the conditional variable
      *> according to the rules for the VALUE clause"; 8.8.4.5.3 GR3: "the result of the test is true if one of
      *> the values corresponding to condition-name-1 equals the value of its associated conditional variable".
      *> Together they make SET <cond> TO TRUE followed by IF <cond> an IDENTITY for every operand form, which is
      *> what this golden measures - every line is a SET whose own condition is then tested.
      *>
      *> The expected images are COMPUTED FROM 8.3.3.6.4 GR2, not measured: the string is "repeated character by
      *> character until the size of the resultant string is greater than or equal to the number of character
      *> positions in the associated data item", then "truncated from the right" to that count. So ALL "*" on
      *> X(4) is ****, ALL "AB" on X(4) is ABAB, and ALL "AB" on X(5) is ABABA - the odd width is the leg that
      *> tells GR2's repeat-then-truncate from a whole-copy fill. In Formats 1-5 the word ALL is OPTIONAL
      *> (8.3.3.6.2 underlines ALL only in Format 6), so ALL SPACES is the Format-2 constant and fills; GR4 gives
      *> the zero format "the numeric value '0'" on a numeric conditional variable.
      *>
      *> MEASURED BEFORE the fix: the rule was written down in three emitters and each covered a different half.
      *> AFTER-SET printed [ALL"] - the glued parse text - and the SET's own condition was FALSE; ALL SPACES
      *> stored correctly but tested FALSE; a figurative ZERO on a numeric conditional variable emitted the bare
      *> identifier ZEROL and failed the C# compilation outright.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB461-ALL-VALUE-85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(4) VALUE "ZZZZ".
          88 A-STAR VALUE ALL "*".
       01 WS-B PIC X(4) VALUE "ZZZZ".
          88 B-AB VALUE ALL "AB".
       01 WS-C PIC X(5) VALUE "ZZZZZ".
          88 C-AB VALUE ALL "AB".
       01 WS-D PIC X(4) VALUE "ZZZZ".
          88 D-SP VALUE ALL SPACES.
       01 WS-E PIC X(4) VALUE "ZZZZ".
          88 E-QU VALUE ALL QUOTES.
       01 WS-F PIC 9(4) VALUE 1234.
          88 F-ZE VALUE ZERO.
       01 WS-G PIC 9(4) VALUE 1234.
          88 G-ZE VALUE ALL ZEROS.
       01 WS-H.
          88 H-STAR VALUE ALL "*".
          05 H-1 PIC X(2) VALUE "ZZ".
          05 H-2 PIC X(2) VALUE "ZZ".
       PROCEDURE DIVISION.
           SET A-STAR TO TRUE
           DISPLAY "A=[" WS-A "]"
           IF A-STAR DISPLAY "A-TRUE" ELSE DISPLAY "A-FALSE" END-IF
           SET B-AB TO TRUE
           DISPLAY "B=[" WS-B "]"
           IF B-AB DISPLAY "B-TRUE" ELSE DISPLAY "B-FALSE" END-IF
           SET C-AB TO TRUE
           DISPLAY "C=[" WS-C "]"
           IF C-AB DISPLAY "C-TRUE" ELSE DISPLAY "C-FALSE" END-IF
           SET D-SP TO TRUE
           DISPLAY "D=[" WS-D "]"
           IF D-SP DISPLAY "D-TRUE" ELSE DISPLAY "D-FALSE" END-IF
           SET E-QU TO TRUE
           DISPLAY "E=[" WS-E "]"
           IF E-QU DISPLAY "E-TRUE" ELSE DISPLAY "E-FALSE" END-IF
           SET F-ZE TO TRUE
           DISPLAY "F=[" WS-F "]"
           IF F-ZE DISPLAY "F-TRUE" ELSE DISPLAY "F-FALSE" END-IF
           SET G-ZE TO TRUE
           DISPLAY "G=[" WS-G "]"
           IF G-ZE DISPLAY "G-TRUE" ELSE DISPLAY "G-FALSE" END-IF
           SET H-STAR TO TRUE
           DISPLAY "H=[" WS-H "]"
           IF H-STAR DISPLAY "H-TRUE" ELSE DISPLAY "H-FALSE" END-IF
           STOP RUN.
