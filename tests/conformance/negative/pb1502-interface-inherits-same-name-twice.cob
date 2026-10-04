      *> reject-at: 2002 2014 2023
      *> ISO 11.6.3 SR6 (cite.py --check 11.6.3 "A given interface-name shall not appear more than once in an
      *> INHERITS clause" -> OK 11.6.3 6)): N6B writes N6A twice -- once in capitals, once not, which are ONE
      *> user-defined word (8.1.3.2 GR3).  The rule is keyed on the WRITTEN interface-name (the alias pair in
      *> conformance:2002/pb1502_interface_inherits_legal is legal).  COBOLNET0840.  kb/Work PB1502.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1502N4.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
       END PROGRAM PB1502N4.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. N6A.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE N6A.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. N6B INHERITS FROM N6A n6a.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE N6A.
       END INTERFACE N6B.
