      *> reject-at: 2002 2014 2023
      *> ISO §8.4.3.9.3 SR2 — an object property whose identifier-1 is
      *> a universal object reference.
      *> "2) Identifier-1 shall be an object reference; neither a
      *>   universal object reference nor the predefined object
      *>   reference NULL shall be specified."
      *>   OK  §8.4.3.9.3 2)  (Syntax rules)
      *> U is USAGE OBJECT REFERENCE with no class or interface: a
      *> universal object reference (§3 definition: "object reference
      *> that is not restricted to a specific class or interface").
      *>   OK  §3.175   (universal object reference)
      *> Everything else is valid: BAL is a REPOSITORY property (SR1)
      *> with a GET PROPERTY method in class L1C20LC (SR3), and the
      *> same statement with the typed reference A compiles. Expected
      *> rejection: COBOLNET2918 (property-object), the code of this rule alone.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C20L.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C20LC.
           PROPERTY BAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE L1C20LC.
       01 U USAGE OBJECT REFERENCE.
       01 W PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE BAL OF U TO W.
           DISPLAY "W=" W.
           STOP RUN.
       END PROGRAM L1C20L.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C20LC.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-BAL PIC 9(5) VALUE 7.
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC 9(5).
       PROCEDURE DIVISION RETURNING LK-R.
       MAIN.
           MOVE W-BAL TO LK-R.
       END METHOD.
       END OBJECT.
       END CLASS L1C20LC.
