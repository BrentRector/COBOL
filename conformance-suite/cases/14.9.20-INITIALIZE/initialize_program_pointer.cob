*>--- conformance-case
*> id: initialize_program_pointer
*> rules: [GR-14.9.20.4-4, GR-14.9.20.4-5, GR-14.9.20.4-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> CA2 (CONFORMANCE-FIX-QUEUE): INITIALIZE of a data-pointer / program-pointer / object-reference item
      *> emits an implicit SET ... TO the predefined NULL (ISO 14.9.20.4 GR4/GR6c), NOT a MOVE. Pre-fix,
      *> INITIALIZE of these categories emitted nothing, so the item kept its prior value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INITPP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET PP TO ENTRY "INITSUB".
           IF PP = NULL DISPLAY "BEFORE-NULL" ELSE DISPLAY "BEFORE-SET" END-IF.
           INITIALIZE PP.
           IF PP = NULL DISPLAY "AFTER-NULL" ELSE DISPLAY "AFTER-SET" END-IF.
           STOP RUN.
       END PROGRAM INITPP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INITSUB.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM INITSUB.
