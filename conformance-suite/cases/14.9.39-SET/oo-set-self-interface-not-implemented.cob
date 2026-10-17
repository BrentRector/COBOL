*>--- conformance-case
*> id: oo-set-self-interface-not-implemented
*> rules: [SR-14.9.39.3-10]
*> source-format: free
*> portable: unverified
*>---
*> ISO §14.9.39.3 SR10d2: when the receiver of a SET … TO SELF is described with an interface-name that
*> identifies int-1, and the SET statement is contained in a method within the INSTANCE definition of the
*> class, "that instance definition shall be described with an IMPLEMENTS clause that references int-1".
*> CSELFN's OBJECT implements nothing, so SET R TO SELF — with R described as OBJECT REFERENCE ISELFN —
*> violates SR10d2 and shall be rejected at bind.
*> Before this check the binder looked the receiver's type up with a CLASS-only Find, so an interface-typed
*> receiver matched neither arm and the statement bound clean; the emitter then rendered a raw (ISELFN)(this)
*> cast, which is an InvalidCastException at run time and — for a sealed class — a Roslyn CS error on
*> generated user source, which the no-CS-on-user-source rule forbids.
IDENTIFICATION DIVISION.
PROGRAM-ID. OOSELFN1.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    CLASS CSELFN
    INTERFACE ISELFN.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 C USAGE OBJECT REFERENCE CSELFN.
PROCEDURE DIVISION.
MAIN.
    INVOKE CSELFN "NEW" RETURNING C.
    INVOKE C "GRAB".
    STOP RUN.
END PROGRAM OOSELFN1.

IDENTIFICATION DIVISION.
INTERFACE-ID. ISELFN.
PROCEDURE DIVISION.
METHOD-ID. PING.
PROCEDURE DIVISION.
END METHOD PING.
END INTERFACE ISELFN.

IDENTIFICATION DIVISION.
CLASS-ID. CSELFN INHERITS FROM BASE.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    CLASS BASE
    INTERFACE ISELFN.
IDENTIFICATION DIVISION.
OBJECT.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 R USAGE OBJECT REFERENCE ISELFN.
PROCEDURE DIVISION.
METHOD-ID. GRAB.
PROCEDURE DIVISION.
MAIN.
    SET R TO SELF.
END METHOD GRAB.
END OBJECT.
END CLASS CSELFN.
