      *> reject-at: 2002 2014 2023
      *> kb/Work PB2078 - the residue of the per-receiver rule. A RECEIVING object property whose object is selected by a
      *>   run-time value is identified when its statement reaches it (ISO 14.7.7 4) b); 14.9.25.4 GR1), which the
      *>   arithmetic statements, MOVE and every implicit move (READ / RETURN INTO, ACCEPT, UNSTRING INTO, INITIALIZE),
      *>   STRING INTO and INSPECT's identifier-1 do by placing the property's GET and SET around the receiver's own
      *>   store (golden 2002/pb2078_property_receivers_in_turn and 2002/pb2078_property_receivers_in_phrases). A
      *>   statement whose other receivers are not stored through that funnel (the INSPECT TALLYING counter here) still
      *>   wraps its accessors around the whole statement, where AR(I) would be evaluated twice, so the shape is
      *>   refused (COBOLNET0899) instead of risking the wrong object.
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
       01 X PIC X(5) VALUE "AAAAA".
       PROCEDURE DIVISION.
           INVOKE PB2078NC "NEW" RETURNING AR(1)
           MOVE 1 TO I
           INSPECT X TALLYING BAL OF AR(I) FOR ALL "A"
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
