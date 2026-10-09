      *> reject-at: 85 2002
      *> kb/Work PB1951 (sibling) -- the Format-2 table SORT of an EXTERNAL table whose element
      *> holds variable-length components, below the edition that introduced them: the
      *> DYNAMIC LENGTH clause (ISO 13.18.19) and the dynamic-capacity table (13.18.38 Format 4)
      *> are COBOL-2014, so a COBOL-2002 program cannot describe the element (COBOLNET0900); the
      *> 2014 positive is conformance:2014/pb1951_table_sort_external_variable_element.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1951N02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BR EXTERNAL.
          05 E OCCURS 3.
             10 EK PIC 9.
             10 ED1 PIC X DYNAMIC LENGTH.
             10 EP PIC X OCCURS DYNAMIC CAPACITY IN ECAP.
       PROCEDURE DIVISION.
       MAIN.
           SORT E ASCENDING KEY EK
           STOP RUN.
