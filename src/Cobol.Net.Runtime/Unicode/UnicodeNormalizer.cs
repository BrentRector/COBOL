// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Runtime.Collation;

namespace CobolNet.Runtime.Unicode;

/// <summary>
/// The PUBLIC, stable Unicode normalization surface of WiseOwl COBOL — putting text into one of the two canonical
/// forms (<see cref="UnicodeNormalizationForm.NFD"/>, <see cref="UnicodeNormalizationForm.NFC"/>) so that two
/// spellings of the same characters can be recognized as the same text. See <c>Unicode/README.md</c> for the why.
/// <para><b>NFD is computed by the collation engine's own table-driven decomposition</b>
/// (<c>Collation/Normalizer.cs</c> over <see cref="CollationTable.Root"/>) — there is exactly ONE NFD in this
/// runtime and this class does not add a second. That decomposition reads the canonical decomposition mappings and
/// combining classes baked into the derived collation table, so its answer is the SAME on every host and depends
/// only on the Unicode version the table was generated from (<see cref="NfdUnicodeVersion"/> — 17.0.0), never on
/// the host's ICU. That independence is not theoretical: the development host's bundled ICU predates Unicode 16
/// (<c>Collation/README.md</c> §1/§4).</para>
/// <para><b>NFC is the host's</b> — <see cref="string.Normalize(NormalizationForm)"/> — because composition needs
/// data the derived table does not carry (the composition mappings plus <c>Full_Composition_Exclusion</c>, which
/// NFD never consults). Its Unicode version is therefore the host's, and on a host built with
/// <c>InvariantGlobalization</c> there is no normalizer at all: <see cref="IsNfcAvailable"/> reports that once per
/// process and <see cref="Normalize"/> then returns the text unchanged rather than throwing. This asymmetry is
/// acceptable because NOTHING in the compiler or the runtime needs NFC to be correct — collation, sort keys,
/// indexed-file keys and canonical equivalence are all decided in NFD by the table's own data; NFC is offered for
/// callers that must hand text to the outside world in its composed spelling. <b>Upgrade path:</b> teach
/// <c>scripts/collation/generate-collation-table.py</c> to emit the canonical composition pairs and the
/// <c>Full_Composition_Exclusion</c> set into <c>root-collation.bin</c>; NFC then becomes table-driven and
/// host-independent too, and <see cref="IsNfcAvailable"/> becomes permanently true.</para>
/// <para>All members are thread-safe (the table is immutable and loaded once; the NFC probe is a
/// <see cref="Lazy{T}"/>).</para>
/// </summary>
public static class UnicodeNormalizer
{
    /// <summary>"e" + COMBINING ACUTE ACCENT — a two-code-point text whose NFC is the single code point U+00E9.
    /// A host that composes it has a real normalizer; a host without one either throws or leaves it alone.</summary>
    private const string NfcProbeText = "e\U00000301";

