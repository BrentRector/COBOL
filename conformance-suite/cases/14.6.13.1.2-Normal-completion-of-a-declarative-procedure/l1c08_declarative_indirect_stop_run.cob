      *>--- conformance-case
      *> id: l1c08_declarative_indirect_stop_run
      *> rules: [GR-14.6.13.1.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.6.13.1.2 2) — a declarative whose indirectly activated
      *> element terminates the run unit does not complete normally
      *> Rule: "A declarative procedure is said to complete normally
      *>   if, during execution of the declarative procedure, none of
      *>   the following occur: ... 2) Any directly or indirectly
      *>   activated runtime element terminates the run unit."
      *> cite.py:
      *>   OK  §14.6.13.1.2 2)  (Normal completion of a declarative
      *>       procedure)
      *>   OK  §14.6.13.1.2   (Normal completion of a declarative
      *>       procedure) [lead sentence]
      *>   OK  §14.6.13.1.4 3)  a nonfatal EC-USER condition
      *>       with an applicable USE runs the declarative; "If
      *>       execution of the declarative completes normally,
      *>       execution continues as specified in the rules for normal
      *>       execution."
      *>   OK  §14.9.42.4 6)  (General rules) "Execution of the run unit
      *>       terminates and control is transferred to the operating
      *>       system."
      *> Main raises EC-USER-STOPIT (checking ON); its USE declarative
      *> CALLs L1C08M, which CALLs L1C08N, which executes STOP RUN.
      *> DERIVATION OF EVERY EXPECTED LINE:
      *> BEFORE        main, before the RAISE.
      *> DECL-START    the declarative runs for the raised condition.
      *> M-START       L1C08M is activated directly by the declarative.
      *> N-STOPS       L1C08N, activated indirectly (through M), then
      *>               terminates the run unit (STOP RUN, 14.9.42 GR6).
      *> Nothing more: by rule 2) the declarative did NOT complete
      *> normally, so there is no "continue after the RAISE" -
      *> M-END, DECL-END and AFTER-RAISE are never displayed.
       >>TURN EC-USER-STOPIT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C08L.
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-STOPIT.
       U-P.
           DISPLAY "DECL-START".
           CALL "L1C08M".
           DISPLAY "DECL-END".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "BEFORE".
           RAISE EXCEPTION EC-USER-STOPIT.
           DISPLAY "AFTER-RAISE".
           STOP RUN.
       END PROGRAM L1C08L.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C08M.
       PROCEDURE DIVISION.
       M-MAIN.
           DISPLAY "M-START".
           CALL "L1C08N".
           DISPLAY "M-END".
           GOBACK.
       END PROGRAM L1C08M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C08N.
       PROCEDURE DIVISION.
       N-MAIN.
           DISPLAY "N-STOPS".
           STOP RUN.
       END PROGRAM L1C08N.
