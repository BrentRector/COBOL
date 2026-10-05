      *> kb/Work PB1425 - identifier Format 7, property-name-1 OF identifier-3, whatever identifier-3 is.
      *>
      *> THE RULE. ISO/IEC 1989:2023 8.4.3.1.2 Format 7 is property-name-1 OF identifier-3, and 8.4.3.1.3 SR1:
      *> "that other identifier may be any of the formats for an identifier". 8.4.3.1.4 GR1 orders the parts:
      *> a) a qualified-data-name-with-subscript or a predefined-object reference, c) "an object-view applies to
      *> the identifier on the left", d) "OF for object properties applies the property-name on the left to the
      *> identifier on the right", g) the reference modifier last. 8.4.3.8.3 SR3 names the object of an
      *> object-property identifier as a home of SUPER; 8.4.3.8.4 GR1 "SELF and SUPER both reference the object
      *> that was used to invoke the method", GR3 starts SUPER's search at the inherited class, GR4 at
      *> object-class-name-1. 8.4.3.9.4 GR1/GR2: a sending property is its GET, a receiving one its SET.
      *>
      *> EXPECTED OUTPUT (each value derived from those rules):
      *>   SUB 00003 00100   MOVE 3 TO BAL OF AR(2): the subscript is identifier-3's (AR(2)), never the
      *>                     property value's; AR(1) still holds its VALUE 100.
      *>   QUAL 00007        BAL OF A OF T(2): identifier-3 is the qualified, subscripted A OF T(2).
      *>   CHAIN XYZ         NM OF NXT OF D: identifier-3 is the property NXT OF D, which references GA's
      *>                     object, whose NM was set to XYZ through NM OF GA.
      *>   INLINE XYZ        NXT OF D :: "GETNM": OF (d) applies before the inline invocation operator (e),
      *>                     so GETNM runs on the object the property NXT OF D references.
      *>   REFMOD BCD        NM OF AR(1) (2:3): the reference modifier applies last, to the value ABCDEF.
      *>   VIEW 00100        BAL OF U AS PB1425FA: U is universal; the view applies before OF (GR1 c) then d)).
      *>   VIEWQ             GA OF G AS PB1425FA: a qualified data name, so the view is of that item (GR1 a)
      *>                     then c)), and it references the same object as GA OF G.
      *>   FUNC 00005        BAL OF FUNCTION PB1425FF (5): the function returns a new object whose BAL is 5.
      *>   RECV 00100        INVOKE NXT OF D "SHOWBAL": a property is an identifier-1 of INVOKE too; the
      *>                     object NXT OF D references (GA's) shows its BAL.
      *>   SELF 00100 00008  in method SHOW of PB1425FA: BAL OF SELF read, then MOVE 8 TO BAL OF SELF
      *>                     and read again (the SET, then the GET, on the object SHOW runs on).
      *>   SELFN 00009       COMPUTE N = BAL OF SELF + 1 (an arithmetic operand).
      *>   OVR 00999         in PB1425FB (which overrides GET PROPERTY BAL to return 999): BAL OF SELF is
      *>                     the overriding accessor (GR2 - the run-time class's method).
      *>   SUPER 00100       BAL OF SUPER: the search starts at PB1425FA (GR3), whose accessor answers 100.
      *>   QSUPER 00100      BAL OF PB1425FA OF SUPER: GR4, the same class here.
      *>   SUPERSET 00042    MOVE 42 TO BAL OF SUPER, then BAL OF SUPER: SUPER's SET and GET.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1425FF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425FA
           PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-SEED PIC 9.
       01 L-OBJ USAGE OBJECT REFERENCE PB1425FA.
       PROCEDURE DIVISION USING L-SEED RETURNING L-OBJ.
           INVOKE PB1425FA "NEW" RETURNING L-OBJ
           MOVE L-SEED TO BAL OF L-OBJ
           GOBACK.
       END FUNCTION PB1425FF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425FP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425FA
           CLASS PB1425FB
           FUNCTION PB1425FF
           PROPERTY BAL
           PROPERTY NM
           PROPERTY NXT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT TYPEDEF STRONG.
          05 A USAGE OBJECT REFERENCE PB1425FA OCCURS 2.
       01 T TYPE TT.
       01 RT TYPEDEF STRONG.
          05 AR USAGE OBJECT REFERENCE PB1425FA OCCURS 2.
       01 R TYPE RT.
       01 GT TYPEDEF STRONG.
          05 GA USAGE OBJECT REFERENCE PB1425FA.
       01 G TYPE GT.
       01 D USAGE OBJECT REFERENCE PB1425FA.
       01 B USAGE OBJECT REFERENCE PB1425FB.
       01 U USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1425FA "NEW" RETURNING D
           INVOKE PB1425FA "NEW" RETURNING AR(1)
           INVOKE PB1425FA "NEW" RETURNING AR(2)
           INVOKE PB1425FA "NEW" RETURNING GA OF G
           INVOKE PB1425FA "NEW" RETURNING A OF T(2)
           INVOKE PB1425FB "NEW" RETURNING B
           SET U TO D
           MOVE 3 TO BAL OF AR(2)
           DISPLAY "SUB " BAL OF AR(2) " " BAL OF AR(1)
           MOVE 7 TO BAL OF A OF T(2)
           DISPLAY "QUAL " BAL OF A OF T(2)
           SET NXT OF D TO GA OF G
           MOVE "XYZ" TO NM OF GA OF G
           DISPLAY "CHAIN " NM OF NXT OF D
           DISPLAY "INLINE " NXT OF D :: "GETNM"
           DISPLAY "REFMOD " NM OF AR(1) (2:3)
           DISPLAY "VIEW " BAL OF U AS PB1425FA
           IF GA OF G AS PB1425FA = GA OF G
               DISPLAY "VIEWQ"
           END-IF
           DISPLAY "FUNC " BAL OF FUNCTION PB1425FF (5)
           INVOKE NXT OF D "SHOWBAL"
           INVOKE D "SHOW"
           INVOKE B "SHOW2"
           STOP RUN.
       END PROGRAM PB1425FP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425FA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB1425FA
           PROPERTY BAL.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(5) VALUE 100 PROPERTY.
       01 NM PIC X(6) VALUE "ABCDEF" PROPERTY.
       01 NXT USAGE OBJECT REFERENCE PB1425FA PROPERTY.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. GETNM.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC X(6).
       PROCEDURE DIVISION RETURNING LN.
           MOVE NM TO LN.
       END METHOD GETNM.
       IDENTIFICATION DIVISION.
       METHOD-ID. SHOWBAL.
       PROCEDURE DIVISION.
           DISPLAY "RECV " BAL.
       END METHOD SHOWBAL.
       IDENTIFICATION DIVISION.
       METHOD-ID. SHOW.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 N PIC 9(5).
       PROCEDURE DIVISION.
           DISPLAY "SELF " BAL OF SELF WITH NO ADVANCING
           MOVE 8 TO BAL OF SELF
           DISPLAY " " BAL OF SELF
           COMPUTE N = BAL OF SELF + 1
           DISPLAY "SELFN " N.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB1425FA.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425FB INHERITS FROM PB1425FA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425FA
           PROPERTY BAL.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. GET PROPERTY BAL OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB PIC 9(5).
       PROCEDURE DIVISION RETURNING LB.
           MOVE 999 TO LB.
       END METHOD.
       IDENTIFICATION DIVISION.
       METHOD-ID. SHOW2.
       PROCEDURE DIVISION.
           DISPLAY "OVR " BAL OF SELF
           DISPLAY "SUPER " BAL OF SUPER
           DISPLAY "QSUPER " BAL OF PB1425FA OF SUPER
           MOVE 42 TO BAL OF SUPER
           DISPLAY "SUPERSET " BAL OF SUPER.
       END METHOD SHOW2.
       END OBJECT.
       END CLASS PB1425FB.
