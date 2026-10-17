*>--- conformance-case
*> id: pb120_when_compiled_contained
*> rules: [RV-15.99.3-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB120 — WHEN-COMPILED across a contained source unit. ISO 15.99.3 r2: "The returned value in
      *> a contained source unit is the compilation date and time associated with the compilation unit in
      *> which it is contained" — the container and its contained program must bake the SAME stamp (one
      *> capture per compilation). The defect was invisible here and live in a long-lived compiler process,
      *> where every later compilation inherited the FIRST one's stamp (the unit tests pin that half); this
      *> golden pins the sharing plus 15.99.3 r1's shape: positions 1-16 numeric (year/month/day/time), the
      *> year plausible, and position 17 one of '+', '-', '0'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB120WC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WC-OUTER PIC X(21) GLOBAL.
       01 WC-INNER PIC X(21) GLOBAL.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION WHEN-COMPILED TO WC-OUTER
           CALL "PB120IN"
           IF WC-OUTER = WC-INNER
               DISPLAY "SAME OK" ELSE DISPLAY "SAME BAD" END-IF
           IF WC-OUTER (1:16) IS NUMERIC
               DISPLAY "NUM OK" ELSE DISPLAY "NUM BAD " WC-OUTER END-IF
           IF WC-OUTER (1:4) >= "2026" AND WC-OUTER (1:4) <= "2199"
               DISPLAY "YEAR OK" ELSE DISPLAY "YEAR BAD" END-IF
           IF WC-OUTER (5:2) >= "01" AND WC-OUTER (5:2) <= "12"
               DISPLAY "MONTH OK" ELSE DISPLAY "MONTH BAD" END-IF
           EVALUATE WC-OUTER (17:1)
               WHEN "+" WHEN "-" WHEN "0" DISPLAY "TZ OK"
               WHEN OTHER DISPLAY "TZ BAD"
           END-EVALUATE
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB120IN.
       PROCEDURE DIVISION.
       MAIN-IN.
           MOVE FUNCTION WHEN-COMPILED TO WC-INNER
           EXIT PROGRAM.
       END PROGRAM PB120IN.
       END PROGRAM PB120WC.
