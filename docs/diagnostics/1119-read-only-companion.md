# WebGL read-only companion (B)

Refs #1119; draft #1230. The [23 October implementation approval](https://github.com/eyalgolan/statecraft-israel/issues/1119#issuecomment-6360904442)
supersedes the original proposal's generations, sequences and action callbacks for this milestone.
The contributor's real VoiceOver pass is still required. B is not a claim of action parity or a fully accessible game.

## What runs

`unity/Assets/Scripts/UI/AccessibilityCompanion.cs` reads the existing UI tree on the next
scheduled frame after `Changed`, `Refused`, `TurnEnded`, pointer/submit/cancel/key and focus
events, including text-change events for late notices. Root geometry changes also request a read.
A second scheduled stage leaves a UI style/layout pass before the read, so resolved visibility is current.
Requests coalesce to at most one
publication per frame. An identical JSON snapshot is not sent again. Presenters and sim
rules are untouched; all text, language fallback, costs and enabled states come from the
rendered elements. The collector reads:

- `event-title`, `event-office`, `event-alt-banner`, `event-text`, `event-objective`,
  `event-delegate` and `event-count` while the card is visible and uncovered;
- the buttons under `event-options` in order, with their enabled state and any child or
  following text (including previews and rejoin notes);
- the panel's current focused element's text, card title, tooltip or visible descendant text;
- the final visible `toast` and the uncovered turn report's title, introduction and rows.

`GameRoot.Begin` owns one collector, made after the presenters and startup notices, under
`UNITY_WEBGL && !UNITY_EDITOR`. `Shutdown` disposes it before rebuilding the language UI
or disabling the root. `Dispose` pauses pending work, unregisters every callback and bridge
subscription, and sends an empty snapshot once. A new game or loaded session on the same
bridge is read through the existing `Changed` path; no old session is retained by the collector.
The optional JSON sink is the EditMode seam. The default transport outside WebGL is a no-op.

`AccessibilityCompanion.jslib` sends the UTF-8 JSON one way to the template renderer.
There is no browser-to-game receiver. The renderer is installed only in the successful
`createUnityInstance` continuation. If it is not mounted yet, the collector retries on
the next frame; nothing is inserted into the panel early. No library, font, image or
additional network request is added.

The HTML panel contains ordinary headings, paragraphs and an ordered list. Every text
element uses `dir="auto"`; `lang` is limited to `en` or `he`, and U+200E marks are retained.
Nodes are built with `createElement` and `textContent`. Invalid JSON or a malformed snapshot
leaves the last good panel intact. Only the pre-existing `role="status"` paragraph is live,
and its contents change only when the status text or turn changes. Focus-only/status-only
updates preserve the event subtree. No panel element takes focus or performs a game action.

## Layout and keyboard play

Above 700 CSS pixels, the game and companion occupy separate grid columns. The companion
is 18–24 rem wide (26 vw between those bounds), with independent vertical scrolling.
At phone width the game is above the panel; the companion occupies 30 dvh, with an 8 rem
minimum. Safe-area inset padding belongs to the outer grid, so neither region covers the
other. The canvas fills its own grid area, and Unity scales its UI to that canvas size.
`SafeArea.jslib` subtracts the canvas's offset from the window's hardware margins, so
the game's existing `SafeAreaFit` does not pad an already inset canvas a second time.
Its full-window behavior is unchanged.
The original game keyboard controls remain the play interface. VoiceOver's own reading
cursor must be used to browse the companion; the panel adds no Tab stops or focus calls.

## Validation and source pin

