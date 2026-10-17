*>--- conformance-case
*> id: pb778_editing_literal_positions
*> rules: [SR-13.18.40.3-9]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> The PICTURE EDITING phrase's literal-1, literal-2 and literal-3
      *> are LITERAL POSITIONS (kb/Work PB778).  ISO 13.18.40.2 Format 1:
      *>     EDITING character-1 { IS literal-1
      *>                         | FOR { NEGATIVE IS literal-2
      *>                               | POSITIVE IS literal-3 } }
      *> 13.10.3 SR2: "constant-name-1 may be used anywhere that a format
      *> specifies a literal"; 8.3.3.6.3 SR1: "A figurative constant may
      *> be used whenever 'literal' appears in a format"; a symbolic-
      *> character is a figurative (8.3.3.6.2 Format 7).  The operands
      *> were the narrow grammar `literal`, so a constant-name was a PARSE
      *> error, and the figuratives and concatenations it did admit were
      *> inserted as their SOURCE TEXT ("SPACE", "-"&"/").
      *>
      *> DERIVATION.  Each item receives 1234 (E6: -123 then +123; E7:
      *> N"ABCD"); T inserts literal-1 at its position (13.18.40.5 rule 3):
      *>   E1 IS KC        - KC is the literal ":" (13.10.4 GR1)  12:34
      *>   E2 IS SPACE     - one character (8.3.3.6.4 GR3b)       12 34
      *>   E3 IS "-" & "/" - the folded literal "-/" (8.8.3.3 GR3) 12-/34
      *>   E4 IS ALL "<>"  - literal-1's length (8.3.3.6.4 GR3c)  12<>34
      *>   E5 IS SLASH     - ordinal 48 in the native set is "/"  12/34
      *>   E6 FOR NEGATIVE IS KCR POSITIVE IS KDB - "CR" when negative,
      *>      "DB" when not (13.18.40.4 GR14)            123CR / 123DB
      *>   E7 national subject: QUOTE is a national '"' (8.3.3.6.4 GR1)
      *>                                                        AB"CD
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB778ELP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SLASH IS 48.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KC  CONSTANT AS ":".
       01 KCR CONSTANT AS "CR".
       01 KDB CONSTANT AS "DB".
       01 E1 PIC 99T99 EDITING T IS KC.
       01 E2 PIC 99T99 EDITING T IS SPACE.
       01 E3 PIC 99T99 EDITING T IS "-" & "/".
       01 E4 PIC 99T99 EDITING T IS ALL "<>".
       01 E5 PIC 99T99 EDITING T IS SLASH.
       01 E6 PIC 999L EDITING L FOR NEGATIVE IS KCR POSITIVE IS KDB.
       01 E7 PIC NNTNN EDITING T IS QUOTE.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1234 TO E1 E2 E3 E4 E5
           MOVE -123 TO E6
           MOVE N"ABCD" TO E7
           DISPLAY "[" E1 "]"
           DISPLAY "[" E2 "]"
           DISPLAY "[" E3 "]"
           DISPLAY "[" E4 "]"
           DISPLAY "[" E5 "]"
           DISPLAY "[" E6 "]"
           MOVE 123 TO E6
           DISPLAY "[" E6 "]"
           DISPLAY "[" E7 "]"
           STOP RUN.
