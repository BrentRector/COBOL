*>--- conformance-case
*> id: pb3165-method-capacity-register-in-sibling-method
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3165 - ISO 13.18.38.3 SR30: data-name-3 is implicitly defined at the OCCURS entry,
      *>   at the same level as it, so it is a name of the entry's source element - here SHOWA, a method
      *>   definition, which is a source element of its own (10.5). SHOWB is another source element, so
      *>   its reference to Q2 identifies nothing (8.4.2.1: "a statement shall contain a reference that
      *>   uniquely identifies that resource"): COBOLNET1639. The register used to leak to the sibling
      *>   and the backend failed with CS0103.
      *>   cite.py --check 13.18.38.3 "Data-name-3 shall not be defined elsewhere in the source element"
      *>     -> OK  13.18.38.3 30)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3165N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3165NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3165NC.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3165NC "NEW" RETURNING OBJ
           INVOKE OBJ "SHOWB"
           STOP RUN.
       END PROGRAM PB3165N.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3165NC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOWA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 U.
          05 F PIC 9(3) OCCURS DYNAMIC CAPACITY IN Q2 FROM 4 TO 10.
       PROCEDURE DIVISION.
           DISPLAY "A " Q2.
       END METHOD SHOWA.
       METHOD-ID. SHOWB.
       PROCEDURE DIVISION.
           DISPLAY "B " Q2.
       END METHOD SHOWB.
       END OBJECT.
       END CLASS PB3165NC.
