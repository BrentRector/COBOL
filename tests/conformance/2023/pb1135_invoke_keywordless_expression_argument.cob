      *> PB1135 - ISO 14.9.23.2 prints BY CONTENT and BY VALUE as OPTIONAL
      *>   over {arithmetic-expression-1 | boolean-expression-1 |
      *>   identifier-5 | literal-2}, so an expression written with no BY is
      *>   a legal INVOKE argument; 14.9.23.4 GR6 a) 2 makes it BY CONTENT.
      *> cite.py --check 14.9.23.4 "If an argument is specified without any
      *>   of the keywords BY REFERENCE, BY CONTENT, or BY VALUE" -> OK
      *>   14.9.23.4 6)
      *> DECISION R59: a keyword-less N + 1 is ONE argument, the
      *>   arithmetic-expression-1 of the format (CALL's twin: CALL SR20 and
      *>   8.7.1 "preceded by a space and followed by a space" make the + the
      *>   binary operator; see pb130_call_format2_bare). The earlier list
      *>   reading (the two arguments N and +1) is gone.
      *> Derivation (N=5, M=7, B1=1100, B2=1010):
      *>   N * 2          -> one argument, +0010
      *>   (N + 1)        -> one argument, +0006
      *>   N + 1          -> one argument, +0006
      *>   N - 1          -> one argument, +0004
      *>   5 + 1          -> one argument (a numeric literal as the left
      *>                     operand), +0006
      *>   N M * 3        -> two arguments N and M * 3 : +0005 +0021
      *>   N M + 1        -> two arguments N and M + 1 : +0005 +0008
      *>   N + 1 M        -> two arguments N + 1 and M : +0006 +0007
      *>   (N + 1) (M - 1)-> +0006 +0006
      *>   B1 B-AND B2    -> one boolean argument, 1000
      *>   B1 B-AND B2 B2 B-OR B1 -> two boolean arguments, 1000 1110
      *>   BY CONTENT N+1 -> +0006 (the spelled form still works)
      *>   BY CONTENT 5 + 1 -> +0006
      *> A SOLE identifier stays identifier-5 (14.9.23.4 GR6 a) 1: it meets
      *>   SR9, so BY REFERENCE): TAKEW's store to its formal reaches N.
       IDENTIFICATION DIVISION.
       CLASS-ID. CPRB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKEN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC S9(4) SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION USING P.
           DISPLAY "P=" P.
       END METHOD TAKEN.
       METHOD-ID. TAKE2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P1 PIC S9(4) SIGN IS LEADING SEPARATE.
       01 P2 PIC S9(4) SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION USING P1 P2.
           DISPLAY "P1=" P1 " P2=" P2.
       END METHOD TAKE2.
       METHOD-ID. TAKEB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 Q PIC 1(4).
       PROCEDURE DIVISION USING Q.
           DISPLAY "Q=" Q.
       END METHOD TAKEB.
       METHOD-ID. TAKEB2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 Q1 PIC 1(4).
       01 Q2 PIC 1(4).
       PROCEDURE DIVISION USING Q1 Q2.
           DISPLAY "Q1=" Q1 " Q2=" Q2.
       END METHOD TAKEB2.
       METHOD-ID. TAKEW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PW PIC S9(4) SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION USING PW.
           MOVE 99 TO PW.
       END METHOD TAKEW.
       END OBJECT.
       END CLASS CPRB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1135.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPRB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CPRB.
       01 N PIC S9(4) SIGN IS LEADING SEPARATE VALUE 5.
       01 M PIC S9(4) SIGN IS LEADING SEPARATE VALUE 7.
       01 B1 PIC 1(4) VALUE B"1100".
       01 B2 PIC 1(4) VALUE B"1010".
       PROCEDURE DIVISION.
           INVOKE CPRB "NEW" RETURNING O
           INVOKE O "TAKEN" USING N * 2
           INVOKE O "TAKEN" USING (N + 1)
           INVOKE O "TAKEN" USING N + 1
           INVOKE O "TAKEN" USING N - 1
           INVOKE O "TAKEN" USING 5 + 1
           INVOKE O "TAKE2" USING N M * 3
           INVOKE O "TAKE2" USING N M + 1
           INVOKE O "TAKE2" USING N + 1 M
           INVOKE O "TAKE2" USING (N + 1) (M - 1)
           INVOKE O "TAKEB" USING B1 B-AND B2
           INVOKE O "TAKEB2" USING B1 B-AND B2 B2 B-OR B1
           INVOKE O "TAKEN" USING BY CONTENT N + 1
           INVOKE O "TAKEN" USING BY CONTENT 5 + 1
           INVOKE O "TAKEW" USING N
           DISPLAY "N=" N
           STOP RUN.
