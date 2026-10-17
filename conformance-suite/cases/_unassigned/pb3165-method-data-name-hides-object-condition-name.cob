*>--- conformance-case
*> id: pb3165-method-data-name-hides-object-condition-name
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3165 review R2-1 sibling arm (SymbolTable.TryResolveCondition). ISO 11.7.4 GR5:
      *>   "The declaration in the containing object definition is inaccessible to this method"
      *>   (cite.py --check 11.7.4 -> OK 11.7.4 5)), whatever the name class (8.4.6.1 "even when it
      *>   is a different type of user-defined word" -> OK 8.4.6.1). In SHOWX, X is the method's
      *>   numeric data-name, so IF X is a data item used as a condition, not the object's
      *>   condition-name X. Before the fix IF X tested the object's level-88 entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3165NBP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3165NBC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3165NBC.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3165NBC "NEW" RETURNING OBJ
           INVOKE OBJ "SHOWX"
           STOP RUN.
       END PROGRAM PB3165NBP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3165NBC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F PIC 9 VALUE 1.
          88 X VALUE 1.
       PROCEDURE DIVISION.
       METHOD-ID. SHOWX.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 X PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
           IF X DISPLAY "Y" ELSE DISPLAY "N" END-IF.
       END METHOD SHOWX.
       END OBJECT.
       END CLASS PB3165NBC.
