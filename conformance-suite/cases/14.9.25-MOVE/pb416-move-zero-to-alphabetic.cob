*>--- conformance-case
*> id: pb416-move-zero-to-alphabetic
*> rules: [SR-14.9.25.3-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB416, the EXPLICIT-MOVE half of the same rule — ISO 14.9.25.3 SR6, "The figurative constant ZERO
*> shall not be moved to an alphabetic data item." It is version-invariant: SR6 is a COBOL-85 rule and the
*> figurative constant ZERO and PIC A are both COBOL-85.
*> This pins a behaviour CHANGE, not only a new INITIALIZE screen. SR6 was unimplemented on BOTH paths, for
*> one structural reason: a figurative constant's Table-16 position is deliberately category-exempt
*> (8.3.3.6.4 GR4 gives ZERO no fixed category), so the table could never refuse it, and MoveTable16's header
*> left the source-shape rules "with the caller" — where MoveBinder had SR7 and SR8 and not SR6. Making
*> MoveTable16 the ONE home of the whole category-keyed validity question (because 14.9.20.3 SR4 makes
*> INITIALIZE ask it too) is what put SR6 somewhere at all. Measured before: this stored "0000" into AB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB416NMZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AB PIC A(4) VALUE "wxyz".
       PROCEDURE DIVISION.
       MAIN.
           MOVE ZERO TO AB.
           STOP RUN.
