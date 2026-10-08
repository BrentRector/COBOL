*> reject-at: 2002 2014 2023
*> ISO 14.9.4.3 SR23 - "If literal-2 or its corresponding formal parameter is specified with the BY VALUE phrase,
*> literal-2 shall be a numeric literal." A symbolic-character is literal-2 - it "defines a figurative constant"
*> (12.3.7.4 GR11 a)) - and a figurative constant other than ZERO is never numeric (8.3.3.6.3 SR1 a)), so BY VALUE SA
*> is refused BY NAME, COBOLNET1762 (kb/Work PB1544: it was "not defined").
IDENTIFICATION DIVISION.
PROGRAM-ID. NEG1544A.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    SYMBOLIC CHARACTERS SA IS 66.
PROCEDURE DIVISION.
MAIN.
    CALL "NEG1544S" AS NESTED USING BY VALUE SA.
    STOP RUN.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEG1544S.
DATA DIVISION.
LINKAGE SECTION.
01 LV PIC 9(4).
PROCEDURE DIVISION USING BY VALUE LV.
P.
    GOBACK.
END PROGRAM NEG1544S.
END PROGRAM NEG1544A.
