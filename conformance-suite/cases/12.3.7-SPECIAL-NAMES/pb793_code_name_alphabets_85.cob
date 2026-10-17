      *>--- conformance-case
      *> id: pb793_code_name_alphabets_85
      *> rules: [SR-12.3.7.3-15, GR-12.3.7.4-7, DOC-A.1-183, DOC-A.1-184]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 12.3.7.3 SR15 / 12.3.7.4 GR7 i AT COBOL-85. The ALPHABET clause's code-name operand is not a
      *> post-85 feature - it is the "implementor-name" alternative the 1985 standard already spelled - so
      *> the code-name set kb/Work PB793 decided (ASCII and EBCDIC) is available in EVERY edition and this
      *> golden is the 85 half of the four-edition claim. Nothing here is later than 85: no intrinsic
      *> function, no in-line PERFORM, no explicit scope terminator on a relation.
      *>
      *> EXPECTED VALUES, from the same two determinations the 2023 golden derives (CONFORMANCE.md
      *> DOC-A.1-183): under the EBCDIC (CCSID 37) collating sequence "a" is at X'81', "A" at X'C1' and "0"
      *> at X'F0', so "a" < "A" and "A" < "0" - both the reverse of the native answer. A-ASC is declared and
      *> referenced as the SORT-free control: 12.3.7.4 GR7 c's ISO/IEC 646 IRV order IS the native order, so
      *> a PROGRAM COLLATING SEQUENCE naming it must leave "A" < "0" FALSE, which the second (sibling, not
      *> contained - 12.3.7.4 GR1 would otherwise pass the containing unit's SPECIAL-NAMES down) unit shows.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB793CN85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XCOMP PROGRAM COLLATING SEQUENCE IS A-EBC.
       SPECIAL-NAMES.
           ALPHABET A-EBC IS EBCDIC.
       PROCEDURE DIVISION.
       MAIN-85.
           IF "a" < "A"
               DISPLAY "EBC a-LT-A=Y"
           ELSE
               DISPLAY "EBC a-LT-A=N".
           IF "A" < "0"
               DISPLAY "EBC A-LT-0=Y"
           ELSE
               DISPLAY "EBC A-LT-0=N".
           CALL "PB793CN85ASC".
           STOP RUN.
       END PROGRAM PB793CN85.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB793CN85ASC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XCOMP PROGRAM COLLATING SEQUENCE IS A-ASC.
       SPECIAL-NAMES.
           ALPHABET A-ASC IS ASCII.
       PROCEDURE DIVISION.
       MAIN-ASC.
           IF "a" < "A"
               DISPLAY "ASC a-LT-A=Y"
           ELSE
               DISPLAY "ASC a-LT-A=N".
           IF "A" < "0"
               DISPLAY "ASC A-LT-0=Y"
           ELSE
               DISPLAY "ASC A-LT-0=N".
           EXIT PROGRAM.
       END PROGRAM PB793CN85ASC.
