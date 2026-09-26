      *> reject-at: 2002 2014 2023
      *> ISO §14.9.23.3 SR22 — "Identifier-4 is a receiving operand."
      *> (cite.py --check 14.9.23.3 OK, Syntax rule 22.)
      *> DERIVED BEFORE MEASURING. The RETURNING operand of INVOKE is a
      *> receiving operand (and GR8, cite.py --check 14.9.23.4 OK: "If a
      *> RETURNING phrase is specified, the result of the activated
      *> method is placed into identifier-4"), so every rule barring a
      *> receiving operand applies to it. §13.18.15.3 SR2 (cite.py
      *> --check OK, SR 2): "Neither the data item described by the
      *> subject of the entry nor any data item subordinate to the
      *> subject of the entry shall be specified as a receiving data
      *> item" — KA is subordinate to the CONSTANT RECORD K, so the
      *> INVOKE shall be REJECTED. Otherwise legal: SR11 (a
      *> working-storage item), and KA matches the method's returning
      *> item L-R (PIC X(4)) for §14.8.3 conformance. The method is
      *> named FETCHR, a user-defined word: GET is a reserved word
      *> (§8.9, cite.py OK) and §8.3.2.4.1 (cite.py OK) bars a reserved
      *> word as a user-defined word, so METHOD-ID. GET would be a
      *> syntax error independent of SR22.
      *> ARM: the RESOLVED path (class-name-1 naming a factory method).
      *> The universal arm is l1-invoke-returning-constant-record-
      *> universal.
      *> Expected: COBOLNET1548 (docs/DIAGNOSTICS.md,
      *> constant-as-receiver).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1IV22RA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1IV22RK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT RECORD.
          05 KA PIC X(4) VALUE "AAAA".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE L1IV22RK "FETCHR" RETURNING KA
           DISPLAY KA
           STOP RUN.
       END PROGRAM L1IV22RA.
       IDENTIFICATION DIVISION.
       CLASS-ID. L1IV22RK.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. FETCHR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-R PIC X(4).
       PROCEDURE DIVISION RETURNING L-R.
       P-MAIN.
           MOVE "ZZZZ" TO L-R.
       END METHOD FETCHR.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS L1IV22RK.
