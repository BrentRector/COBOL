      *> reject-at: 2002 2014 2023
      *> 7.3.11.4 GR2: following a DEFINE with the OFF phrase the name "shall not be used except in a defined
      *> condition"; 13.10.3 SR8: compilation-variable-name-1 "shall be a compilation-variable-name for which the
      *> defined condition is currently true" - FROM X after >>DEFINE X OFF is refused (kb/Work PB1368).
       >>DEFINE X AS 1
       >>DEFINE X OFF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1368OFF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT FROM X.
       PROCEDURE DIVISION.
           STOP RUN.
