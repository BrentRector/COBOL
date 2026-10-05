      *> kb/Work PB1112 (universal leg) / PB480 - an ACTIVE-CLASS formal reached through a UNIVERSAL object
      *> reference. 9.3.6 match rule 3 d) 4.: "If the parameter in the invoked method is described with the
      *> ACTIVE-CLASS phrase and FACTORY phrase is not specified, the corresponding parameter shall evaluate to an
      *> object reference of the same class specified in the invocation" (cite.py --check 9.3.6 -> OK 9.3.6 3) d) 4.),
      *> and 5.: with the FACTORY phrase it "shall evaluate to an object reference to the factory of the class
      *> specified in the invocation" (OK 9.3.6 3) d) 5.)). Through a universal receiver the class is a run-time fact
      *> - "it is the class of the actual object referenced at runtime that is used in resolving a method
      *> invocation" (9.3.6) - so the rule is a condition on the argument's VALUE, evaluated at run time; a method
      *> whose argument fails it does not match, and "6) otherwise, the EC-OO-METHOD exception condition is set to
      *> exist" (OK 9.3.6 6)). Before the fix the formal was keyed by its CONTAINING class, so the argument's
      *> description decided instead of its object.
      *> DERIVATION (C1112U declares TA and TF; D1112U inherits them):
      *>   U = a C1112U, argument a C1112U                      -> TA-RAN
      *>   U = a C1112U, argument a D1112U (another class)      -> EC-OO-METHOD
      *>   U = a D1112U, argument a D1112U (the invoked class)  -> TA-RAN
      *>   U = a D1112U, argument a C1112U                      -> EC-OO-METHOD
      *>   U = a C1112U, FACTORY formal, argument C1112U's factory object -> TF-RAN
      *>   U = a C1112U, FACTORY formal, argument an instance   -> EC-OO-METHOD
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1112U.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C1112U
           CLASS D1112U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 UC USAGE OBJECT REFERENCE.
       01 UD USAGE OBJECT REFERENCE.
       01 FC USAGE OBJECT REFERENCE.
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
           INVOKE C1112U "NEW" RETURNING UC
           INVOKE D1112U "NEW" RETURNING UD
           INVOKE UC "FactoryObject" RETURNING FC
           INVOKE UC "TA" USING UC
           INVOKE UC "TA" USING UD
           INVOKE UD "TA" USING UD
           INVOKE UD "TA" USING UC
           INVOKE UC "TF" USING FC
           INVOKE UC "TF" USING UC
           DISPLAY "END".
           STOP RUN.
       END PROGRAM PB1112U.

       IDENTIFICATION DIVISION.
       CLASS-ID. C1112U INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING LA.
           DISPLAY "TA-RAN".
       END METHOD TA.
       METHOD-ID. TF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LF USAGE OBJECT REFERENCE FACTORY OF ACTIVE-CLASS.
       PROCEDURE DIVISION USING LF.
           DISPLAY "TF-RAN".
       END METHOD TF.
       END OBJECT.
       END CLASS C1112U.

       IDENTIFICATION DIVISION.
       CLASS-ID. D1112U INHERITS FROM C1112U.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C1112U.
       END CLASS D1112U.
