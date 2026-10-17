*>--- conformance-case
*> id: pb1019_address_of_linkage_formal
*> rules: [SR-8.4.3.11.3-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1019 -- ADDRESS OF a program's own LINKAGE formal.
      *> ISO 8.4.3.11.3 SR1: identifier-1 "shall reference a data item
      *> defined in the file section, working-storage section,
      *> local-storage section, or linkage section"; 8.4.3.11.4 GR1: the
      *> result "contains the address of identifier-1".  A formal passed
      *> BY REFERENCE operates "as if the formal parameter occupies the
      *> same storage area as the argument" (14.2.3 GR8), so a store
      *> through a BASED item addressed at the formal reaches the caller's
      *> argument; a BY CONTENT argument's copy absorbs it (14.2.3 GR9).
      *> The third leg: a GLOBAL formal addressed from a contained program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1019M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4) VALUE "ABCD".
       01 N PIC 9(3) VALUE 12.
       01 V PIC 9(2) VALUE 7.
       PROCEDURE DIVISION.
           CALL "PB1019C" USING A N BY CONTENT V
           DISPLAY "M A=" A " N=" N " V=" V
           STOP RUN.
       END PROGRAM PB1019M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1019C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 AB PIC X(4) BASED.
       01 NB PIC 9(3) BASED.
       01 VB PIC 9(2) BASED.
       LINKAGE SECTION.
       01 L PIC X(4) GLOBAL.
       01 LN PIC 9(3).
       01 LV PIC 9(2).
       PROCEDURE DIVISION USING L LN LV.
           SET P TO ADDRESS OF L
           SET ADDRESS OF AB TO P
           DISPLAY "C AB=" AB
           SET ADDRESS OF NB TO ADDRESS OF LN
           ADD 100 TO NB
           SET ADDRESS OF VB TO ADDRESS OF LV
           ADD 1 TO VB
           DISPLAY "C LN=" LN " LV=" LV
           CALL "PB1019N"
           DISPLAY "C L=" L
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1019N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XB PIC X(4) BASED.
       PROCEDURE DIVISION.
           SET ADDRESS OF XB TO ADDRESS OF L
           DISPLAY "N XB=" XB
           MOVE "WXYZ" TO XB
           GOBACK.
       END PROGRAM PB1019N.
       END PROGRAM PB1019C.
