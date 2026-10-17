*>--- conformance-case
*> id: pb410-goback-raising-last-out-of-place
*> rules: [SR-14.9.18.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB410. ISO 14.9.18.3 SR5: "The LAST phrase may be specified only in a declarative procedure
      *> or WHEN phrase of a PERFORM statement." MAIN-PARA is neither, so this program is in error at every
      *> edition that has the RAISING phrase (a 2002 introduction), and 4.2.2 obliges a compile-time
      *> indication. The rule carries NO method qualifier - the same statement inside a method's PERFORM WHEN
      *> is LEGAL, which is the half the compiler used to refuse.
      *> POSITIVE CONTROLS: the declarative position -
      *> ExitPlacementContextDriftTests.RaisingLastInADeclarative_IsAccepted; the WHEN-phrase position, on
      *> BOTH arms of the 14.9.18.4 GR2/GR4 fork -
      *> GobackStatusArmParityTests.RaisingLastInAPerformWhenPhrase_IsAcceptedOnBothArms.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB410.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "RAN".
           GOBACK RAISING LAST EXCEPTION.
