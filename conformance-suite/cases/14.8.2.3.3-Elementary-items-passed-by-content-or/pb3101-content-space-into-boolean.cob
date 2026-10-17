*>--- conformance-case
*> id: pb3101-content-space-into-boolean
*> rules: [GR-14.8.2.3.3-L2.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3101 - a figurative constant BY CONTENT takes 14.8.2.3.3 rule 2 d)'s MOVE, the WHOLE
      *> 14.9.25.3 question: SR7 "Any figurative constant for which the associated character or characters
      *> are not boolean characters shall not be moved to a boolean data item", so SPACE into the PIC 1(4)
      *> formal of a method does not conform (14.9.23.3 SR5 c) applies 14.8.2) -> COBOLNET0828 naming SR7.
      *> The INVOKE lane asked Table 16 alone of a figurative, under any category it could take, and the
      *> program ran.
      *> cite.py --check 14.9.25.3 "Any figurative constant for which the associated character or characters
      *>   are not boolean characters shall not be moved to a boolean data item" -> OK 14.9.25.3 7)
      *> cite.py --check 14.9.23.3 "The rules for conformance as specified in 14.8.2, Parameters and 14.8.3,
      *>   Returning items apply" -> OK 14.9.23.3 5) c)
       IDENTIFICATION DIVISION.
       CLASS-ID. K3101S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K3101S.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. MB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 B PIC 1(4).
       PROCEDURE DIVISION USING BY REFERENCE B.
           DISPLAY B
           GOBACK.
       END METHOD MB.
       END FACTORY.
       END CLASS K3101S.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3101S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K3101S.
       PROCEDURE DIVISION.
           INVOKE K3101S "MB" USING BY CONTENT SPACE
           STOP RUN.
       END PROGRAM N3101S.
