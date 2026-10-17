*>--- conformance-case
*> id: pb866_float_edited_is_form
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB866 - an IS-form PICTURE EDITING phrase on a FLOATING-POINT numeric-edited item.
      *> 13.18.40.3 SR12: "If literal-1 is specified, character-1 is a fixed editing sign control
      *> symbol. If the FOR phrase is specified, character-1 is an extended editing sign control
      *> symbol ... and the following rules apply:" - and it is in THAT list (printed as its second
      *> 'a)', p.442) that "Extended editing sign control symbols shall not be specified for a
      *> floating-point edited item" stands. Only the FOR form is barred. 13.18.40.5 rule 3 makes an
      *> IS-form character-1 a SIMPLE INSERTION symbol, and Table 7 gives this category "Simple
      *> insertion, special insertion, and fixed insertion for the significand part".
      *>
      *> Expected values, derived from the rules (not measured):
      *> FT +9T9.9(3)E+99 EDITING T IS ":" <- 12.345: the significand holds 5 digits with 3 after
      *>    the point, normalized (14.6.8.4 GR1) to 12345 at exponent 0; rule 3 puts ':' at the T
      *>    => "+1:2.345E+00". LENGTH 12 (13.18.40.4 GR14: every symbol, E and both signs counted).
      *> FE +9B9.9(3)E+99 - the B control: the same image with a space.
      *> FW -9T99.99E+99 EDITING T IS "<>" VALUE 1.2345E1: GR14 'es' - "If character-1 is a simple
      *>    insertion symbol ..., the size of literal-1 is counted in the size of the item" => LENGTH
      *>    13; 12.345 normalizes to 12345 over 9 99.99 => 123.45E-01; '-' is blank for a positive
      *>    value (Table 8) => " 1<>23.45E-01".
      *> N  <- MOVE FT: the de-edit (14.9.25.4 GR5) yields 12.345 exactly => +0012.3450.
      *> FW <- COMPUTE -3.5 * 2 = -7 => "-7<>00.00E-02"; MOVE FW TO N => -0007.0000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB866FLOATEDITEDISFORM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FT PIC +9T9.9(3)E+99 EDITING T IS ":".
       01 FW PIC -9T99.99E+99 EDITING T IS "<>" VALUE 1.2345E1.
       01 FE PIC +9B9.9(3)E+99.
       01 N  PIC S9(4)V9(4) SIGN LEADING SEPARATE.
       01 L  PIC 99.
       PROCEDURE DIVISION.
           MOVE 12.345 TO FT
           MOVE 12.345 TO FE
           MOVE FUNCTION LENGTH(FT) TO L
           DISPLAY "FT=[" FT "] LEN=" L
           DISPLAY "FE=[" FE "]"
           MOVE FUNCTION LENGTH(FW) TO L
           DISPLAY "FW=[" FW "] LEN=" L
           MOVE FT TO N
           DISPLAY "N=[" N "]"
           COMPUTE FW = -3.5 * 2
           DISPLAY "FW=[" FW "]"
           MOVE FW TO N
           DISPLAY "N=[" N "]"
           STOP RUN.
