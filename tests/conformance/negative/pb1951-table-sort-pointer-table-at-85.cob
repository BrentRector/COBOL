      *> reject-at: 85
      *> kb/Work PB1951 -- the Format-2 table SORT of a BASED table whose element holds a
      *> pointer TABLE (an inner OCCURS of USAGE POINTER), below the edition that introduced
      *> it: the in-place table sort (ISO 14.9.40 Format 2), the TYPEDEF STRONG declaration
      *> that admits a pointer below level 1 (13.18.60.3 SR14), the BASED clause and
      *> ALLOCATE are all COBOL-2002. The witness is the SORT's own edition gate
      *> (COBOLNET0870); the 2002 positive is conformance:2002/pb1951_table_sort_pointer_table_element.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1951N85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-TAB IS TYPEDEF STRONG.
          05 E OCCURS 3.
             10 EK PIC 9.
             10 EP USAGE POINTER OCCURS 2.
       01 BR TYPE T-TAB BASED.
       PROCEDURE DIVISION.
       MAIN.
           ALLOCATE BR
           SORT E ASCENDING KEY EK
           STOP RUN.
