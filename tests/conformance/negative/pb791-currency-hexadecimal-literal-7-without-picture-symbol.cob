      *> reject-at: 2002 2014 2023
      *> kb/Work PB791 - ISO 12.3.7.3 SR19: "If literal-7 is an alphanumeric or national literal in hexadecimal format,
      *> the PICTURE SYMBOL phrase shall be present in the CURRENCY SIGN clause" (cite.py --check 12.3.7.3 "If literal-7
      *> is an alphanumeric or national literal in hexadecimal format, the PICTURE SYMBOL phrase shall be present in the
      *> CURRENCY SIGN clause"). X"24" without PICTURE SYMBOL is refused under SR19 - detected on the literal's FORM (the
      *> one hexadecimal-format question, CobolLiteral.IsHexadecimalFormat), since its decoded character '$' cannot
      *> tell the forms apart. The positive side is pb791_currency_literal_forms (the PICTURE SYMBOL form).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB791G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS X"24".
       DATA DIVISION.
       PROCEDURE DIVISION.
           STOP RUN.
