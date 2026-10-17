      *>--- conformance-case
      *> id: l1c34-type-same-as-subject
      *> rules: [SR-13.18.57.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.57.3 SR1 — a SAME AS in type-name-1 naming the
      *>   TYPE subject
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
      *> T-B is subordinate to type-name-1 (T) and its SAME AS names
      *>   Y, the
      *> SUBJECT of the entry "01 Y TYPE T" -- the first branch of
      *>   SR1. Nothing
      *> else is illegal: SAME AS names a level-1 item (§13.18.49.3
      *>   SR7), and T-B
      *> is not itself subordinate to Y in the source, so §13.18.49.3
      *>   SR3 read on
      *> the written entry does not apply; only the TYPE-side rule
      *>   catches it.
      *> Expected: rejected with the SAME AS cycle diagnostic
      *>   (COBOLNET1557).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C34A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF.
           05  T-A PIC X(4).
           05  T-B SAME AS Y.
       01  Y TYPE T.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
