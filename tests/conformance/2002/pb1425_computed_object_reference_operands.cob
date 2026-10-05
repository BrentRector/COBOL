      *> kb/Work PB1425 (with PB1197) - an object-reference position takes every identifier format that
      *> can yield an object reference.
      *>
      *> THE RULES. ISO/IEC 1989:2023 14.9.23.3 SR1: "Identifier-1 shall be an object reference." 14.9.29.3
      *> SR2: "Identifier-1 shall be an object reference; the predefined object references NULL and SUPER
      *> shall not be specified." 8.4.3.1.3 SR1 makes an identifier "any of the formats for an identifier", so
      *> an inline method invocation (8.4.3.1.2 Format 4) and a function-identifier (Format 1) are legal there
      *> whenever the item they reference is an object reference: 8.4.3.4.4 GR1 - "An inline method
      *> invocation references a temporary data item with the same class, category, and content as the
      *> temp-identifier", whose description is the method's RETURNING item (GR1 b); 8.4.3.2.1 - a
      *> function-identifier references "the unique data item that results from the evaluation of a function".
      *> A function-identifier is also a legal identifier-1 of an inline invocation (8.4.3.1.3 SR1).
      *>
      *> THE CLASS. PB1425K holds a number NM. SETNM stores it, WHO returns it, ME returns SELF (typed
      *> PB1425K), SPEAK displays SPOKEN-<NM>. The function PB1425MK (n) makes a new PB1425K whose NM is n.
      *>
      *> EXPECTED OUTPUT
      *>   0007          INVOKE A1 :: "ME" "WHO" RETURNING W - the receiver is the temporary ME returns: A1.
      *>   0001          INVOKE FUNCTION PB1425MK (1) "WHO" RETURNING W - the function's new object.
      *>   SPOKEN-0002   INVOKE PB1425MK (2) "SPEAK" - the FUNCTION keyword omitted (8.4.3.2.3 SR2).
      *>   SPOKEN-0007   INVOKE A1 :: "ME" :: " ME " "SPEAK" - a chained segment; the spaces around ME are not
      *>                 part of the method name (the externalized-name formation, DOC-A.1-68).
      *>   0003          MOVE FUNCTION PB1425MK (3) :: "WHO" TO W - a function-identifier as identifier-1.
      *>   CAUGHT-0007   RAISE A1 :: "ME" - the raised object is A1's; the declarative for class PB1425K runs
      *>   AFTER-1       and execution continues after the RAISE (14.9.29.4).
      *>   CAUGHT-0004   RAISE FUNCTION PB1425MK (4)
      *>   AFTER-2
      *>   0005          INVOKE FUNCTION PB1425UF (5) "WHO" RETURNING W - PB1425UF returns a UNIVERSAL object
      *>                 reference, so the INVOKE takes the dynamic path (14.9.23.4 GR7 c)).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1425MK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425K.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-SEED PIC 9(4).
       01 L-OBJ USAGE OBJECT REFERENCE PB1425K.
       PROCEDURE DIVISION USING L-SEED RETURNING L-OBJ.
           INVOKE PB1425K "NEW" RETURNING L-OBJ
           INVOKE L-OBJ "SETNM" USING BY CONTENT L-SEED
           GOBACK.
       END FUNCTION PB1425MK.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1425UF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425K.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 S-SEED PIC 9(4).
       LINKAGE SECTION.
       01 L-SEED PIC 9(4).
       01 L-ANY USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION USING L-SEED RETURNING L-ANY.
           MOVE L-SEED TO S-SEED
           INVOKE PB1425K "NEW" RETURNING L-ANY
           INVOKE L-ANY "SETNM" USING S-SEED
           GOBACK.
       END FUNCTION PB1425UF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425K
           FUNCTION PB1425MK
           FUNCTION PB1425UF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 USAGE OBJECT REFERENCE PB1425K.
       01 U  USAGE OBJECT REFERENCE.
       01 W  PIC 9(4).
       01 SEVEN PIC 9(4) VALUE 7.
       01 RAISES PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       K-SEC SECTION.
           USE AFTER EXCEPTION OBJECT PB1425K.
       K-P.
           SET U TO EXCEPTION-OBJECT
           INVOKE U "WHO" RETURNING W
           DISPLAY "CAUGHT-" W.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE PB1425K "NEW" RETURNING A1
           INVOKE A1 "SETNM" USING SEVEN
           INVOKE A1 :: "ME" "WHO" RETURNING W
           DISPLAY W
           INVOKE FUNCTION PB1425MK (1) "WHO" RETURNING W
           DISPLAY W
           INVOKE PB1425MK (2) "SPEAK"
           INVOKE A1 :: "ME" :: " ME " "SPEAK"
           MOVE FUNCTION PB1425MK (3) :: "WHO" TO W
           DISPLAY W
           RAISE A1 :: "ME"
           ADD 1 TO RAISES
           DISPLAY "AFTER-" RAISES
           RAISE FUNCTION PB1425MK (4)
           ADD 1 TO RAISES
           DISPLAY "AFTER-" RAISES
           INVOKE FUNCTION PB1425UF (5) "WHO" RETURNING W
           DISPLAY W
           STOP RUN.
       END PROGRAM PB1425P.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NM PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. SETNM.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC 9(4).
       PROCEDURE DIVISION USING LN.
           MOVE LN TO NM.
       END METHOD SETNM.
       METHOD-ID. WHO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LW PIC 9(4).
       PROCEDURE DIVISION RETURNING LW.
           MOVE NM TO LW.
       END METHOD WHO.
       METHOD-ID. ME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-ME USAGE OBJECT REFERENCE PB1425K.
       PROCEDURE DIVISION RETURNING LK-ME.
           SET LK-ME TO SELF.
       END METHOD ME.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
           DISPLAY "SPOKEN-" NM.
       END METHOD SPEAK.
       END OBJECT.
       END CLASS PB1425K.
