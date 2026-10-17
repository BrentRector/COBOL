      *> kb/Work PB258 - inventory row SR-14.9.4.3-5, the exception arm.
      *> ISO 14.9.4.3 SR5: "If the BY REFERENCE phrase is specified or
      *> implied for an identifier-2 and identifier-2 is not an
      *> address-identifier, it is a receiving operand." SR4: "... or if
      *> identifier-2 is an address-identifier, identifier-2 is a sending
      *> operand." The exception is load-bearing: 8.4.3.11.3 SR5 and
      *> 8.4.3.13.3 SR4 say of each address-identifier "This identifier
      *> format shall not be specified as a receiving operand", and Annex
      *> D.6.5.6.4: "it will never be updated even when passed by
      *> reference. It behaves like being passed by content."
      *>
      *> THE DISCRIMINATING CASE: in ONE statement, an address-identifier
      *> and an ordinary identifier-2, both BY REFERENCE (specified, then
      *> implied). Each callee stores into BOTH formals. The ordinary
      *> identifier-2 is receiving (14.2.3 GR8: the formal "occupies the
      *> same storage area as the argument"), so its store reaches the
      *> caller; the address-identifier is sending, so its store does not.
      *> In Format 2 the ordinary identifier-2 is itself a POINTER (SR10,
      *> the class-pointer ban, is a FORMAT 1 rule), so the two arguments
      *> share their class and differ ONLY in being an address-identifier.
      *>
      *> EXPECTED VALUES, DERIVED FROM THE RULES:
      *>  1. Format 1, BY REFERENCE ADDRESS OF R N. 8.4.3.11.4 GR1: the
      *>     pointer "contains the address of identifier-1", so LR=HEL and
      *>     the MOVE through it lands in R: R=XYZLO. The callee's ADD 5
      *>     reaches N (receiving): N=0015.
      *>  2. Format 1 again, bare: 14.9.4.4 GR5 assumes BY REFERENCE. The
      *>     first call's SET LA TO NULL did not reach anything (sending),
      *>     so the address is live: LR=XYZ; N receives again: N=0020.
      *>  3. Format 2 data arm, BY REFERENCE ADDRESS OF R, BY REFERENCE P
      *>     with P set to ADDRESS OF R: PA>XYZLO, PP>XYZLO. The callee
      *>     nulls both formals; P received the store: P=NULL.
      *>  4. Format 2 bare (14.9.4.4 GR9 a)1: an argument meeting SR3 is
      *>     BY REFERENCE), P NOT reset: PA>XYZLO (the address-identifier
      *>     is fresh) while PP=NULL (the store of call 3 persisted).
      *>  5. Format 2 program arm, the same shape over PROGRAM-POINTERs:
      *>     the address of PB258TG and Q set to it. Call 5: both
      *>     activate it (TARGET CALLED twice), the callee nulls both
      *>     formals, Q=NULL. Call 6, bare, Q not reset: the
      *>     address-identifier activates PB258TG (TARGET CALLED) and
      *>     QP=NULL.
      *> Below 2002 every address-identifier operand is refused by the
      *> edition gate: conformance:negative/pb239-call-address-identifier-at-85.
      *> 12.3.8.4 GR10 a)/b): each Format-2 callee is given a program
      *> prototype definition ahead of every other unit (10.6.2 SR1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB258PR IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PA USAGE POINTER.
       01 PP USAGE POINTER.
       PROCEDURE DIVISION USING BY REFERENCE PA PP.
       END PROGRAM PB258PR.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB258PQ IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 QA USAGE PROGRAM-POINTER.
       01 QP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION USING BY REFERENCE QA QP.
       END PROGRAM PB258PQ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB258MN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB258PR
           PROGRAM PB258PQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(5) VALUE "HELLO".
       01 N PIC 9(4) VALUE 10.
       01 P USAGE POINTER.
       01 Q USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB258F1" USING BY REFERENCE ADDRESS OF R N
           DISPLAY "R=" R " N=" N
           CALL "PB258F1" USING ADDRESS OF R N
           DISPLAY "R=" R " N=" N
           SET P TO ADDRESS OF R
           CALL PB258PR USING BY REFERENCE ADDRESS OF R
                              BY REFERENCE P
           IF P = NULL DISPLAY "P=NULL" ELSE DISPLAY "P=SET" END-IF
           CALL PB258PR USING ADDRESS OF R P
           SET Q TO ADDRESS OF PROGRAM "PB258TG"
           CALL PB258PQ USING BY REFERENCE
                              ADDRESS OF PROGRAM "PB258TG"
                              BY REFERENCE Q
           IF Q = NULL DISPLAY "Q=NULL" ELSE DISPLAY "Q=SET" END-IF
           CALL PB258PQ USING ADDRESS OF PROGRAM "PB258TG" Q
           STOP RUN.
       END PROGRAM PB258MN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB258F1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA USAGE POINTER.
       01 LN PIC 9(4).
       01 LR PIC X(3) BASED.
       PROCEDURE DIVISION USING LA LN.
       MAIN.
           IF LA = NULL
               DISPLAY "LA=NULL"
           ELSE
               SET ADDRESS OF LR TO LA
               DISPLAY "LR=" LR
               MOVE "XYZ" TO LR
           END-IF
           SET LA TO NULL
           ADD 5 TO LN
           GOBACK.
       END PROGRAM PB258F1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB258PR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PA USAGE POINTER.
       01 PP USAGE POINTER.
       01 LR PIC X(5) BASED.
       PROCEDURE DIVISION USING BY REFERENCE PA PP.
       MAIN.
           IF PA = NULL
               DISPLAY "PA=NULL"
           ELSE
               SET ADDRESS OF LR TO PA
               DISPLAY "PA>" LR
           END-IF
           IF PP = NULL
               DISPLAY "PP=NULL"
           ELSE
               SET ADDRESS OF LR TO PP
               DISPLAY "PP>" LR
           END-IF
           SET PA TO NULL
           SET PP TO NULL
           GOBACK.
       END PROGRAM PB258PR.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB258PQ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 QA USAGE PROGRAM-POINTER.
       01 QP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION USING BY REFERENCE QA QP.
       MAIN.
           IF QA = NULL
               DISPLAY "QA=NULL"
           ELSE
               CALL QA
           END-IF
           IF QP = NULL
               DISPLAY "QP=NULL"
           ELSE
               CALL QP
           END-IF
           SET QA TO NULL
           SET QP TO NULL
           GOBACK.
       END PROGRAM PB258PQ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB258TG.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "TARGET CALLED"
           GOBACK.
       END PROGRAM PB258TG.
