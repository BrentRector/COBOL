*>--- conformance-case
*> id: pb393-move-varlen-to-elementary
*> rules: [SR-14.9.25.3-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB393. ISO 1989:2023 14.9.25.3 SR9: "If identifier-1 or identifier-2 references a
      *> variable-length group then these groups shall be compatible groups as specified in 8.5.1.12,
      *> Variable-length groups." 8.5.1.12.1 states the prohibition over the OTHER operand - a variable-length
      *> group "is not equivalent to an alphanumeric data item and may not undergo a comparison or a move
      *> operation, in either direction, explicitly or otherwise, unless the other operand is a compatible
      *> group" - so an ELEMENTARY receiving item can never be that other operand, whatever its size.
      *> Without this screen the program compiled clean and aborted the run unit inside a Tier-C image guard:
      *> the same hole's other rim, where a COMPATIBLE program also aborted.
      *> The DYNAMIC LENGTH clause is COBOL-2014, so the shape cannot be written below that edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB393NEGVLE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 VG1.
          05 VA PIC X(3).
          05 VD PIC X DYNAMIC LENGTH.
       01 FX PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "QQ" TO VD.
           MOVE VG1 TO FX.
           DISPLAY "FX=[" FX "]".
           STOP RUN.
