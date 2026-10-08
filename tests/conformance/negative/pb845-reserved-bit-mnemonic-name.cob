      *> reject-at: 2002 2014 2023
      *> kb/Work PB845. ISO 8.3.2.1 1): "Reserved words shall not be used
      *> as user-defined words or system-names", and 8.9 reserves BIT
      *> from COBOL-2002. A mnemonic-name is a user-defined word, so
      *> SYSOUT IS BIT is COBOLNET0901. Before the fix BIT was exempt
      *> from the reservation gate and this compiled at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB845NB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSOUT IS BIT.
       PROCEDURE DIVISION.
           DISPLAY "M" UPON BIT
           STOP RUN.
