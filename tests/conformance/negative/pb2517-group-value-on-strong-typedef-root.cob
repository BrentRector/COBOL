      *> reject-at: 2002 2014 2023
      *> kb/Work PB2517, the whole-entry arm.  ISO 13.18.63.3 SR1: "The subject of the entry shall not be a
      *> strongly-typed group item or a variable-length group."  The type declaration U carries the group-level
      *> VALUE on its own entry; a template is not a typed item (8.5.3.1), so the declaration alone conforms.
      *> V is described with a TYPE clause that references U, which specifies the STRONG phrase, so V is a
      *> strongly-typed group item (8.5.3.1, first case), and 13.18.58.4 GR3 gives it U's VALUE ("All other data
      *> description clauses and subordinate data descriptions are assumed by data defined using the type-name").
      *> W SAME AS V assumes the same description (13.18.49.4 GR1) and must NOT draw a second verdict: its source
      *> V is already in the shape and was reported where the TYPE clause composed it.
      *> Twin: pb2517-group-value-in-strong-type-reference (the VALUE on a group SUBORDINATE to the type's root).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2517N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U IS TYPEDEF STRONG VALUE "AB".
          05 A PIC X.
          05 B PIC X.
       01 V TYPE U.
       01 W SAME AS V.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY A OF V
           STOP RUN.
