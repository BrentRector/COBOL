*>--- conformance-case
*> id: pb3101-content-zero-into-alphabetic
*> rules: [GR-14.8.2.3.3-L2.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3101 - a figurative constant BY CONTENT takes 14.8.2.3.3 rule 2 d)'s MOVE, the WHOLE
      *> 14.9.25.3 question: SR6 "The figurative constant ZERO shall not be moved to an alphabetic data item",
      *> so ZERO into the PIC A(4) formal of a contained program activated with the NESTED phrase does not
      *> conform -> COBOLNET1688 naming SR6. The activation lanes asked Table 16 alone of a figurative, whose
      *> row the context chooses, and the program ran, storing "0000" in an alphabetic item.
      *> cite.py --check 14.9.25.3 "The figurative constant ZERO shall not be moved to an alphabetic data
      *>   item" -> OK 14.9.25.3 6)
      *> cite.py --check 14.8.2.3.3 "Otherwise, the conformance rules are the same as for a MOVE statement"
      *>   -> OK 14.8.2.3.3 2) d)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3101Z.
       PROCEDURE DIVISION.
           CALL "N3101ZA" AS NESTED USING BY CONTENT ZERO
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3101ZA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC A(4).
       PROCEDURE DIVISION USING A.
           DISPLAY A
           GOBACK.
       END PROGRAM N3101ZA.
       END PROGRAM N3101Z.
