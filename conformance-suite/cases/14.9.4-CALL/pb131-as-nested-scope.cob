*>--- conformance-case
*> id: pb131-as-nested-scope
*> rules: [SR-14.9.4.3-15, GR-14.9.4.4-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR15 sentence 2: the AS NESTED literal shall name a program contained directly within
      *> the calling program, or a visible COMMON program. The old binder bound the NESTED flag, DISCARDED
      *> it, and let the runtime resolve any outermost program (kb/Work PB131) — the scope is now a bind-time
      *> check over the same 10.7.2 visibility the formal-mode lookup rides.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB131NG.
       PROCEDURE DIVISION.
       MAIN.
           CALL "NOWHERE" AS NESTED
           STOP RUN.
