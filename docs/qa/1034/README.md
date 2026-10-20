# Second Truce / zero Attention: browser QA for #1034

These are unmodified screenshots from a real private WebGL build of product commit `bd2abd9d0a297aa1f7d43d0b4a0bd81ae07645e6` (upstream base `2d3e13c098e147493ab034ab2ff20f0b04765fab`), captured on 20 October 2026. This branch only adds QA evidence and its asset record; no product fix is attributed to it. The fix was merged in upstream PR #1053.

## Reproduction and observations

In two fresh isolated Chrome 155.0.8059.80 profiles, 1920x1080, DPR 1, start a new game from the title screen in EN and HE (`?navlog&lang=en` / `he`). End May and June, close the turn reports, then choose the first Ten Days option and first Jerusalem option. Each costs 2.0 Attention. On 15 July 1948, The Second Truce appears with 0.0 of 4.0 Attention.

In both languages, both options show their 2.0 cost and are disabled; clicking them does not produce the old refusal toast. The visible delegation line names David Ben-Gurion (דוד בן-גוריון in HE); End turn is highlighted. End turn advances to 15 August 1948, closes the card, restores 4.0 Attention and removes the highlight. The Why panel records `Delegated to David Ben-Gurion: Comply with the cease-fire`. The existing event/cause English fallback remains visible in HE; this is not a claim that all content is translated or human-approved.

| Stage | EN | HE |
|---|---|---|
| Second Truce, zero Attention | [image](repro-en-second-truce-attention0.png) | [image](repro-he-second-truce-attention0.png) |
| Next turn, Attention restored | [image](repro-en-aug15-restored-attention4.png) | [image](repro-he-aug15-restored-attention4.png) |
| Delegation in Why | [image](repro-en-aug15-why-delegation.png) | [image](repro-he-aug15-why-delegation.png) |

## Checks and limitations

Unity 6000.3.25f1 `Build.Web` ran under `uv run tools/machine_lock.py --shared --wait-unity --` with `-batchmode -quit -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled`; exit 0 in 341.240 s, `Build Finished, Result: Success.`, zero `warning CS` / `error CS`. The trailing debugger listen-44 diagnostic was after build success; it is retained in the original log and is not described as a globally clean log. The 102 player files were hashed before both browser sessions. The player's embedded simulation DLL matched the Release assembly (SHA-256 `1df0efba3cc4ab34708328ff4acc8f2bccc17220479618dfad934b08b22294a5`).

The current commit's own `uv run tools/web_check.py <build> --query 'golden&smoke=5&lang=en'` and the HE query (`golden&smoke=5&lang=he`), each with nine explicit `--expect` values (all in [commands.json](commands.json)), ran under `machine_lock --shared --wait-unity` and both exited 0 (11.931 s / 10.450 s), checking all eight pinned golden hashes and smoke 5. Those gates are separate from the exact manual-input sequence above. Raw CDP was audited: no Runtime.evaluate/callFunctionOn, no JavaScript exceptions or error-level browser/console messages. EN: 26 actions, all predicates passed. HE: 25 actions, one **harness wait typo** (`togglewhy` versus actual log `toggle why`); the click and final screenshot succeeded. The process's exit 0 must not be read as all HE action predicates passing. Original failed predicate and logs are retained locally, not rewritten.

The same product SHA previously passed `bash scripts/check.sh origin/main` (305 policy / 1973 sim / 1434 tools, 4 skipped + 2 subtests), golden update (1973 passed; no golden moved), and full EditMode (1606 total, 1604 passed, 0 failed, 2 Explicit; zero compiler warnings). Five native `WithTooLittleAttention` cases passed. These checks belong to the product SHA, not this later evidence-only commit. This does not complete PR #1158's future integration/layout/focus acceptance.

## Provenance and licence

Screenshots: **CC BY-SA 4.0**, credit **Statecraft: Israel contributors**. This follows the existing `ASSETS.yaml` screenshot ruling and D13 map-bake share-alike; the more general captures row in `LICENSING.md` is not used to downgrade that licence. Reuse must preserve credit, the licence and changes indication. These PNGs are unmodified captures. Licence: [CC BY-SA 4.0](../../../LICENSES/CC-BY-SA-4.0.txt).

The visible event photograph is the project's existing graded/cropped Ben Merk / Anefo image, Nationaal Archief 934-6899, CC0. [Commons source](https://commons.wikimedia.org/wiki/File:Graaf_Bernadotte_op_Schiphol,_Bestanddeelnr_934-6899.jpg), checked by the agent on 20 October 2026 via Commons imageinfo: CC0; original SHA-1 `fb5bef2114d8d19013884a39b2b3534440e87e47`, matching the project's source row. This is not a human source check. Its existing in-image credit is preserved. The built catalog marks this event photograph non-sensitive; no sensitivity-review flags were changed. No Cabinet portrait, sensitive event card, title painting or video is published here. Existing AI-generated map models are credited to MATKOT under the project's existing CC0 records, not claimed as new original art.

Terrain notice: produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and (c) Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA; all rights reserved. The existing [NOTICE](../../../NOTICE) and font/model/source asset records retain their terms. The private build itself is not published or represented as a release-gated shareable build.

Rules check:
1. Exact source, build, browser input, captured output and SHA checked.
2. Each acceptance claim bounded by direct evidence; human/release approval remains separate.
3. State injection, stale build, false all-green harness, privacy and image-licence risks checked.
4. Evidence-only fork branch; no rule, CI, release gate, signature or personal licence-checkbox change.
