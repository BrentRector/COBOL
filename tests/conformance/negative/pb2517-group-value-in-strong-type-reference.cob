      *> reject-at: 2002 2014 2023
      *> kb/Work PB2517.  ISO 13.18.63.3 SR1: "The subject of the entry shall not be a strongly-typed group item
      *> or a variable-length group."  U is a type declaration with the STRONG phrase and G, inside it, carries a
      *> group-level VALUE.  The declaration alone conforms: a type is a template (8.5.3.1) and G, inside the template,
      *> is not a typed item.  V is described with a TYPE clause that references U, so V is strongly typed, and
      *> V.G is "subordinate to a group item described with the TYPE clause that references a type declaration
      *> specifying the STRONG phrase" (8.5.3.1, second case), i.e. a strongly-typed group item.  13.18.58.4 GR3
      *> gives V.G the VALUE ("All other data description clauses and subordinate data descriptions are assumed by
      *> data defined using the type-name"), so V.G is a strongly-typed group item that is the subject of a VALUE
      *> entry: SR1 is violated, and only the reference composes it.  The screen used to skip every entry whose
      *> VALUE was assumed, on the ground that the template had answered for it -- true of SR13 and SR14, false
      *> of SR1.  Twin: pb184-group-value-strong-subject (the VALUE written on the TYPE entry itself).
      *> Edition band: STRONG TYPEDEF is COBOL-2002, so 85 rejects the declaration itself (COBOLNET0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2517N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U IS TYPEDEF STRONG.
          05 G VALUE "AB".
             10 A PIC X.
             10 B PIC X.
       01 V TYPE U.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY A OF V
           STOP RUN.
