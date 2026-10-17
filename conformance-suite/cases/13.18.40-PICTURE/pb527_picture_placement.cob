      *>--- conformance-case
      *> id: pb527_picture_placement
      *> rules: [SR-13.18.40.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.40.3 SR1 (ALL FORMATS, 85-era and unchanged in all four editions) — "The PICTURE clause may be
      *> specified only at the elementary level", read with 8.5.1.3.1 — "The most basic subdivisions of a record,
      *> that is, those not further subdivided, are called elementary items". The ONE test is the SUBORDINATE
      *> COUNT, and this program is the OVER-REJECTION guard for the screen that enforces it (kb/Work PB527):
      *> every entry below is legal source that the rule must leave alone, and each is a shape the screen could
      *> have mis-read if it had asked DataItem.IsElementary (which is defined as "has a PICTURE") instead.
      *>   G      — a group with no PICTURE, three levels deep; only the leaves carry one.
      *>   R      — a level-66 RENAMES entry: not a Format-1 entry at all (13.16.2 format 2), never PICTURE-bearing.
      *>   N-88   — a level-88 condition-name over a PICTURE-bearing conditional variable, whose PICTURE belongs
      *>            to the variable and not to the 88 (13.16.2 format 3); the 88 is a CHILD of a PICTURE-bearing
      *>            entry, so a screen keyed on "has children" alone would refuse its parent.
      *>   E      — an elementary 01 with a PICTURE and nothing subordinate: SR1's whole permitted population.
      *> Expected values are the declared widths; the point of the case is that it COMPILES.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB527PLACE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  G-SUB.
               10  A   PIC X(3).
               10  B   PIC 9(2).
           05  C       PIC X(4).
       66  R  RENAMES A THRU C.
       01  N           PIC 9(2) VALUE 07.
           88  N-88    VALUE 07.
       01  E           PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "QRS" TO A
           MOVE 42 TO B
           MOVE "WXYZ" TO C
           MOVE "HELLO" TO E
           DISPLAY "G=[" G "] L=" FUNCTION LENGTH(G)
           DISPLAY "R=[" R "] L=" FUNCTION LENGTH(R)
           DISPLAY "E=[" E "]"
           IF N-88 DISPLAY "N88=T" ELSE DISPLAY "N88=F" END-IF
           STOP RUN.
