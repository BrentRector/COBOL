*>--- conformance-case
*> id: pb490_simple_insertion_comma_mode
*> rules: [GR-12.3.7.4-14, GR-13.18.40.5-3, GR-13.18.40.5-6,
*>    GR-13.18.40.5-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB490 - the simple insertion SET is asked which character plays the GROUPING
      *> separator's part, never told. 13.18.40.2 SR13: "When the DECIMAL-POINT IS COMMA clause is
      *> specified, the symbol comma is the decimal separator and the symbol period is the grouping
      *> separator. The rules for the symbol period apply to the symbol comma, and the rules for the
      *> symbol comma apply to the symbol period." So under this clause the PERIOD is a simple
      *> insertion symbol (13.18.40.5 rule 3) and the COMMA is the decimal point, whose editing is
      *> SPECIAL insertion (rule 4) and which STOPS the suppression walk.
      *>
      *> G  ZZ.ZZZ,99 <- 12,34: the grouping period is embedded in the 'Z' string, so rule 7 a) gives
      *>    it the replacement character space along with every 'Z' left of the '1'; the comma is the
      *>    decimal point and is inserted => "    12,34".
      *> GS ZZ/ZZ <- 12: '/' is a simple insertion symbol in either mode => "   12".
      *> GF $$.$$9,99 <- 12,34: rule 6 a) lands the single currency occurrence immediately preceding
      *>    the first nonzero numeric character, the embedded grouping period being part of the
      *>    floating string => "   $12,34".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB490CMA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G        PIC ZZ.ZZZ,99.
       01 GS       PIC ZZ/ZZ.
       01 GF       PIC $$.$$9,99.
       PROCEDURE DIVISION.
           MOVE 12,34 TO G
           MOVE 12 TO GS
           MOVE 12,34 TO GF
           DISPLAY "G=[" G "] GS=[" GS "] GF=[" GF "]"
           STOP RUN.
