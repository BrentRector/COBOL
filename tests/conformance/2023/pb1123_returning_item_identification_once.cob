      *> PB1123 (CALL and INVOKE RETURNING arms) - ISO 14.9.23.4 GR7 a): "Arithmetic-expression-1,
      *>   boolean-expression-1, identifier-1, identifier-2, identifier-3, and identifier-5 are evaluated and item
      *>   identification is done for identifier-4 at the beginning of the execution of the INVOKE statement."
      *>   (cite.py --check 14.9.23.4 -> OK 7)); the CALL twin is 14.9.4.4 GR3 a).
      *> The method (program) adds 1 to its BY REFERENCE first argument I and returns 10 * its BY CONTENT
      *> second argument V = 5, so RETURNING T(I) delivers 50 into the occurrence identified at the START
      *> (T(1)); a store that re-evaluated I after the callee bumped it would land in T(2).
      *> C1 - CALL "BUMPP" USING I BY CONTENT V RETURNING T(I)  => I=0002 T1=0050 T2=0000 T3=0000
      *> I1 - INVOKE OBJ "BUMP" USING I BY CONTENT V RETURNING T(I)    => the same line.
      *>
      *>   C1=I=0002 T1=0050 T2=0000 T3=0000
      *>   I1=I=0002 T1=0050 T2=0000 T3=0000
       IDENTIFICATION DIVISION.
       CLASS-ID. CBUMP INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. BUMP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK PIC 9(4).
       01 LV PIC 9(4).
       01 LR PIC 9(4).
       PROCEDURE DIVISION USING LK LV RETURNING LR.
           ADD 1 TO LK
           COMPUTE LR = LV * 10.
       END METHOD BUMP.
       END OBJECT.
       END CLASS CBUMP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1123RET.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CBUMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE CBUMP.
       01 I PIC 9(4) VALUE 1.
       01 V PIC 9(4) VALUE 5.
       01 TT.
          05 T PIC 9(4) OCCURS 3 VALUE 0.
       PROCEDURE DIVISION.
           CALL "PB1123BUMPP" USING I BY CONTENT V RETURNING T(I)
           DISPLAY "C1=I=" I " T1=" T(1) " T2=" T(2) " T3=" T(3)
           MOVE 1 TO I
           MOVE 0 TO T(1) T(2) T(3)
           INVOKE CBUMP "NEW" RETURNING OBJ
           INVOKE OBJ "BUMP" USING I BY CONTENT V RETURNING T(I)
           DISPLAY "I1=I=" I " T1=" T(1) " T2=" T(2) " T3=" T(3)
           STOP RUN.
       END PROGRAM PB1123RET.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1123BUMPP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK PIC 9(4).
       01 LV PIC 9(4).
       01 LR PIC 9(4).
       PROCEDURE DIVISION USING LK LV RETURNING LR.
           ADD 1 TO LK
           COMPUTE LR = LV * 10
           GOBACK.
       END PROGRAM PB1123BUMPP.
