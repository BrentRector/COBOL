*>--- conformance-case
*> id: pb3165_method_capacity_register_scope
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
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
      *> SHOWA and SHOWB both declare Q2 (legal: two source elements, each its own SR30 scope; the SR30
      *> duplicate test in DataBinder.DynamicResolve judges each register against its own method's names
      *> only, kb/Work PB3153 review R1-2); SHOWC
      *> declares QO, shadowing the object's QO, while SHOWB (which declares no QO) reads the object's.
      *> SHOWD declares an INDEX-name QO that hides the object's QO register the same way (11.7.4 GR5 is about the
      *> word, whatever its name class): SET QO TO 3 moves SHOWD's own index, and the trailing SHOWB shows the
      *> object's register still at its minimum capacity (branch review finding R1-1).
      *> SHOWE declares a CONDITION-name QO, which hides the object's QO register too (11.7.4 GR5: a
      *> word "defined in the data division of this method definition"): SET QO TO TRUE sets SHOWE's
      *> own W to 1, and IF QO tests it (SymbolTable.CapacityCandidates and the literal-alias lookups
      *> ask OoMethodDataScope.Declares, every method name index; the PB3165 merge-queue bounce).
      *> SHOWF declares a CAPACITY register QX spelled like the object's INDEX-name QX: COMPUTE N = QX
      *> reads SHOWF's own register (3, its minimum capacity), not the object's index, which as an
      *> arithmetic operand would be refused (SymbolTable.IndexCandidates asks OoMethodDataScope.Declares;
      *> branch review finding R2-1).
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
           INVOKE OBJ "SHOWD"
           INVOKE OBJ "SHOWE"
           INVOKE OBJ "SHOWF"
           INVOKE OBJ "SHOWB"
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
       01 OV.
          05 H PIC 9 OCCURS 5 INDEXED BY QX.
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
       METHOD-ID. SHOWD.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 T.
          05 G PIC 9 OCCURS 5 INDEXED BY QO.
       01 N PIC 9.
       PROCEDURE DIVISION.
           SET QO TO 3
           MOVE 7 TO G(QO)
           SET N TO QO
           DISPLAY "D " G(QO) " " G(3) " " N.
       END METHOD SHOWD.
       METHOD-ID. SHOWE.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 W PIC 9 VALUE 0.
          88 QO VALUE 1.
       PROCEDURE DIVISION.
           SET QO TO TRUE
           IF QO DISPLAY "E " W.
       END METHOD SHOWE.
       METHOD-ID. SHOWF.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 V.
          05 K PIC 9 OCCURS DYNAMIC CAPACITY IN QX FROM 3 TO 10.
       01 N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
           COMPUTE N = QX
           DISPLAY "F " N.
       END METHOD SHOWF.
       END OBJECT.
       END CLASS PB3165C.
