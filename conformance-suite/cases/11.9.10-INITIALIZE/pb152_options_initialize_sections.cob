*>--- conformance-case
*> id: pb152_options_initialize_sections
*> rules: [GR-11.9.10.4-2, GR-11.9.10.4-3, GR-13.5.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB152 - SECTION SELECTIVITY: the half of 11.9.10 that had NEVER EXECUTED.
      *>
      *> The binder has always built OptionsInitialize.Sections - including 11.9.10.4 GR1's "If ALL is
      *> specified, LOCAL-STORAGE, SCREEN, and WORKING-STORAGE apply" fold - and NOTHING read it. GR2/GR3/GR4
      *> route the three sections separately ("If LOCAL-STORAGE is specified, all data items in the
      *> local-storage section are initialized as indicated in the rules for initial state", and likewise for
      *> SCREEN and WORKING-STORAGE), so a clause naming ONE section must fill that section and LEAVE THE
      *> OTHERS at their no-clause baseline.
      *>
      *> ⛔ THIS GOLDEN PROBES BOTH DIRECTIONS AND MUST KEEP DOING SO. A one-section probe passes for the wrong
      *> reason: an implementation that ignores the section list entirely and fills everything satisfies the
      *> "L is filled" half. W is the half that fails it.
      *>
      *> EXPECTED (11.9.10.4 GR2, and GR4 NOT applying):
      *>   L PIC X(4) in LOCAL-STORAGE  -> ZZZZ   (0x5A = 'Z', GR5 c)
      *>   W PIC X(4) in WORKING-STORAGE -> four SPACES, the no-clause baseline this compiler documents for a
      *>     VALUE-less alphanumeric item under 11.9.10.4 GR6 ("the content ... is undefined or specified by
      *>     the implementor")
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB152SECT.
       OPTIONS.
           INITIALIZE LOCAL-STORAGE SECTION TO X"5A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       LOCAL-STORAGE SECTION.
       01 L PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "W=[" W "]".
           DISPLAY "L=[" L "]".
           STOP RUN.
