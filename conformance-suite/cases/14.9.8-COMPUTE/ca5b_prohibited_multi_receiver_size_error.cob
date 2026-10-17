*>--- conformance-case
*> id: ca5b_prohibited_multi_receiver_size_error
*> rules: [GR-14.9.8.4-2, FMT-14.9.8.2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> CA5 (CONFORMANCE-FIX-QUEUE) part B (mirror bug): a genuine PROHIBITED violation on the OUTERMOST division
      *> of a MULTI-receiver COMPUTE must be caught (ISO 14.7.4.3 GR7). 10 / 3 = 3.333... is inexact at the scale-2
      *> receivers, so ROUNDED MODE IS PROHIBITED raises EC-SIZE-TRUNCATION and leaves each receiver UNCHANGED
      *> (14.7.5 storing rule 2 / 14.7.4.3 r7). NB ROUNDED attaches per-resultant (14.9.8), so BOTH resultants carry
      *> their own PROHIBITED phrase here. Pre-fix the multi-receiver RHS was truncated to the receiver scale, so the
      *> per-store PROHIBITED check saw an identity rescale (no inexactness), MISSED the violation, and stored 3.33.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA5B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 99V99 VALUE 0.
       01 Y PIC 99V99 VALUE 0.
       PROCEDURE DIVISION.
           COMPUTE X ROUNDED MODE IS PROHIBITED
                   Y ROUNDED MODE IS PROHIBITED = 10 / 3
               ON SIZE ERROR DISPLAY "SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "OK".
           DISPLAY X.
           DISPLAY Y.
           STOP RUN.
