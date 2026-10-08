      *> kb/Work PB845. COBOL-85's 8.9 list does not reserve NATIONAL or
      *> BIT (both were added in COBOL-2002), so at this edition each is
      *> an ordinary user-defined word, in every slot that names
      *> something. Since PB845 the two words are reservation-gated (a
      *> NAME only where 8.9 leaves them free), and these two programs
      *> are the witness that the gate frees them at 85 in each slot: a
      *> data-name and an index-name in the first, a class-name and a
      *> mnemonic-name in the second (8.3.2.2 lets a word name only one
      *> type of thing per source element, hence two programs). The
      *> 2002 negatives hold the other side, ISO 8.3.2.1 1): "Reserved
      *> words shall not be used as user-defined words".
      *> EXPECTED, by the rule: A / B2 / I2 / CLASS TRUE / M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB845B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIT PIC X VALUE "A".
       01 T.
          05 E PIC X(2) OCCURS 3 INDEXED BY NATIONAL.
       PROCEDURE DIVISION.
           DISPLAY BIT
           MOVE "B2" TO E (2)
           SET NATIONAL TO 2
           DISPLAY E (NATIONAL)
           SET NATIONAL UP BY 1
           MOVE "I2" TO E (NATIONAL)
           DISPLAY E (3)
           CALL "PB845C"
           STOP RUN.
       END PROGRAM PB845B.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB845C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSOUT IS NATIONAL
           CLASS BIT IS "A" THRU "C".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y PIC X VALUE "B".
       PROCEDURE DIVISION.
           IF Y BIT
               DISPLAY "CLASS TRUE"
           ELSE
               DISPLAY "CLASS FALSE"
           END-IF
           DISPLAY "M" UPON NATIONAL
           EXIT PROGRAM.
       END PROGRAM PB845C.
