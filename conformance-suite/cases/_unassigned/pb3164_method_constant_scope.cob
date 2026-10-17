*>--- conformance-case
*> id: pb3164_method_constant_scope
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3164 - a method's CONSTANT entries are the method's
      *> own: a sibling method, and the object or factory definition,
      *> never see them, and a word the method's data division defines
      *> hides the containing definition's constant of that spelling.
      *> ISO 3.164: a source element is a "source unit excluding any
      *>   contained source units" -> OK 3.164, so a method definition
      *>   is a source element of its own; 8.4.6.1: "each source
      *>   element may use identical user-defined words to name items
      *>   independent of the use of these user-defined words by other
      *>   source elements" -> OK 8.4.6.1; 11.7.4 GR5: "If a given
      *>   user-defined word is defined in the data division of this
      *>   method definition and in the data division of the containing
      *>   object definition, the use of that word in this method refers
      *>   to the declaration in this method" -> OK 11.7.4 5); 13.10.3
      *>   SR9 compares the specifications of a duplicated constant-name
      *>   within one source element, so MSA's N AS 3 and MSB's N AS 4
      *>   are two names.
      *> Expected values:
      *>   FA - its own constant F (AS "G"), not the factory's data F;
      *>   FB - declares nothing: the factory's data item F, "F";
      *>   IA - its index-name K and condition-name C hide the object's
      *>        constants K and C: T is "ZZQ" after SET K TO 3 and MOVE
      *>        "Q" TO E(K), and condition C (V = "Y") is true;
      *>   IB - declares neither: the object's constants, W PIC X(2)
      *>        holds "AB", K is 2, C is "O";
      *>   MSA - W PIC X(N) with its own N AS 3 (declared after W):
      *>        "ABC", and N is 3;
      *>   MSB - its own N AS 4: V PIC X(4) holds "ABCD", N is 4;
      *>   MSC - declares nothing: N is the object's data item, 7 + 1 = 8;
      *>   MSD - its own data item C ("M") hides the object's constant C;
      *>   ME - its own constants CX AS 7 and CQ AS 2 hide the object's
      *>        condition-name CX and index-name CQ (11.7.4 GR5 is about
      *>        the word, whatever its kind; kb/Work PB3153 review round
      *>        3): "ME 7 2";
      *>   MF - declares neither: SET CQ TO 3 moves the object's index,
      *>        MOVE 5 TO OE(CQ), and the object's condition CX (OW = 1)
      *>        is true: "MF 5" and "MF CX".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3164M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3164S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3164S.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3164S "FA"
           INVOKE PB3164S "FB"
           INVOKE PB3164S "NEW" RETURNING OBJ
           INVOKE OBJ "IA"
           INVOKE OBJ "IB"
           INVOKE OBJ "MSA"
           INVOKE OBJ "MSB"
           INVOKE OBJ "MSC"
           INVOKE OBJ "MSD"
           INVOKE OBJ "ME"
           INVOKE OBJ "MF"
           STOP RUN.
       END PROGRAM PB3164M.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3164S INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F PIC X VALUE "F".
       PROCEDURE DIVISION.
       METHOD-ID. FA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 F CONSTANT AS "G".
       PROCEDURE DIVISION.
           DISPLAY "FA " F.
       END METHOD FA.
       METHOD-ID. FB.
       PROCEDURE DIVISION.
           DISPLAY "FB " F.
       END METHOD FB.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT AS 2.
       01 C CONSTANT AS "O".
       01 N PIC 9 VALUE 7.
       01 OW PIC 9 VALUE 1.
          88 CX VALUE 1.
       01 OT.
          05 OE PIC 9 OCCURS 3 INDEXED BY CQ VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. IA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 T.
          05 E PIC X OCCURS 3 INDEXED BY K VALUE "Z".
       01 V PIC X VALUE "Y".
          88 C VALUE "Y".
       PROCEDURE DIVISION.
           SET K TO 3
           MOVE "Q" TO E(K)
           DISPLAY "IA " T
           IF C
               DISPLAY "IA C"
           END-IF.
       END METHOD IA.
       METHOD-ID. IB.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 W PIC X(K) VALUE "AB".
       PROCEDURE DIVISION.
           DISPLAY "IB " W " " K " " C.
       END METHOD IB.
       METHOD-ID. MSA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 W PIC X(N) VALUE "ABC".
       01 N CONSTANT AS 3.
       PROCEDURE DIVISION.
           DISPLAY "MSA " W " " N.
       END METHOD MSA.
       METHOD-ID. MSB.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 N CONSTANT AS 4.
       01 V PIC X(N) VALUE "ABCD".
       PROCEDURE DIVISION.
           DISPLAY "MSB " V " " N.
       END METHOD MSB.
       METHOD-ID. MSC.
       PROCEDURE DIVISION.
           ADD 1 TO N
           DISPLAY "MSC " N.
       END METHOD MSC.
       METHOD-ID. MSD.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 C PIC X VALUE "M".
       PROCEDURE DIVISION.
           DISPLAY "MSD " C.
       END METHOD MSD.
       METHOD-ID. ME.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 CX CONSTANT AS 7.
       01 CQ CONSTANT AS 2.
       PROCEDURE DIVISION.
           DISPLAY "ME " CX " " CQ.
       END METHOD ME.
       METHOD-ID. MF.
       PROCEDURE DIVISION.
           SET CQ TO 3
           MOVE 5 TO OE(CQ)
           DISPLAY "MF " OE(3)
           IF CX
               DISPLAY "MF CX"
           END-IF.
       END METHOD MF.
       END OBJECT.
       END CLASS PB3164S.
