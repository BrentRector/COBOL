      *> kb/Work PB988 -- every end-marker alternative of ISO 10.7.2,
      *> each written the way 10.7.3 permits.  The names are COBOL words,
      *> which compare case-insensitively (8.3.2.2), so a lowercase or
      *> mixed-case end marker is identical to its upper-case ID name:
      *>   SR2  END PROGRAM pb988n / Pb988M  -- the program-names declared
      *>   SR3  the contained PB988N's marker precedes PB988M's
      *>   SR7  END FUNCTION pb988f          -- the user-function-name
      *>   SR8  END PROGRAM pb988pp          -- the program-prototype-name
      *>   SR9  END FUNCTION pb988fp         -- the function-prototype-name
      *>   SR4  END CLASS Pb988c; SR6 END INTERFACE pb988i
      *>   SR5  END METHOD twice (a factory and an interface prototype),
      *>        and a bare END METHOD on the GET PROPERTY method (its
      *>        method-name-1 "shall be omitted")
      *>   SR1  PB988L, the LAST source unit, contains nothing and
      *>        precedes nothing, so it may omit its END PROGRAM.
      *> Expected (the statements run in order; 8.4.3.2.4 for the
      *> function, 14.9.23 for INVOKE, 8.4.3.9.4 GR1 for the property):
      *>   NESTED        the contained program PB988N
      *>   F=14          FUNCTION PB988F(7) returns 7 * 2
      *>   MAKE          the factory method MAKE
      *>   V=5           the GET PROPERTY method returns W-V VALUE 5
      *>   LAST          the last program PB988L
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB988PP IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-A PIC 9.
       PROCEDURE DIVISION USING LK-A.
       END PROGRAM pb988pp.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB988FP IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-B PIC 9.
       PROCEDURE DIVISION RETURNING LK-B.
       END FUNCTION pb988fp.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB988F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-X PIC 99.
       01 LK-R PIC 99.
       PROCEDURE DIVISION USING LK-X RETURNING LK-R.
       FMAIN.
           COMPUTE LK-R = LK-X * 2.
           GOBACK.
       END FUNCTION pb988f.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB988M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB988C
           FUNCTION PB988F
           PROPERTY V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB988C.
       01 R PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB988N".
           COMPUTE R = FUNCTION PB988F(7).
           DISPLAY "F=" R.
           INVOKE PB988C "MAKE" RETURNING O.
           DISPLAY "V=" V OF O.
           CALL "PB988L".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB988N.
       PROCEDURE DIVISION.
       NMAIN.
           DISPLAY "NESTED".
           GOBACK.
       END PROGRAM pb988n.
       END PROGRAM Pb988M.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB988I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD ping.
       END INTERFACE pb988i.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB988C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. MAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-O USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING LK-O.
       MMAIN.
           DISPLAY "MAKE".
           INVOKE SELF "NEW" RETURNING LK-O.
       END METHOD Make.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-V PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. GET PROPERTY V.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC 9.
       PROCEDURE DIVISION RETURNING LK-V.
       GMAIN.
           MOVE W-V TO LK-V.
       END METHOD.
       END OBJECT.
       END CLASS Pb988c.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB988L.
       PROCEDURE DIVISION.
       LMAIN.
           DISPLAY "LAST".
           GOBACK.
