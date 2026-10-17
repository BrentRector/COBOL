      *>--- conformance-case
      *> id: l1c06_entry_convention_oo
      *> rules: [GR-8.3.2.2-L4.2, GR-11.9.7.4-2]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §11.9.7.4 GR2 + GR4 a) b) c) ENTRY-CONVENTION — COBOL
      *>   method-name mapping, inherited convention
      *> GR2: "When COBOL is specified, the naming convention and
      *>   mapping of method-names
      *> and program-names are as specified in 8.3.2.2, User-defined
      *>   words; other aspects of
      *> the entry convention are implementor-defined."
      *>   cite.py --check 11.9.7.4 "When COBOL is specified, the naming
      *>     convention and
      *>   mapping of method-names and program-names are as specified in
      *>     8.3.2.2"
      *>   -> OK  §11.9.7.4 2)  (General rules)
      *> GR4: "When ENTRY-CONVENTION is not specified, the entry
      *>   convention used is as
      *> follows: a) If a class definition includes the INHERITS clause,
      *>   the entry
      *> convention is inherited from the first class specified in the
      *>   INHERITS clause.
      *> b) If an interface definition includes the INHERITS clause, the
      *>   entry convention
      *> is inherited from the first interface specified in the INHERITS
      *>   clause. c) In all
      *> other cases, the entry convention is COBOL."
      *>   cite.py --check 11.9.7.4 "If a class definition includes the
      *>     INHERITS clause,
      *>   the entry convention is inherited from the first class
      *>     specified in the INHERITS
      *>   clause."  -> OK  §11.9.7.4 4)  (a)
      *>   cite.py --check 11.9.7.4 "If an interface definition includes
      *>     the INHERITS
      *>   clause, the entry convention is inherited from the first
      *>     interface specified in
      *>   the INHERITS clause."  -> OK  §11.9.7.4 4)  (b)
      *>   cite.py --check 11.9.7.4 "In all other cases, the entry
      *>     convention is COBOL."
      *>   -> OK  §11.9.7.4 4)  (c)
      *>   cite.py --check 11.9.7.3 "The ENTRY-CONVENTION clause may be
      *>     specified only in a
      *>   class definition"  -> OK  §11.9.7.3 1)  (Syntax rule)
      *> §8.3.2.2: method-names are externalized user-defined words and,
      *>   without an AS
      *> phrase, the implementor defines their mapping:
      *>   cite.py --check 8.3.2.2 "program-names of outermost programs,
      *>   object-class-names, function-prototype-names,
      *>     interface-names, method-names"
      *>   -> OK  §8.3.2.2 1)  (User-defined words)
      *>   cite.py --check 8.3.2.2 "the implementor defines the mapping
      *>     between the
      *>   user-defined word and the corresponding name"  -> OK
      *>     §8.3.2.2 2)
      *> COBOL.NET's mapping (docs/CONFORMANCE.md DOC-A.1-64):
      *>   case-insensitive, i.e. the
      *> §8.3.2.2 equivalence of upper- and lowercase letters in a
      *>   user-defined word.
      *> The only entry convention COBOL.NET provides is COBOL, so the
      *>   observable of "the
      *> convention is COBOL" is the GR2 name mapping: a method whose
      *>   METHOD-ID is SayHi is
      *> found by the method-name literal "sayhi" / "SAYHI".
      *> Derivation of each expected line:
      *>   BASE-HI     L1C06JB states ENTRY-CONVENTION IS COBOL (GR2);
      *>     INVOKE "sayhi"
      *>               reaches METHOD-ID SayHi.
      *>   BASE-HI     the derived L1C06JD has no clause and INHERITS
      *>     FROM L1C06JB: by
      *>               GR4 a) its convention is L1C06JB's, COBOL, so
      *>                 "SAYHI" on a
      *>               L1C06JD object finds the inherited SayHi.
      *>   DER-EXTRA   likewise its OWN method Extra is found by "extra"
      *>     (GR4 a).
      *>   IF-PING     interface L1C06JIB has no clause and INHERITS
      *>     FROM L1C06JIA,
      *>               which states COBOL: GR4 b) gives COBOL, so "ping"
      *>                 invoked through
      *>               an L1C06JIB-typed reference reaches Ping of
      *>                 L1C06JC.
      *>   MAIN-END    the outermost program L1C06J has no clause: GR4
      *>     c), COBOL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C06J.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C06JB
           CLASS L1C06JD
           CLASS L1C06JC
           INTERFACE L1C06JIB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OB USAGE OBJECT REFERENCE L1C06JB.
       01 OD USAGE OBJECT REFERENCE L1C06JD.
       01 OI USAGE OBJECT REFERENCE L1C06JIB.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE L1C06JB "NEW" RETURNING OB.
           INVOKE OB "sayhi".
           INVOKE L1C06JD "NEW" RETURNING OD.
           INVOKE OD "SAYHI".
           INVOKE OD "extra".
           INVOKE L1C06JC "NEW" RETURNING OI.
           INVOKE OI "ping".
           DISPLAY "MAIN-END".
           STOP RUN.
       END PROGRAM L1C06J.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C06JB INHERITS FROM BASE.
       OPTIONS.
           ENTRY-CONVENTION IS COBOL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SayHi.
       PROCEDURE DIVISION.
           DISPLAY "BASE-HI".
       END METHOD SayHi.
       END OBJECT.
       END CLASS L1C06JB.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C06JD INHERITS FROM L1C06JB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C06JB.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. Extra.
       PROCEDURE DIVISION.
           DISPLAY "DER-EXTRA".
       END METHOD Extra.
       END OBJECT.
       END CLASS L1C06JD.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C06JIA.
       OPTIONS.
           ENTRY-CONVENTION IS COBOL.
       PROCEDURE DIVISION.
       METHOD-ID. Ping.
       PROCEDURE DIVISION.
       END METHOD Ping.
       END INTERFACE L1C06JIA.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C06JIB INHERITS FROM L1C06JIA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE L1C06JIA.
       PROCEDURE DIVISION.
       END INTERFACE L1C06JIB.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C06JC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE L1C06JIB.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS L1C06JIB.
       PROCEDURE DIVISION.
       METHOD-ID. Ping.
       PROCEDURE DIVISION.
           DISPLAY "IF-PING".
       END METHOD Ping.
       END OBJECT.
       END CLASS L1C06JC.
