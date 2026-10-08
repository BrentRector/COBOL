      *> reject-at: 85
      *> The REJECT half of 2002/pb2659_call_resources_recursion
      *> (kb/Work PB2659). ISO 14.9.4.4 GR3 c)'s EC-PROGRAM-RESOURCES
      *> is an exception condition, and exception conditions, the
      *> TURN directive that enables their checking, the RECURSIVE
      *> clause and the EXCEPTION-STATUS function are ISO/IEC
      *> 1989:2002 introductions. At COBOL-85 this source does not
      *> describe a program, so the compiler refuses the first
      *> construct the edition does not have.
       >>TURN EC-PROGRAM-RESOURCES CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2659N85 RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LEVEL     PIC 9(9) VALUE 0.
       PROCEDURE DIVISION.
           ADD 1 TO LEVEL
           CALL "PB2659N85"
               ON EXCEPTION
                   DISPLAY "ON EXCEPTION " FUNCTION EXCEPTION-STATUS
           END-CALL
           GOBACK.
