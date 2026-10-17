      *>--- conformance-case
      *> id: l1c08-env-division-section-order
      *> rules: [FMT-12.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.2.1 FMT — the input-output section may not precede the
      *> configuration section.
      *> Format: "ENVIRONMENT DIVISION." "[ configuration-section ]"
      *>   "[ input-output-section ]" - fixed order.
      *> cite.py:
      *>   OK  §12.2.1   (General format)  [ configuration-section ]
      *>   OK  §12.2.1   (General format)  [ input-output-section ]
      *> Both sections below are individually valid (the positive
      *> l1c08_environment_division_shapes uses them in format order);
      *> only their order violates the general format, so the source
      *> shall be rejected (a general-format violation: parse error).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C08S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF1 ASSIGN TO "L1C08S.DAT"
               ORGANIZATION IS SEQUENTIAL.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       FILE SECTION.
       FD SF1.
       01 SR PIC X(4).
       PROCEDURE DIVISION.
       S-MAIN.
           DISPLAY "NOT-REACHED".
           STOP RUN.
