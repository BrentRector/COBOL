      *> ISO 13.18.60.4 GR22 a) (kb/Work PB1527, DOC-A.1-214): "The amount of storage allocated for an object reference
      *> data item is implementor-defined and is not necessarily the same for every object reference." docs/CONFORMANCE.md
      *> DOC-A.1-214 documents WiseOwl COBOL's amount: 8 bytes, one managed reference. 13.18.60.3 SR14 admits an object
      *> reference as an elementary item subordinate to a type declaration with the STRONG phrase, so a strongly-typed
      *> group COUNTS those 8 positions, and the group's CHARACTER image shows them as the 8-position placeholder
      *> (eight SPACES, never the reference): the group-image determination of DOC-A.1-56, read by DISPLAY and by a
      *> MOVE to a receiver that is not strongly typed (14.9.25.4 GR4: no conversion of data).
      *> 14.9.43.3 SR1 (and 14.9.48.3 SR2) refuse that SAME group as a STRING / UNSTRING operand at compile time - the
      *> negative corpus entry pb1527-string-object-reference-group - so no consumer of the group's characters can
      *> reach a run-time refusal.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE DETERMINATION - NOT FROM A RUN:
      *>   LEN=00011        FUNCTION BYTE-LENGTH(G): PIC X(3) plus one 8-byte reference (15.14.4 rule 1)
      *>   [ABC" "x8]       DISPLAY G: "ABC" then the 8-position placeholder (11 characters)
      *>   [ABC" "x17]      MOVE G TO XX (PIC X(20)): the 11-character image, space-filled to 20
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1527OBJ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1527C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG TYPEDEF STRONG.
          05 A PIC X(3).
          05 R USAGE OBJECT REFERENCE PB1527C.
       01 G TYPE TG.
       01 XX PIC X(20) VALUE ALL "-".
       01 N PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABC" TO A.
           MOVE FUNCTION BYTE-LENGTH(G) TO N.
           DISPLAY "LEN=" N.
           DISPLAY "[" G "]".
           MOVE G TO XX.
           DISPLAY "[" XX "]".
           STOP RUN.
       END PROGRAM PB1527OBJ.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1527C.
       OBJECT.
       END OBJECT.
       END CLASS PB1527C.
