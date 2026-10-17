      *>--- conformance-case
      *> id: pb3154_contained_symbolic_character
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3154 (sweep) - a contained program inherits its
      *>   container's SYMBOLIC CHARACTERS clause.
      *> ISO 12.3.4 GR1: "The entries explicitly or implicitly
      *>   specified in the configuration section of a source unit that
      *>   contains other source units apply to each directly or
      *>   indirectly contained source unit" -> OK 12.3.4; 8.4.6.1 lists
      *>   symbolic-character among the names that, "when they are
      *>   declared within a configuration section, may be referenced
      *>   only by statements and entries either in that source element
      *>   that contains a configuration section or in any source
      *>   element contained within that source element" -> OK 8.4.6.1.
      *> The contained program PB3154I may not have a configuration
      *>   section of its own (12.3.3 SR1), so SYM-B is its container's.
      *>   It was COBOLNET1639 "not defined" in PB3154I before.
      *> Expected values: SYM-B is the character at ordinal 67 of the
      *>   native alphabet (12.3.7.4 GR11), X"42" = "B", in both
      *>   programs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3154O.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-B IS 67.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE SYM-B TO X.
           DISPLAY "OUTER " X.
           CALL "PB3154I".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3154I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y PIC X.
       PROCEDURE DIVISION.
       INNER-PARA.
           MOVE SYM-B TO Y.
           DISPLAY "INNER " Y.
       END PROGRAM PB3154I.
       END PROGRAM PB3154O.
