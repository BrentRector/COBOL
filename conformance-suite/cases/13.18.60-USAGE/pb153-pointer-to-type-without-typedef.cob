      *>--- conformance-case
      *> id: pb153-pointer-to-type-without-typedef
      *> rules: [SR-13.18.60.3-18]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.60.3 SR18: "If type-name-1 is specified, the
      *> TYPEDEF clause shall be specified for the subject of the
      *> entry." SO THE OBVIOUS SPELLING OF A RESTRICTED POINTER IS
      *> ITSELF NONCONFORMING - it must be declared as a TYPE
      *> DECLARATION and reached through a TYPE clause. The legal
      *> shape is pinned by the positive golden
      *> conformance-suite/cases/13.18.60-USAGE/pb153_restricted_pointer.
      *> This exact illegal spelling had reached the corpus: the
      *> version-matrix row usage-pointer-to-type-2014 carried it as
      *> the construct's canonical witness, unexercised and therefore
      *> never contradicted. kb/Work PB153.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB153N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TPT TYPEDEF STRONG.
          05 F PIC 9(4).
       01 P USAGE POINTER TO TPT.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
