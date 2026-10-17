      *>--- conformance-case
      *> id: l1c25-rewrite-from-invalid-move-sender
      *> rules: [SR-14.9.35.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.35.3 SR6 — REWRITE record-name-1 FROM identifier-1
      *> whose MOVE to record-name-1 is invalid.
      *> "If record-name-1 is specified, identifier-1 or literal-1 shall
      *> be valid as a sending operand in a MOVE statement specifying
      *> record-name-1 as the receiving operand."
      *> cite.py: OK  §14.9.35.3 6)  (Syntax rules)
      *> cite.py: OK  §14.9.25.3 10)  (Syntax rules) - "table 16,
      *> Validity of types of MOVE statements, specifies the validity
      *> of the move."
      *> Table 16: sending Alphabetic -> receiving Numeric = "No".
      *> R-REC is an elementary numeric record PIC 9(5); WS-ALPHA is
      *> alphabetic PIC A(5). MOVE WS-ALPHA TO R-REC is invalid, so the
      *> REWRITE ... FROM WS-ALPHA violates SR6 and nothing else: the
      *> file is RELATIVE with sequential access (no INVALID KEY, SR2),
      *> record-name-1 is a record of this FD (SR1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C25N6.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R-FILE ASSIGN TO "L1C25N6.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD R-FILE.
       01 R-REC PIC 9(5).
       WORKING-STORAGE SECTION.
       01 WS-ALPHA PIC A(5) VALUE "ABCDE".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O R-FILE.
           READ R-FILE.
           REWRITE R-REC FROM WS-ALPHA.
           CLOSE R-FILE.
           STOP RUN.
