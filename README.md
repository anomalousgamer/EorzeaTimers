# Eorzea Timers

A customizable timer plugin for FFXIV and Dalamud.

## Version 0.8.0.0

Stage 8 expands the original Housing Lottery integration into a categorized catalog of 37 selectable linked timers. Add only the timers you want; every linked timer keeps its automatic target while allowing custom names, notes, icons, colors, alerts, sounds, volume, and overlay visibility.

### Linked timer catalog

**World**

- Housing Lottery entry/results cycle

**Daily resets**

- Daily Reset
- Duty Roulettes
- Allied Society Quests
- Daily Hunt Bills
- Frontline Daily Challenge
- Mini Cactpot

**Grand Company**

- Grand Company Reset
- Supply & Provisioning
- Squadron Training Allowance
- Collectable Deliveries

**Weekly resets**

- Weekly Reset
- Tomestone Cap
- Raid Loot Lockouts
- Challenge Log
- Custom Deliveries
- Wondrous Tails journal expiration

**Allowances and character activity**

- Leve Allowance
- Treasure Map Allowance
- Squadron Mission return
- Squadron Training completion
- Next Retainer Venture
- Retainer Market Expiration
- Island Granary expedition completion
- Free Company Airship voyage return
- Free Company Submersible voyage return

**Scheduled content**

- Ocean Fishing departures
- Gold Saucer GATEs
- Jumbo Cactpot - Japan
- Jumbo Cactpot - North America
- Jumbo Cactpot - Europe
- Jumbo Cactpot - Oceania
- Fashion Report theme change
- Fashion Report judging transition
- Island Sanctuary cycle
- Island Sanctuary season
- Cosmic Exploration daily reset

Fixed schedules are calculated locally from their recurring UTC schedules. Character-specific timers read data already supplied by the game client. If live data is not loaded or an activity is not active, the catalog labels that source unavailable instead of inventing a target. Retainer sources may require opening a summoning bell once; granary sources require Island Sanctuary data; Free Company voyages require visiting the workshop to load their current data.

Gathering nodes and rare-fish windows are intentionally excluded.

### Compact linked notes

Linked phase text is no longer forced into the main countdown row. It is generated as a smaller note beneath the timer and follows the timer's **Show notes in overlay** setting. Right-click an interactive overlay row to show or hide both its generated linked details and its custom note.

## Current features

- Multiple persistent manual and linked countdown timers.
- Add, edit, duplicate, delete, enable, and disable controls.
- A categorized linked-timer catalog with availability states.
- Automatic fixed schedules and supported live character-data targets.
- Clear unavailable states instead of guessed timer values.
- Automatic migration of timers and settings from earlier releases.
- Target date/time or relative duration input for manual timers.
- Custom-interval, daily, weekly, selected-weekday, and monthly repeating manual timers.
- Original-schedule and dismissal-time recurrence behavior.
- Read-only automatic targets for linked timers.
- Conversion from a linked timer to a manual snapshot.
- Timer names, custom notes, icons, colors, and display formats.
- A persistent, titleless in-game timer overlay.
- Per-timer overlay visibility and note visibility.
- Generated linked notes displayed separately from compact countdown text.
- Click a timer to edit it, right-click to toggle notes, or drag a row to move the overlay.
- Adjustable overlay scale, width, opacity, lock, and click-through behavior.
- Off, Persistent, and hold-to-show Key Bound overlay modes.
- Combat, duty, cutscene, hidden-UI, and empty-overlay visibility settings.
- A floating bottom-right width resize grip and width presets.
- A subtle pulsing border around completed overlay timers.
- Per-timer popup, sound, volume, and chat completion alerts.
- The standard notification plus twelve selectable FFXIV chat sounds.
- Sound preview and full completion-alert testing from unsaved editor settings.
- Preset and custom snooze durations.
- Per-timer alert volume from 0% through 200%.
- Queued completion popups and duplicate-alert prevention.
- Context-sensitive Save, Cancel, Revert Changes, Close, and Restart controls.
- A per-version changelog shown three seconds after the character is fully loaded.

Alert volume above 100% is experimental. FFXIV may clamp louder values or introduce distortion depending on the sound and game audio settings.

## Commands

- `/etimers` - Open or close the timer manager.
- `/etimers help` - Show command and overlay guidance.
- `/etimers overlay` - Open overlay settings.
- `/etimers overlay off` - Disable the overlay.
- `/etimers overlay persistent` - Keep the overlay visible.
- `/etimers overlay key` - Show it only while the configured key is held.
- `/etimers clickthrough` - Toggle overlay click-through.
- `/etimers clickthrough on` - Enable overlay click-through.
- `/etimers clickthrough off` - Disable overlay click-through.
- `/etimers changes` - Reopen the current changelog.

## Development build

Build the solution in Visual Studio and add the generated `EorzeaTimers.dll` as a Dalamud dev plugin.