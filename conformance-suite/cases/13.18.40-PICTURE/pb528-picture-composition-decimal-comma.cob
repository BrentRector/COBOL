*>--- conformance-case
*> id: pb528-picture-composition-decimal-comma
*> rules: [SR-13.18.40.3-13, SR-13.18.40.3-17, SR-13.18.40.3-20]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB528 - ISO 1989:2023 13.18.40.3 SR13: "When the DECIMAL-POINT IS COMMA clause is specified,
      *> the symbol comma is the decimal separator and the symbol period is the grouping separator. The rules
      *> for the symbol period apply to the symbol comma, and the rules for the symbol comma apply to the
      *> symbol period." 13.18.40.6 says the same of the precedence rules: "When the DECIMAL-POINT IS COMMA
      *> clause is specified, the precedence rules for the symbols comma and period are interchanged."
      *> SR13's second sentence was VACUOUS while the period rules it transfers did not exist. It is not any
      *> more: each entry is the comma-mode image of a rule the sibling golden proves in period mode, and
      *> each is COBOLNET1934 naming both the transferred rule and SR13.
      *>
      *> PE01  SR20 through SR13 - 'V' and the DECIMAL separator, which is now the comma.
      *> PE02  SR17 through SR13 - 'P' and the decimal separator.
      *> PE03  SR12 b through SR13 - the decimal separator may appear only once; the two PERIODS beside it are
      *>     now GROUPING separators and may repeat, which is exactly what the positive golden proves.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB528DPC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PE01 PIC 9V9,9.
       01 PE02 PIC PP99,99.
       01 PE03 PIC 9.999,999,99.
       PROCEDURE DIVISION.
           STOP RUN.
