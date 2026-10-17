*>--- conformance-case
*> id: pb4_hex_literal_value
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4 - a HEXADECIMAL literal is an ALPHANUMERIC literal (ISO 8.3.3.2: "each pair of hexadecimal
      *> digits represents a single character"), so every position that accepts an alphanumeric literal
      *> accepts this form. Five sites did not decode it, and each failed differently:
      *>   VALUE X"4142"           stored the literal's SOURCE TEXT truncated to the picture -> [X"]
      *>   VALUE ALL X"41"         stored [ALLX]
      *>   OCCURS ... VALUE X"..." same as VALUE
      *>   88 ... VALUE X"4142"    the condition never matched
      *>   MOVE ALL X"41"          parsed, then died at RUN time on 'figurative constant ALLX"41"'
      *> while MOVE X"4142" decoded correctly all along - so the data division and the procedure division
      *> disagreed about what the same literal meant. Root cause: the prefix-letter list was written down
      *> twice in CobolLiteral and BOTH copies omitted X.
      *>
      *> Expected values are 8.3.3.2 arithmetic: X"41" = 'A', X"42" = 'B', X"4142" = "AB".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4HEXVALUE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V   PIC X(2) VALUE X"4142".
          88 IS-AB    VALUE X"4142".
          88 IS-CD    VALUE X"4344".
       01 A   PIC X(4) VALUE ALL X"41".
       01 T.
          05 E OCCURS 2 PIC X(2) VALUE X"4142".
       01 M   PIC X(2).
       01 W   PIC X(3) VALUE "XYZ".
       PROCEDURE DIVISION.
           DISPLAY V
           DISPLAY A
           DISPLAY E(1) E(2)
           IF IS-AB DISPLAY "AB-TRUE" ELSE DISPLAY "AB-FALSE" END-IF
           IF IS-CD DISPLAY "CD-TRUE" ELSE DISPLAY "CD-FALSE" END-IF
           MOVE X"4142" TO M
           DISPLAY M
           MOVE ALL X"41" TO M
           DISPLAY M
           DISPLAY W
           STOP RUN.
