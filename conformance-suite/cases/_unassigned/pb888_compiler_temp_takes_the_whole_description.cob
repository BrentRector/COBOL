*>--- conformance-case
*> id: pb888_compiler_temp_takes_the_whole_description
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB888 - a compiler TEMPORARY takes the WHOLE description of the item it stands for.
      *>
      *> THE RULE. ISO 8.4.3.2.4 GR1: "the description, class, and category of the temporary data item is
      *> that specified by the description in the linkage section of the item specified in the RETURNING
      *> phrase". The whole entry - not a chosen subset of its clauses - so a RETURNING item described with
      *> GROUP-USAGE NATIONAL (13.18.29) yields a temporary that IS a national group. 14.9.25.4 GR1 makes the
      *> same demand of the intermediate result item of a multi-receiver MOVE: "MOVE a (b) TO b, c (b)" is
      *> equivalent to "MOVE a (b) TO temp / MOVE temp TO b / MOVE temp to c (b)", so each receiver owes
      *> exactly what a single-receiver MOVE of the same sender gives.
      *>
      *> EXPECTED VALUES, DERIVED - never measured.
      *>   15.50.4 r2: LENGTH of a national group item counts NATIONAL character positions, so the
      *>     function result over 02 PIC N(4) is 4 - the same as its byte-identical working-storage control.
      *>   13.18.29.4 GR2 b): a national group "is treated as though it were an elementary data item of usage
      *>     national and class and category national described with PICTURE N(m)", so moving the result to
      *>     a PIC N(6) receiver is an elementary national MOVE: "WXYZ" left-justified, space-filled.
      *>   The table element NG(I) over 03 PIC N(3) holding "XYZ" moved to TWO PIC N(6) receivers gives
      *>     "XYZ   " in each, exactly what the one-receiver MOVE gives.
      *> Before the fix both temporaries were cloned from a hand list without GROUP-USAGE: LEN-FN=8, and the
      *> two-receiver MOVE put " X   " where the one-receiver MOVE put "XYZ   ".
      *>
      *> COBOL-2002: user-defined functions and GROUP-USAGE are 2002 introductions - the negative twin
      *> tests/conformance/negative/pb888-national-group-result-below-2002.cob pins the gate below it.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB888NGFN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RESULT-NG GROUP-USAGE NATIONAL.
          02 RN PIC N(4).
       PROCEDURE DIVISION RETURNING RESULT-NG.
           MOVE N"WXYZ" TO RN
           GOBACK.
       END FUNCTION PB888NGFN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB888TEMP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB888NGFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CTL-NG GROUP-USAGE NATIONAL.
          02 CN PIC N(4).
       01 NTBL.
          05 NG OCCURS 2 GROUP-USAGE NATIONAL.
             10 NX PIC N(3).
       01 I PIC 9 VALUE 1.
       01 NA PIC N(6).
       01 NB PIC N(6).
       01 NC PIC N(6).
       PROCEDURE DIVISION.
           MOVE N"ABCD" TO CN
           DISPLAY "LEN-CTL=" FUNCTION LENGTH(CTL-NG)
           DISPLAY "LEN-FN=" FUNCTION LENGTH(FUNCTION PB888NGFN)
           MOVE FUNCTION PB888NGFN TO NA
           DISPLAY "FN=[" NA "]"
           MOVE N"XYZ" TO NX(1)
           MOVE N"PQR" TO NX(2)
           MOVE NG(I) TO NC
           DISPLAY "ONE=[" NC "]"
           MOVE NG(I) TO NA NB
           DISPLAY "TWO=[" NA "][" NB "]"
           STOP RUN.
       END PROGRAM PB888TEMP.
