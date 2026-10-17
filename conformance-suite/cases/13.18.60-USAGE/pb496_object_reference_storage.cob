*>--- conformance-case
*> id: pb496_object_reference_storage
*> rules: [GR-13.18.60.4-22, DOC-A.1-214]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.60.4 GR22 a) — "The amount of storage allocated for an object reference
      *> data item is implementor-defined and is not necessarily the same for every object
      *> reference."  Annex A.1 item 214 makes that amount REQUIRED user documentation, and
      *> COBOL.NET's determination is docs/CONFORMANCE.md item DOC-A.1-214: an object
      *> reference IS one .NET managed reference, so every shape of the §13.18.60.2 general
      *> format allocates the SAME amount — the runtime's reference width, 8 bytes on the
      *> 64-bit runtime this compiler targets.  This golden is that determination's witness.
      *> kb/Work PB496.
      *>
      *> §15.14.3 argument rule 1 admits the measurement: "Argument-1 shall be an alphanumeric
      *> or national literal, a based entry, a type-name, or a data item of any class or
      *> category" — an object reference is a data item of class object (GR22), so
      *> FUNCTION BYTE-LENGTH is defined on one, and §15.14.4 rule 1 makes its returned value
      *> "the length of argument-1 in number of bytes": exactly the GR22 a) amount.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE DETERMINATION — NOT FROM A RUN:
      *>   U=00008   the universal object reference (GR22 b))
      *>   T=00008   an object-class-name reference (GR22 d)1.b.)
      *>   O=00008   the same with the ONLY phrase (GR22 d)2.b.)
      *>   F=00008   a FACTORY OF reference (GR22 d)1.a.)
      *>   I=00008   an interface-name reference (GR22 c))
      *> DISCRIMINATOR: GR22 a) expressly permits these to DIFFER, so five equal numbers is a
      *> determination and not a tautology — an implementation carrying a fat pointer for the
      *> universal shape and a thin one for the typed shapes conforms too, and answers
      *> differently here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB496S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB496SI
           CLASS PB496SC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       01 T USAGE OBJECT REFERENCE PB496SC.
       01 O USAGE OBJECT REFERENCE PB496SC ONLY.
       01 F USAGE OBJECT REFERENCE FACTORY OF PB496SC.
       01 I USAGE OBJECT REFERENCE PB496SI.
       01 N PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION BYTE-LENGTH(U) TO N.
           DISPLAY "U=" N.
           MOVE FUNCTION BYTE-LENGTH(T) TO N.
           DISPLAY "T=" N.
           MOVE FUNCTION BYTE-LENGTH(O) TO N.
           DISPLAY "O=" N.
           MOVE FUNCTION BYTE-LENGTH(F) TO N.
           DISPLAY "F=" N.
           MOVE FUNCTION BYTE-LENGTH(I) TO N.
           DISPLAY "I=" N.
           STOP RUN.
       END PROGRAM PB496S.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB496SI.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE PB496SI.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB496SC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB496SI.
       IDENTIFICATION DIVISION.
       FACTORY. IMPLEMENTS PB496SI.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN.
           CONTINUE.
       END METHOD PING.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS PB496SI.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN.
           CONTINUE.
       END METHOD PING.
       END OBJECT.
       END CLASS PB496SC.
