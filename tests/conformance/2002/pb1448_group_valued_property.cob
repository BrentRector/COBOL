      *> kb/Work PB1448 - an object property whose accessors describe a GROUP is a legal reference. ISO/IEC 1989:2023
      *> section 8.4.3.9.4 GR1: "The data description of temp-1 is the same as the data description of the item
      *> specified in the RETURNING phrase of the get property method."
      *>   cite.py: OK  8.4.3.9.4 1)  (General rules)
      *> GR2: "The data description of temp-2 is the same as the data description of the item specified as the USING
      *> parameter of the set property method."
      *>   cite.py: OK  8.4.3.9.4 2)  (General rules)
      *> GR3: "temp-1 and temp-2 are the same temporary data item, where temp-2 redefines temp-1."
      *>   cite.py: OK  8.4.3.9.4 3)  (General rules)
      *> SR5: "This object property may be specified wherever a data item with that description would be valid as a
      *> sending item." SR6 says the same for a receiving item.
      *>   cite.py: OK  8.4.3.9.3 5)  and  8.4.3.9.3 6)  (Syntax rules)
      *>
      *> DERIVATION. The class's group (R1 PIC 9(3), R2 PIC X(3)) holds "123DEF". A group is a valid MOVE sender,
      *> a valid MOVE receiver and a valid INSPECT subject, so each use of GRP OF A is legal.
      *> SEND (GR1, sending only): the GET runs, then the group moves into W: "123DEF".
      *> RECV (GR2, receiving only): the SET runs with the group "456XYZ" and the GET does NOT run for the MOVE;
      *> the later MOVE of the property into W runs the GET and reads "456XYZ".
      *> RMW (GR3, sending and receiving): INSPECT reads the group (GET), replaces every "X" by "Z" in it and
      *> stores it back (SET with "456ZYZ"); a last MOVE reads it again.
      *> The trace lines GET-CALLED / SET-CALLED prove which accessor ran, and in which order.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1448GP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1448GC.
           PROPERTY GRP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE PB1448GC.
       01 W PIC X(6).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1448GC "NEW" RETURNING A.
           MOVE GRP OF A TO W.
           DISPLAY "SEND=" W.
           MOVE "456XYZ" TO GRP OF A.
           MOVE GRP OF A TO W.
           DISPLAY "RECV=" W.
           INSPECT GRP OF A REPLACING ALL "X" BY "Z".
           MOVE GRP OF A TO W.
           DISPLAY "RMW=" W.
           STOP RUN.
       END PROGRAM PB1448GP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1448GC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-GRP PIC X(6) VALUE "123DEF".
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY GRP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R.
          05 R1 PIC 9(3).
          05 R2 PIC X(3).
       PROCEDURE DIVISION RETURNING LK-R.
       MAIN.
           DISPLAY "GET-CALLED".
           MOVE W-GRP TO LK-R.
       END METHOD.
       METHOD-ID. SET PROPERTY GRP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V.
          05 V1 PIC 9(3).
          05 V2 PIC X(3).
       PROCEDURE DIVISION USING LK-V.
       MAIN.
           DISPLAY "SET-CALLED " LK-V.
           MOVE LK-V TO W-GRP.
       END METHOD.
       END OBJECT.
       END CLASS PB1448GC.
