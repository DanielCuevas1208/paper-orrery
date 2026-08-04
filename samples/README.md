# Sample chart

Open `greenwich-2025-06-01.svg` in a browser.

The sample uses 2025-06-01 00:00 UTC.
It uses latitude 51.4769 degrees and east longitude 0 degrees.

Generate the sample with this command from the repository root.

```bash
dotnet run --project src/PaperOrrery.Cli -- chart --date 2025-06-01T00:00:00 --lat 51.4769 --lon 0 --out samples/greenwich-2025-06-01.svg
```

The chart shows bodies at or above zero degrees altitude.
Stars use magnitudes from the bundled bright-star catalog.
