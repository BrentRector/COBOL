      *> kb/Work PB533 - SR8's basic letters, the POSITIVE control for the extended-letter negative. ISO
      *> 1989:2023 13.18.40.3 SR8: "Character-1 shall be any basic letter in the COBOL character set except
      *> those specified in a CURRENCY-SIGN clause or a basic letter character A, B, C, D, E, N, P, R, S, V, X,
      *> Z or their lowercase equivalents." 8.1.3.1 Table 1: the basic letters are A-Z and a-z. So an UPPERCASE
      *> basic letter outside the excluded set (T) and a LOWERCASE one (g) are both legal character-1, and the
      *> predicate that turns an extended letter away (CobolCharacterRepertoire.IsBasicLetter) must not turn
      *> either of these away. 13.18.40.5 rule 3: character-1 is a simple
      *> insertion editing symbol, so its literal is inserted at its
      *> position: 1230 into 99T99 reads 12:30, and into 99g99 with the
      *> literal "/" it reads 12/30.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB533POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W1 PIC 99T99 EDITING T IS ":".
       01 W2 PIC 99g99 EDITING g IS "/".
       PROCEDURE DIVISION.
           MOVE 1230 TO W1 W2.
           DISPLAY "W1=[" W1 "] W2=[" W2 "]".
           STOP RUN.
