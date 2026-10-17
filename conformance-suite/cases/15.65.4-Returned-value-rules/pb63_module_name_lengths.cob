*>--- conformance-case
*> id: pb63_module_name_lengths
*> rules: [RV-15.65.4-1, RV-15.65.4-4, RV-15.65.4-5, RV-15.65.4-6,
*>    RV-15.65.4-9, DOC-A.1-135, DOC-A.1-136, DOC-A.1-137]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB63 - FUNCTION MODULE-NAME's returned value (15.65.4): r1 "an alphanumeric dynamic length elementary
      *> item with no trailing spaces, except that in a COBOL main program with the ACTIVATING keyword a
      *> single space is returned" - every LENGTH below equals its content (the sweep's RV-15.65.4-1 pin);
      *> r5 ACTIVATING names the element that activated the current one - "by a CALL statement, an INVOKE
      *> statement, a function reference" - so a plain program CALLed from a METHOD names the METHOD-ID
      *> (S-ACT=SHOW, RV-15.65.4-5); r6 (an activation from a NESTED program): COBOL.NET returns the nested
      *> program's OWN name (CONFORMANCE.md A.1 item 137), and r9's STACK chain agrees entry for entry
      *> (X-ACT=INR and X-STK=EXT;INR;PB63MNMAIN; - RV-15.65.4-6); r10 TOP-LEVEL is the run-unit main
      *> (A.1 item 136 - the element RunMain started).
      *> M-*: the main (10 characters). ACTIVATING in the main is the single space (r5): length 1. STACK is
      *>   "PB63MNMAIN; " (r9 - CURRENT, then the final single-space entry): length 12.
      *> X-*: PB63MNEXT, a separately compiled program CALLed by the CONTAINED PB63MNINR.
      *> S-*: PB63MNSUB, a plain program CALLed from method SHOW of class PB63MNCLS (INVOKEd by the main).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB63MNMAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB63MNCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-L PIC 9(3).
       PROCEDURE DIVISION.
           DISPLAY "M-CUR=[" FUNCTION MODULE-NAME(CURRENT) "] "
                   FUNCTION LENGTH(FUNCTION MODULE-NAME(CURRENT))
           DISPLAY "M-ACT=[" FUNCTION MODULE-NAME(ACTIVATING) "] "
                   FUNCTION LENGTH(FUNCTION MODULE-NAME(ACTIVATING))
           DISPLAY "M-TOP=[" FUNCTION MODULE-NAME(TOP-LEVEL) "] "
                   FUNCTION LENGTH(FUNCTION MODULE-NAME(TOP-LEVEL))
           DISPLAY "M-STK=[" FUNCTION MODULE-NAME(STACK) "] "
                   FUNCTION LENGTH(FUNCTION MODULE-NAME(STACK))
           CALL "PB63MNINR"
           INVOKE PB63MNCLS "SHOW"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB63MNINR.
       PROCEDURE DIVISION.
           CALL "PB63MNEXT"
           GOBACK.
       END PROGRAM PB63MNINR.
       END PROGRAM PB63MNMAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB63MNEXT.
       PROCEDURE DIVISION.
           DISPLAY "X-CUR=[" FUNCTION MODULE-NAME(CURRENT) "]"
           DISPLAY "X-ACT=[" FUNCTION MODULE-NAME(ACTIVATING) "] "
                   FUNCTION LENGTH(FUNCTION MODULE-NAME(ACTIVATING))
           DISPLAY "X-STK=[" FUNCTION MODULE-NAME(STACK) "]"
           DISPLAY "X-TOP=[" FUNCTION MODULE-NAME(TOP-LEVEL) "]"
           GOBACK.
       END PROGRAM PB63MNEXT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB63MNSUB.
       PROCEDURE DIVISION.
           DISPLAY "S-CUR=[" FUNCTION MODULE-NAME(CURRENT) "]"
           DISPLAY "S-ACT=[" FUNCTION MODULE-NAME(ACTIVATING) "] "
                   FUNCTION LENGTH(FUNCTION MODULE-NAME(ACTIVATING))
           DISPLAY "S-STK=[" FUNCTION MODULE-NAME(STACK) "]"
           GOBACK.
       END PROGRAM PB63MNSUB.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB63MNCLS.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY "C-CUR=[" FUNCTION MODULE-NAME(CURRENT) "]"
           DISPLAY "C-ACT=[" FUNCTION MODULE-NAME(ACTIVATING) "]"
           CALL "PB63MNSUB"
           GOBACK.
       END METHOD SHOW.
       END FACTORY.
       END CLASS PB63MNCLS.
