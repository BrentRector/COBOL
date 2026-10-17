*>--- conformance-case
*> id: goback_raising_main
*> rules: [GR-14.9.18.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> §14.9.18.4 GR3 (P13 review C3 fix): a GOBACK executed in a program NOT under
      *> the control of a calling runtime element operates as STOP — "A RAISING phrase,
      *> if specified, is ignored." EC-STORAGE-NOT-AVAIL is FATAL (Table 13) and
      *> checking is NOT enabled; before the fix the main-program stage threw the
      *> checking-off fatal-termination arm (§14.6.13.1.3 #8) — the run unit died
      *> loudly. Now the phrase is ignored: BEFORE prints and the run unit terminates
      *> NORMALLY (exit 0). The called-program staging half is unchanged (GR1b — the
      *> EC propagation suite pins it).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GBRMAIN.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "BEFORE"
           GOBACK RAISING EXCEPTION EC-STORAGE-NOT-AVAIL.
