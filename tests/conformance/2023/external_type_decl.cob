       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXTTYPEDECL.
      *> VCR 63 — an EXTERNAL type declaration (the EXTERNAL clause on a
      *> level-1 TYPEDEF entry) is a COBOL-2023 addition (ISO 13.18.22.3
      *> SR1/SR5; 8.5.3; Annex E.3 item 10). SR5 forces a strongly-typed
      *> external record's type to itself be an external type declaration,
      *> so a strong external item (R) references the external type (T).
      *> Below 2023 the co-occurrence is rejected (COBOLNET0900) — that
      *> below-edition leg is asserted by the version matrix.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF STRONG IS EXTERNAL.
          05 A PIC X(4).
       01 R TYPE T IS EXTERNAL.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABCD" TO A OF R.
           DISPLAY A OF R.
           STOP RUN.
