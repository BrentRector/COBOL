      *> reject-at: 2002 2014 2023
      *> ISO §8.4.3.13.4 GR3 + §14.9.39.3 SR22 — ADDRESS OF PROGRAM program-prototype-name-1 is a
      *> program-pointer restricted to THAT prototype, so SETting it into a pointer restricted to a prototype
      *> of a different signature is refused.
      *> GR3: "When program-prototype-name-1 is specified, the program-address-identifier has the
      *>   characteristics of a program-pointer restricted to program-prototype-name-1."
      *>   cite.py: OK  §8.4.3.13.4 3)  (General rules)
      *> SR22: "If identifier-7 references a restricted program-pointer, identifier-8 shall be the predefined
      *>   address NULL or shall reference a program-pointer and the program-prototypes associated with
      *>   identifier-7 and identifier-8 shall have the same signature."   cite.py: OK  §14.9.39.3 22)
      *> RP is restricted to L1M7PNA (one USING formal, PIC X(2)); the sender is restricted to L1M7PNB (two
      *> USING formals), so the two prototypes' signatures differ (§8.13: "This information about a source
      *> unit, excluding the externalized name of the source unit, is called its signature." — cite.py: OK
      *> §8.13; the information listed there includes the parameters).  Every other rule is met: both prototypes
      *> precede the program and are named in its REPOSITORY (§8.4.3.13.3 SR3), RPT is a TYPEDEF
      *> (§13.18.60.3 SR19).  Expected: COBOLNET1959 (prototype-pointer-signature, docs/DIAGNOSTICS.md).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M7PNA IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC X(2).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM L1M7PNA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M7PNB IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC X(2).
       01 L-Y PIC X(2).
       PROCEDURE DIVISION USING L-X L-Y.
       END PROGRAM L1M7PNB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M7PNM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM L1M7PNA
           PROGRAM L1M7PNB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RPT IS TYPEDEF USAGE PROGRAM-POINTER TO L1M7PNA.
       01 RP TYPE RPT.
       PROCEDURE DIVISION.
       MAIN-P.
           SET RP TO ADDRESS OF PROGRAM L1M7PNB
           STOP RUN.
       END PROGRAM L1M7PNM.
