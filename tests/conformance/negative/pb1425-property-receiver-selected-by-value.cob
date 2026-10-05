      *> reject-at: 2002 2014 2023
      *> kb/Work PB1425, PB2078 (train 1021 review A finding 3) - a RECEIVING object property whose object is selected
      *>   by a run-time value. The property's GET ran before the statement and its SET after it, each evaluating
      *>   AR(I) afresh: ADD 1 TO I, BAL OF AR(I) stored AR(1)'s BAL + 1 into AR(2) (AR2=11 where ISO 14.7.7 4) b)
      *>   gives 21, the receiver identified "as each data item is accessed"), and MOVE 2 TO BAL OF AR(I), I set AR(2)
      *>   where 14.9.25.4 GR1 identifies AR(1) ("immediately before the data is moved to the respective data item").
      *>   Until each receiver's accessors interleave with its own store (PB2078) the shape is refused, as it was
      *>   before PB1425 admitted the subscript. A literal subscript (MOVE 3 TO BAL OF AR(2)) and a sending
      *>   reference stay admitted: golden 2002/pb1425_object_property_identifier.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425RV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425RA
           PROPERTY BAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT TYPEDEF STRONG.
          05 AR USAGE OBJECT REFERENCE PB1425RA OCCURS 2.
       01 R TYPE RT.
       01 I PIC 9.
       PROCEDURE DIVISION.
           INVOKE PB1425RA "NEW" RETURNING AR(1)
           INVOKE PB1425RA "NEW" RETURNING AR(2)
           MOVE 1 TO I
           ADD 1 TO I, BAL OF AR(I)
           MOVE 2 TO BAL OF AR(I), I
           DISPLAY "BAL " BAL OF AR(I)
           STOP RUN.
       END PROGRAM PB1425RV.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425RA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB1425RA.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(5) VALUE 100 PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB1425RA.
