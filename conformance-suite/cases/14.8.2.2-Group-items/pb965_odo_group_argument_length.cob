*>--- conformance-case
*> id: pb965_odo_group_argument_length
*> rules: [SR-14.8.2.2-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB965 (finisher) - AN OCCURS DEPENDING GROUP ARGUMENT: MAXIMUM LENGTH BY REFERENCE, CURRENT
      *> LENGTH BY CONTENT, at the CALL and at the INVOKE.
      *>
      *> 14.8.2.2: "For an argument or formal parameter that is described as an occurs-depending group item
      *> passed by reference, the maximum length is used. For an occurs-depending group item passed by content,
      *> the length of the argument is determined by the rules of the OCCURS clause for a sending data item" -
      *> 13.18.38.4 GR8's current-count part. The compiler sent the MAXIMUM image BY CONTENT too, so the
      *> formal saw all five occurrences while N said two.
      *>
      *> EXPECTED VALUES, DERIVED (OG = ab + OT with N = 2 of 5, storage "abcdefg"):
      *>   R  - BY REFERENCE, the maximum length: [abcdefg].
      *>   C  - BY CONTENT, the sending length 2 + 2 = 4, moved into X(7) (14.8.2.2 rule 2): [abcd   ].
      *>   M  - the INVOKE twins: BY CONTENT [abcd   ], BY REFERENCE [abcdefg].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965OD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB965OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB965OK.
       01 N PIC 9 VALUE 2.
       01 OG.
          05 O1 PIC X(2) VALUE "ab".
          05 OT PIC X OCCURS 1 TO 5 DEPENDING ON N.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 5 TO N
           MOVE "cdefg" TO OG(3:5)
           MOVE 2 TO N
           CALL "PB965OR" USING BY REFERENCE OG
           CALL "PB965OC" USING BY CONTENT OG
           INVOKE PB965OK "NEW" RETURNING O
           INVOKE O "M" USING BY CONTENT OG
           INVOKE O "M" USING BY REFERENCE OG
           STOP RUN.
       END PROGRAM PB965OD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965OR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR PIC X(7).
       PROCEDURE DIVISION USING LR.
           DISPLAY "R=[" LR "]"
           GOBACK.
       END PROGRAM PB965OR.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965OC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LC PIC X(7).
       PROCEDURE DIVISION USING LC.
           DISPLAY "C=[" LC "]"
           GOBACK.
       END PROGRAM PB965OC.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB965OK INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LM.
          05 LM1 PIC X(7).
       PROCEDURE DIVISION USING LM.
       MAIN-P.
           DISPLAY "M=[" LM "]".
       END METHOD M.
       END OBJECT.
       END CLASS PB965OK.
