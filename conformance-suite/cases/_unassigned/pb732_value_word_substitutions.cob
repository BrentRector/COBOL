*>--- conformance-case
*> id: pb732_value_word_substitutions
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB732 POSITIVE TWIN — every word spelling a VALUE literal position DOES admit, so the
*> literal-position screen that closed PB732 (a word that is neither a constant-name nor a
*> symbolic-character draws COBOLNET1639) cannot be tightened into a rejection of legal source.
*>   K-NUM / K-TXT  ISO 13.10.3 SR2: "constant-name-1 may be used anywhere that a format specifies a
*>                  literal of the class and category of constant-name-1"; 13.10.4 GR1 substitutes it
*>                  "as if [the] literal were written".
*>   SYM-A          ISO 8.3.3.6.2 Format 7 (symbolic-character-1) + 8.3.3.6.3 SR4; 12.3.7.4 GR11 makes
*>                  integer-1 the ORDINAL POSITION in the native character set, so 66 is 'A'.
*>   ALL SYM-A      8.3.3.6.4 GR2: the one-character string repeated to the receiving size -> "AAA".
*>   ALL "AB"       same GR2 over literal-1 -> "ABAB" in four character positions.
*>   SPACES         8.3.3.6.3 SR1 (a figurative constant stands wherever 'literal' appears) + GR2.
*>   -12            8.3.3.3.2 rule 2 - the written sign is part of the numeric literal, not an operator.
*>   WHEN SET TO FALSE IS literal-4 - Format 3's SECOND literal position (13.18.63.2 format 3). It is a
*>                  literal position like every other, so 13.10.3 SR2 (constant-name, here K-ZERO) and
*>                  8.3.3.6.2 format 7 (symbolic-character, here SYM-A) both stand there; the grammar
*>                  used to write it as `literal` and refused BOTH with COBOL0001 + COBOL0309, which is
*>                  what this arm pins (kb/Work PB732). 13.18.63.3 SR27 - "The value of literal-4 shall
*>                  not be equal to the value of any occurrence of literal-2" - is satisfied by
*>                  construction: 0 against 7, and "A" against "B".
*>                  The VALUES themselves are unobservable here on purpose: SET condition-name TO FALSE
*>                  (14.9.39 SR7) is a declared not-implemented feature (COBOLNET1756 + a runtime
*>                  abort), so this program never writes one. What it proves is that the conforming
*>                  spellings COMPILE and the condition-names still evaluate by their literal-2 values.
*> Formats exercised: 1 (data-item), 2 (table, with its FROM/TO subscripts) and 3 (condition-name,
*> singleton, THROUGH, and the WHEN SET TO FALSE phrase).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB732POS.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    SYMBOLIC CHARACTERS SYM-A IS 66.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 K-NUM CONSTANT AS 7.
01 K-TXT CONSTANT AS "HI".
01 K-ZERO CONSTANT AS 0.
01 P1 PIC 9 VALUE K-NUM.
01 P2 PIC X(2) VALUE K-TXT.
01 P3 PIC X(3) VALUE ALL SYM-A.
01 P4 PIC X VALUE SYM-A.
01 P5 PIC 9 VALUE 7.
   88 P5-IS-K VALUE K-NUM WHEN SET TO FALSE IS K-ZERO.
   88 P5-IN-RANGE VALUE 1 THRU K-NUM.
01 P6 PIC S9(3) VALUE -12.
01 P7 PIC X(4) VALUE ALL "AB".
01 P8 PIC X(4) VALUE SPACES.
01 P9 PIC X VALUE "A".
   88 P9-IS-B VALUE "B" WHEN SET TO FALSE IS SYM-A.
01 TBL.
   05 T PIC X(2) OCCURS 3 TIMES VALUE K-TXT FROM (1) TO (3).
PROCEDURE DIVISION.
    DISPLAY "P1=" P1.
    DISPLAY "P2=" P2.
    DISPLAY "P3=" P3.
    DISPLAY "P4=" P4.
    IF P5-IS-K DISPLAY "P5K=Y" ELSE DISPLAY "P5K=N" END-IF.
    IF P5-IN-RANGE DISPLAY "P5R=Y" ELSE DISPLAY "P5R=N" END-IF.
    IF P6 = -12 DISPLAY "P6=OK" ELSE DISPLAY "P6=BAD" END-IF.
    DISPLAY "P7=" P7.
    DISPLAY "P8=[" P8 "]".
    DISPLAY "T1=" T(1) " T3=" T(3).
    IF P9-IS-B DISPLAY "P9B=Y" ELSE DISPLAY "P9B=N" END-IF.
    STOP RUN.
