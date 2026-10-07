      *> kb/Work PB239 - an ADDRESS-IDENTIFIER as a CALL argument. ISO
      *> 14.9.4.3 SR3: "Identifier-2 shall reference an address-identifier
      *> or a data item defined in the file, working-storage, local-storage,
      *> or linkage section"; SR4: "... or if identifier-2 is an
      *> address-identifier, identifier-2 is a sending operand". The
      *> address-identifier is 8.4.3.1.2 identifier Format 9: the 8.4.3.11
      *> data arm (ADDRESS OF identifier-1) and the 8.4.3.13 program arm.
      *> Before PB239 every CALL below was a COBOL0001 parse error.
      *>
      *> EXPECTED VALUES, DERIVED FROM THE RULES:
      *>  1. ADDRESS OF R, BY REFERENCE implied (14.9.4.4 GR5). 8.4.3.11.4
      *>     GR1: the pointer "contains the address of identifier-1", so the
      *>     callee's BASED LR (14.9.39.4 GR13) sees R - LR=HEL - and its
      *>     MOVE lands in R: R=XYZLO. Its SET P TO NULL changes nothing
      *>     the caller can see (SR4/SR5: a sending operand; Annex
      *>     D.6.5.6.4 "it will never be updated even when passed by
      *>     reference") - the second call proves it: it reaches R again.
      *>  2. BY CONTENT ADDRESS OF E(2): the address OF THE OCCURRENCE
      *>     (8.4.3.11.4 GR1) - LR=BBB, then T=AAAXYZCCC.
      *>  3. BY VALUE ADDRESS OF R to a BY VALUE pointer formal (14.9.4.3
      *>     SR22 admits class pointer; Format 2 via the program prototype):
      *>     LV=XYZLO, then R=VVVVV.
      *>  4. ADDRESS OF PROGRAM "PB239TG": 8.4.3.13.4 GR1b - the callee's
      *>     program-pointer activates PB239TG: TARGET CALLED. A program
      *>     that cannot be located is GR4's predefined address NULL (EC
      *>     checking is not enabled, so nothing is raised): Q=NULL.
      *>  5. THE EVIDENCE HALF: a BY CONTENT data item. 14.2.3 GR9 - the
      *>     formal is a record "allocated by the activating runtime
      *>     element" into which the argument is moved, so the callee's
      *>     ADD 10 reaches that record and never N: N=005.
      *> kb/Work PB989 - ISO 12.3.8.4 GR10 a): the details come from a program definition specified PREVIOUSLY in the
      *> compilation group; a definition that follows the REPOSITORY entry is not one. The callee below is therefore
      *> given a program prototype definition (11.10.2 Format 2) ahead of every other unit (10.6.2 SR1), which
      *> supplies the details under GR10 b) - the same signature as the definition (10.6.2 SR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB239VL IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 V USAGE POINTER.
       PROCEDURE DIVISION USING BY VALUE V.
       END PROGRAM PB239VL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB239MN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB239VL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(5) VALUE "HELLO".
       01 T.
          05 E PIC X(3) OCCURS 3.
       01 N PIC 9(3) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "AAABBBCCC" TO T
           CALL "PB239SB" USING ADDRESS OF R
           DISPLAY "R=" R
           CALL "PB239SB" USING BY REFERENCE ADDRESS R
           DISPLAY "R=" R
           CALL "PB239SB" USING BY CONTENT ADDRESS OF E(2)
           DISPLAY "T=" T
           CALL PB239VL USING BY VALUE ADDRESS OF R
           DISPLAY "R=" R
           CALL "PB239PP" USING ADDRESS OF PROGRAM "PB239TG"
           CALL "PB239PP" USING BY CONTENT ADDRESS OF PROGRAM "PB239NO"
           CALL "PB239AD" USING BY CONTENT N
           DISPLAY "N=" N
           STOP RUN.
       END PROGRAM PB239MN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB239SB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P USAGE POINTER.
       01 LR PIC X(3) BASED.
       PROCEDURE DIVISION USING P.
       MAIN.
           SET ADDRESS OF LR TO P
           DISPLAY "LR=" LR
           MOVE "XYZ" TO LR
           SET P TO NULL
           EXIT PROGRAM.
       END PROGRAM PB239SB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB239VL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 V USAGE POINTER.
       01 LV PIC X(5) BASED.
       PROCEDURE DIVISION USING BY VALUE V.
       MAIN.
           SET ADDRESS OF LV TO V
           DISPLAY "LV=" LV
           MOVE "VVVVV" TO LV
           EXIT PROGRAM.
       END PROGRAM PB239VL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB239PP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 Q USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION USING Q.
       MAIN.
           IF Q = NULL
               DISPLAY "Q=NULL"
           ELSE
               CALL Q
           END-IF
           EXIT PROGRAM.
       END PROGRAM PB239PP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB239TG.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "TARGET CALLED"
           EXIT PROGRAM.
       END PROGRAM PB239TG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB239AD.
       DATA DIVISION.
       LINKAGE SECTION.
       01 F PIC 9(3).
       PROCEDURE DIVISION USING F.
       MAIN.
           ADD 10 TO F
           EXIT PROGRAM.
       END PROGRAM PB239AD.
