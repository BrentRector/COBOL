      *> reject-at: 2014
      *> kb/Work PB1951 -- the Format-2 table SORT of an EXTERNAL table whose element holds a
      *> dynamic-capacity POINTER table, below the edition that admits its description: USAGE
      *> POINTER below level 1 needs a STRONG type declaration (ISO 13.18.60.3 SR14), and an
      *> EXTERNAL record's strongly-typed declaration must itself be EXTERNAL (13.18.22.3 SR5),
      *> a COBOL-2023 description (COBOLNET0900); the 2023 positive is
      *> conformance:2023/pb1951_table_sort_external_pointer_dynamic_table.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1951N14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-TAB TYPEDEF STRONG EXTERNAL.
          05 E OCCURS 3.
             10 EK PIC 9.
             10 EP USAGE POINTER OCCURS DYNAMIC CAPACITY IN ECAP.
       01 BR TYPE T-TAB EXTERNAL.
       PROCEDURE DIVISION.
       MAIN.
           SORT E ASCENDING KEY EK
           STOP RUN.
