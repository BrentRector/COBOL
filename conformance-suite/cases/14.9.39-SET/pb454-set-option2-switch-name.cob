*>--- conformance-case
*> id: pb454-set-option2-switch-name
*> rules: [SR-14.9.39.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB454 - an Option-2 switch clause (status condition-names, NO mnemonic-name) declares
      *> nothing SET Format 3 can name. ISO 14.9.39.3 SR5: "Mnemonic-name-1 shall be associated with an
      *> external switch, the status of which may be altered." (python scripts/spec/cite.py --check
      *> 14.9.39.3 "Mnemonic-name-1 shall be associated with an external switch, the status of which may be
      *> altered" -> OK 14.9.39.3 5)). SWITCH-1 is a switch-name - "A switch-name identifies an
      *> implementor-defined external switch" (cite.py --check 8.3.2.3.11 -> OK) - not a mnemonic-name, so
      *> `SET SWITCH-1 TO ON` is refused at every edition. The binder used to enter the switch-NAME itself
      *> into its switch-mnemonic map when the entry had no mnemonic, so this statement bound and ran,
      *> printing ON; SET now reads the ONE mnemonic registry, whose rows come only from written mnemonics,
      *> and the diagnostic says SWITCH-1 is itself a switch-name.
      *> The conforming Option-1 spelling is 85/nested_special_names_inheritance (SWITCH-1 IS SW1 ... SET SW1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB454NEG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 ON STATUS IS SW-ON OFF STATUS IS SW-OFF.
       PROCEDURE DIVISION.
       MAIN-P.
           SET SWITCH-1 TO ON.
           IF SW-ON DISPLAY "ON" ELSE DISPLAY "OFF" END-IF.
           STOP RUN.
