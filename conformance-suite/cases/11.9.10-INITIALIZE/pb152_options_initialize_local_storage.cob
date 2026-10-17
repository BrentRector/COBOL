*>--- conformance-case
*> id: pb152_options_initialize_local_storage
*> rules: [GR-11.9.10.4-1, GR-11.9.10.4-2, GR-14.6.2.3.2-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB152 - the background is RE-LAID ON EVERY ACTIVATION for LOCAL-STORAGE.
      *>
      *> 13.6.4 GR1 makes local-storage automatic data: its storage is allocated afresh each time the runtime
      *> element is activated, and 14.6.2.3.2's actions are the initial state that allocation enters. So the
      *> 11.9.10.4 GR2 fill is not a one-time seed - a second CALL must see the background again, whatever the
      *> first activation left behind. This golden mutates the item between the two calls, so an implementation
      *> that laid the fill once at program load fails it.
      *>
      *> It also carries the 11.9.10.4 GR1 fold from the LOCAL-STORAGE side: the clause names ALL, and GR1 says
      *> "If ALL is specified, LOCAL-STORAGE, SCREEN, and WORKING-STORAGE apply" - so LS is selected without
      *> being named. (pb152_options_initialize_sections carries the other direction: a clause naming ONE
      *> section leaves the other at its baseline.)
      *>
      *> EXPECTED: both activations report ZZZZ (0x5A = 'Z', 11.9.10.4 GR5 c), and the mutation the first
      *> activation makes is invisible to the second.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB152LSMAIN.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB152LSSUB".
           CALL "PB152LSSUB".
           STOP RUN.
       END PROGRAM PB152LSMAIN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB152LSSUB.
       OPTIONS.
           INITIALIZE ALL TO X"5A".
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 L PIC X(4).
       PROCEDURE DIVISION.
       SUB-MAIN.
           DISPLAY "L=[" L "]".
           MOVE "muta" TO L.
           DISPLAY "AFTER=[" L "]".
       END PROGRAM PB152LSSUB.
