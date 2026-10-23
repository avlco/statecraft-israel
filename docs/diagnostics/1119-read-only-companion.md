# WebGL read-only companion (B)

Refs #1119; draft #1230. The [23 October implementation approval](https://github.com/eyalgolan/statecraft-israel/issues/1119#issuecomment-6360904442)
supersedes the original proposal's generations, sequences and action callbacks for this milestone.
The contributor's real VoiceOver pass is still required. B is not a claim of action parity or a fully accessible game.

## What runs

`unity/Assets/Scripts/UI/AccessibilityCompanion.cs` reads the existing UI tree on the next
scheduled frame after `Changed`, `Refused`, `TurnEnded`, pointer/submit/cancel/key and focus
events. Root geometry changes also request a read. Requests coalesce to at most one
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

Validation receipts, the tested source revision, built-file hashes, size comparison,
matched captures and boot medians are recorded in the PR evidence. The original
[`1119-webgl-accessibility.md`](1119-webgl-accessibility.md) retains its historical
`3afb171` / `507348f` observations; they do not describe this new player.

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
ordinary input/DOM evidence, not speech evidence.

```sh
uv run tools/companion_check.py --build <gzip-player> --out <evidence-directory>
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/headed.py boot --build <gzip-player> --runs 3 --cpu 4
```

The contributor will run the [real diagnostic checklist](1119-webgl-accessibility.md#real-diagnostic-still-required)
manually at the Mac in fresh EN and HE windows. Enable VoiceOver and its caption panel,
retain a caption-panel screenshot and notes for each step, and record silence or an
unreachable control as a failure. No VoiceOver automation or human acceptance is claimed.
C requires a separate decision after that real test of B. Keep #1230 draft meanwhile.
