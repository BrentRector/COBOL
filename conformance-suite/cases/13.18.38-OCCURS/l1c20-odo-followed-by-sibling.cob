      *>--- conformance-case
      *> id: l1c20-odo-followed-by-sibling
      *> rules: [SR-13.18.38.3-22]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.38.3 SR22 — an occurs-depending subject followed by
      *> a non-subordinate entry in its record.
      *> "22) The subject of the entry may be followed within that
      *>   record description only by data description entries that
      *>   are subordinate to it."
      *>   OK  §13.18.38.3 22)  (Syntax rules)
      *> R-TAIL is a sibling of the ODO subject R-TAB, written after
      *> it in the same record: it is not subordinate to R-TAB, so the
      *> record description violates SR22. Every other rule is met:
      *> R-N precedes the table (SR20), is an integer (SR17), and
      *> 0 <= 1 < 5 (SR16). The expected rejection is COBOLNET0856,
      *> whose meaning is exactly this rule's violation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C20A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 R-N    PIC 9 VALUE 2.
          05 R-TAB  PIC X OCCURS 1 TO 5 TIMES DEPENDING ON R-N.
          05 R-TAIL PIC X.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY R.
           STOP RUN.
