      *> reject-at: 2002 2014 2023
      *> ISO §13.18.58.4 GR2 — "A type declaration has no storage
      *> associated with it."  T is a type declaration: a type is a
      *> "template that contains all the characteristics of a data item"
      *> (§3.174, §8.5.3.1), not a data item, so T names no storage.
      *> The receiving operand of MOVE "A" TO T therefore identifies no
      *> data item to receive the value, and §8.4.2.1 ("In order to use
      *> a resource, a statement shall contain a reference that uniquely
      *> identifies that resource") makes the statement invalid.
      *> Control in the same source: V, described with TYPE T, IS a data
      *> item with its own storage (§13.18.58.4 GR3), and MOVE "A" TO V
      *> is legal - only the reference to T may be diagnosed.
      *> Expected diagnostic: COBOLNET1639 undefined-reference (the
      *> adjudicator's probe td06storage of the same shape).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TDS01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T IS TYPEDEF PIC X(5).
       01 V TYPE T.
       PROCEDURE DIVISION.
           MOVE "A" TO V
           MOVE "A" TO T
           DISPLAY V
           STOP RUN.
