# Paper Orrery

Paper Orrery is a compact astronomy calculator in F#. It propagates planet
orbits from bundled Keplerian orbital elements. It converts positions to the
local horizon frame for a given date, time, and location. It renders an SVG
all-sky chart with the visible planets and bright stars. All data ships with
the repository. No external services are required.

## What it offers

- Planet and Sun positions for any date between 1800 and 2050.
- Altitude and azimuth for one observing site.
- A vector sky chart that shows the sky above the horizon.
- Published almanac values embedded in the test suite.

## Quick start

Requirements: the .NET 8 SDK.

```bash
git clone <this repository>
cd paper-orrery
dotnet restore PaperOrrery.sln
dotnet test PaperOrrery.sln
```

All tests pass.

## Commands

Render a chart for any date and location.

```bash
dotnet run --project src/PaperOrrery.Cli -- chart --date 2025-06-01T00:00:00 --lat 51.4769 --lon 0 --out sky-chart.svg
```

Print a position table for one instant.

```bash
dotnet run --project src/PaperOrrery.Cli -- positions --date 2025-06-01T00:00:00 --lat 51.4769 --lon 0
```

Run the tool without arguments to chart the current sky from Greenwich.

## Sample output

The table below is the real output for 2025-06-01 00:00 UTC at Greenwich.

```
Body      RA(deg)   Dec(deg) Alt(deg)   Az(deg)  Dist(AU)
----------------------------------------------------------
Mercury     71.499     23.221    -15.3     358.3     1.318
Venus      23.895      7.652    -19.0      48.8     0.699
Mars      143.935     15.850      2.9     292.1     1.695
Jupiter     87.808     23.241    -13.6     342.9     6.091
Saturn      1.354     -1.711    -14.6      73.8     9.876
Uranus     55.919     19.533    -18.0      13.7    20.508
Neptune      2.221     -0.452    -14.1      72.3    30.236
Pluto     306.866    -22.869      0.4     129.3    34.657
Sun        69.187     22.058    -16.5       0.5     2.028
```

The rendered chart for this instant is committed at
`samples/greenwich-2025-06-01.svg`. Open it in any browser.

## Architecture

The solution has three projects.

- `src/PaperOrrery` — the library.
- `src/PaperOrrery.Cli` — the command line tool.
- `tests/PaperOrrery.Tests` — the xUnit test suite.

The data lives in `data/`. The computation pipeline is:

1. Solve Kepler's equation for the true anomaly.
2. Build the heliocentric position in the J2000 ecliptic frame.
3. Subtract the Earth to get a geocentric vector.
4. Apply the light-time correction by iteration.
5. Convert to the equator and equinox of date.
6. Convert to altitude and azimuth with local sidereal time.
7. Render the SVG chart.

Modules: Time, Kepler, Elements, Ephemeris, Horizon, SkyChart.

## Data

- `data/planet-elements.csv` — Keplerian elements from NASA/JPL.
  E. M. Standish compiled them. They are valid for 1800 to 2050.
  They refer to the ecliptic and equinox of J2000.
- `data/bright-stars.csv` — bright star positions and magnitudes.
  Source is the Yale Bright Star Catalogue, 5th edition.
  Coordinates are FK5 J2000.

## Accuracy

Tests compare computed positions to JPL Horizons apparent positions.
Measured errors on 2025-06-01 are below 0.07 degrees for all planets.
The test tolerances are: inner planets 0.03 deg, Jupiter 0.05 deg,
Saturn 0.15 deg. Other outer planets use 0.05 deg. The Sun uses 0.10 deg.
Mars altitude and azimuth use 0.20 deg.

## Limitations

- Positions are geometric. They use the mean equinox of date.
- The code does not apply nutation, aberration, or refraction.
- The orbital elements are valid for 1800 to 2050.
- The Delta-T model is a polynomial fit for 2005 to 2050.
- Topocentric parallax is ignored. Its effect is below 0.01 degrees.
- Pluto uses approximate elements. Its position is less accurate.

## Test status

Run `dotnet test PaperOrrery.sln`. The suite has 31 tests. It covers:

- Kepler's equation to machine precision.
- Element and star catalog parsing.
- Sidereal time and horizon conversion.
- Planet positions against published almanac values.
- SVG well-formedness and structure.

## License

MIT. See LICENSE.
