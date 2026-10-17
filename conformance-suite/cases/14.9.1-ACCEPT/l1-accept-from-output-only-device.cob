      *>--- conformance-case
      *> id: l1-accept-from-output-only-device
      *> rules: [SR-14.9.1.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.1.3 SR2 - the mnemonic shall be associated with a
      *> device-name "identified in the operating environment as a
      *> hardware or software device capable of providing data to the
      *> program". OUT-DEV IS declared, but it is bound to SYSOUT, an
      *> output-only implementor device-name (12.3.7.3), so the second
      *> conjunct of SR2 fails: rejected (COBOLNET0817).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1ACC02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSOUT IS OUT-DEV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           ACCEPT WS-X FROM OUT-DEV
           STOP RUN.
