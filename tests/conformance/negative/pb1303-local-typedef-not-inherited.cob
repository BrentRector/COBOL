      *> reject-at: 2002 2014 2023
      *> kb/Work PB1303 - a type-name NOT described with a GLOBAL clause is a LOCAL name (8.4.6.2.2: "When a ... type-name
      *>   is not a global name, it is a local name"), so a contained program that names it has no declaration to find.
      *>   13.18.58.4 GR3 applies the GLOBAL clause to the scope of the type-name; without it the scope is the declaring
      *>   source element alone. The positive twin is conformance/2002/pb1303_global_typedef_inherited.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1303N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF.
          05 A PIC X(2) VALUE "GL".
       PROCEDURE DIVISION.
           CALL "INNER1303"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER1303.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R TYPE T.
       PROCEDURE DIVISION.
           DISPLAY A OF R
           GOBACK.
       END PROGRAM INNER1303.
       END PROGRAM PB1303N1.
