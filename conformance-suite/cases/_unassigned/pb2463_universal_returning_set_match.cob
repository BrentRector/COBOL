*>--- conformance-case
*> id: pb2463_universal_returning_set_match
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2463 - 9.3.6 match rule 6 for an OBJECT REFERENCE
      *> RETURNING item of an INVOKE through a UNIVERSAL object
      *> reference. Rule 6 (cite.py --check 9.3.6 -> OK 9.3.6 6)): "there
      *> shall be a corresponding specification in the invoked method
      *> that may be a sending item in a SET statement with the returning
      *> item as the receiving item" - the SET's VALIDITY by 14.9.39.3
      *> Format 5, whose SR10 / SR12 / SR14 relate the two descriptions'
      *> CLASSES (OK 14.9.39.3 10), 12), 14)). No match: the search goes
      *> up the INHERITS chain and "6) otherwise, the EC-OO-METHOD
      *> exception condition is set to exist" (OK 9.3.6 6)).
      *> Classes: C2463P; C2463S INHERITS C2463P, its objects IMPLEMENT
      *> I2463B, and I2463B INHERITS I2463A; C2463O unrelated; C2463M the
      *> class whose methods return each description.
      *> DERIVATION (method / its returning item -> receiving item):
      *>  c01 RS  C2463S      -> C2463S      SR12 a)2. same    match
      *>  c02 RS  C2463S      -> C2463P      SR12 a)2. a subclass
      *>                                                       match
      *>  c03 RS  C2463S      -> C2463O      SR12 a)2. "same class or
      *>      a subclass" - unrelated                      EC-OO-METHOD
      *>  c04 RS  C2463S      -> C2463S ONLY SR12 a)1. the sender shall
      *>      be ONLY                                      EC-OO-METHOD
      *>  c05 RSO C2463S ONLY -> C2463S ONLY SR12 a)1.         match
      *>  c06 RS  C2463S      -> FACTORY OF C2463S  SR12 a)3. FACTORY
      *>      presence differs                             EC-OO-METHOD
      *>  c07 RFC FACTORY OF C2463S -> FACTORY OF C2463P  a)2. + a)3.
      *>                                                       match
      *>  c08 RS  C2463S      -> I2463A      SR10 b)2. its objects
      *>      implement I2463B, which inherits I2463A          match
      *>  c09 RO  C2463O      -> I2463A      SR10 b)2. its objects do
      *>      not implement I2463A                         EC-OO-METHOD
      *>  c10 RI  I2463B      -> I2463A      SR10 a) an interface
      *>      inheriting from int-1                            match
      *>  c11 RI  I2463B      -> C2463S      SR12's closed list has no
      *>      interface-name sender                        EC-OO-METHOD
      *>  c12 RU  universal   -> C2463S      SR12's closed list has no
      *>      universal sender                             EC-OO-METHOD
      *>  c13 RU  universal   -> universal   SR8               match
      *>  c14 RA  ACTIVE-CLASS (in C2463M) -> C2463M  SR12 b)2. "the
      *>      class containing the data item referenced by identifier-4"
      *>      is C2463M                                        match
      *>  c15 RA  ACTIVE-CLASS (in C2463M) -> C2463P  SR12 b)2. C2463M
      *>      is not C2463P or a subclass                  EC-OO-METHOD
      *>  c16 BASE's New (16.2: "usage object reference active-class",
      *>      written in the standard class BASE) on C2463S's factory
      *>      held in a universal reference -> C2463S  SR12 b)2. BASE is
      *>      not C2463S or a subclass                     EC-OO-METHOD
      *>  c17 the same New -> universal  SR8                   match
      *> A matching case prints only its tag line; a refused one prints
      *> its HANDLED= line first (RESUME AT NEXT STATEMENT).
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2463M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C2463M
           CLASS C2463P
           CLASS C2463S
           CLASS C2463O
           INTERFACE I2463A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TAG PIC X(3).
       01 U   USAGE OBJECT REFERENCE.
       01 UF  USAGE OBJECT REFERENCE.
       01 UR  USAGE OBJECT REFERENCE.
       01 OP  USAGE OBJECT REFERENCE C2463P.
       01 OS  USAGE OBJECT REFERENCE C2463S.
       01 OSO USAGE OBJECT REFERENCE C2463S ONLY.
       01 OO  USAGE OBJECT REFERENCE C2463O.
       01 OM  USAGE OBJECT REFERENCE C2463M.
       01 FP  USAGE OBJECT REFERENCE FACTORY OF C2463P.
       01 FS  USAGE OBJECT REFERENCE FACTORY OF C2463S.
       01 IA  USAGE OBJECT REFERENCE I2463A.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO.
       H-P.
           DISPLAY TAG " HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE C2463M "NEW" RETURNING U
           SET UF TO C2463S
           MOVE "c01" TO TAG
           INVOKE U "RS" RETURNING OS
           DISPLAY TAG
           MOVE "c02" TO TAG
           INVOKE U "RS" RETURNING OP
           DISPLAY TAG
           MOVE "c03" TO TAG
           INVOKE U "RS" RETURNING OO
           DISPLAY TAG
           MOVE "c04" TO TAG
           INVOKE U "RS" RETURNING OSO
           DISPLAY TAG
           MOVE "c05" TO TAG
           INVOKE U "RSO" RETURNING OSO
           DISPLAY TAG
           MOVE "c06" TO TAG
           INVOKE U "RS" RETURNING FS
           DISPLAY TAG
           MOVE "c07" TO TAG
           INVOKE U "RFC" RETURNING FP
           DISPLAY TAG
           MOVE "c08" TO TAG
           INVOKE U "RS" RETURNING IA
           DISPLAY TAG
           MOVE "c09" TO TAG
           INVOKE U "RO" RETURNING IA
           DISPLAY TAG
           MOVE "c10" TO TAG
           INVOKE U "RI" RETURNING IA
           DISPLAY TAG
           MOVE "c11" TO TAG
           INVOKE U "RI" RETURNING OS
           DISPLAY TAG
           MOVE "c12" TO TAG
           INVOKE U "RU" RETURNING OS
           DISPLAY TAG
           MOVE "c13" TO TAG
           INVOKE U "RU" RETURNING UR
           DISPLAY TAG
           MOVE "c14" TO TAG
           INVOKE U "RA" RETURNING OM
           DISPLAY TAG
           MOVE "c15" TO TAG
           INVOKE U "RA" RETURNING OP
           DISPLAY TAG
           MOVE "c16" TO TAG
           INVOKE UF "NEW" RETURNING OS
           DISPLAY TAG
           MOVE "c17" TO TAG
           INVOKE UF "NEW" RETURNING UR
           DISPLAY TAG
           DISPLAY "END".
           STOP RUN.
       END PROGRAM PB2463M.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. I2463A.
       END INTERFACE I2463A.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. I2463B INHERITS FROM I2463A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE I2463A.
       END INTERFACE I2463B.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2463P INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS C2463P.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2463S INHERITS FROM C2463P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C2463P
           INTERFACE I2463B.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS I2463B.
       END OBJECT.
       END CLASS C2463S.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2463O INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS C2463O.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2463M INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS C2463S
           CLASS C2463O
           INTERFACE I2463B.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. RS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR USAGE OBJECT REFERENCE C2463S.
       PROCEDURE DIVISION RETURNING LR.
           INVOKE C2463S "NEW" RETURNING LR.
       END METHOD RS.
       METHOD-ID. RSO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR USAGE OBJECT REFERENCE C2463S ONLY.
       PROCEDURE DIVISION RETURNING LR.
           INVOKE C2463S "NEW" RETURNING LR.
       END METHOD RSO.
       METHOD-ID. RFC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR USAGE OBJECT REFERENCE FACTORY OF C2463S.
       PROCEDURE DIVISION RETURNING LR.
           SET LR TO C2463S.
       END METHOD RFC.
       METHOD-ID. RO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR USAGE OBJECT REFERENCE C2463O.
       PROCEDURE DIVISION RETURNING LR.
           INVOKE C2463O "NEW" RETURNING LR.
       END METHOD RO.
       METHOD-ID. RI.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR USAGE OBJECT REFERENCE I2463B.
       PROCEDURE DIVISION RETURNING LR.
           INVOKE C2463S "NEW" RETURNING LR.
       END METHOD RI.
       METHOD-ID. RU.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION RETURNING LR.
           INVOKE C2463S "NEW" RETURNING LR.
       END METHOD RU.
       METHOD-ID. RA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING LR.
           SET LR TO SELF.
       END METHOD RA.
       END OBJECT.
       END CLASS C2463M.
