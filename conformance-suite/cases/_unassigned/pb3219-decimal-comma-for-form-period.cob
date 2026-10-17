*>--- conformance-case
*> id: pb3219-decimal-comma-for-form-period
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3219 - ISO 1989:2023 13.18.40.3 SR12 b (FOR form): "Character-string-1 shall contain character-1
      *> and only the symbols '9', '.', the currency symbol, 'P', 'V', or 'Z'", read through SR13 ("The rules for
      *> the symbol period apply to the symbol comma, and the rules for the symbol comma apply to the symbol
      *> period"). Under DECIMAL-POINT IS COMMA the period is the GROUPING separator, which SR12 b does not list,
      *> so an extended editing sign control picture may not hold one. The compiler read the literal period and
      *> accepted it: PIC LLL9.999 bound and rendered "  -1.234". Positive sibling:
      *> 2023/pb3219_decimal_point_comma_sr12_point_2023 (the comma spelling renders).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3219NFP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC LLL9.999 EDITING L FOR NEGATIVE IS "-".
       PROCEDURE DIVISION.
           STOP RUN.
