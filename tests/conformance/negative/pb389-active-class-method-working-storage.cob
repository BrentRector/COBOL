      *> reject-at: 2002 2014 2023
      *> ISO §13.18.60.3 syntax rule 16, the SECOND arm: within a METHOD definition the
      *> ACTIVE-CLASS phrase is admitted only in "the linkage or local-storage section".  A
      *> method's WORKING-STORAGE is per-class storage shared across every activation and
      *> every instance (§11.7), so it cannot hold a reference bound to the class of the
      *> object that invoked THIS activation.  The same clause's positive shapes ship in
      *> tests/conformance/2002/pb389_object_reference_descriptor.cob (LOCAL-STORAGE).
      *> kb/Work PB1308 (decision R62): a method's WORKING-STORAGE SECTION is itself refused (COBOLNET1519, §13.5.3
      *> SR1) at every edition; the entry is still bound in its own section, so this SR16 arm keeps answering too.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB389N2.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
       END PROGRAM PB389N2.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB389N2C.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION.
       MAIN.
           SET A TO SELF.
       END METHOD MK.
       END OBJECT.
       END CLASS PB389N2C.
