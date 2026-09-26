      *> reject-at: 2002 2014 2023
      *> ISO §8.4.3.7.3 SR1 — the predefined object reference NULL
      *> written as the RECEIVING operand of SET (object-reference
      *> format).
      *> Rule: "NULL shall not be specified as a receiving operand."
      *> cite.py: OK  §8.4.3.7.3 1)  (Syntax rules)
      *> §14.9.39.3 SR8: "Identifier-3 shall be any item of class
      *> object that is permitted as a receiving item."
      *> cite.py: OK  §14.9.39.3 8)
      *> §14.9.39.3 SR9: "Identifier-4 shall be an object reference;"
      *> cite.py: OK  §14.9.39.3 9)
      *> The first SET (OR1 receives NULL) is the legal direction —
      *> NULL is an object reference (§8.4.3.7.3 SR2), so
      *> §14.9.39.3 SR9 admits it
      *> as the sender.  The second SET reverses the operands: NULL is
      *> class object, so SR8 would accept it on class grounds, and only
      *> SR1 forbids it as a receiving operand.  NULL is a reserved word
      *> from COBOL 2002 on, so the grammar cannot take it as a
      *> receiving identifier; the diagnostic observed by the row's
      *> adjudicator is COBOLNET0901 (reserved word used as a
      *> user-defined word).  Below 2002 USAGE OBJECT REFERENCE does not
      *> exist at all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M7NUL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OR1 USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN-P.
           SET OR1 TO NULL
           SET NULL TO OR1
           STOP RUN.
       END PROGRAM L1M7NUL.
