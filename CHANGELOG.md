## 0.8.9.0 - Preview: 0.9 Overlay Features and Vessel Probe

- Added selectable overlay border colors.
- Added a per-timer option to undock a timer into its own movable overlay; dragging it onto another overlay docks it back into the main list.
- Added `/etimers vesselprobe on|off|now` as an opt-in, read-only probe. It records available ContentsTimer agent state and workshop voyage data after login or on request in the Dalamud log.
- Added a Dalamud notification and chat message once per newly available plugin version after login, with periodic checks while logged in. `/etimers checkupdates` reports availability on demand. The installed-version changelog remains separate.
- Clarified that the project is open source under the MIT License and uses the Anomaly author name in its copyright notice and project metadata.
- The probe does not open or click game UI, send requests, or create voyage timers. The remote Estate data source and exact headless refresh path still require a Windows client trace.
- Kept the existing Stage 8 linked timer catalog and timer configuration.
- Reserve 0.9.0.0 for the confirmed release after testing.

## 0.8.0.0 - Stage 8: Linked Timer Catalog

- Expanded the Housing Lottery integration into a categorized catalog of 37 selectable linked timers.
- Replaced the one-click Housing Timer button with a Linked Timer catalog.
- Added source descriptions, availability states, duplicate prevention, and a manual availability refresh inside the catalog.
- Added general Daily Reset plus named timers for Duty Roulettes, Allied Society Quests, Daily Hunt Bills, Frontline Daily Challenge, and Mini Cactpot.
- Added Grand Company Reset plus named timers for Supply & Provisioning, Squadron Training Allowance, and Collectable Deliveries.
- Added general Weekly Reset plus named timers for the weekly Tomestone Cap and Raid Loot Lockouts.
- Added Challenge Log, Custom Deliveries, and Wondrous Tails timers backed by loaded game data.
- Added Leve Allowance and Treasure Map Allowance timers backed by loaded game data.
- Added active Squadron Mission and Squadron Training completion timers.
- Added the earliest active Retainer Venture and loaded Retainer Market Expiration timers.
- Added Ocean Fishing departures on the reliable two-hour UTC schedule.
- Added Gold Saucer GATEs at :00, :20, and :40.
- Added separate weekly Jumbo Cactpot drawing timers for Japan, North America, Europe, and Oceania.
- Added Fashion Report weekly theme and judging-period transition timers.
- Added Island Sanctuary daily cycle and weekly season timers.
- Added the next active Island Granary expedition completion when Island Sanctuary data is loaded.
- Added the next Free Company airship and submersible return when workshop data is loaded.
- Added the Cosmic Exploration daily reset.
- Fixed schedules calculate locally and advance automatically without a Sync command.
- Character-specific sources use game-client data and display Unavailable when the data or activity does not exist.
- Retainer availability explains that opening a summoning bell may be required to load fresh retainer data.
- Kept gathering nodes and rare-fish windows out of the plugin.
- Removed linked phase text from the main countdown string so timer rows stay compact.
- Added generated linked details as optional smaller notes under the timer title.
- The existing Show notes in overlay option and right-click action now control both generated linked details and custom notes.
- Existing manual timers and the 0.7.0.0 Housing Lottery timer migrate without losing their settings.
- Added cached linked-source display state so the overlay does not read game memory every frame.
- Fixed the nullable-reference warnings in `CompletionAlertWindow.cs` by capturing the timer ID before mutating the active alert.
