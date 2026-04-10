#!/usr/bin/env python3
"""
Transforms Kaggle FIFA World Cup CSVs into the JSON seed files used by
World Cup Formations.

Required files in scripts/kaggle_data/:
  WorldCups.csv, WorldCupMatches.csv, WorldCupPlayers.csv

Download (after placing kaggle.json at ~/.config/kaggle/kaggle.json):
  kaggle datasets download abecklas/fifa-world-cup \\
      -p scripts/kaggle_data --unzip

Or download the ZIP manually from the Kaggle page and unzip into
scripts/kaggle_data/.

Then run:
  python3 scripts/import_kaggle.py
"""

import csv
import json
import re
import sys
from collections import defaultdict
from datetime import datetime
from pathlib import Path

ROOT       = Path(__file__).parent.parent
KAGGLE_DIR = Path(__file__).parent / "kaggle_data"
OUTPUT_DIR = ROOT / "src/WorldCupFormations.Web/wwwroot/data"

# ── Formation overrides ────────────────────────────────────────────────────────
# Kaggle stores GK/DF/MF/FW only; simple counts are historically wrong for
# pre-1966 tournaments. Uses Kaggle's own team initials as keys.
FORMATION_OVERRIDES: dict[tuple[int, str], str] = {
    # 1930 – classic 2-3-5 pyramid universally
    **{(1930, t): "2-3-5" for t in [
        "URU","ARG","USA","YUG","CHI","FRA","MEX","BRA","BOL","PER","PAR","ROU","BEL"]},
    # 1934
    **{(1934, t): "2-3-5" for t in [
        "ITA","TCH","GER","SPA","HUN","SWE","ARG","USA","EGY","BEL","NED","FRA","ROU","BRA","SUI"]},
    (1934, "AUT"): "2-3-2-3",      # Austrian Wunderteam
    # 1938
    **{(1938, t): "2-3-5" for t in [
        "HUN","SWE","FRA","CUB","ROU","SUI","POL","NOR","GER","BEL","NED","TCH"]},
    (1938, "ITA"): "2-3-2-3",
    (1938, "BRA"): "3-2-2-3",
    (1938, "INH"): "2-3-5",        # Dutch East Indies
    # 1950
    (1950, "BRA"): "4-2-4",
    (1950, "URU"): "4-4-2",
    (1950, "SWE"): "3-4-3",
    (1950, "SPA"): "3-3-4",
    (1950, "ENG"): "3-2-5",
    (1950, "USA"): "3-3-4",
    (1950, "ITA"): "3-2-2-3",
    (1950, "SUI"): "3-4-3",
    (1950, "YUG"): "3-2-2-3",
    **{(1950, t): "2-3-5" for t in ["CHI","BOL","PAR","MEX"]},
    # 1954
    (1954, "HUN"): "4-2-4",
    (1954, "FRG"): "3-4-3",        # West Germany (Kaggle: FRG)
    (1954, "AUT"): "3-4-3",
    (1954, "BRA"): "4-2-4",
    (1954, "ENG"): "3-2-5",
    (1954, "URU"): "3-3-4",
    (1954, "YUG"): "3-3-4",
    (1954, "ITA"): "3-2-2-3",
    (1954, "TCH"): "3-2-2-3",      # Czechoslovakia (Kaggle: TCH)
    **{(1954, t): "3-4-3" for t in ["SUI","SWE"]},
    **{(1954, t): "3-2-5" for t in ["KOR","MEX","TUR","SCO"]},
    # 1958
    (1958, "BRA"): "4-2-4",
    (1958, "SWE"): "3-2-5",
    (1958, "FRG"): "4-2-4",
    (1958, "HUN"): "4-2-4",
    (1958, "URS"): "3-4-3",        # Soviet Union (Kaggle: URS)
    (1958, "YUG"): "3-3-4",
    (1958, "AUT"): "3-4-3",
    **{(1958, t): "3-2-5" for t in ["FRA","PAR","SCO","ENG","NOR","MEX","ARG","WAL"]},
    # 1962
    (1962, "BRA"): "4-3-3",
    (1962, "TCH"): "4-2-4",
    **{(1962, t): "4-2-4" for t in ["CHI","YUG"]},
    # 1966 onwards: count-derived formations are accurate enough
}

# ── Team name normalisation ────────────────────────────────────────────────────
# Kaggle has HTML artefacts (rn">) and encoding issues in a few names.
def normalise_team_name(raw: str) -> str:
    # Strip leading HTML artefact: "rn">Name" → "Name"
    raw = re.sub(r'^rn">', '', raw.strip())
    # Known substitutions
    subs = {
        "Germany FR":        "West Germany",
        "C\xf4te d'Ivoire": "Ivory Coast",
        "C\ufffd\ufffd\ufffd te d'Ivoire": "Ivory Coast",
    }
    # Regex for broken Côte encoding variants
    if re.search(r"C.{1,4}te d.Ivoire", raw):
        return "Ivory Coast"
    return subs.get(raw, raw)

