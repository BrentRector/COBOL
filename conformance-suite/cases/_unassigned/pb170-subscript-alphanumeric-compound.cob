*>--- conformance-case
*> id: pb170-subscript-alphanumeric-compound
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> THE CONTRAST CLAIM kb/Work PB170's note got WRONG, pinned so the fix
      *> cannot land on the simple arm alone and look green. The note asserted
      *> that E(XE + 1) "now draws COBOLNET0844" and only the SIMPLE form
      *> bypassed the screen. It does not: RenderSegment routes to the screened
      *> D18 path only for a slash, a function, an unresolvable name, a SCALED
      *> operand inside a compound, or a token with no case arm - and plain '+'
      *> over an UNSCALED alphanumeric name is none of those. Measured on
      *> 9a89fbd1: compiled clean and printed R=B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB170N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XE PIC X(4) VALUE "0001".
       01 R  PIC X.
       01 T.
          05 E PIC X OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABC" TO T
           MOVE E(XE + 1) TO R
           STOP RUN.
