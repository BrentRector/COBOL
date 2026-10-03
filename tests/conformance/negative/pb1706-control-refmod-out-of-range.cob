      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1706. A CONTROL operand's reference-modification literals are range-checked against the item
      *> at compile time. ISO/IEC 1989:2023 8.4.3.3.4 5) c): "The sum of leftmost-position and length minus the
      *> value one shall be less than or equal to the number of positions in the data item referenced by
      *> identifier-1" (cite.py OK). CX has 6 positions and CX(1:99) needs 99; 13.18.16.3 SR4 makes the
      *> positions integer literals, so the violation is known at compile time (the 13.18.16.4 GR3 prior
      *> control has no well-formed description for the slice) and the fatal EC-BOUND-REF-MOD is refused as
      *> COBOLNET2670 (14.6.13.1.3 8): the implementor is not required to produce executable code for a fatal
      *> condition the compiler detects; the same screen as negative/pb1707-refmod-literal-out-of-range.
      *> Before PB1707 part 1 the clause compiled clean and the report broke at the wrong times.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1706CR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1706-control-refmod.txt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 CX      PIC X(6) VALUE SPACES.
       REPORT SECTION.
       RD R-1 CONTROLS ARE CX(1:99) CX(4:).
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(6) SOURCE IS CX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           MOVE "AAA111" TO CX
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
