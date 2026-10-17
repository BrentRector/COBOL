*>--- conformance-case
*> id: pb976_class_for_phrase_rules
*> rules: [SR-12.3.7.3-17, GR-12.3.7.4-12]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB976 - the CLASS clause's FOR ALPHANUMERIC / FOR NATIONAL phrase is READ, and every
      *> class-dependent rule of ISO 12.3.7.3 SR17 keys on it; kb/Work PB976's sweep - the CLASS operand
      *> decoder is the ALPHABET clause's (figurative constants and ALL literal-1 are characters, never
      *> their own spelling).  The FOR phrase is a COBOL-2002 introduction, so this is the introducing
      *> edition; the version matrix owns the negative below it, and the SR17 violations are the
      *> negative pb976-class-national-alnum-literal plus ClassClauseForPhraseTests.
      *> DERIVATION of the expected lines (12.3.7.4 GR12 - "The characters specified by the values of
      *> the literals in this clause define the exclusive set of characters of which class-name-1
      *> consists"; 8.8.4.4.4 - the condition is true when the item consists entirely of them):
      *>  . HEX-N FOR NATIONAL: SR17 c3 - each noninteger literal is a NATIONAL literal. N"1F9" is in
      *>    0-9/A-F -> yes; N"1G" has G -> no.
      *>  . ORD-A FOR ALPHANUMERIC ... IN STD1: GR12 a - a numeric literal is the ordinal "within the
      *>    character set referenced by alphabet-name-4"; STANDARD-1 is ISO/IEC 646, whose 66th..68th
      *>    characters (one-based) are A, B, C. "CAB" -> yes; "CAD" -> no.  (SR17 b1 - STD1 defines an
      *>    alphanumeric set, so the IN phrase is legal here.)
      *>  . ORD-N FOR NATIONAL ... IN NAT: the national NATIVE set is UTF-16; ordinals 946..970 are
      *>    U+03B1..U+03C9 (alpha..omega).  N"(alpha)(omega)" -> yes; N"a(omega)" -> no.
      *>  . SPC IS SPACE "X": 8.3.3.6.4 GR1 - SPACE is the space character, so the class is {space, X}:
      *>    "X X" -> yes; "XYX" -> no.  (It used to be the class {S, P, A, C, E, X}.)
      *>  . ALLAB IS ALL "AB": 8.3.3.6.4 GR3 c - "the length of the string is the length of literal-1",
      *>    so the members are A and B: "ABBA" -> yes; "ABC " -> no.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB976CLFOR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET STD1 FOR ALPHANUMERIC IS STANDARD-1
           ALPHABET NAT FOR NATIONAL IS NATIVE
           CLASS HEX-N FOR NATIONAL IS N"0" THRU N"9" N"A" THRU N"F"
           CLASS ORD-A FOR ALPHANUMERIC IS 66 THRU 68 IN STD1
           CLASS ORD-N FOR NATIONAL IS 946 THRU 970 IN NAT
           CLASS SPC IS SPACE "X"
           CLASS ALLAB IS ALL "AB".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NH PIC N(3) VALUE N"1F9".
       01 NG PIC N(2) VALUE N"1G".
       01 A1 PIC X(3) VALUE "CAB".
       01 A2 PIC X(3) VALUE "CAD".
       01 G1 PIC N(2) VALUE NX"03B103C9".
       01 G2 PIC N(2) VALUE NX"006103C9".
       01 S1 PIC X(3) VALUE "X X".
       01 S2 PIC X(3) VALUE "XYX".
       01 L1 PIC X(4) VALUE "ABBA".
       01 L2 PIC X(4) VALUE "ABC ".
       PROCEDURE DIVISION.
       MAIN.
           IF NH IS HEX-N DISPLAY "NH-HEXN=YES" ELSE DISPLAY "NH-HEXN=NO" END-IF
           IF NG IS HEX-N DISPLAY "NG-HEXN=YES" ELSE DISPLAY "NG-HEXN=NO" END-IF
           IF A1 IS ORD-A DISPLAY "A1-ORDA=YES" ELSE DISPLAY "A1-ORDA=NO" END-IF
           IF A2 IS ORD-A DISPLAY "A2-ORDA=YES" ELSE DISPLAY "A2-ORDA=NO" END-IF
           IF G1 IS ORD-N DISPLAY "G1-ORDN=YES" ELSE DISPLAY "G1-ORDN=NO" END-IF
           IF G2 IS ORD-N DISPLAY "G2-ORDN=YES" ELSE DISPLAY "G2-ORDN=NO" END-IF
           IF S1 IS SPC DISPLAY "S1-SPC=YES" ELSE DISPLAY "S1-SPC=NO" END-IF
           IF S2 IS SPC DISPLAY "S2-SPC=YES" ELSE DISPLAY "S2-SPC=NO" END-IF
           IF L1 IS ALLAB DISPLAY "L1-ALLAB=YES" ELSE DISPLAY "L1-ALLAB=NO" END-IF
           IF L2 IS ALLAB DISPLAY "L2-ALLAB=YES" ELSE DISPLAY "L2-ALLAB=NO" END-IF
           STOP RUN.
