*>--- conformance-case
*> id: pb867-raise-checking-below-2002
*> rules: [GR-14.9.29.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> The REJECT half of conformance:2002/pb867_raise_checking_not_enabled
      *> (kb/Work PB867). RAISE (ISO 14.9.29), USE AFTER EXCEPTION
      *> CONDITION (14.9.49) and the >>TURN directive (7.3.25) are all
      *> ISO/IEC 1989:2002 introductions, so at COBOL-85 this source does
      *> not describe a program: the compiler refuses the first construct
      *> the edition does not have with COBOLNET0900.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB867N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-PB867N.
       U-P.
           DISPLAY "DECL [" FUNCTION EXCEPTION-STATUS "]".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           RAISE EXCEPTION EC-USER-PB867N
           DISPLAY "AFTER [" FUNCTION EXCEPTION-STATUS "]"
       >>TURN EC-USER-PB867N CHECKING ON
           RAISE EXCEPTION EC-USER-PB867N
           STOP RUN.
       END PROGRAM PB867N.
