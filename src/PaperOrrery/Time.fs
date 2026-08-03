namespace PaperOrrery

open System

/// Calendar and time-scale helpers.
module Time =

    let private j2000Jd = 2451545.0
    let private jdPerCentury = 36525.0

    /// Converts a civil (Gregorian) calendar instant to a Julian day number.
    /// Algorithm 7 from "Astronomical Algorithms", 2nd ed., J. Meeus.
    let julianDay (year: int) (month: int) (day: int) (hourOfDay: float) : float =
        let y = if month <= 2 then year - 1 else year
        let m = if month <= 2 then month + 12 else month
        let a = float (y / 100)
        let b = 2.0 - a + float (y / 400)
        let dayFraction = float day + hourOfDay / 24.0
        float (int (365.25 * float (y + 4716)))
        + float (int (30.6001 * float (m + 1)))
        + dayFraction
        + b
        - 1524.5

    /// Julian day for a UTC instant.
    let julianDayOfUtc (instant: DateTime) : float =
        if instant.Kind = DateTimeKind.Unspecified then
            invalidArg "instant" "Expected a UTC DateTime."
        julianDay
            instant.Year
            instant.Month
            instant.Day
            (float instant.Hour + float instant.Minute / 60.0 + float instant.Second / 3600.0)

    /// Delta-T (TT - UT) in seconds, polynomial fit valid 2005-2050.
    /// Source: Espenak and Meeus, "Five Millennium Canon of Solar Eclipses".
    let deltaTSeconds (year: float) : float =
        62.92 + 0.32217 * (year - 2000.0) + 0.005589 * (year - 2000.0) ** 2.0

    /// Julian centuries of Terrestrial Time since J2000.0.
    let julianCenturies (jdTt: float) : float =
        (jdTt - j2000Jd) / jdPerCentury

    /// Terrestrial Time Julian day for a UTC instant.
    let jdTtOfUtc (instant: DateTime) : float =
        let jd = julianDayOfUtc instant
        let year = float instant.Year + (float instant.Month - 0.5) / 12.0
        jd + deltaTSeconds year / 86400.0
