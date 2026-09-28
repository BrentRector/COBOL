      *> reject-at: 2014 2023
      *> kb/Work PB1284 — ISO/IEC 1989:2023 §13.18.45.3 SR8: none of the items within the range "shall be ... a
      *> variable-length data item", and §8.5.1.11.1: "The term variable-length data item refers to either a
      *> dynamic-capacity table or a dynamic-length elementary item." D lies between A and C. Before the fix the
      *> alias was tiled over the fixed leaves only and displayed ABCD, silently dropping D's content.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1284RD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 A PIC X(2) VALUE "AB".
          05 D PIC X DYNAMIC LENGTH.
          05 C PIC X(2) VALUE "CD".
       66 X1 RENAMES A THRU C.
       PROCEDURE DIVISION.
           MOVE "XYZ" TO D.
           DISPLAY "[" X1 "]".
           STOP RUN.
