*> reject-at: 2002 2014 2023
*> ISO 14.9.4.3 SR23 - "If literal-2 or its corresponding formal parameter is specified with the BY VALUE phrase,
*> literal-2 shall be a numeric literal." A constant-name is literal-2: "the effect of specifying constant-name-1 in
*> other than this entry is as if literal-1 ... were written where constant-name-1 is written" (13.10.4 GR1), so
*> BY VALUE KT, KT standing for "Q", is refused BY NAME, COBOLNET1762 (kb/Work PB1544: it was COBOLNET0844, the
*> arithmetic-expression refusal of a different rule).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEG1544B.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 KT CONSTANT AS "Q".
PROCEDURE DIVISION.
MAIN.
    CALL "NEG1544T" AS NESTED USING BY VALUE KT.
    STOP RUN.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEG1544T.
DATA DIVISION.
LINKAGE SECTION.
01 LV PIC 9(4).
PROCEDURE DIVISION USING BY VALUE LV.
P.
    GOBACK.
END PROGRAM NEG1544T.
END PROGRAM NEG1544B.
