      *> reject-at: 2002 2014 2023
      *> ISO §14.9.23.3 SR20 — "If identifier-3 does not reference an
      *> address-identifier, identifier-3 is a receiving operand."
      *> (cite.py --check 14.9.23.3 OK, Syntax rule 20.)
      *> THE UNIVERSAL ARM of the same rule as l1-invoke-byref-constant-
      *> record. U is a universal object reference, so the method cannot
      *> be resolved at compile time (SR4 applies only to a
      *> non-universal identifier-1) — but SR20 does not depend on the
      *> receiver: KA is not an address-identifier, so it is a
      *> receiving operand, and
      *> §13.18.15.3 SR2 (cite.py --check OK, SR 2) — "Neither the data
      *> item described by the subject of the entry nor any data item
      *> subordinate to the subject of the entry shall be specified as a
      *> receiving data item" — bars it, because KA is subordinate to
      *> the CONSTANT RECORD K. Statically decidable, so REJECTED at
      *> compile time. BY REFERENCE is the only mode SR6 permits here
      *> ("neither the BY CONTENT nor the BY VALUE phrase shall be
      *> specified"), and SR9 is met (a working-storage item).
      *> Expected: COBOLNET1548 (docs/DIAGNOSTICS.md,
      *> constant-as-receiver).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1IV20UA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       01 K CONSTANT RECORD.
          05 KA PIC X(4) VALUE "AAAA".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE U "PUT" USING BY REFERENCE KA
           DISPLAY KA
           STOP RUN.
