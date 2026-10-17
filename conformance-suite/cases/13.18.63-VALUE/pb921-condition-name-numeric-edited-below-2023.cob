*>--- conformance-case
*> id: pb921-condition-name-numeric-edited-below-2023
*> rules: [SR-13.18.63.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR6 - "If the item is of category numeric-edited, then, subject to Syntax rules 2 and 3,
      *> literals in formats 1, 2, and 4 of the VALUE clause may be numeric when they shall be converted to their
      *> numeric-edited forms according to the rules for the MOVE statement" - is the ONLY rule that admits a
      *> numeric literal on a numeric-edited subject, and Annex E.3.3 item 43 DATES it: "VALUE clause,
      *> numeric-edited items and numeric literals. It is now permitted to allow numeric-edited data items to be
      *> assigned values specified as numeric literals." A COBOL-2023 addition, so below 2023 the edited image had
      *> to be written as an alphanumeric or national literal (SR7).
      *>
      *> THIS IS THE FORMAT-3 (condition-name) SPELLING OF THAT SAME FACT, and it is the gating negative that
      *> conformance-suite/cases/13.18.63-VALUE/pb560_condition_value_numeric_edited_2023 never had: PB560's own gating negative
      *> writes the FORMAT-1 spelling (`01 WS-A PIC ZZ9.99 VALUE 10.`), so it was green while the construct its
      *> positive actually rides was ungated. MEASURED BEFORE (kb/Work PB921): at --std 85 this program compiled
      *> clean, ran, and stored the COBOL-2023 edited image ` 10.00` - a COBOL-85 compiler has no such conversion,
      *> so a program relying on it is not COBOL-85 source, and `--std 85` said it was.
      *>
      *> 13.18.63.4 GR19 ("The characteristics of a condition-name are implicitly those of its conditional
      *> variable") and 14.9.39.4 GR6 (`SET condition-name TO TRUE` places the literal "according to the rules for
      *> the VALUE clause") are why format 3 rides SR6's conversion, hence SR6's edition; the determination and
      *> the strict-reading counter-argument are recorded on kb/Work PB921.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB921CONDNE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC ZZ9.99.
          88 A-TEN VALUE 10.
       PROCEDURE DIVISION.
       MAIN.
           SET A-TEN TO TRUE
           DISPLAY "A=[" WS-A "]"
           STOP RUN.
