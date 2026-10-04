      *> kb/Work PB1051 -- a METHOD formal declared BY VALUE (numeric, pointer and object-reference formals).
      *> ISO 14.2.2 SR2 (cite.py --check 14.2.2 "Each data-name-1 specified in a BY VALUE phrase shall be
      *> defined as a data item of class numeric, message-tag, object, or pointer" -> OK 14.2.2 2)).
      *> 14.9.23.3 SR5 b) (cite.py --check 14.9.23.3 "If a BY VALUE phrase is specified for an argument, a BY
      *> VALUE phrase shall be specified or implied for the corresponding formal parameter" -> OK 14.9.23.3 5) b))
      *> and 14.9.23.4 GR6 b) (cite.py --check 14.9.23.4 "When the BY VALUE phrase is specified or implied for the
      *> corresponding formal parameter, BY VALUE is assumed" -> OK 14.9.23.4 6) b)): a bare argument into a BY
      *> VALUE formal is BY VALUE. 14.2.3 GR10: the formal is a detached copy; the callee's store never reaches the
      *> caller. 14.2.3 GR4 (cite.py --check 14.2.3 "Both the BY REFERENCE and the BY VALUE phrases are
      *> transitive" -> OK 14.2.3 4)): the second formal of USING BY VALUE A B is BY VALUE too.
      *> 9.3.8.2.3 rule 1: the class implements the interface prototype with the identical BY VALUE formal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1051D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CBV
           INTERFACE IBV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CBV.
       01 N PIC S9(4) COMP-5 VALUE 42.
       01 M PIC S9(4) COMP-5 VALUE 5.
       01 K PIC S9(4) COMP-5 VALUE 0.
       01 E PIC -(4)9.
       01 PTR USAGE POINTER.
       01 OTH USAGE OBJECT REFERENCE CBV.
       01 IB USAGE OBJECT REFERENCE IBV.
       01 TOT PIC S9(6) COMP-5.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CBV "NEW" RETURNING O.
           INVOKE CBV "NEW" RETURNING OTH.
      *> numeric, written BY VALUE: the callee adds 1 to its copy
           INVOKE O "M" USING BY VALUE N.
           MOVE N TO E.
           DISPLAY "N=" E.
      *> the same with no phrase: the formal is BY VALUE, so the argument is (GR6 b))
           INVOKE O "M" USING N.
           MOVE N TO E.
           DISPLAY "N=" E.
      *> a literal, and the transitive second formal
           INVOKE O "ADD2" USING BY VALUE 1 2 RETURNING TOT.
           MOVE TOT TO E.
           DISPLAY "TOT=" E.
           INVOKE O "ADD2" USING N M RETURNING TOT.
           MOVE TOT TO E.
           DISPLAY "TOT=" E.
      *> pointer: the callee SETs its copy to NULL; the caller's pointer is untouched
           SET PTR TO ADDRESS OF N.
           INVOKE O "SETPTR" USING BY VALUE PTR.
           IF PTR = NULL
              DISPLAY "PTR NULL"
           ELSE
              DISPLAY "PTR KEPT"
           END-IF.
      *> object reference: the callee SETs its copy to NULL; the caller's reference is untouched
           INVOKE O "SETOBJ" USING BY VALUE OTH.
           IF OTH = NULL
              DISPLAY "OTH NULL"
           ELSE
              DISPLAY "OTH KEPT"
           END-IF.
      *> BY VALUE is transitive until BY REFERENCE (14.2.3 GR4): A is BY VALUE, B and C are BY REFERENCE, so
      *> the bare arguments for B and C are written back and the one for A is not
           MOVE 10 TO N.
           MOVE 20 TO M.
           MOVE 30 TO K.
           INVOKE O "MIX" USING N M K.
           MOVE N TO E.
           DISPLAY "MIX N=" E.
           MOVE M TO E.
           DISPLAY "MIX M=" E.
           MOVE K TO E.
           DISPLAY "MIX K=" E.
      *> an interface prototype with a BY VALUE formal, implemented identically
           SET IB TO O.
           MOVE 6 TO N.
           INVOKE IB "TWICE" USING BY VALUE N RETURNING TOT.
           MOVE TOT TO E.
           DISPLAY "TWICE=" E.
      *> the INLINE form (8.4.3.4.3): no BY phrase, so the BY VALUE formal makes the argument BY VALUE (GR6 b))
           MOVE O :: "TWICE" (N) TO TOT.
           MOVE TOT TO E.
           DISPLAY "INLINE=" E.
           STOP RUN.
       END PROGRAM PB1051D.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. IBV.
       PROCEDURE DIVISION.
       METHOD-ID. TWICE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC S9(4) COMP-5.
       01 R PIC S9(6) COMP-5.
       PROCEDURE DIVISION USING BY VALUE A RETURNING R.
       END METHOD TWICE.
       END INTERFACE IBV.

       IDENTIFICATION DIVISION.
       CLASS-ID. CBV INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE IBV.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS IBV.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 E PIC -(4)9.
       LINKAGE SECTION.
       01 LN PIC S9(4) COMP-5.
       PROCEDURE DIVISION USING BY VALUE LN.
           MOVE LN TO E.
           DISPLAY "LN=" E.
           ADD 1 TO LN.
       END METHOD M.
       METHOD-ID. ADD2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC S9(4) COMP-5.
       01 B PIC S9(4) COMP-5.
       01 R PIC S9(6) COMP-5.
       PROCEDURE DIVISION USING BY VALUE A B RETURNING R.
           COMPUTE R = A + B.
           MOVE 0 TO A.
       END METHOD ADD2.
       METHOD-ID. MIX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC S9(4) COMP-5.
       01 B PIC S9(4) COMP-5.
       01 C PIC S9(4) COMP-5.
       PROCEDURE DIVISION USING BY VALUE A BY REFERENCE B C.
           ADD 1 TO A.
           ADD 1 TO B.
           ADD 1 TO C.
       END METHOD MIX.
       METHOD-ID. SETPTR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE POINTER.
       PROCEDURE DIVISION USING BY VALUE LP.
           SET LP TO NULL.
       END METHOD SETPTR.
       METHOD-ID. SETOBJ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LO USAGE OBJECT REFERENCE CBV.
       PROCEDURE DIVISION USING BY VALUE LO.
           SET LO TO NULL.
       END METHOD SETOBJ.
       METHOD-ID. TWICE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC S9(4) COMP-5.
       01 R PIC S9(6) COMP-5.
       PROCEDURE DIVISION USING BY VALUE A RETURNING R.
           COMPUTE R = A * 2.
       END METHOD TWICE.
       END OBJECT.
       END CLASS CBV.
