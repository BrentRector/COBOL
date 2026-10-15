      *> kb/Work PB3165 - a method's named OCCURS DYNAMIC CAPACITY register is a name of the METHOD.
      *> ISO 13.18.38.3 SR30: data-name-3 "shall not be defined elsewhere in the source element" and "shall
      *>   be treated as though implicitly defined at the same level as the entry containing the OCCURS
      *>   clause", so it has the visibility of that entry's own data-names. A method definition is a
      *>   source element of its own (10.5), and ISO 11.7.4 GR5 makes a word declared in both a method and
      *>   its containing object definition mean the method's declaration, the object's being
      *>   "inaccessible to this method".
      *>   cite.py --check 13.18.38.3 "Data-name-3 shall not be defined elsewhere in the source element"
      *>     -> OK  13.18.38.3 30)
      *>   cite.py --check 11.7.4 "The declaration in the containing object definition is inaccessible to
      *>     this method" -> OK  11.7.4 5)
      *> SHOWA and SHOWB both declare Q2 (legal: two source elements, each its own SR30 scope); SHOWC
      *> declares QO, shadowing the object's QO, while SHOWB (which declares no QO) reads the object's.
      *> ISO 14.6.2.3.2 item 6: each dynamic-capacity table starts at the FROM (minimum) capacity.
      *>   cite.py --check 14.6.2.3.2 "the capacity of the table is set to the minimum capacity specified
      *>     in the corresponding OCCURS clause" -> OK  14.6.2.3.2 6)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3165P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3165C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3165C.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3165C "NEW" RETURNING OBJ
           INVOKE OBJ "SHOWA"
           INVOKE OBJ "SHOWB"
           INVOKE OBJ "SHOWC"
           STOP RUN.
       END PROGRAM PB3165P.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3165C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OU.
          05 F PIC 9(3) OCCURS DYNAMIC CAPACITY IN QO FROM 2 TO 10.
       PROCEDURE DIVISION.
       METHOD-ID. SHOWA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 U.
          05 F PIC 9(3) OCCURS DYNAMIC CAPACITY IN Q2 FROM 4 TO 10.
       PROCEDURE DIVISION.
           DISPLAY "A " Q2
           SET Q2 TO 7
           DISPLAY "A " Q2.
       END METHOD SHOWA.
       METHOD-ID. SHOWB.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 U.
          05 F PIC 9(3) OCCURS DYNAMIC CAPACITY IN Q2 FROM 5 TO 10.
       PROCEDURE DIVISION.
           DISPLAY "B " Q2 " " QO.
       END METHOD SHOWB.
       METHOD-ID. SHOWC.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 U.
          05 F PIC 9(3) OCCURS DYNAMIC CAPACITY IN QO FROM 6 TO 10.
       PROCEDURE DIVISION.
           DISPLAY "C " QO.
       END METHOD SHOWC.
       END OBJECT.
       END CLASS PB3165C.
