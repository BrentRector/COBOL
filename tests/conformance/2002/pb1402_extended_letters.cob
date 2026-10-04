      *> kb/Work PB1402 - extended letters in COBOL words (ISO 8.3.2.1; 8.1.3.2 GR4; Annex B; Annex A.4.6).
      *> COBOL 2002 introduced them: data-, condition-, paragraph- and program-names hold Annex B letters,
      *> and an uppercase letter folds to its Annex C lowercase (8.1.3.2 GR4 b), so CAFE-acute and cafe-acute
      *> are one word. Below 2023 Annex C also maps U+0131 to i and U+03C2 to U+03C3 (deleted at 2023 by
      *> Annex E.2 item 14), and U+30FB may end a word (made medial at 2023, E.2 item 4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1402XL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ZÄHLER          PIC 9(2) VALUE 0.
          88 ÜBER-FÜNF    VALUE 6 THRU 99.
       01 CAFÉ            PIC X(5) VALUE "CREME".
       01 ΠΟΣΌ            PIC 9(3) VALUE 42.
       01 ДАННЫЕ          PIC X(3) VALUE "ABC".
       01 ıTEM-A          PIC X    VALUE "Q".
       01 WORTς           PIC X    VALUE "S".
       01 ABC・           PIC X(3) VALUE "MXA".
       PROCEDURE DIVISION.
       ANFANG.
           PERFORM SCHRITT-É 7 TIMES.
           IF über-fünf DISPLAY "ZAEHLER " zähler END-IF.
           DISPLAY café.
           DISPLAY ποσό.
           DISPLAY данные.
           DISPLAY ITEM-A.
           DISPLAY wortσ.
           DISPLAY abc・.
           CALL "prüfung" USING café.
           STOP RUN.
       SCHRITT-É.
           ADD 1 TO Zähler.
       END PROGRAM PB1402XL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PRÜFUNG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TEXT-É PIC X(5).
       PROCEDURE DIVISION USING text-é.
           DISPLAY "PRUEFUNG " TEXT-É.
           GOBACK.
       END PROGRAM PRÜFUNG.
