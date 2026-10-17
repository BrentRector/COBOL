*>--- conformance-case
*> id: pb3154_half_capacity_register_hides_symbolic
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3154 review R2-1 - inside a class definition, a named
      *>   OCCURS DYNAMIC CAPACITY register of the OBJECT, or of one
      *>   METHOD, HIDES the class's SYMBOLIC CHARACTERS name of the
      *>   same spelling; a sibling method still sees the symbolic.
      *> ISO 13.18.38.3 SR30: "Data-name-3 shall not be defined elsewhere
      *>   in the source element" -> OK 13.18.38.3 30); the register is
      *>   a data-name of the element that declares the table.
      *> ISO 8.4.6.1: "even when it is a different type of user-defined
      *>   word, is to the item which that source element describes"
      *>   -> OK 8.4.6.1.
      *> ISO 8.4.6.2.1 3) a): "If the name is declared in source element
      *>   B, the item in source element B is the referenced item"
      *>   -> OK 8.4.6.2.1 3) a).
      *> NESTING: class PB3154RC declares the symbolic-characters S1
      *>   (67) and S2 (68). Its OBJECT's table names its register S1
      *>   (FROM 3); its method SHOWA's local table names its register
      *>   S2 (FROM 4); SHOWB declares nothing.
      *> EXPECTED OUTPUT, DERIVED:
      *>   A1 003   SHOWA: S1 is the object's register, capacity 3.
      *>   A2 004   SHOWA: S2 is the method's own register, capacity 4.
      *>   B1 003   SHOWB: S1 is the object's register.
      *>   B2 C     SHOWB: S2 is the class's symbolic, ordinal 68 =
      *>            X"43" = "C" (12.3.7.4 GR11); SHOWA's register is
      *>            SHOWA's own.
      *> Before the fix the registers were not asked: MOVE S1 and
      *>   MOVE S2 moved the figuratives ALL "B" and ALL "C" (refused
      *>   COBOLNET0902 at 2023).
      *> kb/Work PB3165 moved a method's registers into the method's
      *>   own scope; the method tier asked the class's register index
      *>   and A2 printed CCC (the PB3165 merge-queue bounce) until it
      *>   asked OoMethodDataScope.Declares, every method name index.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3154RP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3154RC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3154RC.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3154RC "NEW" RETURNING OBJ
           INVOKE OBJ "SHOWA"
           INVOKE OBJ "SHOWB"
           STOP RUN.
       END PROGRAM PB3154RP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3154RC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS S1 S2 ARE 67 68.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC 9(3) OCCURS DYNAMIC CAPACITY IN S1 FROM 3 TO 10.
       01 N PIC 9(3).
       PROCEDURE DIVISION.
       METHOD-ID. SHOWA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 U.
          05 F PIC 9(3) OCCURS DYNAMIC CAPACITY IN S2 FROM 4 TO 10.
       PROCEDURE DIVISION.
           MOVE S1 TO N
           DISPLAY "A1 " N
           MOVE S2 TO N
           DISPLAY "A2 " N.
       END METHOD SHOWA.
       METHOD-ID. SHOWB.
       PROCEDURE DIVISION.
           MOVE S1 TO N
           DISPLAY "B1 " N
           DISPLAY "B2 " S2.
       END METHOD SHOWB.
       END OBJECT.
       END CLASS PB3154RC.
