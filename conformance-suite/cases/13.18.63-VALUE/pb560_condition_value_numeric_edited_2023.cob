*>--- conformance-case
*> id: pb560_condition_value_numeric_edited_2023
*> rules: [SR-13.18.63.3-6, GR-13.18.63.4-19, GR-14.9.39.4-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB560 - the NUMERIC-EDITED legs of the ONE level-88 VALUE recipe. IN THE 2023 CORPUS because a
      *> numeric-literal VALUE for a numeric-edited item is itself a COBOL-2023 introduction (Annex E.3.3 item
      *> 43, "It is now permitted to allow numeric-edited data items to be assigned values specified as numeric
      *> literals"; the gating negative is tests/conformance/negative/pb560-numeric-edited-value-below-2023).
      *>
      *> ISO 14.9.39.4 GR6: the literal "is placed in the conditional variable according to the rules for the
      *> VALUE clause"; 13.18.63.4 GR19: "The characteristics of a condition-name are implicitly those of its
      *> conditional variable"; 8.8.4.5.3 GR3 makes the test true exactly when the stored value is one of the
      *> condition's values. So SET <cond> TO TRUE followed by IF <cond> is an IDENTITY, and every pair below
      *> measures it. The SET emitter used to carry its own literal recipe, which stored the raw literal TEXT
      *> where the VALUE clause composes an EDITED IMAGE - so each of these stored a wrong value AND left its
      *> own condition false.
      *>
      *> The expected images are COMPUTED FROM THE RULES, not measured:
      *>   A  13.18.63.3 SR6 - a numeric literal for a numeric-edited item is "converted to their
      *>      numeric-edited forms according to the rules for the MOVE statement". 10 into PIC ZZ9.99:
      *>      13.18.40.5 editing rule 7 a) puts the replacement character (a space, for 'Z') in "any character
      *>      position immediately preceding ... the first nonzero numeric character in the item", so the first
      *>      Z is a space, the second carries the 1, the 9 carries the 0, and the fraction is 00 - " 10.00".
      *>      MEASURED BEFORE: [10    ], and A-TEN was FALSE right after the SET.
      *>   B  13.18.63.3 SR8 NOTE 2 - "When the BLANK WHEN ZERO clause appears in the data description entry
      *>      of a numeric-edited item and the VALUE clause is a numeric literal, including the figurative
      *>      constant ZERO, then the BLANK WHEN ZERO clause does effect initialization." So 0 into
      *>      PIC ZZ9 BLANK WHEN ZERO is three spaces, not "0  ". MEASURED BEFORE: [0  ], B-ZERO FALSE.
      *>   C  the same SR6 conversion carrying a simple insertion character: 1234 into PIC 9,999 is "1,234".
      *>      MEASURED BEFORE: [1234 ], C-1234 FALSE.
      *>   D  a PICTURE format-2 (LOCALE) numeric-edited variable has NO compile-time image at all - 13.18.40.5
      *>      editing rule 11 ("When locale-name-1 is specified, the locale used in editing and de-editing the
      *>      item is the one associated with that name ...; otherwise, the current locale is used") - so its value
      *>      is composed at RUN TIME through the one locale composer, exactly as its own VALUE clause and its
      *>      condition test already were. Hand-derived (en-US): 10 into $Z9.99 SIZE 8 is "  $10.00".
      *>      MEASURED BEFORE: the raw text [10      ], D-TEN FALSE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB560-COND-VALUE-2023.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC ZZ9.99.
          88 A-TEN VALUE 10.
       01 WS-B PIC ZZ9 BLANK WHEN ZERO.
          88 B-ZERO VALUE 0.
       01 WS-C PIC 9,999.
          88 C-1234 VALUE 1234.
       01 WS-D PIC $Z9.99 LOCALE IS US SIZE IS 8.
          88 D-TEN VALUE 10.
       PROCEDURE DIVISION.
           SET A-TEN TO TRUE
           DISPLAY "A=[" WS-A "]"
           IF A-TEN DISPLAY "A-TRUE" ELSE DISPLAY "A-FALSE" END-IF
           SET B-ZERO TO TRUE
           DISPLAY "B=[" WS-B "]"
           IF B-ZERO DISPLAY "B-TRUE" ELSE DISPLAY "B-FALSE" END-IF
           SET C-1234 TO TRUE
           DISPLAY "C=[" WS-C "]"
           IF C-1234 DISPLAY "C-TRUE" ELSE DISPLAY "C-FALSE" END-IF
           SET D-TEN TO TRUE
           DISPLAY "D=[" WS-D "]"
           IF D-TEN DISPLAY "D-TRUE" ELSE DISPLAY "D-FALSE" END-IF
           STOP RUN.
