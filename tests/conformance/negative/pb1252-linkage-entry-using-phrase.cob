      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1252 - ISO 13.7.2 general format: LINKAGE SECTION. followed by 77-level-description-entry,
      *> constant-entry, record-description-entry or type-declaration-entry, and nothing else.
      *> cite.py: OK 13.7.2 (General format) "type-declaration-entry". No edition defines a linkage entry with a
      *> USING phrase; the grammar used to parse one at 2002+ and every binder dropped it, so this compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W1020HPB1252LKUSING.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC 9(4) VALUE 12.
       LINKAGE SECTION.
       01 L-X USING BY VALUE W-A PIC 9(4).
       PROCEDURE DIVISION.
           DISPLAY "A=" W-A.
           STOP RUN.
