*>--- conformance-case
*> id: l1-display-upon-input-only-device
*> rules: [SR-12.3.7.3-7, DOC-A.1-59]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.11.3 SR2 DISPLAY statement — the UPON mnemonic "shall be associated with an implementor-defined
      *> device-name that is identified in the operating environment as a hardware or software device CAPABLE OF
      *> RECEIVING DATA FROM THE PROGRAM".
      *>   python scripts/spec/cite.py --check 14.9.11.3 "shall be associated with an implementor-defined
      *>   device-name that is identified in the operating environment as a hardware or software device capable
      *>   of receiving data from the program"  ->  OK  §14.9.11.3 2)
      *>
      *> The COMPLEMENT of Annex A.1 item 59's determination (docs/CONFORMANCE.md#DOC-A.1-59), and the leg that
      *> makes the item-59 golden a MEASUREMENT of a device SET rather than of one name: SYSIN is the implementor
      *> device-name for the ACCEPT side, is NOT capable of receiving data from the program, and a DISPLAY UPON a
      *> mnemonic bound to it violates SR2. An implementation whose UPON phrase accepted every declared mnemonic
      *> would compile this and write program output onto the input stream.
      *> The rule is a SYNTAX rule at every edition — SPECIAL-NAMES device-name mnemonics and the DISPLAY UPON
      *> phrase are both COBOL-85 — so the rejection is required at all four.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DSPN1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSIN IS L1-IN.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X" UPON L1-IN
           STOP RUN.
