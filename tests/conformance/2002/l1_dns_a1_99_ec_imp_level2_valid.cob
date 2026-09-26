      *> ISO §14.6.13.1.1 — the level-2 name EC-IMP stays valid when
      *> no EC-IMP-suffix is provided (Annex A.1 item 99, Not provided):
      *> the complement of the negative/l1-dns-a1-99-* refusals.
      *>
      *> THE RULES. §14.6.13.1.1: "Level-2 consists of the exception-
      *>   names EC-ARGUMENT, EC-BOUND, EC-DATA, EC-EXTERNAL, EC-FLOW,
      *>   EC-FUNCTION, EC-I-O, EC-IMP" ...   OK  §14.6.13.1.1
      *> §7.3.25.3 2): "Exception-name-1 shall be one of the exception
      *>   names listed in 14.6.13.1, Exception conditions."
      *>   OK  §7.3.25.3 2)
      *> §7.3.25.4 3): "if exception-name-1 is one of the level-2
      *>   exception-names, the effect is as if that TURN directive were
      *>   specified containing all exception-names that are subordinate
      *>   to that level-2 exception-name"   OK  §7.3.25.4 3)
      *>
      *> THE DOCUMENTED CHOICE, docs/CONFORMANCE.md DOC-A.1-99: "The
      *>   level-2 name EC-IMP is still valid wherever Table 13 allows a
      *>   level-2 name, and no level-3 condition exists under it."
      *>
      *> WHY IT IS OWED. The refusal of EC-IMP-suffix (kb/Work PB1531)
      *>   must not over-reach to the level-2 name itself, which is
      *>   LISTED in §14.6.13.1.1 whatever the implementor provides.
      *>
      *> DERIVATION. The >>TURN names a listed level-2 name, legal by
      *>   SR2; by GR3 it enables checking for the level-3 names under
      *>   EC-IMP, of which none exist, so it enables nothing. The USE
      *>   declarative names the same listed name (USE Format 3
      *>   exception-name-1). No statement raises any condition, so the
      *>   declarative never runs and the one DISPLAY prints
      *>   EC-IMP-L2=OK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DNS99P.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-IMP.
       D-PARA.
           DISPLAY "HANDLER".
       END DECLARATIVES.
       MAIN SECTION.
       M-PARA.
       >>TURN EC-IMP CHECKING ON
           DISPLAY "EC-IMP-L2=OK".
           STOP RUN.
