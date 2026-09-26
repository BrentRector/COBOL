      *> reject-at: 2002 2014 2023
      *> ISO §14.9.23.3 SR20 — "If identifier-3 does not reference an
      *> address-identifier, identifier-3 is a receiving operand."
      *> (cite.py --check 14.9.23.3 OK, Syntax rule 20.)
      *> DERIVED BEFORE MEASURING. KA is a plain data item, not an
      *> address-identifier, so as a USING argument (identifier-3) it IS
      *> a receiving operand, and every rule barring a receiving operand
      *> applies to it. §13.18.15.3 SR2 (cite.py --check OK, SR 2):
      *> "Neither the data item described by the subject of the entry
      *> nor any data item subordinate to the subject of the entry shall
      *> be specified as a receiving data item" — KA is subordinate to
      *> the CONSTANT RECORD K, so the INVOKE shall be REJECTED. The
      *> argument is otherwise legal: SR9 (a working-storage item), SR5
      *> a) (BY REFERENCE against the BY REFERENCE formal L-A).
      *> ARM: the RESOLVED path (class-name-1 naming a factory method of
      *> a class of the group). The universal arm is the separate
      *> fixture l1-invoke-byref-constant-record-universal.
      *> Editions: INVOKE and CONSTANT RECORD are both COBOL-2002.
      *> Expected: COBOLNET1548 (docs/DIAGNOSTICS.md,
      *> constant-as-receiver:
      *> "a data item of a CONSTANT RECORD shall not be specified as a
      *> receiving operand").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1IV20RA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1IV20RK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT RECORD.
          05 KA PIC X(4) VALUE "AAAA".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE L1IV20RK "PUT" USING BY REFERENCE KA
           DISPLAY KA
           STOP RUN.
       END PROGRAM L1IV20RA.
       IDENTIFICATION DIVISION.
       CLASS-ID. L1IV20RK.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. PUT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-A PIC X(4).
       PROCEDURE DIVISION USING L-A.
       P-MAIN.
           MOVE "ZZZZ" TO L-A.
       END METHOD PUT.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS L1IV20RK.
