*>--- conformance-case
*> id: module_name
*> rules: [RV-15.65.4-7, RV-15.65.4-8]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.65 FUNCTION MODULE-NAME — the running COBOL hierarchy. CURRENT (r7) = the outermost program of
      *> the running compilation unit; ACTIVATING (r5) = the activator, a single space in the main program;
      *> TOP-LEVEL (r10) = the run-unit main; NESTED (r8) = the most-recently-nested running program;
      *> STACK (r9) = CURRENT;<activators>;TOP-LEVEL;<space>. MAINMOD is the main; it CALLs NESTPROG (contained)
      *> and SUBMOD (separate).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINMOD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NAME PIC X(20).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION MODULE-NAME(CURRENT)    TO WS-NAME.
           DISPLAY "M-CUR=[" WS-NAME "]".
           MOVE FUNCTION MODULE-NAME(ACTIVATING) TO WS-NAME.
           DISPLAY "M-ACT=[" WS-NAME "]".
           MOVE FUNCTION MODULE-NAME(TOP-LEVEL)  TO WS-NAME.
           DISPLAY "M-TOP=[" WS-NAME "]".
           CALL "NESTPROG".
           CALL "SUBMOD".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NAME PIC X(20).
       PROCEDURE DIVISION.
       NP.
           MOVE FUNCTION MODULE-NAME(CURRENT)    TO WS-NAME.
           DISPLAY "N-CUR=[" WS-NAME "]".
           MOVE FUNCTION MODULE-NAME(NESTED)     TO WS-NAME.
           DISPLAY "N-NST=[" WS-NAME "]".
           MOVE FUNCTION MODULE-NAME(ACTIVATING) TO WS-NAME.
           DISPLAY "N-ACT=[" WS-NAME "]".
           EXIT PROGRAM.
       END PROGRAM NESTPROG.
       END PROGRAM MAINMOD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBMOD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-STK  PIC X(40).
       PROCEDURE DIVISION.
       SM.
           MOVE FUNCTION MODULE-NAME(ACTIVATING) TO WS-STK.
           DISPLAY "S-ACT=[" WS-STK "]".
           MOVE FUNCTION MODULE-NAME(STACK)      TO WS-STK.
           DISPLAY "S-STK=[" WS-STK "]".
           EXIT PROGRAM.
       END PROGRAM SUBMOD.
