*>--- conformance-case
*> id: pb1548-new-without-base
*> rules: [SR-14.9.23.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.23.3 SR3: "If object-class-name-1 is specified, literal-1 shall be specified. The value of
      *> literal-1 shall be the name of a method defined in the factory interface of object-class-name-1."
      *>                                                            cite.py: OK §14.9.23.3 3)
      *> New is not predefined for every class: §16.2 "The interface BaseFactoryInterface specifies the factory
      *> interface of the BASE class" (cite.py: OK §16.2), and a class has BASE's methods only by inheriting it
      *> (§9.3.9). PB1548NC has no INHERITS clause, so its factory interface is empty and "NEW" names no method
      *> of it (kb/Work PB1548) -> COBOLNET2448. Before PB1548 this compiled: New was treated as predefined.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1548NM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1548NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1548NC.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1548NC "NEW" RETURNING O
           STOP RUN.
       END PROGRAM PB1548NM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1548NC.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       PROCEDURE DIVISION.
           DISPLAY "M".
       END METHOD M.
       END OBJECT.
       END CLASS PB1548NC.
