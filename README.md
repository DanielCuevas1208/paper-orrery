# Paper Orrery

Paper Orrery calculates approximate planet positions and draws a local sky chart from bundled astronomy data.

## Value

Use it for a repeatable, offline view of the sky above one observing site.
One request produces coordinates, horizon positions, and an SVG chart.

## Scope

Current scope includes:

- Planet and Sun positions from orbital elements valid for 1800 to 2050.
- Geocentric right ascension, declination, and distance in astronomical units.
- Altitude and azimuth for one observer and east-positive longitude.
- An SVG chart for visible planets, the Sun, and bright stars.
- Command-line output for charts and position tables.

## Setup

Install the .NET 8 SDK.
Run these commands from the repository root.

```bash
dotnet restore PaperOrrery.sln --locked-mode
dotnet build PaperOrrery.sln -c Release --no-restore
dotnet test PaperOrrery.sln -c Release --no-build --no-restore
```

The repository commits one lockfile for each .NET project.
No external services are required.

## Use

Render an SVG chart.

```bash
dotnet run --project src/PaperOrrery.Cli -- chart --date 2025-06-01T00:00:00 --lat 51.4769 --lon 0 --out sky-chart.svg
```

Print a position table.

```bash
dotnet run --project src/PaperOrrery.Cli -- positions --date 2025-06-01T00:00:00 --lat 51.4769 --lon 0
```

Use ISO-8601 input with a UTC offset or a UTC designator.
Latitude uses north-positive degrees.
Longitude uses east-positive degrees.

## Demo

Run the Greenwich example.

```bash
dotnet run --project src/PaperOrrery.Cli -- chart --date 2025-06-01T00:00:00 --lat 51.4769 --lon 0 --out sky-chart.svg
```

Open the generated SVG in a browser.
The committed [Greenwich sample chart](samples/greenwich-2025-06-01.svg) uses the same input.
See the [sample notes](samples/README.md) for the demo scope.

## Sample output

The following table comes from the Greenwich example.

```text
Body      RA(deg)   Dec(deg) Alt(deg)   Az(deg)  Dist(AU)
----------------------------------------------------------
Mercury     71.499     23.221    -15.3     358.3     1.318
Venus       23.895      7.652    -19.0      48.8     0.699
Mars       143.935     15.850      2.9     292.1     1.695
Jupiter     87.808     23.241    -13.6     342.9     6.091
Saturn       1.354     -1.711    -14.6      73.8     9.876
Uranus      55.919     19.533    -18.0      13.7    20.508
Neptune      2.221     -0.452    -14.1      72.3    30.236
Pluto      306.866     -22.869      0.4     129.3    34.657
Sun          69.187      22.058    -16.5       0.5     2.028
```

## Architecture

Three projects separate computation, command-line behavior, and verification.

- `src/PaperOrrery` contains the library.
- `src/PaperOrrery.Cli` contains the command-line tool.
- `tests/PaperOrrery.Tests` contains deterministic xUnit tests.

The library uses these modules:

- `Time` converts UTC instants to Julian dates.
- `Kepler` solves Kepler's equation.
- `Elements` loads the bundled catalogs.
- `Ephemeris` computes geocentric equatorial positions.
- `Horizon` converts equatorial positions to altitude and azimuth.
- `Observation` assembles one observer result.
- `SkyChart` renders visible bodies as SVG.

`Observation.calculate` is the orchestration boundary for the CLI and tests.
The CLI supplies the same observation to the position table and chart paths.

The computation pipeline follows these steps:

1. Evaluate orbital elements for the requested time.
2. Solve Kepler's equation for each body.
3. Build heliocentric positions in the J2000 ecliptic frame.
4. Subtract Earth and apply an iterative light-time correction.
5. Precess coordinates to the mean equinox of date.
6. Convert coordinates to the local horizon frame.
7. Render bodies above the horizon.

The library embeds `data/planet-elements.csv` and `data/bright-stars.csv`.

## Accuracy

Tests compare selected positions with published JPL Horizons values.
The test suite stores reference values for 2025-06-01 00:00 UTC.
The reference tolerances range from 0.03 to 0.20 degrees.

## Limitations

- Positions are geometric.
- Nutation, aberration, and refraction are not applied.
- Orbital elements are valid for 1800 to 2050.
- Delta-T uses a polynomial fit for 2005 to 2050.
- Topocentric parallax is ignored.
- Pluto uses approximate orbital elements.
- The chart excludes bodies below zero degrees altitude.

## Roadmap

See [ROADMAP.md](ROADMAP.md) for possible future work and explicit boundaries.

## Test status

| Check | Status |
| --- | --- |
| `dotnet build PaperOrrery.sln -c Release` | Passed locally |
| `dotnet test PaperOrrery.sln -c Release` | Passed locally |
| Greenwich SVG generation | Passed locally |
| GitHub Actions | Configured in `.github/workflows/ci.yml` |

Run the commands in [Setup](#setup) to repeat the local checks.

## License

MIT. See [LICENSE](LICENSE).
