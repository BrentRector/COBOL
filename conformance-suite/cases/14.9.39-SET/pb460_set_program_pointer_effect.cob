*>--- conformance-case
*> id: pb460_set_program_pointer_effect
*> rules: [GR-14.9.39.4-17, DOC-A.1-175, DOC-A.1-217]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB460 - the WITNESS for docs/CONFORMANCE.md DOC-A.1-175, Annex A.1 item 175 "SET statement
      *> (effect of SET on program whose address is being stored in a program-pointer). This item is
      *> required. This item shall be documented in the implementor's user documentation."
      *> (python scripts/spec/cite.py --check A.1 "SET statement (effect of SET on program whose address
      *> is being stored in a program-pointer)" -> OK A.1 175)), over ISO 14.9.39.4 GR17: "The effect of the
      *> SET statement on the program whose address is being stored in the program-pointer is
      *> implementor-defined." (cite.py --check 14.9.39.4 -> OK 17)).
      *> THE DETERMINATION BEING WITNESSED: NONE - storing a program's address neither activates, loads,
      *> cancels nor re-initializes the program. The observation that distinguishes "none" from "places the
      *> program in its initial state": PB460SUB counts its own activations in WORKING-STORAGE, which
      *> 8.6.4 keeps in its last-used state ("the content of the data item ... is that of the last time
      *> it was modified") between CALLs of a non-INITIAL program.
      *>  . CALL "PB460SUB" -> the first activation: COUNT=1.
      *>  . SET PP-1 TO ENTRY "PB460SUB" / SET PP-2 TO PP-1 / SET PP-3 TO ENTRY "PB460SUB" - three SETs
      *>    storing its address; none may touch it, so CALL PP-2 -> COUNT=2 (not 1, which a SET that
      *>    re-initialized the program would give).
      *>  . SET PP-4 TO ENTRY "PB460NEVER" names a program never called; the SET does not activate it
      *>    (it would DISPLAY "NEVER-RAN" if it did); PP-4 is not NULL - the program was located - so PP4-SET.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB460PPMAIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PP-1 USAGE PROGRAM-POINTER.
       01 PP-2 USAGE PROGRAM-POINTER.
       01 PP-3 USAGE PROGRAM-POINTER.
       01 PP-4 USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB460SUB"
           SET PP-1 TO ENTRY "PB460SUB"
           SET PP-2 TO PP-1
           SET PP-3 TO ENTRY "PB460SUB"
           SET PP-4 TO ENTRY "PB460NEVER"
           IF PP-4 NOT = NULL DISPLAY "PP4-SET" END-IF
           CALL PP-2
           STOP RUN.
       END PROGRAM PB460PPMAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB460SUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-COUNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           ADD 1 TO WS-COUNT
           DISPLAY "COUNT=" WS-COUNT
           GOBACK.
       END PROGRAM PB460SUB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB460NEVER.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NEVER-RAN"
           GOBACK.
       END PROGRAM PB460NEVER.
