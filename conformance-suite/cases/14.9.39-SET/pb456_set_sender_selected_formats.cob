*>--- conformance-case
*> id: pb456_set_sender_selected_formats
*> rules: [SR-14.9.39.3-8, SR-14.9.39.3-17]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.2 Formats 5 and 7 - the COMPLEMENT of the sender tie-break (kb/Work PB456). A sender of
      *> class object or of category data-pointer identifies its general format only where the RECEIVING list
      *> could not: this golden writes the statements where the receiving list already decides, and they must
      *> still bind and RUN. Together with 85/pb449_set_format_selection_receiving_list - which writes the
      *> Format-1 senders the tie-break must NEVER claim (an index-name and an index data item) - the two sides
      *> of the selector are measured, not just the side that rejects.
      *>
      *> EXPECTED VALUES, DERIVED FROM THE GENERAL RULES, NOT MEASURED:
      *>   SET P1 TO ADDRESS OF WS-A - Format 7, GR16: P1 contains the address of WS-A.
      *>   SET P2 TO P1              - Format 7 with identifier-6 as the sender (SR17/SR18), GR17: the content
      *>                               of P1 is placed in P2, so P2 addresses WS-A too.
      *>   SET ADDRESS OF LK TO P2   - Format 7 receiving ADDRESS OF, GR15: LK is based at that address, so
      *>                               DISPLAY LK reads WS-A's VALUE - "ABCD".
      *>   SET U TO G                - Format 5, SR8 + SR10: a UNIVERSAL receiving operand takes any object
      *>                               reference, so U references the CPB456X instance INVOKE created, and the
      *>                               8.8.4.4 object-reference relation to NULL is FALSE - "U=OBJ".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB456-SET-SENDER-SEL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB456X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       01 G USAGE OBJECT REFERENCE CPB456X.
       01 WS-A PIC X(4) VALUE "ABCD".
       01 P1 USAGE POINTER.
       01 P2 USAGE POINTER.
       LINKAGE SECTION.
       01 LK PIC X(4) BASED.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET P1 TO ADDRESS OF WS-A
           SET P2 TO P1
           SET ADDRESS OF LK TO P2
           DISPLAY "LK=" LK
           INVOKE CPB456X "NEW" RETURNING G
           SET U TO G
           IF U = NULL
               DISPLAY "U=NULL"
           ELSE
               DISPLAY "U=OBJ"
           END-IF
           STOP RUN.
       END PROGRAM PB456-SET-SENDER-SEL.

       IDENTIFICATION DIVISION.
       CLASS-ID. CPB456X INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS CPB456X.
