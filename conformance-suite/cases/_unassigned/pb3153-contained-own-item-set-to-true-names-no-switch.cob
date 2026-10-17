*>--- conformance-case
*> id: pb3153-contained-own-item-set-to-true-names-no-switch
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3153 review w1085d R1-1 (SetAlterBinder.SwitchCondOf
      *>   asks ReferenceResolver.DescribesOwnItem). PB53SN2 describes
      *>   its own boolean item PB53NON, the spelling of the switch-status
      *>   condition-name its container declares. "A source element's
      *>   reference to such a name, even when it is a different type of
      *>   user-defined word, is to the item which that source element
      *>   describes" (8.4.6.1 -> OK 8.4.6.1), so SET PB53NON TO TRUE
      *>   names a data item, no condition-name: "Condition-name-1 shall
      *>   be associated with a conditional variable" (14.9.39.3 SR6 ->
      *>   OK 14.9.39.3 6)). COBOLNET1639 (not a condition-name), the
      *>   code a program with no switch of that spelling gets. Before
      *>   the fix the message named external switch SWITCH-1, and the
      *>   word was also refused as used as two types (COBOLNET2692).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB53SN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS PB53NS1 ON STATUS IS PB53NON.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB53SN2".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB53SN2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PB53NON PIC 1 VALUE B"1".
       PROCEDURE DIVISION.
       MAIN-P.
           SET PB53NON TO TRUE.
           EXIT PROGRAM.
       END PROGRAM PB53SN2.
       END PROGRAM PB53SN.