# ── Stage name normalisation ───────────────────────────────────────────────────
def normalise_stage(raw: str) -> str:
    raw = raw.strip()
    if raw.startswith("Group ") or raw.startswith("Pool "):
        return "Group Stage"
    return {
        "Preliminary round":    "Preliminary Round",
        "First round":          "Group Stage",
        "Final round":          "Final Round",
        "Second round":         "Second Round",
        "Second Group Stage":   "Second Round",
        "Quarter-finals":       "Quarter-final",
        "Semi-finals":          "Semi-final",
        "Third place play-off": "Third Place",
        "Third Place":          "Third Place",
        "Group Stage":          "Group Stage",
        "Round of 16":          "Round of 16",
        "Quarter-final":        "Quarter-final",
        "Semi-final":           "Semi-final",
        "Final":                "Final",
    }.get(raw, raw)

# ── Date parsing ───────────────────────────────────────────────────────────────
def parse_date(raw: str) -> str:
    raw = raw.strip()
    # Kaggle uses both abbreviated ("13 Jul 1930 - 15:00") and
    # full ("17 June 1970 - 16:00") month names
    for fmt in ("%d %b %Y - %H:%M", "%d %B %Y - %H:%M",
                "%d %b %Y",         "%d %B %Y",
                "%b %d, %Y",        "%B %d, %Y"):
        try:
            return datetime.strptime(raw, fmt).strftime("%Y-%m-%d")
        except ValueError:
            pass
    if len(raw) >= 10 and raw[4] == "-":
        return raw[:10]
    return raw

# ── Formation derivation from position counts ──────────────────────────────────
def derive_formation(pos_counts: dict[str, int]) -> str | None:
    df = pos_counts.get("DF", 0)
    mf = pos_counts.get("MF", 0)
    fw = pos_counts.get("FW", 0)
    if pos_counts.get("GK", 0) == 0 or df + mf + fw == 0:
        return None
    return "-".join(str(n) for n in [df, mf, fw] if n > 0)

# ── CSV helpers ────────────────────────────────────────────────────────────────
def read_csv(name: str) -> list[dict]:
    path = KAGGLE_DIR / name
    if not path.exists():
        sys.exit(f"ERROR: {path} not found.\n"
                 "Run: kaggle datasets download abecklas/fifa-world-cup "
                 "-p scripts/kaggle_data --unzip")
    with open(path, encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))

