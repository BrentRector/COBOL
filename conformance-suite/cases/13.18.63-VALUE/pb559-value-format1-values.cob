*>--- conformance-case
*> id: pb559-value-format1-values
*> rules: [FMT-13.18.63.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB559, the other half - ISO 1989:2023 13.18.63.2 Format 1 (data-item), printed in full as:
*>     VALUE IS literal-1
*> VALUE is underlined, IS is not, and NEITHER "VALUES" NOR "ARE" appears anywhere in the figure. What
*> admits VALUES at all is 13.18.63.3 SR17, "The words VALUE and VALUES are equivalent" - and SR17 is
*> printed in the FORMAT 2 block. Format 3 reaches it through SR24 ("Syntax rules 10 and 17 above apply")
*> and Format 4 through SR34 ("Syntax rules 11, 17, and 25 above apply"). FORMAT 1 APPLIES NEITHER, so on a
*> plain data item VALUES is a spelling no general format of the clause prints.
*> This entry is Format 1 and not 2, 3 or 4 by elimination: Format 2 requires the FROM phrase (13.18.63.2),
*> Format 3 requires level-number 88 (SR33), Format 4 is the report section (SR30 excludes 3 and 5 there,
*> and this is the working-storage section). That is why the screen takes the format from its CALLER rather
*> than re-deriving it from the token sequence, which is identical to Format 3's.
*> The LEGAL Format-2 and Format-3 spellings of VALUES are pinned positively by
*> 2002/l1_value_values_equivalent_2002 and 2023/l1_value_values_equivalent, so this negative cannot be
*> satisfied by refusing VALUES everywhere.
*> EDITION-INVARIANT: Format 1 has printed one word since 1985 and Annex E records no change; MEASURED
*> rejected at all four.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB559B.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-X PIC 9 VALUES 1.
PROCEDURE DIVISION.
    DISPLAY WS-X.
    STOP RUN.
