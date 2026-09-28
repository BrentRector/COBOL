      *> reject-at: 85 2002 2014 2023
      *> A CONTROL OPERAND'S REFERENCE-MODIFICATION LITERAL BEYOND THE
      *> IMPLEMENTATION LIMIT (kb/Work PB1579). ISO/IEC 1989:2023 §13.18.16.3 SR4:
      *> "Data-name-1 may be reference-modified. If it is, leftmost-position and
      *> length shall be integer literals." 77777777777 IS an integer literal,
      *> so SR4 holds; but §13.18.16.4 GR3 defines a prior control "having the
      *> same data description as the corresponding data item", and a slice
      *> 77,777,777,777 positions long exceeds the 2,147,483,647 this
      *> implementation lays out — ISO §4.5: "Translation may be unsuccessful due
      *> to factors other than lack of conformance of a compilation group"
      *> (docs/CONFORMANCE.md §3 "Integer operands and host carriers"). The
      *> verdict is COBOLNET2427, the one limit screen. Before PB1579 the literal
      *> was read by int.TryParse and refused under SR4 as "not an integer
      *> literal" — a false sentence; with the reader fixed but no limit screen,
      *> the saturated length reached the prior control's copy and the program
      *> died "Out of memory." at its first GENERATE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1579CR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1579-control-refmod-beyond-limit.txt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 CX      PIC X(6) VALUE SPACES.
       REPORT SECTION.
       RD R-1 CONTROL IS CX(1:77777777777).
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
