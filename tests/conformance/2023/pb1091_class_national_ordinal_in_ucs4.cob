      *> kb/Work PB1091 - the legal neighbours of pb1091-class-supplementary-through (the negative).
      *> ISO 12.3.7.4 GR12 a): "When the literal is numeric, the ordinal number of a character within the relevant
      *> native character set, or, when the IN phrase is specified, within the character set referenced by
      *> alphabet-name-4." GR7 f: the UCS-4 set is ISO/IEC 10646's, so ordinal n names the scalar value n - 1 for
      *> every BMP character below the surrogate block: ordinal 66 is U+0041 'A' and ordinal 91 is U+005A 'Z'.
      *> 12.3.7.3 SR17 c2 is the only rule of a NUMERIC operand - each ordinal exists in the IN set - and SR17 c4
      *> ("Each national literal, when a THROUGH phrase is specified, shall be one character in length") speaks of
      *> national LITERALS, so `66 THRU 91 IN U4` is legal and is exactly the national letters A through Z.
      *> A supplementary ordinal outside a THROUGH phrase (65537 = U+10800) is legal too: its rule is c2's range.
      *> EXPECTED, DERIVED: N1 = "AB" (U+0041, U+0042) lies in U+0041..U+005A          => N1=YES
      *>                    N2 = "Ab" (U+0041, U+0062) - 'b' is U+0062 = ordinal 99 > 91 => N2=NO
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1091G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET U4 FOR NATIONAL IS UCS-4
           CLASS UPPER-ORD FOR NATIONAL IS 66 THRU 91 IN U4
           CLASS SUP-ORD FOR NATIONAL IS 65537 IN U4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC N(2) USAGE NATIONAL VALUE N"AB".
       01 N2 PIC N(2) USAGE NATIONAL VALUE N"Ab".
       PROCEDURE DIVISION.
       MAIN.
           IF N1 IS UPPER-ORD
               DISPLAY "N1=YES"
           ELSE
               DISPLAY "N1=NO"
           END-IF
           IF N2 IS UPPER-ORD
               DISPLAY "N2=YES"
           ELSE
               DISPLAY "N2=NO"
           END-IF
           STOP RUN.
