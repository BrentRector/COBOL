# 2026-10-03 — Local models on the owner's GPU: what they are worth to this project

Experiment record (owner standing rule 2026-09-28: every fleet experiment goes here, `.md` plus raw `.json`). Raw results and
the scripts are in `2026-10-03-local-model-spike/`. Nothing here changed the compiler, a golden, or `kb/Work/`.

**Hardware and software.** RTX 5090 (32 GB), 128 GB RAM, Ollama 0.32.13. Models pulled for the trial: `qwen3.8:27b`,
`gemma4:31b`, `muse-glimmer:30b`, `nemotron-3.5-lightning:30b`, `granite4.2:30b`, `north-mini-code-1.0:q4_K_M`,
`glm-4.7-flash:q4_K_M`, plus the embedding models `qwen3-embedding:4b` and `embeddinggemma`. (`glm-5.3-flash` has only a `:cloud`
tag, which would send repository content off the machine; it was not used.) All runs were local, at temperature 0 where it applies,
with the model unloaded afterwards (`keep_alive` 0) because the GPU is shared with the owner's other work.

**Rules the experiment kept.** The ISO spec decides (CLAUDE.md rule 1); a local model produces leads, indexes and drafts only,
and nothing it writes enters `kb/Work/` or a golden without a Claude or human check (owner, 2026-10-03).

## T1 — retrieval: does the governing clause of a `kb/Work/` note appear in the results?
Ground truth: each note's own `spec_refs` (1,446 notes). Corpus: `specs/ISO_COBOL.md` cut into 3,019 clause chunks. Query: the note
title. A hit means a returned chunk is the cited clause or its parent or child. RRF is reciprocal-rank fusion of BM25 with the model.

| Method | Hit in top 1 | top 5 | top 10 | top 20 | MRR |
|---|---|---|---|---|---|
| BM25 (no model) | 0.312 | 0.609 | 0.713 | 0.802 | 0.444 |
| embeddinggemma | 0.420 | 0.678 | 0.768 | 0.833 | 0.535 |
| qwen3-embedding:4b | 0.382 | 0.651 | 0.740 | 0.823 | 0.503 |
| **BM25 + embeddinggemma (RRF)** | 0.431 | 0.730 | **0.810** | 0.883 | 0.564 |
| **BM25 + qwen3-embedding:4b (RRF)** | 0.428 | 0.738 | **0.820** | 0.882 | 0.565 |

## T2 — report digest: can a model reproduce what an implementer returned as structured fields?
24 implementer reports (waves 1003 to 1008); ground truth is the workflow journal's result record for each report.

| Model | Landed-note F1 | Leads whose code site is kept | Status exact | s / report |
|---|---|---|---|---|
| qwen3.8:27b | 0.981 | 0.725 | 0.875 | 2.2 |
| granite4.2:30b | 0.992 | 0.683 | 0.875 | 3.8 |
| muse-glimmer:30b | 0.992 | 0.680 | 1.000 | 3.5 |
| gemma4:31b | 0.977 | 0.634 | 0.875 | 2.9 |

The three "status misses" of qwen3.8, granite4.2 and gemma4 were the same three reports (PB1668 twice, PB1226): the implementer's own
field said DONE for a group that left notes open, and the models said SPLIT, which the reports support. `muse-glimmer`'s Ollama build
appends a literal `<|eot|>` to every reply; unstripped, JSON parsing failed and scored a false zero, so the harness strips it for every model.

## T3 — lead-to-note matching: does nearest-note search reproduce the registrar's `cluster` grouping?
717 notes with a usable `cluster` of 1,655 notes; title text only; hit means a clustered note is among the top k.

| Method | Top 1 | Top 5 | Top 10 |
|---|---|---|---|
| BM25 | 0.234 | 0.515 | 0.609 |
| embeddinggemma | 0.192 | 0.384 | 0.498 |
| qwen3-embedding:4b | 0.225 | 0.470 | 0.597 |
| BM25 + embeddinggemma (RRF) | 0.225 | 0.488 | 0.589 |
| BM25 + qwen3-embedding:4b (RRF) | 0.272 | 0.562 | 0.662 |

## Reading of the results
- **Retrieval is the one clear gain:** the hybrid finds a governing clause in the top 10 about 10 points more often than keyword search
  (0.82 against 0.71) and ranks it higher (MRR 0.565 against 0.444). The two embedding models are about equal once fused, so the smaller
  `embeddinggemma` (621 MB) is enough.
- **The digest is feasible but low value:** facts are right, leads are kept only about 70% of the time, and a dropped lead is the costly
  error. The orchestrator's context cost per report is already small (returns are capped, PB1912).
- **Lead matching is not good enough to automate:** at best a suggestion list (top-10 recall 0.66); `cluster` groups by root cause, so
  titles often differ, which makes this a noisy proxy.

## What this does NOT show
- Whether better retrieval saves implementer turns. Orientation was about 46% of implementer tokens (waves 45 to 57); showing an effect
  needs a controlled run of real agents with and without the index, which costs quota. It is the next experiment, not done.
- Anything about vision models as second readers of rendered spec diagrams, local fuzz-program generation, or a local model as the
  agent itself (`ollama launch claude --model <tag>`). The web reviews rank qwen3.8 above muse-glimmer for that use, and no review
  measures COBOL compiler work, so a held-out test on an already-landed fix is the honest way to find out.
- Queries here are clean note titles; an agent's real queries are messier. Ground truth is each note author's `spec_refs`, which can
  under-list the clauses that are really relevant.

## Decision
No adoption yet. If the turn-saving test shows a gain, build the hybrid retrieval as a `scripts/` tool with a drift test and a
`kb/Work/` decision note (owner approval, rule 9 and the tooling feedback).

## Reproduce
`t1_retrieval.py <bm25|embeddinggemma|qwen3-embedding:4b|rrf:<model>>`, `t2_digest.py <model>`, `t3_neighbors.py`, run with the
GPU free (ask the owner first). The scripts read the repository and the workflow journals at absolute paths of the 2026-10-03
session; the embedding caches (`emb-*.npy`) are rebuilt on first run and were not committed.
