      *> kb/Work PB2073 - ISO 14.9.4.3 SR20's carve-out for an object property whose object is a FUNCTION-IDENTIFIER.
      *>
      *> THE RULE. 14.9.4.3 SR20: "BY CONTENT shall not be omitted when identifier-4 is an identifier that is permitted
      *> as a receiving operand, except that BY CONTENT may be omitted when identifier-4 is an object property." An
      *> object property is "property-name-1 OF identifier-3" (8.4.3.1.2 Format 7), and 8.4.3.1.3 SR1 makes identifier-3
      *> "any of the formats for an identifier" - a function-identifier is one (8.4.3.2). So BAL OF FUNCTION F (A) is an
      *> object property, BY CONTENT may be omitted before it, and 14.9.4.4 GR9 a) 2 then assumes BY CONTENT: the callee
      *> receives a copy of the property's value and its changes never reach the property.
      *> The same fact decides the sibling positions that ask "is this operand an object property": the INVOKE USING
      *> argument (14.9.23.4 GR6 a)), a user function's argument (8.4.3.2.4 GR5 a) "other than an object property").
      *> 8.4.3.2.4 GR1 says what identifies the object without activating the function: the function-identifier's
      *> temporary has "the description, class, and category ... specified by the description in the linkage section of
      *> the item specified in the RETURNING phrase" of the function prototype, or - for function-pointer-name-1 - of the
      *> prototype its USAGE TO phrase names. Both forms are exercised.
      *>
      *> EXPECTED OUTPUT (each value derived from those rules):
      *>   IN=00100     CALL ... USING BAL OF FUNCTION PB2073ID (A): PB2073ID returns its argument A's object, whose
      *>                BAL holds the declared VALUE 100; the sending occurrence is a GET (8.4.3.9.4 GR1).
      *>   AFTER=00100  the callee's MOVE 777 reached only the BY CONTENT copy (14.2.3 GR9): the SET accessor did not run.
      *>   INV=00100    INVOKE A "SHOW" USING BAL OF FUNCTION PB2073ID (A): the same, through the INVOKE lane.
      *>   AFTER2=00100 SHOW's MOVE 555 reached only its copy.
      *>   ARG=00101    FUNCTION PB2073PL (BAL OF FUNCTION PB2073ID (A)): the argument crosses BY CONTENT (GR5 b)) with
      *>                value 100; PB2073PL returns its formal plus 1.
      *>   IN=00100     CALL ... USING BAL OF FUNCTION FP (A): FP holds PB2073ID's address (SET Format 8), and
      *>                function-pointer-name-1's object is identified by the prototype named in FP's TO phrase.
      *>   AFTER3=00100 the callee's MOVE 777 again reached only the copy.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB2073ID.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB2073A.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-IN USAGE OBJECT REFERENCE CPB2073A.
       01 L-OBJ USAGE OBJECT REFERENCE CPB2073A.
       PROCEDURE DIVISION USING L-IN RETURNING L-OBJ.
           SET L-OBJ TO L-IN
           GOBACK.
       END FUNCTION PB2073ID.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB2073PL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-V PIC 9(5).
       01 L-R PIC 9(5).
       PROCEDURE DIVISION USING L-V RETURNING L-R.
           COMPUTE L-R = L-V + 1
           GOBACK.
       END FUNCTION PB2073PL.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2073PR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB2073A
           FUNCTION PB2073ID
           FUNCTION PB2073PL
           PROPERTY BAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE CPB2073A.
       01 FP USAGE FUNCTION-POINTER TO PB2073ID.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CPB2073A "NEW" RETURNING A
           CALL "PB2073PS" AS NESTED USING BAL OF FUNCTION PB2073ID (A)
           DISPLAY "AFTER=" BAL OF A
           INVOKE A "SHOW" USING BAL OF FUNCTION PB2073ID (A)
           DISPLAY "AFTER2=" BAL OF A
           DISPLAY "ARG="
               FUNCTION PB2073PL (BAL OF FUNCTION PB2073ID (A))
           SET FP TO ADDRESS OF FUNCTION PB2073ID
           CALL "PB2073PS" AS NESTED USING BAL OF FUNCTION FP (A)
           DISPLAY "AFTER3=" BAL OF A
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2073PS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB PIC 9(5).
       PROCEDURE DIVISION USING BY REFERENCE LB.
       M1.
           DISPLAY "IN=" LB
           MOVE 777 TO LB
           GOBACK.
       END PROGRAM PB2073PS.
       END PROGRAM PB2073PR.

       IDENTIFICATION DIVISION.
       CLASS-ID. CPB2073A INHERITS FROM BASE.
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
       IDENTIFICATION DIVISION.
       METHOD-ID. SHOW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-V PIC 9(5).
       PROCEDURE DIVISION USING BY REFERENCE L-V.
           DISPLAY "INV=" L-V
           MOVE 555 TO L-V.
       END METHOD SHOW.
       END OBJECT.
       END CLASS CPB2073A.
