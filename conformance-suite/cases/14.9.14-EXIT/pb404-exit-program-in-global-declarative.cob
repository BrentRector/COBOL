*>--- conformance-case
*> id: pb404-exit-program-in-global-declarative
*> rules: [SR-14.9.14.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB404. ISO 14.9.14.3 SR2, stated under FORMAT 2 (EXIT PROGRAM): "The EXIT statement shall
      *> not be specified in a declarative procedure for which the GLOBAL phrase is specified in the
      *> associated USE statement." EXIT PROGRAM and USE ... GLOBAL are both 1985 elements, so the rule
      *> applies at EVERY edition - which is why this case rejects at 85 while its GOBACK twin
      *> (pb409-goback-in-global-declarative) cannot, the GOBACK statement being a 2002 introduction.
      *> The 2023 archaic-feature warning on EXIT PROGRAM is a WARNING (Annex F.1); the rejection is SR2's.
      *> It is a FLAT prohibition: 14.9.14.4 has no counterpart to RESUME's 14.9.33.4 GR1, which turns a
      *> RESUME inside a global declarative's DYNAMIC scope into a CONTINUE.
      *> POSITIVE CONTROL: the same declarative without the GLOBAL phrase is accepted -
      *> ExitPlacementContextDriftTests.ReturnStatementInADeclarative_FollowsTheGlobalPhrase row PBGD03.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB404.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "negpb404.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON F.
       D-PARA.
           EXIT PROGRAM.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           STOP RUN.
