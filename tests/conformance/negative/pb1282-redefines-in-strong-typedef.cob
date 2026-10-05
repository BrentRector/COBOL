      *> reject-at: 2002 2014 2023
      *> kb/Work PB1282. ISO 13.18.44.3 SR14: "Data-name-2 shall not be of class object, message-tag, or pointer, a
      *> strongly-typed group item, or an item subordinate to a strongly-typed group item." 13.18.58.4 GR3: "All other
      *> data description clauses and subordinate data descriptions are assumed by data defined using the type-name."
      *> So the REDEFINES written inside the STRONG template STRT is assumed by S1, a strongly-typed group item (Annex
      *> D.8.3: "subordinate to a type declaration with the STRONG phrase"), and its data-name-2 SA is an item
      *> subordinate to that strongly-typed group item: refused COBOLNET1697. 13.18.57.3 SR2 forbids a written
      *> subordinate entry under a TYPE entry, so the template is the only place the clause could be written at all;
      *> the earlier golden pb183_redefines_in_strong_typedef_ok answered 13.18.57.3 SR4 (the TYPE entry's own
      *> subject), a different rule, and pinned the acceptance this negative replaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1282N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 STRT IS TYPEDEF STRONG.
          05 SA PIC X(4) VALUE "abcd".
          05 SB REDEFINES SA PIC X(2).
       01 S1 TYPE STRT.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "SA=" SA IN S1 " SB=" SB IN S1.
           STOP RUN.
