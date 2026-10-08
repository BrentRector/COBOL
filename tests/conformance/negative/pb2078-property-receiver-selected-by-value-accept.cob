      *> reject-at: 2002 2014 2023
      *> kb/Work PB2078 - the residue of the per-receiver rule. A RECEIVING object property whose object is selected by a
      *>   run-time value is identified when its statement reaches it (ISO 14.7.7 4) b); 14.9.25.4 GR1), which the
      *>   arithmetic statements and MOVE do by placing the property's GET and SET around each receiver's store
      *>   (golden 2002/pb2078_property_receivers_in_turn). A statement that does not store its receivers one at a time
      *>   (ACCEPT here) still wraps its accessors around the whole statement, where AR(I) would be evaluated twice,
      *>   so the shape is refused (COBOLNET0899) instead of risking the wrong object.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2078NA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2078NC
           PROPERTY BAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT TYPEDEF STRONG.
          05 AR USAGE OBJECT REFERENCE PB2078NC OCCURS 2.
       01 R TYPE RT.
       01 I PIC 9.
       PROCEDURE DIVISION.
           INVOKE PB2078NC "NEW" RETURNING AR(1)
           MOVE 1 TO I
           ACCEPT BAL OF AR(I) FROM DATE
           STOP RUN.
       END PROGRAM PB2078NA.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2078NC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB2078NC.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(8) VALUE 100 PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB2078NC.
