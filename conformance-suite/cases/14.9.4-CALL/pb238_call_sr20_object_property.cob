*>--- conformance-case
*> id: pb238_call_sr20_object_property
*> rules: [SR-14.9.4.3-17, SR-14.9.4.3-20]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB238 - ISO 14.9.4.3 SR20's carve-out, the leg that had no arm at all: "BY CONTENT shall
      *> not be omitted when identifier-4 is an identifier that is permitted as a receiving operand, EXCEPT
      *> that BY CONTENT may be omitted when identifier-4 is an object property."
      *> EXPECTED VALUES, DERIVED:
      *>  IN=00100 - 14.9.4.4 GR9 a)'s test is SYNTAX RULE 3 ("an address-identifier or a data item defined
      *>    in the file, working-storage, local-storage, or linkage section"). An object property is none of
      *>    those, so a)1 does not apply and a)2 assumes BY CONTENT; 8.4.3.9.4 GR1/GR2 then make the sending
      *>    occurrence a GET, which yields the declared VALUE 100 through the PIC 9(5) formal.
      *>  AFTER=00100 - 14.2.3 GR9 allocates the BY CONTENT record so that it "does not occupy the same
      *>    storage area as the argument", and SR17 makes identifier-4 a SENDING operand, so the callee's
      *>    MOVE 777 reaches only that record. The property's SET accessor is NOT invoked. Before PB238 the
      *>    bare argument bound BY REFERENCE, BoundStores classified the occurrence ReadWrite, and the SET
      *>    ran: this same source printed AFTER=00777 (measured, by disabling the arm and rebuilding).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB238PRP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB238P
           PROPERTY BAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE CPB238P.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CPB238P "NEW" RETURNING A
           CALL "PB238PS1" AS NESTED USING BAL OF A
           DISPLAY "AFTER=" BAL OF A
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB238PS1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB PIC 9(5).
       PROCEDURE DIVISION USING BY REFERENCE LB.
       M1.
           DISPLAY "IN=" LB
           MOVE 777 TO LB
           GOBACK.
       END PROGRAM PB238PS1.
       END PROGRAM PB238PRP.

       IDENTIFICATION DIVISION.
       CLASS-ID. CPB238P INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(5) VALUE 100 PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS CPB238P.
