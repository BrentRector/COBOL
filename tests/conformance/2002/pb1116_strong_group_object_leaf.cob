      *> kb/Work PB1116 -- a strongly-typed group whose subordinate is an
      *> object reference crosses an INVOKE. ISO 14.8.2.2: "If either the
      *> formal parameter or the corresponding argument is a
      *> strongly-typed group item, both shall be of the same type" -- G,
      *> H and LG/RG are all TYPE T, and the program's T and the class's T
      *> are equivalent type declarations (8.5.3.1), so every pairing
      *> conforms (14.9.23.3 SR5 c)) and none needs a character image.
      *> (1) BY REFERENCE: TAKEG sees TX ABC, TN(2) 042 and a set TA, and
      *>     its MOVEs (TX XYZ, TN(1) 7) are visible to the caller:
      *>     REF:XYZ N1=007.
      *> (2) BY CONTENT: TAKEG sees DEF; its MOVE does not come back:
      *>     CON:DEF.
      *> (3) RETURNING (14.8.3.2, same type): MAKEG returns TX RET and TA
      *>     set to SELF, the very object O references: RET:RET, SAMEOBJ.
      *> (4) INVOKE SELF between two methods of the class: SELF-REF:QQQ.
      *> OO and strong type declarations are COBOL 2002 introductions.
       IDENTIFICATION DIVISION.
       CLASS-ID. P1116K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF STRONG.
          05 TA USAGE OBJECT REFERENCE.
          05 TX PIC X(3).
          05 TN PIC 9(3) OCCURS 2.
       PROCEDURE DIVISION.
       METHOD-ID. TAKEG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG TYPE T.
       PROCEDURE DIVISION USING LG.
           DISPLAY "LX=" TX OF LG " N2=" TN OF LG (2)
           IF TA OF LG = NULL
               DISPLAY "TA:NULL"
           ELSE
               DISPLAY "TA:SET"
           END-IF
           MOVE "XYZ" TO TX OF LG
           MOVE 7 TO TN OF LG (1)
           GOBACK.
       END METHOD TAKEG.
       METHOD-ID. MAKEG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RG TYPE T.
       PROCEDURE DIVISION RETURNING RG.
           MOVE "RET" TO TX OF RG
           SET TA OF RG TO SELF
           GOBACK.
       END METHOD MAKEG.
       METHOD-ID. SETQ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 QG TYPE T.
       PROCEDURE DIVISION USING QG.
           MOVE "QQQ" TO TX OF QG
           GOBACK.
       END METHOD SETQ.
       METHOD-ID. RUNSELF.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 SG TYPE T.
       PROCEDURE DIVISION.
           INVOKE SELF "SETQ" USING SG
           DISPLAY "SELF-REF:" TX OF SG
           GOBACK.
       END METHOD RUNSELF.
       END OBJECT.
       END CLASS P1116K.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1116M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P1116K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF STRONG.
          05 TA USAGE OBJECT REFERENCE.
          05 TX PIC X(3).
          05 TN PIC 9(3) OCCURS 2.
       01 G TYPE T.
       01 H TYPE T.
       01 O USAGE OBJECT REFERENCE P1116K.
       PROCEDURE DIVISION.
           INVOKE P1116K "NEW" RETURNING O
           MOVE "ABC" TO TX OF G
           MOVE 42 TO TN OF G (2)
           SET TA OF G TO O
           INVOKE O "TAKEG" USING G
           DISPLAY "REF:" TX OF G " N1=" TN OF G (1)
           MOVE "DEF" TO TX OF G
           INVOKE O "TAKEG" USING BY CONTENT G
           DISPLAY "CON:" TX OF G
           INVOKE O "MAKEG" RETURNING H
           DISPLAY "RET:" TX OF H
           IF TA OF H = O
               DISPLAY "SAMEOBJ"
           ELSE
               DISPLAY "OTHER"
           END-IF
           INVOKE O "RUNSELF"
           STOP RUN.
       END PROGRAM P1116M.
