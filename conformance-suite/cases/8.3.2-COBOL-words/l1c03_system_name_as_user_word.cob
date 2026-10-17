      *>--- conformance-case
      *> id: l1c03_system_name_as_user_word
      *> rules: [SR-8.3.2.1-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.3.2.1 4) — one word used both as a system-name and as a
      *> user-defined word
      *>
      *> "4) A given word may be used as a system-name and as a
      *> user-defined word, subject to the rules
      *> specified in 8.3.2.2, User-defined words, and 8.3.2.3,
      *> System-names."
      *>   cite.py: OK  §8.3.2.1 4)  (General)
      *> The 8.3.2.3 half: "Within an implementation, a given
      *> system-name shall not belong to more than one
      *> of the following types of system-names: device-name,
      *> feature-name, and switch-name."
      *>   cite.py: OK  §8.3.2.3.1   (General)
      *> DISPLAY UPON: "Mnemonic-name-1 shall be specified in the
      *> SPECIAL-NAMES paragraph of the environment
      *> division and shall be associated with an implementor-defined
      *> device-name"
      *>   cite.py: OK  §14.9.11.3 2)  (Syntax rules)
      *>
      *> Every system-name below is ALSO declared as a user-defined word
      *> of another type. Rule 4 makes
      *> each pairing legal, so the program compiles, and each word
      *> keeps both meanings:
      *>   MYBOX    computer-name (SOURCE-/OBJECT-COMPUTER) and a
      *>   data-name.
      *>   SYSOUT   device-name (SPECIAL-NAMES) and a data-name.
      *>   CONSOLE  device-name AND the mnemonic-name bound to it
      *>   (CONSOLE IS CONSOLE).
      *>   C01      feature-name (SPECIAL-NAMES, bound to TOP-PAGE) and
      *>   a data-name.
      *>   SWITCH-1 switch-name (SPECIAL-NAMES, bound to SW1) and a
      *>   data-name.
      *>   SYSIN    device-name (not bound here) used as a
      *>   paragraph-name.
      *> Devices CONSOLE and SYSOUT are the standard output
      *> (docs/CONFORMANCE.md rows DOC-A.1-59 and
      *> DOC-A.1-189), so both UPON lines land in stdout.
      *>
      *> DERIVATION of each expected line.
      *>   DISPLAY MYBOX SYSOUT C01 SWITCH-1  -> the four DATA items'
      *>   VALUEs: "MB" "SO" "C1" "S1"
      *>                                         -> "MBSOC1S1". A
      *>                                         binding that resolved
      *>                                         any of these
      *>                                         words as the
      *>                                         system-name would not
      *>                                         print its value.
      *>   DISPLAY ... UPON OUTDEV            -> OUTDEV names device
      *>   SYSOUT: "VIA OUTDEV".
      *>   DISPLAY ... UPON CONSOLE           -> the mnemonic-name
      *>   CONSOLE: "VIA CONSOLE".
      *>   PERFORM SYSIN                      -> the PARAGRAPH SYSIN
      *>   runs: "PARA SYSIN".
      *>   MOVE "XY" TO SYSOUT, DISPLAY       -> the data-name is a
      *>   receiving item: "XY".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C03A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. MYBOX.
       OBJECT-COMPUTER. MYBOX.
       SPECIAL-NAMES.
           SYSOUT IS OUTDEV
           CONSOLE IS CONSOLE
           C01 IS TOP-PAGE
           SWITCH-1 IS SW1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MYBOX     PIC XX VALUE "MB".
       01  SYSOUT    PIC XX VALUE "SO".
       01  C01       PIC XX VALUE "C1".
       01  SWITCH-1  PIC XX VALUE "S1".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY MYBOX SYSOUT C01 SWITCH-1.
           DISPLAY "VIA OUTDEV" UPON OUTDEV.
           DISPLAY "VIA CONSOLE" UPON CONSOLE.
           PERFORM SYSIN.
           MOVE "XY" TO SYSOUT.
           DISPLAY SYSOUT.
           STOP RUN.
       SYSIN.
           DISPLAY "PARA SYSIN".
