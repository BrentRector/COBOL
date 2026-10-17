*>--- conformance-case
*> id: pb303_as_class_id_zero_length_accepted
*> rules: [SR-11.3.3-1, GR-11.3.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       *> kb/Work PB303 - the deliberate asymmetry of ISO 11.3.3 syntax rule 1.
       *>
       *> Four of the five id paragraphs state the AS-literal rule as "shall be an
       *> alphanumeric literal or a national literal and shall be neither a figurative
       *> constant nor a zero-length literal" (11.10.3 SR1, 11.5.3 SR1, 11.6.3 SR1,
       *> 11.7.3 SR1).  11.3.3 SR1 states it WITHOUT the zero-length half: "shall be an
       *> alphanumeric literal or a national literal and shall not be a figurative
       *> constant."  Verified against the PRINTED page, not the transcription.
       *>
       *> So a zero-length AS literal on CLASS-ID is LEGAL and this program compiles and
       *> runs, while negative/pb303_as_literal_zero_length rejects the identical literal
       *> on PROGRAM-ID.  Without this golden the shared screen could silently reject it
       *> everywhere and every other test would stay green - the rejecting side is the
       *> side tests naturally cover.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB303CZ AS "" INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. PB303CZM.
       PROCEDURE DIVISION.
       CZ-P.
           DISPLAY "CLASS-ID-ZERO-LENGTH-AS-ACCEPTED".
           GOBACK.
       END METHOD PB303CZM.
       END OBJECT.
       END CLASS PB303CZ.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB303CZP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB303CZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-OBJ USAGE OBJECT REFERENCE PB303CZ.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PB303CZ "NEW" RETURNING WS-OBJ.
           INVOKE WS-OBJ "PB303CZM".
           STOP RUN.
       END PROGRAM PB303CZP.
