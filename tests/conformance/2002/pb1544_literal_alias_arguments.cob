       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1544G1.
      *> A word that stands for a literal is literal-2 of the
      *> program-prototype CALL (ISO 14.9.4.2 Format 2), never
      *> identifier-2 (kb/Work PB1544):
      *>   a constant-name - 13.10.4 GR1: "the effect of specifying
      *>   constant-name-1 in other than this entry is as if
      *>   literal-1 ... were written where constant-name-1 is
      *>   written";
      *>   a symbolic-character - 12.3.7.4 GR11 a): "Symbolic-
      *>   character-1 defines a figurative constant", and 8.3.3.6.3
      *>   SR1: "A figurative constant may be used whenever 'literal'
      *>   appears in a format".
      *> Literal-2 never meets 14.9.4.3 SR3, so a keyword-less one is
      *> passed BY CONTENT (14.9.4.4 GR9 a) 2.).
      *>
      *> C1  BY CONTENT KT   the literal "Q" moved to P PIC X(3):
      *>                     [Q  ]
      *> C2  BY CONTENT SA   SA IS 66, the 66th character of the
      *>                     native set, "A"; a figurative constant
      *>                     moved to a fixed-length item is repeated
      *>                     to its size (8.3.3.6.4 GR2, and GR10:
      *>                     "one or more of the character"): [AAA]
      *> C3  USING SA        the same argument, BY CONTENT by GR9:
      *>                     [AAA]
      *> F1  FUNCTION ORD(SA)          66
      *> I1  INSPECT ... FOR ALL SA    "BANANA" holds three "A": 03
      *> V1  INVOKE ... USING SA       literal-2 of INVOKE (14.9.23.2),
      *>                               BY CONTENT (14.9.23.4 GR6 a) 2.),
      *>                               filled to LX PIC X(3): [AAA]
      *> V2  INVOKE ... BY CONTENT KT  [Q  ]
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SA IS 66.
       REPOSITORY.
           CLASS PB1544K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KT CONSTANT AS "Q".
       01 X PIC X(6) VALUE "BANANA".
       01 N PIC 99 VALUE 0.
       01 R PIC 999.
       01 O USAGE OBJECT REFERENCE PB1544K.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1544K "NEW" RETURNING O.
           DISPLAY "V1" WITH NO ADVANCING.
           INVOKE O "TAKEX" USING SA.
           DISPLAY "V2" WITH NO ADVANCING.
           INVOKE O "TAKEX" USING BY CONTENT KT.
           DISPLAY "C1" WITH NO ADVANCING.
           CALL "PB1544S" AS NESTED USING BY CONTENT KT.
           DISPLAY "C2" WITH NO ADVANCING.
           CALL "PB1544S" AS NESTED USING BY CONTENT SA.
           DISPLAY "C3" WITH NO ADVANCING.
           CALL "PB1544S" AS NESTED USING SA.
           MOVE FUNCTION ORD(SA) TO R.
           DISPLAY "F1 " R.
           INSPECT X TALLYING N FOR ALL SA.
           DISPLAY "I1 " N.
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1544S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC X(3).
       PROCEDURE DIVISION USING P.
       S0.
           DISPLAY " [" P "]".
           GOBACK.
       END PROGRAM PB1544S.
       END PROGRAM PB1544G1.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1544K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKEX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX PIC X(3).
       PROCEDURE DIVISION USING LX.
           DISPLAY " [" LX "]".
       END METHOD TAKEX.
       END OBJECT.
       END CLASS PB1544K.
