*>--- conformance-case
*> id: l1-call-returning-constant-record
*> rules: [SR-14.9.4.3-9]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.4.3 SR9 — "Identifier-3 is a receiving operand."
      *> (cite.py --check 14.9.4.3 "Identifier-3 is a receiving operand." -> OK, §14.9.4.3 Syntax rules 9.)
      *>
      *> DERIVED BEFORE MEASURING. SR9 classifies the CALL … RETURNING operand, and the classification is
      *> not decorative: every "shall not be specified as a receiving operand" rule in the standard then
      *> applies to it. §13.18.15.3 SR2 is one of them — no part of a CONSTANT RECORD may be a receiving
      *> operand — so `CALL "SUB" RETURNING <item of a CONSTANT RECORD>` shall be REJECTED, at every
      *> edition where CALL … RETURNING exists (2002+).
      *>
      *> WHY THIS FIXTURE AND NOT THE BY REFERENCE ONE. `conformance:negative/pb128-call-constant-by-
      *> reference` pins the SAME §13.18.15.3 SR2 screen in the identifier-2 slot (§14.9.4.3 SR5), and the two slots
      *> are separate arms of one dispatch: identifier-2 rode ExpressionBinder.ResolveReceiving while
      *> identifier-3 was resolved with the plain sending resolver, so the RETURNING position bound a
      *> writable carrier over a structured constant and the callee's returned value overwrote it with no
      *> diagnostic. Fixing one arm of a two-arm dispatch and testing only that arm is this repository's
      *> most reproducible defect shape; this fixture is the second arm's witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CRETK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT RECORD.
          05 KA PIC 9(4) VALUE 1234.
       PROCEDURE DIVISION.
       MAIN.
           CALL "SUB" RETURNING KA
           STOP RUN.
