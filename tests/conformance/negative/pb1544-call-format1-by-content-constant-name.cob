*> reject-at: 2002 2014 2023
*> ISO 14.9.4.2 Format 1 (no AS phrase): BY CONTENT admits identifier-2 only; literal-2 belongs to the
*> program-prototype Format 2. A constant-name is literal-2 ("as if literal-1 ... were written where constant-name-1
*> is written", 13.10.4 GR1), so BY CONTENT KN on a Format-1 CALL is refused exactly as BY CONTENT 7 is
*> (kb/Work PB1544: the constant-name compiled clean as an "identifier").
IDENTIFICATION DIVISION.
PROGRAM-ID. NEG1544F.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 KN CONSTANT AS 7.
PROCEDURE DIVISION.
MAIN.
    CALL "NEG1544G" USING BY CONTENT KN.
    STOP RUN.
