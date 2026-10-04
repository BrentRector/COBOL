      *> kb/Work PB1402 - the COBOL 2023 Annex B and Annex C (ISO 8.1.3.2 GR4; 8.3.2.1). U+0131 and
      *> U+03C2 no longer fold (Annex E.2 item 14), so each names a word of its own; E.3.3 items 5 and 6
      *> add the capital sharp s, Adlam and the combining marks; E.3.3 item 4 lets the Kelvin sign start
      *> a word; U+30FB is medial only (E.2 item 4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1402XM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ıTEM            PIC X    VALUE "D".
       01 ITEM            PIC X    VALUE "I".
       01 Xς              PIC X    VALUE "F".
       01 XΣ              PIC X    VALUE "S".
       01 GRÖẞE           PIC X(3) VALUE "BIG".
       01 𞤀𞤁            PIC X    VALUE "A".
       01 CAFÉ           PIC X    VALUE "C".
       01 K-WERT          PIC X    VALUE "K".
       01 A・B            PIC X    VALUE "M".
       PROCEDURE DIVISION.
       ANFANG.
           DISPLAY ıtem item.
           DISPLAY xς xσ.
           DISPLAY größe.
           DISPLAY 𞤢𞤣.
           DISPLAY café.
           DISPLAY k-wert.
           DISPLAY a・b.
           STOP RUN.
       END PROGRAM PB1402XM.
