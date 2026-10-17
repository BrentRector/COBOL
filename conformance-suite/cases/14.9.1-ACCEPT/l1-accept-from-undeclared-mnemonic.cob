      *>--- conformance-case
      *> id: l1-accept-from-undeclared-mnemonic
      *> rules: [SR-14.9.1.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.1.3 SR2 - "Mnemonic-name-1 shall be specified in the
      *> SPECIAL-NAMES paragraph of the environment division and shall
      *> be associated with an implementor-defined device-name ...".
      *> NO-SUCH-DEV is declared nowhere, so the ACCEPT is rejected
      *> (COBOLNET0817). The paragraph DOES declare a mnemonic, so an
      *> empty registry cannot pass for the SR2 rejection.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1ACC01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSIN IS IN-DEV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           ACCEPT WS-X FROM NO-SUCH-DEV
           STOP RUN.
