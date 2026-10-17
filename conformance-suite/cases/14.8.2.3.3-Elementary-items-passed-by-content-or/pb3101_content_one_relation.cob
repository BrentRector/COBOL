*>--- conformance-case
*> id: pb3101_content_one_relation
*> rules: [GR-14.8.2.3.3-L2.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3101 - a BY CONTENT argument is asked the ONE relation the run-time CALL lane asks
      *> (ActivationRelations.CallArgumentViolation) on every bind-time lane: CALL with the NESTED phrase and
      *> INVOKE alike. Each verdict below is derived from the rule, not measured:
      *>  C1 a group argument BY CONTENT into a NUMERIC formal of a contained program (NESTED): 14.8.2.1 sends
      *>     the pair to 14.8.2.2, since one side is a group, and its rule 2 is "the same as for a MOVE
      *>     statement"; a MOVE of a group to an elementary item is valid (Table 16's group row), so the pair
      *>     conforms and the formal holds the group's characters, "1234". The bind-time screen used to refuse
      *>     it ("a group argument does not conform to a numeric formal").
      *>  C2 the same pair through INVOKE (14.9.23.3 SR5 c) applies 14.8.2): "5678".
      *>  C3 ALL "01" BY CONTENT into a PIC 1(4) formal: 14.9.25.3 SR7 bars only a figurative constant "for
      *>     which the associated character or characters are not boolean characters", and these are, so the
      *>     MOVE is valid; the formal holds "01" repeated to its four positions (8.3.3.6.4 GR2), "0101".
      *> cite.py --check 14.8.2.2 "If the argument is passed by content, the conformance rules are the same as
      *>   for a MOVE statement" -> OK 14.8.2.2 2)
      *> cite.py --check 14.9.23.3 "The rules for conformance as specified in 14.8.2, Parameters and 14.8.3,
      *>   Returning items apply" -> OK 14.9.23.3 5) c)
      *> cite.py --check 14.9.25.3 "Any figurative constant for which the associated character or characters
      *>   are not boolean characters shall not be moved to a boolean data item" -> OK 14.9.25.3 7)
      *> cite.py --check 8.3.3.6.4 "the string of characters is repeated character by character"
      *>   -> OK 8.3.3.6.4 2)
       IDENTIFICATION DIVISION.
       CLASS-ID. K3101C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K3101C.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. MN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 N PIC 9(4).
       PROCEDURE DIVISION USING BY REFERENCE N.
           DISPLAY "C2 N=" N
           GOBACK.
       END METHOD MN.
       END FACTORY.
       END CLASS K3101C.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3101C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K3101C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 G1 PIC X(2) VALUE "12".
          05 G2 PIC X(2) VALUE "34".
       01 H.
          05 H1 PIC X(2) VALUE "56".
          05 H2 PIC X(2) VALUE "78".
       PROCEDURE DIVISION.
           CALL "N3101C" AS NESTED USING BY CONTENT G
           INVOKE K3101C "MN" USING BY CONTENT H
           CALL "N3101B" AS NESTED USING BY CONTENT ALL "01"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3101C.
       DATA DIVISION.
       LINKAGE SECTION.
       01 N PIC 9(4).
       PROCEDURE DIVISION USING N.
           DISPLAY "C1 N=" N
           GOBACK.
       END PROGRAM N3101C.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3101B.
       DATA DIVISION.
       LINKAGE SECTION.
       01 B PIC 1(4).
       PROCEDURE DIVISION USING B.
           DISPLAY "C3 B=" B
           GOBACK.
       END PROGRAM N3101B.
       END PROGRAM P3101C.
