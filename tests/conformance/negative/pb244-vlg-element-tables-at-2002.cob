      *> reject-at: 85 2002
      *> kb/Work PB244 shape (b) - a variable-length group whose OCCURS DEPENDING
      *> or dynamic-capacity table holds variable-length ELEMENTS is a COBOL-2014
      *> program: the DYNAMIC LENGTH clause that makes each occurrence a
      *> variable-length group (ISO 8.5.1.12.1, 13.18.19) and the OCCURS DYNAMIC
      *> table of 8.5.1.9 are 2014 additions, so editions 85 and 2002 reject it
      *> with the edition-band diagnostic COBOLNET0900 BEFORE the group's display
      *> format (14.9.11.4 GR7) is ever in question.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244NEE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 VALUE 2.
       01 G1.
          05 TE OCCURS 1 TO 3 DEPENDING ON K.
             10 DD PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX PIC X.
       01 G2.
          05 TD OCCURS DYNAMIC CAPACITY IN CAP FROM 1.
             10 DD2 PIC X DYNAMIC LENGTH LIMIT 5.
       PROCEDURE DIVISION.
           DISPLAY "[" G1 "][" G2 "]"
           STOP RUN.