# ── Main ───────────────────────────────────────────────────────────────────────
def main():
    print("Reading Kaggle CSVs…")
    match_rows  = read_csv("WorldCupMatches.csv")
    player_rows = read_csv("WorldCupPlayers.csv")

    # Keep existing world_cups.json (already has 1930-2022 including Qatar)
    existing_cups = json.loads((OUTPUT_DIR / "world_cups.json").read_text())
    year_to_wcid  = {wc["year"]: wc["id"] for wc in existing_cups}
    print(f"  {len(existing_cups)} world cups (existing file kept as-is)")

    # ── Teams ──────────────────────────────────────────────────────────────────
    team_map: dict[str, str] = {}   # initials → clean name
    for row in match_rows:
        for name_col, init_col in [("Home Team Name", "Home Team Initials"),
                                    ("Away Team Name", "Away Team Initials")]:
            name  = normalise_team_name(row[name_col])
            inits = row[init_col].strip()
            if inits and name:
                team_map[inits] = name

    sorted_teams = sorted(team_map.items())
    init_to_tid  = {inits: i + 1 for i, (inits, _) in enumerate(sorted_teams)}
    teams_json   = [{"id": init_to_tid[inits], "name": name, "code": inits}
                    for inits, name in sorted_teams]
    print(f"  {len(teams_json)} teams")

    # ── Matches ────────────────────────────────────────────────────────────────
    seen_mids: set[str] = set()
    matches_raw: list[dict] = []

    for row in match_rows:
        mid = row.get("MatchID", "").strip()
        if not mid or mid in seen_mids:
            continue
        seen_mids.add(mid)

        try:
            year = int(row["Year"])
        except (ValueError, KeyError):
            continue
        if year not in year_to_wcid:
            continue

        home_i = row["Home Team Initials"].strip()
        away_i = row["Away Team Initials"].strip()
        if home_i not in init_to_tid or away_i not in init_to_tid:
            continue

        try:
            hs = int(row["Home Team Goals"])
            as_ = int(row["Away Team Goals"])
        except (ValueError, KeyError):
            hs = as_ = 0

        matches_raw.append({
            "_mid":   mid,
            "_year":  year,
            "_home":  home_i,
            "_away":  away_i,
            "worldCupId": year_to_wcid[year],
            "homeTeamId": init_to_tid[home_i],
            "awayTeamId": init_to_tid[away_i],
            "homeScore":  hs,
            "awayScore":  as_,
            "stage":      normalise_stage(row.get("Stage", "").strip()),
            "date":       parse_date(row.get("Datetime", "").strip()),
        })

    matches_raw.sort(key=lambda r: (r["_year"], r["date"]))
    kaggle_to_id: dict[str, int] = {m["_mid"]: i + 1 for i, m in enumerate(matches_raw)}

    matches_json = [
        {
            "id":         kaggle_to_id[m["_mid"]],
            "worldCupId": m["worldCupId"],
            "homeTeamId": m["homeTeamId"],
            "awayTeamId": m["awayTeamId"],
            "homeScore":  m["homeScore"],
            "awayScore":  m["awayScore"],
            "stage":      m["stage"],
            "date":       m["date"],
        }
        for m in matches_raw
    ]
    mid_meta = {m["_mid"]: m for m in matches_raw}
    print(f"  {len(matches_json)} matches")

    # ── Players (deduplicated by name + team initials) ─────────────────────────
    player_key_to_id:    dict[tuple[str, str], int] = {}
    player_key_to_shirt: dict[tuple[str, str], str | None] = {}
    pid = 1

    for row in player_rows:
        mid   = row.get("MatchID", "").strip()
        inits = row.get("Team Initials", "").strip()
        name  = row.get("Player Name", "").strip()
        shirt = row.get("Shirt Number", "").strip() or None
        if not (mid and inits and name):
            continue
        key = (name, inits)
        if key not in player_key_to_id:
            player_key_to_id[key]    = pid
            player_key_to_shirt[key] = shirt
            pid += 1

    players_json = []
    for (name, inits), pid_ in sorted(player_key_to_id.items(), key=lambda x: x[1]):
        if inits not in init_to_tid:
            continue
        players_json.append({
            "id":          pid_,
            "teamId":      init_to_tid[inits],
            "name":        name,
            "shirtNumber": player_key_to_shirt[(name, inits)],
        })
    print(f"  {len(players_json)} players")

    # ── Lineups ────────────────────────────────────────────────────────────────
    # Group player rows by (MatchID, TeamInitials)
    groups: dict[tuple[str, str], list[dict]] = defaultdict(list)
    for row in player_rows:
        mid   = row.get("MatchID", "").strip()
        inits = row.get("Team Initials", "").strip()
        if mid and inits:
            groups[(mid, inits)].append(row)

    lineups_json: list[dict] = []
    lid = 1

    for (mid, inits), rows in sorted(groups.items()):
        if mid not in mid_meta or inits not in init_to_tid:
            continue

        meta    = mid_meta[mid]
        our_mid = kaggle_to_id[mid]
        year    = meta["_year"]
        team_id = init_to_tid[inits]

        starters = [r for r in rows if r.get("Line-up", "").strip() == "S"]
        subs     = [r for r in rows if r.get("Line-up", "").strip() == "N"]

        # Formation: override first, then derive from position counts
        formation = FORMATION_OVERRIDES.get((year, inits))
        if formation is None:
            counts: dict[str, int] = defaultdict(int)
            for r in starters:
                pos = r.get("Position", "").strip()
                if pos:
                    counts[pos] += 1
            formation = derive_formation(counts) or "4-4-2"

        for is_starting, subset in [(True, starters), (False, subs)]:
            for row in subset:
                name  = row.get("Player Name", "").strip()
                pos   = row.get("Position", "").strip() or "DF"
                key   = (name, inits)
                if not name or key not in player_key_to_id:
                    continue
                lineups_json.append({
                    "id":           lid,
                    "matchId":      our_mid,
                    "teamId":       team_id,
                    "playerId":     player_key_to_id[key],
                    "positionRole": pos,
                    "formation":    formation,
                    "isStarting":   is_starting,
                })
                lid += 1

    print(f"  {len(lineups_json)} lineup entries")

    # ── Write output ───────────────────────────────────────────────────────────
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    def write_json(fname: str, data: list):
        (OUTPUT_DIR / fname).write_text(json.dumps(data, ensure_ascii=False, indent=2))
        print(f"  Wrote {OUTPUT_DIR / fname}  ({len(data)} records)")

    print("\nWriting JSON files…")
    write_json("teams.json",   teams_json)
    write_json("matches.json", matches_json)
    write_json("players.json", players_json)
    write_json("lineups.json", lineups_json)

    # ── Famous Formations lookup ───────────────────────────────────────────────
    print("\n── Famous Formations ID lookup ─────────────────────────────────────")
    famous = [
        (1930, "Final", "URU", "Uruguay 1930"),
        (1958, "Final", "BRA", "Brazil 1958"),
        (1970, "Final", "BRA", "Brazil 1970"),
        (1974, "Final", "NED", "Netherlands 1974"),
        (1986, "Final", "ARG", "Argentina 1986"),
        (1998, "Final", "FRA", "France 1998"),
        (2010, "Final", "ESP", "Spain 2010"),
        (2014, "Final", "GER", "Germany 2014"),
    ]
    for year, stage, inits, label in famous:
        for m in matches_raw:
            if m["_year"] == year and m["stage"] == stage and inits in (m["_home"], m["_away"]):
                mid  = kaggle_to_id[m["_mid"]]
                tid  = init_to_tid.get(inits, "?")
                print(f"  {label}: /formation/{mid}/{tid}")
                break

    print("\nDone. Reset the database and reseed:")
    print("  rm src/WorldCupFormations.Web/App_Data/worldcup.db")
    print("  dotnet run --project src/WorldCupFormations.Web")


if __name__ == "__main__":
    main()
