      *> reject-at: 2002 2014 2023
      *> kb/Work PB3287 - ISO 13.7.3 SR4: "a data item defined in the linkage section of a source element may be
      *> referenced within the procedure division of that source element if, and only if, it satisfies one of the
      *> following conditions" (an operand of the USING or RETURNING phrase, subordinate to one, a REDEFINES or RENAMES
      *> of one, subordinate to such a redefinition, or - 13.18.5 - BASED).  L-REC below is none of them.  ADDRESS OF
      *> L-REC is a reference to it (8.4.3.11.3 SR1 admits a linkage item as identifier-1 only where the procedure
      *> division may reference it at all), so it is refused - COBOLNET2746, the verdict `MOVE L-REC TO WS-X` already
      *> drew.  ResolveForAddressOf used to re-sequence the identifier resolution by hand and skip the screen, so
      *> the program compiled clean and took a pointer to a record the program may not reference.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3287N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-P USAGE POINTER.
       LINKAGE SECTION.
       01  L-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN.
           SET WS-P TO ADDRESS OF L-REC.
           STOP RUN.
