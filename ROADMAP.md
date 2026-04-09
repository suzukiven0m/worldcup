# Roadmap

A prioritised list of features to add to the World Cup Formations app.

---

## Phase 1 — Data & Coverage

These are purely data work, no code changes required.

- **Full 2022 Qatar dataset** — all 64 matches, all 32 squads (see DATA-POPULATION-C.md)
- **Full 1930–2018 dataset** — all group stage and knockout matches via Kaggle import (see DATA-POPULATION-A.md and TECHNICAL.md Option B)
- **Substitution timeline** — record the minute each sub came on/off; display in the substitutes panel
- **Scorers** — record goalscorers and minutes; show below the match header

---

## Phase 2 — Formation Page Improvements

- **Highlight selected team** — when the user clicked a specific team to reach the formation page, dim the opposing team slightly so it is clear which side is "in focus"
- **Toggle between teams** — a button on the formation page to flip the view and show the other team's perspective without going back to the match list
- **Player detail on click** — clicking a player circle shows a small popover with their full name, shirt number, club at the time, and caps
- **Animated formation reveal** — players fade in one line at a time (GK → defenders → midfield → attack) when the page loads
- **Formation comparison mode** — split the pitch vertically to show two different formations side by side for tactical comparison

---

## Phase 3 — Navigation & Discovery

- **Search** — global search bar to find a player name across all tournaments and jump straight to their match
- **Player profile page** — `/player/{id}` showing all World Cup appearances for a player across tournaments, with links to each formation view
- **Team history page** — `/team/{id}` showing all World Cups a team participated in, their results, and formations used over the decades
- **Stage filter on matches page** — filter the match list by stage (Group Stage, Quarter-final, Final, etc.)
- **"Famous formations" shortcut page** — curated list of historically significant lineups (Brazil 1970, Hungary 1954, Netherlands 1974 Total Football) as direct deep links

---

## Phase 4 — Visualisation Enhancements

- **Heatmap overlay** — toggle an approximate positional heatmap per player based on their role, showing typical zones of influence
- **Formation shape outline** — draw a subtle polygon connecting the players in each line to make the shape of the formation more legible at a glance
- **Jersey kit colours** — replace the solid blue/red circles with team-specific primary kit colours per tournament year
- **Player photos** — small circular photos inside the player markers (requires an image source, e.g. Wikipedia Commons)
- **Mobile bottom sheet** — on small screens, replace the substitutes list with a swipeable bottom sheet so the pitch takes up the full viewport

---

## Phase 5 — Stats & Analysis

- **Formation frequency chart** — bar chart per team showing which formations they used across all their World Cup matches
- **Head-to-head record** — when viewing a match, show the all-time World Cup record between the two teams
- **Top scorers per tournament** — sidebar or page listing the Golden Boot winner and goals per team
- **Formation timeline** — a visual timeline for a team showing how their formation evolved across World Cups (e.g. Brazil: 2-3-5 → 4-2-4 → 4-3-3)
- **Most-capped XI** — for a given team, calculate and display the starting XI with the most combined caps at the time of the tournament

---

## Phase 6 — Social & Sharing

- **Shareable formation URL** — the current `/formation/{matchId}/{teamId}` URL is already shareable; add Open Graph meta tags so it previews nicely when shared on social media
- **Export as image** — a button to download the current pitch SVG as a PNG (using the browser's SVG-to-canvas API via JS interop)
- **"Build your own" mode** — drag-and-drop interface to compose a custom XI from any players in the database and save it with a shareable link

---

## Phase 7 — Infrastructure

- **Pagination on match list** — once full data is loaded, the match list for Group Stage-heavy views will be long; add pagination or infinite scroll
- **EF Core query caching** — cache `GetMatchesForYearAsync` results in memory for the lifetime of the process since the data is read-only at runtime
- **Dark/light theme toggle** — persist preference in `localStorage` via JS interop
- **PWA support** — add a web manifest and service worker so the app can be installed on mobile as a home screen app and browse cached data offline
- **Docker image** — `Dockerfile` + `docker-compose.yml` so the app can be deployed anywhere with one command
