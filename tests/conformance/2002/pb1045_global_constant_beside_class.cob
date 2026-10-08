      *> kb/Work PB1045 - ISO §13.18.27.3 4) bars the GLOBAL clause in a factory, instance or method
      *> definition, and ONLY there: the screen is per definition, never per compilation group.
      *> RULE §13.18.27.3 4): "The GLOBAL clause shall not be specified in a factory definition, an
      *>   instance definition, or a method definition." cite.py --check 13.18.27.3 "The GLOBAL clause
      *>   shall not be specified in a factory definition, an instance definition, or a method
      *>   definition" -> OK §13.18.27.3 4)
      *> RULE §13.18.27.3 1) a): "A constant entry." -> OK §13.18.27.3 1) a) (a program's constant
      *>   entry may carry GLOBAL), and §13.18.27.4 2): "A statement in a program contained directly or
      *>   indirectly within a program that describes a global name may reference that name without
      *>   describing it again." -> OK §13.18.27.4 2)
      *> SHAPE: PB1045P declares the GLOBAL constant KG and contains PB1045N; the class PB1045C in the
      *>   same compilation group declares constants WITHOUT the clause in its OBJECT working storage
      *>   (KO) and in its method's local storage (KM).
      *> EXPECTED OUTPUT, DERIVED (a constant-name is "as if literal-1 ... were written where
      *>   constant-name-1 is written", §13.10.4 1) -> OK):
      *>   NESTED 7  PB1045N references the container's global constant-name KG.
      *>   OBJECT 5 3  the class's own constants, legal because neither carries GLOBAL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1045P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1045C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KG CONSTANT IS GLOBAL AS 7.
       01 O USAGE OBJECT REFERENCE PB1045C.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB1045N".
           INVOKE PB1045C "NEW" RETURNING O.
           INVOKE O "SHOW".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1045N.
       PROCEDURE DIVISION.
           DISPLAY "NESTED " KG.
           GOBACK.
       END PROGRAM PB1045N.
       END PROGRAM PB1045P.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1045C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KO CONSTANT AS 5.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 KM CONSTANT AS 3.
       PROCEDURE DIVISION.
           DISPLAY "OBJECT " KO " " KM.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB1045C.
