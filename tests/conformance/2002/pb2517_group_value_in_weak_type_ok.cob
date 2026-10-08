      *> kb/Work PB2517's conforming counterpart.  ISO 13.18.63.3 SR1 bars a group-level VALUE on "a
      *> strongly-typed group item" -- and only that: 8.5.3.1 makes a group strongly typed when it is described with
      *> a TYPE clause that references a type declaration specifying the STRONG phrase, or is subordinate to such a
      *> group.  WEAKT is declared WITHOUT the phrase, so W1.G is not strongly typed and carries the group VALUE
      *> that 13.18.58.4 GR3 assumes from the template; 13.18.63.4 GR5 initializes the group area, so G displays AB.
      *> STRONGT is a STRONG declaration whose VALUE sits on an ELEMENTARY item (SR1 speaks of the group item only),
      *> so S1.E is initialized CD.  The strong spelling of the group-VALUE template is the negative
      *> pb2517-group-value-in-strong-type-reference.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2517WEAK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WEAKT IS TYPEDEF.
          05 G VALUE "AB".
             10 A PIC X.
             10 B PIC X.
       01 STRONGT IS TYPEDEF STRONG.
          05 E PIC X(2) VALUE "CD".
       01 W1 TYPE WEAKT.
       01 S1 TYPE STRONGT.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "G=" G OF W1 " A=" A OF W1 " B=" B OF W1.
           DISPLAY "E=" E OF S1.
           STOP RUN.
