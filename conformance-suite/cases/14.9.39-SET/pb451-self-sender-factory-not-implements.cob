*>--- conformance-case
*> id: pb451-self-sender-factory-not-implements
*> rules: [SR-14.9.39.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 10 d)1.: with a SELF sender and identifier-3 described
      *> with an interface-name that identifies int-1, "if the SET statement is contained in a
      *> method within the factory definition of the class, that factory definition shall be
      *> described with an IMPLEMENTS clause that references int-1."
      *> The FACTORY / INSTANCE axis is the load-bearing one and this fixture isolates it: the
      *> INSTANCE definition DOES implement the interface and the FACTORY definition does not,
      *> so a screen keyed on the wrong definition — or on the class as a whole — accepts the
      *> program.  d)2., the instance twin, is pinned by conformance:2002/oo_set_self_interface
      *> and conformance:negative/oo-set-self-interface-not-implemented; d)1. had no shipped
      *> witness before this one.  The relation is §11.4.4 GR2's — direct IMPLEMENTS, an
      *> implemented interface that INHERITS int-1, or an inherited class whose factory object
      *> implements it — and all three legs are exercised positively by
      *> conformance:2002/pb451_set_format5_self_sender; none of them holds here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB451N9.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB451N9C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB451N9C "MK".
           STOP RUN.
       END PROGRAM PB451N9.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB451N9I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE PB451N9I.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451N9C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB451N9I.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. MK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 R USAGE OBJECT REFERENCE PB451N9I.
       PROCEDURE DIVISION.
       MAIN.
           SET R TO SELF.
       END METHOD MK.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS PB451N9I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "IPING".
       END METHOD PING.
       END OBJECT.
       END CLASS PB451N9C.
