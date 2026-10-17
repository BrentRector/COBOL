*>--- conformance-case
*> id: pb451-classname-sender-factory-not-implements
*> rules: [SR-14.9.39.3-11]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 11: "If object-class-name-1 is specified and the data item
      *> referenced by identifier-3 is described with an interface-name that identifies the
      *> interface int-1, the factory object of object-class-name-1 shall be described with an
      *> IMPLEMENTS clause that references int-1."  §14.9.39.4 GR10 makes the class NAME send
      *> the class's FACTORY object, so it is the FACTORY definition that has to implement the
      *> interface.  Here the INSTANCE definition does and the FACTORY definition does not —
      *> the discriminator, because a screen that asked the instance-side closure (or asked
      *> merely "is the receiver typed at all") would accept this program.
      *> The conforming twin is conformance:cases/13.18.60-USAGE/pb451_set_format5_class_name_sender.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB451N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB451N1I
           CLASS PB451N1C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R USAGE OBJECT REFERENCE PB451N1I.
       PROCEDURE DIVISION.
       MAIN.
           SET R TO PB451N1C.
           STOP RUN.
       END PROGRAM PB451N1.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB451N1I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE PB451N1I.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451N1C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB451N1I.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS PB451N1I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "IPING".
       END METHOD PING.
       END OBJECT.
       END CLASS PB451N1C.