    private static readonly Lazy<bool> s_nfcAvailable = new(ProbeNfc, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Can this host compose (NFC)? False under <c>InvariantGlobalization</c>, where .NET's normalization
    /// APIs refuse every non-ASCII text; <see cref="Normalize"/> and <see cref="IsNormalized"/> then treat every
    /// text as already composed and return it unchanged, instead of throwing. Probed once per process.
    /// <para><see cref="UnicodeNormalizationForm.NFD"/> has no such property: it is always available, because it is
    /// computed from the collation table's own data.</para></summary>
    public static bool IsNfcAvailable => s_nfcAvailable.Value;

    /// <summary>The Unicode version whose data decides <see cref="UnicodeNormalizationForm.NFD"/> here — the derived
    /// collation table's (<see cref="CollationTable.UcaVersion"/>). NFC follows the HOST's Unicode version instead,
    /// which is why the two forms may disagree about a character added after the host's ICU was built.</summary>
    public static string NfdUnicodeVersion => CollationTable.Root.UcaVersion;

    /// <summary>The <paramref name="form"/> of <paramref name="input"/>.
    /// <para>NFD is the collation table's own decomposition (host-independent, <see cref="NfdUnicodeVersion"/>);
    /// a text that is already its own NFD is returned BY REFERENCE, unchanged and without allocating.</para>
    /// <para>NFC is the host's composition. On a host that cannot normalize (<see cref="IsNfcAvailable"/> false) the
    /// input is returned unchanged. Ill-formed UTF-16 (an unpaired surrogate) is also returned unchanged rather than
    /// throwing — NFD passes such a code unit through as itself, and this keeps the two forms consistent.</para></summary>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="form"/> is not one of the two forms.</exception>
    public static string Normalize(string input, UnicodeNormalizationForm form)
    {
        ArgumentNullException.ThrowIfNull(input);
        return form switch
        {
            UnicodeNormalizationForm.NFD => ToNfd(input),
            UnicodeNormalizationForm.NFC => ToNfc(input),
            _ => throw new ArgumentOutOfRangeException(nameof(form), form, "normalization form must be NFC or NFD"),
        };
    }

    /// <summary>Is <paramref name="input"/> already in <paramref name="form"/> — i.e. would
    /// <see cref="Normalize"/> return it unchanged? For NFD this is decided by the collation table's data (and
    /// answered without allocating for the common text that holds no decomposable character and no combining mark);
    /// for NFC it is the host's answer, and TRUE on a host that cannot normalize (there, NFC is the identity).</summary>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="form"/> is not one of the two forms.</exception>
    public static bool IsNormalized(string input, UnicodeNormalizationForm form)
    {
        ArgumentNullException.ThrowIfNull(input);
        switch (form)
        {
            case UnicodeNormalizationForm.NFD:
            {
                var table = CollationTable.Root;
                // Nothing decomposable and no combining mark ⇒ the text IS its own NFD, with no work done.
                if (!Normalizer.NeedsNfd(input, table, forIdentical: true)) return true;
                // A combining mark alone does not make a text un-normalized: it may already be decomposed AND in
                // canonical order. Only the decomposition itself can say.
                return string.Equals(input, Normalizer.ToNfd(input, table), StringComparison.Ordinal);
            }
            case UnicodeNormalizationForm.NFC:
                if (!IsNfcAvailable) return true;
                try { return input.IsNormalized(NormalizationForm.FormC); }
                catch (NotSupportedException) { return true; }
                catch (ArgumentException) { return true; }   // ill-formed: Normalize leaves it alone, so it is its own NFC
            default:
                throw new ArgumentOutOfRangeException(nameof(form), form, "normalization form must be NFC or NFD");
        }
    }

    /// <summary>Normalize both texts to <paramref name="form"/> and compare them with the collation engine
    /// (<see cref="CollationEngine.Compare(string?,string?)"/> — the CLDR root order). Null is the empty string.
    /// <para><b>This is not how canonical equivalence is achieved</b> — the engine already gives canonically
    /// equivalent texts the same place in the order, because it decomposes with the table's own data whenever a
    /// text holds a combining mark (and, at <see cref="CollationStrength.Identical"/>, whenever it holds any
    /// decomposable character). The value of this helper is (a) an NFC-normalized comparison for a caller whose
    /// semantics are stated in composed form, and (b) an EXPLICIT, host-visible normalization step, so that what
    /// the comparison saw is the text the caller can also print, log or store.</para>
    /// <para>Consequently it returns the same SIGN as <see cref="CollationEngine.Compare(string?,string?)"/> on the
    /// unnormalized texts for every canonically equivalent pair (asserted by
    /// <c>UnicodeNormalizerTests.CompareNormalized_AgreesWithTheEngine_OnACorpus</c>).</para></summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="form"/> is not one of the two forms.</exception>
    public static int CompareNormalized(string? a, string? b, UnicodeNormalizationForm form) =>
        CollationEngine.Compare(Normalize(a ?? "", form), Normalize(b ?? "", form));

    // ---- the two forms ------------------------------------------------------------------------------------------

    /// <summary>NFD through the collation engine's table-driven decomposition — the ONE NFD in this runtime.</summary>
    private static string ToNfd(string input)
    {
        var table = CollationTable.Root;
        // Fast path: no decomposable code point, no Hangul syllable, no combining mark ⇒ the input IS its NFD.
        return Normalizer.NeedsNfd(input, table, forIdentical: true) ? Normalizer.ToNfd(input, table) : input;
    }

    /// <summary>NFC through the host, with the invariant-globalization and ill-formed-text fallbacks.</summary>
    private static string ToNfc(string input)
    {
        if (input.Length == 0 || !IsNfcAvailable) return input;
        try { return input.Normalize(NormalizationForm.FormC); }
        catch (NotSupportedException) { return input; }     // PlatformNotSupportedException: no normalizer on this host
        catch (ArgumentException) { return input; }         // ill-formed UTF-16 — NFD passes it through, so does this
    }

    private static bool ProbeNfc()
    {
        try
        {
            // Not merely "did it return" — a usable host actually COMPOSES: two code points become U+00E9.
            return NfcProbeText.Normalize(NormalizationForm.FormC) == "\U000000E9";
        }
        catch (NotSupportedException) { return false; }
        catch (ArgumentException) { return false; }
    }
}
