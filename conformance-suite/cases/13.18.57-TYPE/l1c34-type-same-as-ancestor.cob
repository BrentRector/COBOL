      *>--- conformance-case
      *> id: l1c34-type-same-as-ancestor
      *> rules: [SR-13.18.57.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.57.3 SR1 — a SAME AS in type-name-1 naming a group
      *>   above
      *> the TYPE subject.
      *> "The description of any data item subordinate to type-name-1
      *>   shall not
      *> contain a SAME AS clause that references the subject of this
      *>   entry or
      *> any group item to which this entry is subordinate."
      *> cite.py --check 13.18.57.3 "shall not contain a SAME AS
      *>   clause that
      *>   references the subject of this entry or any group item to
      *>     which this
      *>   entry is subordinate" -> OK §13.18.57.3 1) (Syntax rules)
      *> The subject of "10 R-X TYPE T" is subordinate to R-G and to
      *>   REC. T-B,
      *> subordinate to type-name-1 (T), says SAME AS REC -- a group
      *>   item to
      *> which the entry is subordinate (two levels up), the second
      *>   branch of
      *> SR1. Otherwise legal: REC is a level-1 group (§13.18.49.3
      *>   SR7) and T-B
      *> is not written inside REC, so only the TYPE-side rule applies.
      *> Expected: rejected with the SAME AS cycle diagnostic
      *>   (COBOLNET1557).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C34B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF.
           05  T-A PIC X(2).
           05  T-B SAME AS REC.
       01  REC.
           05  R-G.
               10  R-X TYPE T.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
