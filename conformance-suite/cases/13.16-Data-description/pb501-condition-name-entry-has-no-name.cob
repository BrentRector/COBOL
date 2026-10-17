*>--- conformance-case
*> id: pb501-condition-name-entry-has-no-name
*> rules: [GR-13.16.4-3, FMT-13.16.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.16.2, RENDERED from the printed page (folio 364 / PDF p394 - the whole verdict turns on one
      *> bracket, so the figure is read, never the OCR):
      *>     Format 3 (condition-name):  88 condition-name-1 value-clause .      <- NOT bracketed: required
      *>     Format 4 (validation):      88 [ condition-name-2 ] value-clause .  <- bracketed: optional
      *> Format 4's licence to omit the name does not stand alone: its value-clause is 13.18.63.2 format 5
      *> (content-validation-entry), whose figure ends in a REQUIRED { INVALID / VALID } choice. This entry
      *> carries no VALID or INVALID phrase, so it is not a format-4 entry, and it has no condition-name, so it
      *> is not a format-3 entry. 13.16.3 SR24 - "Format 3 or 4 is used for each condition-name" - leaves no
      *> third possibility, and the rule is edition-independent (measured identically at all four).
      *> Before kb/Work PB501 this compiled CLEAN at every edition and produced a .dll: `VALUE` opens the value
      *> clause rather than filling the name slot, so the entry parsed, and DataBinder.BindCondition's first
      *> statement returns on a missing data-name - the entry evaporated with no diagnostic at all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB501NEGNONAME.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(2) VALUE 07.
          88 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "N=" WS-N
           STOP RUN.