The [B audit receipt](1119-b-audit.json) pins runtime source
`4d681e5eaf6f648bab85fb37b9f1fe8b7ffca609`, built with Unity 6000.3.25f1 and SDK
10.0.401 on macOS 27.0.1 arm64. It includes the template, all nine script `.jslib`
files (including ShareShot), the collector and the existing presenters it reads.
The Unity sim DLL matches the Release SDK output byte for byte. Normal merge
`f9d8371d693c5996399c13fe5513bd225ec2f472` integrates main `a250e295` and is the
baseline player. Later commits through `6fe8dea9` change only the browser checkers and
the existing template-layout test contract; runtime source is unchanged.
Both players have a complete 113-file SHA-256 receipt; their receipt hashes and
compressed runtime file hashes are in the audit. The original
[`1119-webgl-accessibility.md`](1119-webgl-accessibility.md) retains its historical
`3afb171` / `507348f` observations; they do not describe this new player.

| Gzip file | Baseline bytes | B bytes | Change |
| --- | ---: | ---: | ---: |
| data | 25,696,308 | 25,696,349 | +41 |
| wasm | 14,343,950 | 14,343,739 | -211 |
| framework.js | 97,719 | 97,858 | +139 |

The three cold-load CPU-4 medians are 4,282 → 4,610 ms for map build and
15,454 → 17,703 ms for the first frame. Both pass the existing checker's default
allowed growth, max(300 ms, 50%). No running Unity shared either measurement.

Automated checkpoint, 23 October: the complete managed check at `6fe8dea9` passed
323 policy, 2,443 sim and 1,519 tools tests (five tools skips, two passing subtests).
The optional standalone Unity compile explicitly skipped in that clean validation
checkout without a Library. The actual WebGL build succeeded; all nine exact
manifest goldens, smoke5 and orders passed in both languages without re-recording.

The first unfiltered Native run completed 2,356 cases: 2,351 passed, one failed,
four skipped. All four new payload methods passed. The failure was the existing
`ProjectSettingsTests.TheTemplateFillsTheWindowAndPersistsSaves`, which still
required the old full-window fixed canvas. Its contract now checks the approved
separate game region and retains the save/no-alert assertions. A full unfiltered
rerun is pending the coordinated machine window. The skips were two Explicit
capture/timing jobs and two optional-art comparisons without local prefabs; none
is waived or relabelled. The first failed XML/log remain preserved.

The ordinary start-screen check also exposed the old fixed End Turn coordinate
after the narrower canvas re-tiers the HUD. The checker now follows B's mirrored
focus using ordinary Tab/Enter; older players retain their coordinate path. Its
unit tests pass, including a guard against drift from the shipped EN/HE labels;
the actual start-screen rerun and matched phone baseline captures remain pending.
These pending reruns are not recorded as passes.

The payload regression tests cover EN/HE card text, choice order and disabled state,
literal hostile markup and direction marks, focus labels, a refusal without `Changed`
including an appended notice, final End Turn output, coverage and empty/cancelled disposal.
The product renderer runs in a real headless Chrome DOM test: hostile markup, atomic
rejection, language/direction, status mutation boundaries, event-subtree retention,
unchanged `document.activeElement` and no focusable/action elements.

The headed check uses the existing checker server and fresh profiles. It starts a new
game through `firstcard`, answers the actual event, changes a budget line and submits it
with too little Attention (a real refusal), then ends the turn, in EN and HE. It captures
both 1920×1080 and 390×844 layouts. It injects no sim state or game commands. This is
ordinary input/DOM evidence, not speech evidence. The existing browser checker maps its
game-relative clicks into the current canvas rectangle, retaining the old full-window
coordinates while supporting the companion column/row and safe-area offset.

```sh
uv run tools/companion_check.py --build <gzip-player> --out <evidence-directory>
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/headed.py boot --build <gzip-player> --runs 3 --cpu 4
```

The contributor will run the [real diagnostic checklist](1119-webgl-accessibility.md#real-diagnostic-still-required)
manually at the Mac in fresh EN and HE windows. Enable VoiceOver and its caption panel,
retain a caption-panel screenshot and notes for each step, and record silence or an
unreachable control as a failure. No VoiceOver automation or human acceptance is claimed.
C requires a separate decision after that real test of B. Keep #1230 draft meanwhile.
