*>--- conformance-case
*> id: pb3165-method-condition-name-hides-object-data-name
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3165 review R2-1 sibling arm (SymbolTable.TryResolve). ISO 11.7.4 GR5: a word
      *>   defined in the data division of this method and of the containing object definition means
      *>   the method's declaration there; "The declaration in the containing object definition is
      *>   inaccessible to this method" (cite.py --check 11.7.4 -> OK 11.7.4 5)), whatever the name
      *>   class (8.4.6.1 "even when it is a different type of user-defined word" -> OK 8.4.6.1).
      *>   In SHOWX, X is the method's condition-name, so MOVE X TO N names no data item (8.4.2.1);
      *>   the object's data-name X is not reachable. Before the fix the MOVE read the object's X.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3165NAP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3165NAC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3165NAC.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3165NAC "NEW" RETURNING OBJ
           INVOKE OBJ "SHOWX"
           STOP RUN.
       END PROGRAM PB3165NAP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3165NAC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       METHOD-ID. SHOWX.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 N PIC 9(4) VALUE 0.
       01 F PIC 9 VALUE 1.
          88 X VALUE 1.
       PROCEDURE DIVISION.
           MOVE X TO N
           DISPLAY "X " N.
       END METHOD SHOWX.
       END OBJECT.
       END CLASS PB3165NAC.
