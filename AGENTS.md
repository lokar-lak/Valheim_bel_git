# AGENTS.md

## What this repo is

A Valheim **Belarusian localization mod** ("Bielarusizatar Valheim"), shipped to Thunderstore/r2modman (`manifest.json`) and Nexus/Vortex. It is a localization workspace, not an app codebase.

- The BepInEx plugin C# source is **not in this repo** — only the prebuilt `plugins/Bielarusizatar_Valheim.dll`. There is nothing to build; `src/bin|obj` in `.gitignore` is legacy.
- Install layout matters: players extract the **contents of `plugins/`** into `<Valheim>/BepInEx/plugins/`.

## What to edit

- `plugins/Assets/Translations/Belarusian/belarusian.json` — the actual in-game text (Jotunn Localization `key → "Беларуская text"`). **Source of truth for players.** Keep valid JSON, UTF-8, literal Cyrillic (no `\u` escapes, no BOM); around 4k keys.
- `localization-resources(be).po` — Crowdin export of the translations; `msgctxt` = game string key. Poedit-produced header. It has ~6k keys and drifts ahead of `belarusian.json` — they are **not** guaranteed in sync. Keys carry `[RU]/[UA]/[PL]` reference comments for ambiguous strings.
- `glossary-4032.tbx` — Crowdin terminology glossary (TBX, en↔be). Consult it to keep terminology consistent across the strings. **Look a term up before coining a new word; the glossary leads and the JSON follows.**

## Terminology patterns (from `glossary-4032.tbx`, 1180 entries)

- `<note>` tag marks the game scope: Valheim (44), Terraria (139), Stardew/STS2/Don't Starve (≈210), **Агульнае** = shared lexicon. Search Valheim → Агульнае → other games.
- Proper names (NPCs, mythic figures) → **Belarusian Cyrillic transliteration**, official orthography: Eikthyr→Эйкцюр, Hugin→Хугін, Haldor→Хальдор, Yggdrasil→Ігдрасіль, Dyrnwyn→Дэрнўін.
- Creatures → **descriptive native/folk words**, not loanwords: Greydwarf→шэры карла, Drake→цмок, Abomination→гідота, Seeker→цікун.
- Productive **stacked compounding**: Sealbreaker→Пячаткалом, Dead Raiser→мерцвякліч, Asksvin→тленнавепр, Bonemaw→косцепашча, Vineberry→лазаягады.
- Nordic-mythology loans stay as loanwords, with Belarusian orthography (``ў`` short-u, apostrophe): Neck→нёк, Draugr→драўгр, gjall→г'яль.
- Locale/dialectal flavor for gear & buildings: Cooking Station→**пожаг**, Frostner→Сцюжнік; common nouns lowercase, group epithets **capitalized** (Белабародыя, Дзіцё Зімы, Смарагдавае Полымя).
- Entries may carry `grammaticalGender` and multiple POS variants — pick the form matching the in-game part of speech (Amber: бурштын *noun masc* vs бурштынавы *adj*).
- Official literary norm only (ў, apostrophe, аканне); the shipped `belarusian.json` matches glossary terms verbatim — keep new strings aligned with both.
- `plugins/Assets/Translations/English/english.json` — intentionally contains **only** `"language_belarusian": "Беларуская*"`. Do not expand it.

## Conventions / gotchas

- Language-name values carry a trailing `*` (e.g. `"Беларуская*"`) — in-game mark for "incomplete"; preserve it.
- Translation pipeline in git history (not in working tree): game-extracted `source/Text/localization-resources.csv` → Crowdin → PO → JSON. `CSVtoJSON.py` (pandas) builds the key→value JSON from the first + last CSV columns with `ensure_ascii=False`; `CSVtoTXT.py` serializes to a Unity `.txt` TextAsset. Recover these from git if you need to regenerate.
- `plugins/Norse.ttf` / `Norsebold.ttf` are fonts modified to add Belarusian glyphs (ў, і, …); binary, don't edit casually.
- Releases: bump `version_number` in `manifest.json` **and** add to `CHANGELOG.md`. `manifest.json` depends on `ValheimModding-Jotunn-2.24.3`.
- Repo language is Belarusian (README, CHANGELOG); commit messages are a Belarusian/Russian/English mix.
- `.megaignore`, `.debris/`, `.directory` are local tooling (Mega sync ignores, KDE metadata, lock file) — leave them alone.