*>--- conformance-case
*> id: pb992_returning_character_content
*> rules: [GR-14.2.3-8]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB992 - the CALL RETURNING channel. ISO 14.6.5 (cite.py --check 14.6.5 "the content of the data
      *> item" -> OK 14.6.5): the result of the activated program "is the content of the data item referenced by
      *> that RETURNING phrase", and 14.9.4.4 GR4 (cite.py --check 14.9.4.4 "is placed into identifier-3" -> OK
      *> 14.9.4.4 4)) places that result into identifier-3. A CONTENT transfer: the callee's PIC 9(3) returning
      *> item holds three spaces (stored through its REDEFINES), and 14.8.3.3's conforming receiver (the same
      *> PICTURE and USAGE) receives exactly those characters.
      *>
      *> DERIVED VALUES:
      *>   R=[   ]    the three spaces the callee's returning item holds.
      *>   R2=[042]   an ordinary numeric result still arrives as its digits.
      *> Before PB992 R displayed 000 - the receiver's native carrier held the value decoded from the spaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB992RT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(3) VALUE 456.
       01 R2 PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
           CALL "PB992SR" RETURNING R
           DISPLAY "R=[" R "]"
           CALL "PB992NR" RETURNING R2
           DISPLAY "R2=[" R2 "]"
           STOP RUN.
       END PROGRAM PB992RT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB992SR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SP PIC X(3) VALUE SPACES.
       LINKAGE SECTION.
       01 RV PIC 9(3).
       01 RVX REDEFINES RV PIC X(3).
       PROCEDURE DIVISION RETURNING RV.
           MOVE SP TO RVX
           GOBACK.
       END PROGRAM PB992SR.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB992NR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 NV PIC 9(3).
       PROCEDURE DIVISION RETURNING NV.
           MOVE 42 TO NV
           GOBACK.
       END PROGRAM PB992NR.
