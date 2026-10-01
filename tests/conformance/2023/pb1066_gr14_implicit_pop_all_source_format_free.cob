>>SOURCE FORMAT IS FREE
*> kb/Work PB1066 (the SOURCE FORMAT half), the FREE-to-FIXED direction and THE DECISION THE RESTORED FORMAT
*> GOVERNS THE END-PERFORM LINE ITSELF. 14.9.28.4 GR14: "Immediately preceding the END PERFORM phrase, there is
*> an implicit POP ALL". A reference format is a per-line property (6.5), so the POP is a boundary BEFORE the
*> physical line that holds the phrase, and 7.3.24.3 GR1 makes a format govern "the source text ... following the
*> directive": the END-PERFORM phrase follows the POP, so it is read in the RESTORED format.
*>
*> THE PROGRAM is free form (the directive above, which 7.3.24.3 GR4 allows as the first line of either form). The
*> handler switches to FIXED and its statement is written in fixed form (column 12). END-PERFORM is written at
*> COLUMN 1, which only free form reads as a word - in fixed form its first six characters would be a sequence
*> area and its seventh an indicator. It compiles only if the POP restored FREE before that line.
*>
*> EXPECTED OUTPUT, DERIVED: the STRING into a 3-character item overflows, so the handler runs and prints H1;
*> END-PERFORM then closes the construct and L1 prints, both read in free form.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1066SF2.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-D PIC X(3).
PROCEDURE DIVISION.
M-1.
PERFORM
    STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
WHEN EC-OVERFLOW-STRING
>>SOURCE FORMAT IS FIXED
           DISPLAY "H1"
END-PERFORM
DISPLAY "L1 FREE-AFTER".
STOP RUN.
