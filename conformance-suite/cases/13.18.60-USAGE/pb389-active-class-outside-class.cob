*>--- conformance-case
*> id: pb389-active-class-outside-class
*> rules: [SR-13.18.60.3-16]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.60.3 syntax rule 16: "The ACTIVE-CLASS phrase may be specified only in a
      *> factory definition, an instance definition, or the linkage or local-storage section
      *> of a method definition."  A PROGRAM is none of those, and §13.18.60.4 GR22 e) shows
      *> why the rule exists: the phrase names "the same class as the object that was used to
      *> invoke the method in which this data description entry is specified", and a program
      *> has no such method.  Before kb/Work PB389 ACTIVE-CLASS had no grammar alternative at
      *> all, so this drew COBOLNET0813 ("names the unknown class or interface 'ACTIVE-CLASS'")
      *> plus COBOLNET0901 ("is a reserved word") — a required word of the general format
      *> reported twice over as a botched user-defined name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB389N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
