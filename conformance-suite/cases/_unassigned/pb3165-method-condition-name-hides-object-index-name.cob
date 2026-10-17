*>--- conformance-case
*> id: pb3165-method-condition-name-hides-object-index-name
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3165 review R2-1 (SymbolTable.IndexCandidates). ISO 11.7.4 GR5: "The declaration
      *>   in the containing object definition is inaccessible to this method" (cite.py --check
      *>   11.7.4 -> OK 11.7.4 5)), whatever the name class (8.4.6.1 "even when it is a different
      *>   type of user-defined word" -> OK 8.4.6.1). In SHOWX, QI is the method's condition-name,
      *>   so SET QI TO 2 names no index-name or data item; the object's index-name QI is not
      *>   reachable. Before the fix the SET moved the object's index.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3165NCP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3165NCC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3165NCC.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3165NCC "NEW" RETURNING OBJ
           INVOKE OBJ "SHOWX"
           STOP RUN.
       END PROGRAM PB3165NCP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3165NCC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OT.
          05 G PIC 9 OCCURS 5 INDEXED BY QI.
       PROCEDURE DIVISION.
       METHOD-ID. SHOWX.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 F PIC 9 VALUE 1.
          88 QI VALUE 1.
       PROCEDURE DIVISION.
           SET QI TO 2
           DISPLAY "Z".
       END METHOD SHOWX.
       END OBJECT.
       END CLASS PB3165NCC.
