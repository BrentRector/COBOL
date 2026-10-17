      *>--- conformance-case
      *> id: l1_start_first_last_relative
      *> rules: [GR-14.9.41.4-11, GR-14.9.41.4-12]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.41.4 GR11 and GR12 (RELATIVE FILES).
      *> GR11 — "If FIRST is specified, the file position indicator is
      *> set to the relative record number of the first existing
      *> logical record in the file. If no records exist in the file,
      *> the invalid key condition exists and the execution of the
      *> START statement is unsuccessful."
      *> GR12 — the same for LAST and "the last existing logical
      *> record".
      *> The word the goldens have to bite on is EXISTING. FR is
      *> SPARSE: its records are at relative record numbers 3, 7 and 9,
      *> so "first existing" is 3 (not 1, the lowest number the file
      *> could hold) and "last existing" is 9 (not 3, the count).
      *>   FK  START FIRST, then READ NEXT. §14.9.30.4 GR21b makes the
      *>       record the START selected available and GR25 moves its
      *>       relative record number into the RELATIVE KEY item, so
      *>       the indicator's value prints outright: 0003, with R003.
      *>       RK is deliberately set to 5 beforehand — FIRST takes no
      *>       operand, and a phrase that (wrongly) consulted RK could
      *>       not answer 3.
      *>   LK  START LAST, then READ NEXT: 0009 with R009. RK holds
      *>       0003 at that point, so again the answer cannot have come
      *>       from the key item.
      *>   EF/EL  A relative file with no records at all. Both phrases
      *>       take the INVALID KEY imperative and set '23': §9.1.13.5
      *>       lists the invalid-key values, and '21', '22' and '24'
      *>       are all WRITE or REWRITE conditions, so '23' is the only
      *>       one a START can produce — the value the standard states
      *>       outright in the indexed and sequential twins of these
      *>       rules (GR18, GR19, GR20, GR21).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1STRFL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FR ASSIGN TO "l1strflr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS ST-R.
           SELECT FE ASSIGN TO "l1strfle.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS EK
               FILE STATUS IS ST-E.
       DATA DIVISION.
       FILE SECTION.
       FD FR.
       01 R-REC.
          05 R-VAL PIC X(4).
       FD FE.
       01 E-REC.
          05 E-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK   PIC 9(4).
       01 EK   PIC 9(4).
       01 ST-R PIC XX.
       01 ST-E PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FR
           MOVE 3 TO RK
           MOVE "R003" TO R-VAL
           WRITE R-REC
           MOVE 7 TO RK
           MOVE "R007" TO R-VAL
           WRITE R-REC
           MOVE 9 TO RK
           MOVE "R009" TO R-VAL
           WRITE R-REC
           CLOSE FR
      *> A relative file with no records at all.
           OPEN OUTPUT FE
           CLOSE FE
      *> ---- FIRST on a sparse file (GR11) -----------------------
           OPEN INPUT FR
           MOVE 5 TO RK
           MOVE "XXXX" TO R-VAL
           START FR FIRST
               INVALID KEY DISPLAY "F1INV=YES"
               NOT INVALID KEY DISPLAY "F1INV=NO"
           END-START
           DISPLAY "F1=" ST-R
           READ FR NEXT AT END CONTINUE END-READ
           DISPLAY "FK=" RK " FV=" R-VAL
      *> ---- LAST on a sparse file (GR12) ------------------------
           MOVE "XXXX" TO R-VAL
           START FR LAST
               INVALID KEY DISPLAY "L1INV=YES"
               NOT INVALID KEY DISPLAY "L1INV=NO"
           END-START
           DISPLAY "L1=" ST-R
           READ FR NEXT AT END CONTINUE END-READ
           DISPLAY "LK=" RK " LV=" R-VAL
           CLOSE FR
      *> ---- both phrases on a file with no records --------------
           OPEN INPUT FE
           DISPLAY "EOPEN=" ST-E
           START FE FIRST
               INVALID KEY DISPLAY "EFINV=YES"
               NOT INVALID KEY DISPLAY "EFINV=NO"
           END-START
           DISPLAY "EF=" ST-E
           START FE LAST
               INVALID KEY DISPLAY "ELINV=YES"
               NOT INVALID KEY DISPLAY "ELINV=NO"
           END-START
           DISPLAY "EL=" ST-E
           CLOSE FE
           STOP RUN.
