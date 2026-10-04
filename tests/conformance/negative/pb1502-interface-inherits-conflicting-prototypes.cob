      *> reject-at: 2002 2014 2023
      *> ISO 11.6.3 SR5 (cite.py --check 11.6.3 "If a given method-name is inherited from more than one interface, the
      *> method prototype in each inherited interface shall be such that this interface conforms to all inherited
      *> interfaces" -> OK 11.6.3 5)) and 9.3.10 ("The inheriting interface shall always conform to each of the
      *> inherited interfaces"): N5C inherits SPEAK from N5A (one formal) and from N5B (none), and one method SPEAK
      *> cannot conform to both (9.3.8.2.3 rule 1: the same number of formal parameters).  Before PB1502 the
      *> violation surfaced only when a class IMPLEMENTS N5C; N5C itself, implemented by nothing, was accepted.
      *> COBOLNET2762 on the inheriting interface.  kb/Work PB1502.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1502N1.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
       END PROGRAM PB1502N1.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. N5A.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION USING X.
       END METHOD SPEAK.
       END INTERFACE N5A.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. N5B.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       END METHOD SPEAK.
       END INTERFACE N5B.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. N5C INHERITS FROM N5A N5B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE N5A
           INTERFACE N5B.
       END INTERFACE N5C.
