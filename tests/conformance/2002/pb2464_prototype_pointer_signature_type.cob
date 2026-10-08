      *> kb/Work PB2464 - the TYPE of a restricted PROGRAM-POINTER is its
      *> prototype's SIGNATURE, not its prototype's name.
      *> 13.18.60.4 GR25 (cite.py --check 13.18.60.4 -> OK 13.18.60.4 25)):
      *> "A restricted program-pointer shall contain only the predefined
      *> address NULL or the address of a program with the same signature
      *> as that identified by the specified program-prototype-name." So
      *> PT1 (TO P1) and PT2 (TO P2) hold the same set of addresses when
      *> P1 and P2 have the same signature, and 14.9.39.3 SR22 (OK
      *> 14.9.39.3 22)) admits a SET between them: "the program-
      *> prototypes associated with identifier-7 and identifier-8 shall
      *> have the same signature". 14.8.2.3.2 (OK 14.8.2.3.2 4)): "If
      *> either is a restricted pointer, both shall be restricted and of
      *> the same type".
      *> DERIVATION (P1: USING 9(4); P2: USING 9(4), another data-name,
      *> the same signature; P3: USING X(4), a different one):
      *>   TYPED   INVOKE OC "RP1" RETURNING R2 - 14.8.3.3 (OK 14.8.3.3 2))
      *>           asks the same USAGE clause of the returning pair, and
      *>           the type is the signature: legal, r2 = NULL.
      *>   UNIVERSAL  INVOKE U "RP1" RETURNING R1 (TO P1)   9.3.6 rule 6:
      *>           SET valid (SR22, the same prototype): r1 ok
      *>   UNIVERSAL  ... RETURNING R2 (TO P2, same signature): SR22
      *>           holds, so rule 6 MATCHES (OK 9.3.6 6)); 14.8.3.3 holds
      *>           (one type): no exception, r2 ok
      *>   UNIVERSAL  ... RETURNING R3 (TO P3, another signature): SR22
      *>           refuses the SET, so no match, the INHERITS chain ends,
      *>           "otherwise, the EC-OO-METHOD exception condition is
      *>           set to exist" (OK 9.3.6 6)): HANDLED=EC-OO-METHOD
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM P1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Y PIC 9(4).
       PROCEDURE DIVISION USING L-Y.
       END PROGRAM P2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Z PIC X(4).
       PROCEDURE DIVISION USING L-Z.
       END PROGRAM P3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2464M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P1
           PROGRAM P2
           PROGRAM P3
           CLASS C2464.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT1 IS TYPEDEF USAGE PROGRAM-POINTER TO P1.
       01 PT2 IS TYPEDEF USAGE PROGRAM-POINTER TO P2.
       01 PT3 IS TYPEDEF USAGE PROGRAM-POINTER TO P3.
       01 U USAGE OBJECT REFERENCE.
       01 OC USAGE OBJECT REFERENCE C2464.
       01 R1 TYPE PT1.
       01 R2 TYPE PT2.
       01 R3 TYPE PT3.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE C2464 "NEW" RETURNING OC
           INVOKE OC "RP1" RETURNING R2
           IF R2 = NULL DISPLAY "typed r2 null"
           ELSE DISPLAY "typed r2 WRONG" END-IF
           INVOKE C2464 "NEW" RETURNING U
           INVOKE U "RP1" RETURNING R1
           DISPLAY "r1 ok"
           INVOKE U "RP1" RETURNING R2
           DISPLAY "r2 ok"
           INVOKE U "RP1" RETURNING R3
           DISPLAY "r3 done"
           STOP RUN.
       END PROGRAM PB2464M.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2464 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P1
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT1 IS TYPEDEF USAGE PROGRAM-POINTER TO P1.
       PROCEDURE DIVISION.
       METHOD-ID. RP1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE PT1.
       PROCEDURE DIVISION RETURNING LP.
           SET LP TO NULL.
       END METHOD RP1.
       END OBJECT.
       END CLASS C2464.
