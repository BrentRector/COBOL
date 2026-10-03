      *> reject-at: 2014 2023
      *> 13.10.3 SR10: "Data-name-1 and data-name-2 shall not be described with the ANY LENGTH clause" - the
      *> data-name-1 (BYTE-LENGTH OF) half (kb/Work PB1227). The ANY LENGTH clause itself is legal in a contained
      *> program's linkage section, so the constant entry's rule is the only one broken.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1227OUTER.
       PROCEDURE DIVISION.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1227INNER.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       01 K CONSTANT AS BYTE-LENGTH OF L.
       PROCEDURE DIVISION USING L.
           GOBACK.
       END PROGRAM PB1227INNER.
       END PROGRAM PB1227OUTER.
