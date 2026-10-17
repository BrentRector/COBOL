*>--- conformance-case
*> id: pb3219_decimal_point_comma_sr12_point_2023
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3219 - ISO 1989:2023 13.18.40.3 SR13: "When the DECIMAL-POINT IS COMMA clause is specified, the
      *> symbol comma is the decimal separator and the symbol period is the grouping separator. The rules for the
      *> symbol period apply to the symbol comma, and the rules for the symbol comma apply to the symbol period."
      *> Two rules of SR12 name "the symbol period" and both transfer to the comma:
      *>   - SR12 b of the FOR form: "Character-string-1 shall contain character-1 and only the symbols '9', '.',
      *>     the currency symbol, 'P', 'V', or 'Z'" - so under DECIMAL-POINT IS COMMA the COMMA is admitted beside
      *>     an extended editing sign control symbol and the period (now a grouping separator) is not;
      *>   - SR12 b of the no-FOR form: "Each of the symbols from the set 'CR', 'DB', 'E', 'S', 'V' '.' may appear
      *>     only once in character-string-1" - so a floating-point numeric-edited significand holds ONE comma
      *>     and any number of periods, which are simple insertion (grouping) symbols.
      *> The compiler read a literal period in both: it refused the legal comma of a FOR picture (COBOLNET1596),
      *> refused the second grouping period of a significand (COBOLNET1658), and admitted the period of a FOR
      *> picture and the second comma of a significand. The negative siblings
      *> negative/pb3219-decimal-comma-for-form-period and negative/pb3219-decimal-comma-float-two-points hold the
      *> two refusals the swap makes; this golden holds the images the legal spellings render.
      *>
      *> D01 LLL9,99 FOR NEGATIVE "-" with -12,34 - floating L, three integer digit positions, the comma the
      *>     decimal point: the sign literal stands immediately left of the first nonzero digit => " -12,34".
      *> D02 9,99L FOR NEGATIVE "-" with -1,5 - one FIXED trailing L => "1,50-".
      *> D03 +9.9.9E+99 with 150 - the two periods are grouping insertion symbols and there is NO decimal point,
      *>     so the significand is the integer 150 and the exponent 00 => "+1.5.0E+00".
      *> D04 +9,9.9E+99 with 1,5 - the comma is the one decimal point (after the first digit), the period a
      *>     grouping insertion symbol between the second and third digit => "+1,5.0E+00".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3219DPC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D01 PIC LLL9,99 EDITING L FOR NEGATIVE IS "-".
       01 D02 PIC 9,99L EDITING L FOR NEGATIVE IS "-".
       01 D03 PIC +9.9.9E+99.
       01 D04 PIC +9,9.9E+99.
       PROCEDURE DIVISION.
           MOVE -12,34 TO D01
           DISPLAY "D01=[" D01 "] LEN=" FUNCTION LENGTH(D01)
           MOVE -1,5 TO D02
           DISPLAY "D02=[" D02 "] LEN=" FUNCTION LENGTH(D02)
           MOVE 150 TO D03
           DISPLAY "D03=[" D03 "] LEN=" FUNCTION LENGTH(D03)
           MOVE 1,5 TO D04
           DISPLAY "D04=[" D04 "] LEN=" FUNCTION LENGTH(D04)
           STOP RUN.
