      *> reject-at: 2002 2014 2023
      *> ISO §13.18.2.3 SR4 — a FUNCTION's ANY LENGTH item that the
      *> procedure division header does not reference at all is refused.
      *>   cite.py --check 13.18.2.3 "If the source element containing the
      *>     ANY LENGTH clause is a function, the subject of the entry
      *>     shall be referenced in its procedure division header as a
      *>     formal parameter with the BY REFERENCE phrase." -> OK 4)
      *> L1ALFU01 declares two ANY LENGTH items. A is referenced as a
      *> BY REFERENCE formal (the legal arm, SR4 satisfied); M is named
      *> nowhere in the header, so SR4's "shall be referenced" is
      *> violated for M and the source element must be rejected. The SR2
      *> arms are satisfied (elementary level-1 LINKAGE entries of a
      *> function, one-symbol PICTUREs).
      *> 2002 is the floor: FUNCTION-ID and ANY LENGTH are COBOL-2002.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. L1ALFU01.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A   PIC X ANY LENGTH.
       01 M   PIC X ANY LENGTH.
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING BY REFERENCE A RETURNING L-R.
       F-MAIN.
           MOVE FUNCTION LENGTH(A) TO L-R.
           GOBACK.
       END FUNCTION L1ALFU01.
