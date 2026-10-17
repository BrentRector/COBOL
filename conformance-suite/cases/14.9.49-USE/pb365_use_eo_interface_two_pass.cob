*>--- conformance-case
*> id: pb365_use_eo_interface_two_pass
*> rules: [SR-14.9.49.3-16, SR-14.9.49.3-17, GR-14.9.49.4-1, GR-14.9.49.4-14, FMT-14.9.49.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.49.2 Format 4:  USE AFTER {EXCEPTION OBJECT | EO} {object-class-name-1 | interface-name-1}
      *> — a brace group requiring exactly one of TWO alternatives.  §14.9.49.3 SR16 scopes the class arm and
      *> SR17 the interface arm: "Interface-name-1 shall be the name of an interface specified in the REPOSITORY
      *> paragraph."  §14.9.49.4 GR14 selects among them in TWO PASSES:
      *>   a) "If object-class-name-1 is specified and the exception object that was raised is a factory object
      *>      or instance object of object-class-name-1 or of a subclass of object-class-name-1, the associated
      *>      declarative is executed and no other declaratives are executed; otherwise, all of the USE
      *>      statements in the source element are analyzed again and:"
      *>   b) "If interface-name-1 is specified and the exception object that was raised is described with an
      *>      IMPLEMENTS clause that references interface-name-1, the associated declarative is executed and no
      *>      other declaratives are executed".
      *> §11.8.4 GR2 defines "implements" for an instance object as a CLOSURE — the direct IMPLEMENTS, plus
      *> anything an implemented interface inherits, plus anything an inherited class implements.
      *>
      *> THE POINT OF THIS PROGRAM IS THE PASS BOUNDARY, which a single interleaved scan cannot honour.  The
      *> INTERFACE declarative IF-SEC is written FIRST and CX and CY both IMPLEMENT IZ, so a one-pass
      *> source-order selector would run IF-SEC for BOTH raises.  GR14 runs every class entry first, so the
      *> raise of a CY runs CL-SEC even though IF-SEC is written earlier and also qualifies; only the raise of
      *> a CX — for which no class entry qualifies — reaches pass b).
      *>
      *> Before kb/Work PB365 this program did not compile at all: DeclBindUse resolved the Format-4 operand
      *> with the class table alone, so `USE AFTER EXCEPTION OBJECT IZ` was rejected COBOLNET0859 "does not
      *> name a class of the compilation group" — SR16's diagnostic answering SR17's question — and pass b)
      *> had no code at all.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  RAISE OY: §14.9.29.4 GR1 makes OY the exception object; §14.9.49.4 GR3 makes
      *> Format 4 selection replace the F1/F3 tiers; GR14 a) finds CL-SEC (OY is an instance object of CY) and
      *> "no other declaratives are executed" -> CLASS-CY.  A RAISE is not by itself fatal (§14.9.29.4 GR2), so
      *> control returns after it -> AFTER-1.  RAISE OX: GR14 a) finds no qualifying class entry (CX is neither
      *> CY nor a subclass of it), the USE statements are analyzed again, and GR14 b) finds IF-SEC because CX's
      *> OBJECT paragraph is described with IMPLEMENTS IZ -> IFACE, then AFTER-2.  Expected, in order:
      *> CLASS-CY, AFTER-1, IFACE, AFTER-2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB365F4.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CX365
           CLASS CY365
           INTERFACE IZ365.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OX USAGE OBJECT REFERENCE CX365.
       01 OY USAGE OBJECT REFERENCE CY365.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IF-SEC SECTION.
           USE AFTER EXCEPTION OBJECT IZ365.
       IF-P.
           DISPLAY "IFACE".
       CL-SEC SECTION.
           USE AFTER EO CY365.
       CL-P.
           DISPLAY "CLASS-CY".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE CY365 "NEW" RETURNING OY.
           RAISE OY.
           DISPLAY "AFTER-1".
           INVOKE CX365 "NEW" RETURNING OX.
           RAISE OX.
           DISPLAY "AFTER-2".
           STOP RUN.
       END PROGRAM PB365F4.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. IZ365.
       END INTERFACE IZ365.

       IDENTIFICATION DIVISION.
       CLASS-ID. CX365 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE IZ365.
       IDENTIFICATION DIVISION.
       OBJECT.
       IMPLEMENTS IZ365.
       END OBJECT.
       END CLASS CX365.

       IDENTIFICATION DIVISION.
       CLASS-ID. CY365 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE IZ365.
       IDENTIFICATION DIVISION.
       OBJECT.
       IMPLEMENTS IZ365.
       END OBJECT.
       END CLASS CY365.
