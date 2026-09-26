      *> reject-at: 2002 2014 2023
      *> ISO §13.18.2.3 SR4 — a FUNCTION's ANY LENGTH item used as its
      *> RETURNING item is refused.
      *>   cite.py --check 13.18.2.3 "If the source element containing the
      *>     ANY LENGTH clause is a function, the subject of the entry
      *>     shall be referenced in its procedure division header as a
      *>     formal parameter with the BY REFERENCE phrase." -> OK 4)
      *> SR3 b) admits "the returning item" for a CONTAINED PROGRAM or a
      *> METHOD only; SR4 gives a FUNCTION no such arm. L1ALFR01's L-R is
      *> ANY LENGTH and appears in the header ONLY after RETURNING (the
      *> formal L is an ordinary fixed-length item), so the source element
      *> violates SR4 and must be rejected. The sibling SR2 arms (outermost
      *> program, group subject, class linkage) are each satisfied here:
      *> L-R is an elementary level-1 LINKAGE entry of a function with a
      *> one-symbol PICTURE (SR1).
      *> 2002 is the floor: FUNCTION-ID and ANY LENGTH are COBOL-2002.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. L1ALFR01.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L   PIC X(4).
       01 L-R PIC X ANY LENGTH.
       PROCEDURE DIVISION USING BY REFERENCE L RETURNING L-R.
       F-MAIN.
           MOVE L TO L-R.
           GOBACK.
       END FUNCTION L1ALFR01.
