*>--- conformance-case
*> id: oo_method_redefines_linkage
*> rules: [GR-13.7.4-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §11.7 / §13.18.44 / §14.2.3 GR8 — a Tier-B REDEFINES canonical (NUM 9(4), redefined by NCHARS X(4))
      *> used as a method BY REFERENCE USING formal (M2-OO-1h review A/D). Its storage IS the string backing, so
      *> the copy-out must write that back to the caller. Driver ARG=1234; the method mutates NCHARS→"0042"; on
      *> return ARG must reflect it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOLNK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS AGG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE AGG.
       01 ARG PIC 9(4) VALUE 1234.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE AGG "NEW" RETURNING A.
           INVOKE A "DOIT" USING ARG.
           DISPLAY "ARG=" ARG.
           STOP RUN.
       END PROGRAM OOLNK.

       IDENTIFICATION DIVISION.
       CLASS-ID. AGG INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. DOIT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 NUM PIC 9(4).
       01 NCHARS REDEFINES NUM PIC X(4).
       PROCEDURE DIVISION USING NUM.
       MAIN.
           DISPLAY "NUM=" NUM.
           DISPLAY "NC=" NCHARS.
           MOVE "0042" TO NCHARS.
           GOBACK.
       END METHOD DOIT.
       END OBJECT.
       END CLASS AGG.
