*>--- conformance-case
*> id: pb1227-constant-length-any-length
*> rules: [SR-13.10.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> 13.10.3 SR10: "Data-name-1 and data-name-2 shall not be described with the ANY LENGTH clause" - the
      *> data-name-2 (LENGTH OF) half (kb/Work PB1227). The ANY LENGTH clause itself is legal in a contained
      *> program's linkage section, so the constant entry's rule is the only one broken.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1227LOUTER.
       PROCEDURE DIVISION.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1227LINNER.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       01 K CONSTANT AS LENGTH OF L.
       PROCEDURE DIVISION USING L.
           GOBACK.
       END PROGRAM PB1227LINNER.
       END PROGRAM PB1227LOUTER.
