      *> kb/Work PB1019 (method arm) -- ADDRESS OF a METHOD's own
      *> LINKAGE formal and RETURNING item.  ISO 8.4.3.11.3 SR1:
      *> identifier-1 "shall reference a data item defined in the file
      *> section, working-storage section, local-storage section, or
      *> linkage section"; 8.4.3.11.4 GR1: the result "contains the
      *> address of identifier-1".  A formal passed BY REFERENCE operates
      *> "as if the formal parameter occupies the same storage area as
      *> the argument" (14.2.3 GR8), so a store through a BASED item
      *> addressed at the formal reaches the invoker's argument; a store
      *> through the RETURNING item's address is the result placed into
      *> identifier-4 (14.9.23.4 GR8).  An OMITTED formal is not
      *> addressed.  Legs: alphanumeric, DISPLAY numeric, COMP, COMP-2,
      *> a group, and an overriding method that addresses the formals
      *> its base method does not.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1019BA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. BUMP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC 9(3).
       01 LC PIC S9(4) COMP.
       01 G.
          05 G1 PIC X(2).
          05 G2 PIC 9(2).
       PROCEDURE DIVISION USING LN LC G.
           ADD 1 TO LN LC.
       END METHOD BUMP.
       METHOD-ID. DBL.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 FB COMP-2 BASED.
       01 XB PIC X(3) BASED.
       LINKAGE SECTION.
       01 F COMP-2.
       01 X PIC X(3).
       01 R COMP-2.
       PROCEDURE DIVISION USING F OPTIONAL X RETURNING R.
           SET ADDRESS OF FB TO ADDRESS OF F
           COMPUTE FB = FB * 2
           IF X IS NOT OMITTED
               SET ADDRESS OF XB TO ADDRESS OF X
               DISPLAY "DBL X=" XB
               MOVE "YES" TO XB
           END-IF
           SET ADDRESS OF FB TO ADDRESS OF R
           COMPUTE FB = F + 1.
       END METHOD DBL.
       END OBJECT.
       END CLASS PB1019BA.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1019DE INHERITS PB1019BA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1019BA.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. BUMP OVERRIDE.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 P USAGE POINTER.
       01 NB PIC 9(3) BASED.
       01 CB PIC S9(4) COMP BASED.
       01 GB BASED.
          05 GB1 PIC X(2).
          05 GB2 PIC 9(2).
       LINKAGE SECTION.
       01 LN PIC 9(3).
       01 LC PIC S9(4) COMP.
       01 G.
          05 G1 PIC X(2).
          05 G2 PIC 9(2).
       PROCEDURE DIVISION USING LN LC G.
           SET P TO ADDRESS OF LN
           SET ADDRESS OF NB TO P
           SET ADDRESS OF CB TO ADDRESS OF LC
           SET ADDRESS OF GB TO ADDRESS OF G
           DISPLAY "DE GB=" GB " NB=" NB
           ADD 100 TO NB CB
           MOVE "ZZ" TO GB1
           ADD 5 TO GB2.
       END METHOD BUMP.
       END OBJECT.
       END CLASS PB1019DE.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1019OM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1019BA
           CLASS PB1019DE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B USAGE OBJECT REFERENCE PB1019BA.
       01 N PIC 9(3) VALUE 12.
       01 C PIC S9(4) COMP VALUE -7.
       01 WG.
          05 WG1 PIC X(2) VALUE "AB".
          05 WG2 PIC 9(2) VALUE 10.
       01 V COMP-2 VALUE 2.5.
       01 RV COMP-2 VALUE 0.
       01 T PIC X(3) VALUE "NO ".
       01 D PIC 9(3).99.
       01 E PIC -9(4).
       PROCEDURE DIVISION.
           INVOKE PB1019BA "NEW" RETURNING B
           INVOKE B "BUMP" USING N C WG
           MOVE C TO E
           DISPLAY "BA N=" N " C=" E " WG=" WG
           INVOKE PB1019DE "NEW" RETURNING B
           INVOKE B "BUMP" USING N C WG
           MOVE C TO E
           DISPLAY "DE N=" N " C=" E " WG=" WG
           INVOKE B "DBL" USING V T RETURNING RV
           MOVE V TO D
           DISPLAY "V=" D " T=" T
           MOVE RV TO D
           DISPLAY "RV=" D
           INVOKE B "DBL" USING V OMITTED RETURNING RV
           MOVE V TO D
           DISPLAY "V=" D
           MOVE RV TO D
           DISPLAY "RV=" D
           STOP RUN.
       END PROGRAM PB1019OM.
