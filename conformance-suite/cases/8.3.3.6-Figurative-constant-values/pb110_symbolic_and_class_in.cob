*>--- conformance-case
*> id: pb110_symbolic_and_class_in
*> rules: [SR-8.3.3.6.3-4, GR-8.3.3.6.4-2, GR-8.3.3.6.4-3, SR-12.3.7.3-16, SR-12.3.7.3-17, GR-12.3.7.4-7, GR-12.3.7.4-11, GR-12.3.7.4-12]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7 SYMBOLIC CHARACTERS and CLASS ... IN alphabet-name (kb/Work PB110 - the clause was accepted-inert
      *> and the IN phrase of CLASS was silently ignored, building the class from NATIVE ordinals - a wrong answer).
      *> 12.3.7.4 GR11: symbolic-character-1 defines a FIGURATIVE CONSTANT whose value is the character at ordinal
      *> integer-1 in the native character set (b) or in the coded character set of the IN alphabet (GR11 b's "if the
      *> IN phrase is specified"); 8.3.3.6.2 Format 7 [ALL] symbolic-character-1; 8.3.3.6.4 GR2 the figurative fill,
      *> GR3 b one character in an unsized context. GR12 a: a numeric CLASS literal under IN is the ordinal within
      *> alphabet-name-4's character set - for REV ("Z" THRU "A") ordinal 1 is "Z", so BYORD = ordinals 1-3 = Z,Y,X.
      *>
      *> What each line proves:
      *>   VAL-ESC   - VALUE ESC: ordinal 28 of the native set = char 27 (ORD 28); the VALUE-clause figurative fill.
      *>   VAL-FILL3 - VALUE BELL on PIC X(3): position 3 also holds the character (GR2's repetition).
      *>   MOVE-FILL - MOVE BELL TO X(4): the receiver fills (GR2).
      *>   ALL-ZED   - MOVE ALL ZED (ZED IS 1 IN REV = "Z"): the explicit-ALL Format 7 fills with Z (ORD 91).
      *>   STRING-SC - STRING with a symbolic operand: ONE character (GR3 b).
      *>   BYORD-ZYX / BYORD-ABC - CLASS BYORD IS 1 THRU 3 IN REV: "ZYX" IS in the class, "ABC" is NOT (the native
      *>               ordinals 1-3 would be NUL/SOH/STX - the old wrong answer made neither true; a class from REV's
      *>               ordinals makes exactly one true).
      *>   EQ-ESC    - a relation against the figurative.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB110SYM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS BYORD IS 1 THRU 3 IN REV
           ALPHABET REV IS "Z" THRU "A"
           SYMBOLIC CHARACTERS ESC BELL ARE 28 8
           SYMBOLIC CHARACTERS ZED IS 1 IN REV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  Y               PIC X VALUE ESC.
       01  F3              PIC X(3) VALUE BELL.
       01  ZYX             PIC X(3) VALUE "ZYX".
       01  ABC             PIC X(3) VALUE "ABC".
       01  S               PIC X(4).
       01  N               PIC 9(5).
       PROCEDURE DIVISION.
           MOVE FUNCTION ORD(Y) TO N
           DISPLAY "VAL-ESC=" N
           MOVE FUNCTION ORD(F3(3:1)) TO N
           DISPLAY "VAL-FILL3=" N
           MOVE BELL TO S
           MOVE FUNCTION ORD(S(4:1)) TO N
           DISPLAY "MOVE-FILL=" N
           MOVE ALL ZED TO S
           MOVE FUNCTION ORD(S(2:1)) TO N
           DISPLAY "ALL-ZED=" N
           MOVE SPACES TO S
           STRING "A" ZED DELIMITED BY SIZE INTO S
           MOVE FUNCTION ORD(S(2:1)) TO N
           DISPLAY "STRING-SC=" N
           IF ZYX IS BYORD DISPLAY "BYORD-ZYX=yes" ELSE DISPLAY "BYORD-ZYX=no" END-IF
           IF ABC IS BYORD DISPLAY "BYORD-ABC=yes" ELSE DISPLAY "BYORD-ABC=no" END-IF
           IF Y = ESC DISPLAY "EQ-ESC=yes" ELSE DISPLAY "EQ-ESC=no" END-IF
           STOP RUN.
