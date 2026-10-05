      *> kb/Work PB1282's conforming counterpart. ISO 13.18.44.3 SR14 bars a data-name-2 that is "an item
      *> subordinate to a strongly-typed group item" -- and only that: 13.18.57.3 SR4 and Annex D.8.3 restrict items of
      *> a type declared with the STRONG phrase. WEAKT is a type declaration WITHOUT it, so the REDEFINES written
      *> inside its template, assumed by every group defined with the type-name (13.18.58.4 GR3), names a data-name-2
      *> that is subordinate to no strongly-typed group, and is legal. The strong spelling of the same template is the
      *> negative pb1282-redefines-in-strong-typedef.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1282WEAK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WEAKT IS TYPEDEF.
          05 SA PIC X(4) VALUE "abcd".
          05 SB REDEFINES SA PIC X(2).
       01 S1 TYPE WEAKT.
       01 S2 TYPE WEAKT.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "SA=" SA IN S1 " SB=" SB IN S1.
           MOVE "wxyz" TO SA IN S2.
           DISPLAY "SB2=" SB IN S2.
           STOP RUN.
