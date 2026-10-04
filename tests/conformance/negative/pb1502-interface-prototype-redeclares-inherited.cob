      *> reject-at: 2002 2014 2023
      *> ISO 11.7.3 SR4 b) (cite.py --check 11.7.3 "no inherited method prototype shall have the same method
      *> resolution signature as the method prototype declared by this method definition" -> OK 11.7.3 4) b)):
      *> MQ5J inherits the prototype PING from MQ5I and declares PING again.  OVERRIDE, the only way to redeclare an
      *> inherited method, is forbidden in a prototype (SR2, cite.py --check 11.7.3 "The OVERRIDE phrase shall not
      *> be specified in a method prototype" -> OK 11.7.3 2)), so there is no legal spelling.  The class arm (SR4 a)
      *> was enforced already (COBOLNET0837); only the interface arm was missing.  COBOLNET2761.  kb/Work PB1502.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1502N2.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
       END PROGRAM PB1502N2.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. MQ5I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE MQ5I.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. MQ5J INHERITS FROM MQ5I.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE MQ5I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE MQ5J.
