*>--- conformance-case
*> id: pb716-special-names-bare-word
*> rules: [FMT-12.3.7.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB716 - a BARE WORD is no SPECIAL-NAMES entry.  ISO 12.3.7.2 (rendered, PDF p320 / folio 290)
      *> prints three implementor-name arms and every one has a REQUIRED continuation: the switch-name arm's is
      *> in braces (IS mnemonic-name-1 [status phrases] | {status phrases}), and the feature-name and
      *> device-name arms require IS mnemonic-name.  The grammar spelled the entry
      *> `cobolWord (IS? cobolWord)? switchOnClause? switchOffClause?` - minimum spelling ONE word - inside the
      *> unbounded entry loop, so any word left over after a clause became a complete, silent entry: this
      *> program compiled and ran at every edition, ZOTZOT simply discarded.  The word now reaches the
      *> closed-format error production and is named (COBOLNET1970).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB716BARE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS DIGITS IS "0" THRU "9" ZOTZOT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X VALUE "0".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF A IS DIGITS DISPLAY "SHOULD NOT COMPILE".
           STOP RUN.
