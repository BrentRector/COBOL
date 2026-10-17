      *>--- conformance-case
      *> id: l1c34_type_subject_based
      *> rules: [GR-13.18.57.4-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.57.4 GR4 — the TYPE subject's own BASED clause
      *>   applies
      *> "If a BASED clause is specified in the data description of
      *>   the subject
      *> of the entry, that BASED clause applies and any BASED clause
      *>   specified
      *> in the description of type-name-1 is ignored for this entry."
      *> cite.py --check 13.18.57.4 "that BASED clause applies and any
      *>   BASED
      *>   clause specified in the description of type-name-1 is
      *>     ignored for
      *>   this entry" -> OK §13.18.57.4 4) (General rules)
      *> T and TG are NOT based; V and VG write BASED beside TYPE.
      *>   Under GR4 the
      *> subject's BASED applies, so V and VG are based items with no
      *>   storage
      *> of their own: after SET ADDRESS OF they ARE the storage of Y.
      *>   An
      *> implementation that let the type description decide (dropping
      *>   the
      *> subject's BASED) would give V its own storage, and a MOVE to
      *>   V would
      *> leave Y unchanged. (The "ignored" half has no divergent
      *>   observable
      *> when the subject is based either way, so only "applies" is
      *>   pinned.)
      *> DERIVATION:
      *>  V addresses Y (VALUE "HELLO")                  -> V=[HELLO]
      *>  MOVE "WORLD" TO V writes Y's storage           -> Y=[WORLD]
      *>  VG (G1 X(2), G2 X(3)) addresses Y too: G2 = Y(3:3) -> G2=[RLD]
      *>  MOVE "AB" TO G1 OF VG writes Y(1:2)             -> Y=[ABRLD]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C34F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF PIC X(5).
       01  TG TYPEDEF.
           05  G1          PIC X(2).
           05  G2          PIC X(3).
       01  V TYPE T BASED.
       01  VG TYPE TG BASED.
       01  Y               PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
           SET ADDRESS OF V TO ADDRESS OF Y.
           DISPLAY "V=[" V "]".
           MOVE "WORLD" TO V.
           DISPLAY "Y=[" Y "]".
           SET ADDRESS OF VG TO ADDRESS OF Y.
           DISPLAY "G2=[" G2 OF VG "]".
           MOVE "AB" TO G1 OF VG.
           DISPLAY "Y=[" Y "]".
           STOP RUN.
