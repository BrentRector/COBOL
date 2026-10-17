*>--- conformance-case
*> id: pb3165-method-capacity-register-duplicates-method-data-name
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3165 - the SR30 duplicate check is made within the METHOD's source element: Q2 below
      *>   is both the method's own data-name and its table's CAPACITY register, which 13.18.38.3 SR30
      *>   forbids ("Data-name-3 shall not be defined elsewhere in the source element"), COBOLNET1523.
      *>   (The check against the method's names, not the object's, must still fire after the
      *>   register moved into the method's own scope.)
      *>   cite.py --check 13.18.38.3 "Data-name-3 shall not be defined elsewhere in the source element"
      *>     -> OK  13.18.38.3 30)
       IDENTIFICATION DIVISION.
       CLASS-ID. PB3165DC INHERITS FROM BASE.
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
       01 Q2 PIC 9.
       01 U.
          05 F PIC 9(3) OCCURS DYNAMIC CAPACITY IN Q2 FROM 4 TO 10.
       PROCEDURE DIVISION.
           DISPLAY "A " Q2.
       END METHOD SHOWA.
       END OBJECT.
       END CLASS PB3165DC.
