namespace PaperOrrery

open System

/// Converts equatorial coordinates to the local horizontal frame.
module Horizon =

    /// Greenwich mean sidereal time, in degrees, for a UT Julian day.
    /// Method from "Astronomical Algorithms", 2nd ed., J. Meeus, chapter 12.
    let greenwichMeanSiderealDeg (jdUt: float) : float =
        let t = (jdUt - 2451545.0) / 36525.0

        let value =
            280.46061837
            + 360.98564736629 * (jdUt - 2451545.0)
            + 0.000387933 * t * t
            - t * t * t / 38710000.0

        let wrapped = value % 360.0
        if wrapped < 0.0 then wrapped + 360.0 else wrapped

    /// Local mean sidereal time, in degrees, for an east-longitude site.
    let localSiderealDeg (jdUt: float) (eastLongitudeDeg: float) : float =
        let value = greenwichMeanSiderealDeg jdUt + eastLongitudeDeg
        let wrapped = value % 360.0
        if wrapped < 0.0 then wrapped + 360.0 else wrapped

    /// A position in the local horizontal frame.
    type HorizontalPosition =
        { AltitudeDeg: float
          AzimuthDeg: float }

    let private deg = Math.PI / 180.0

    /// Converts equatorial-of-date coordinates to altitude and azimuth.
    /// Azimuth is measured clockwise from North (0 = North, 90 = East).
    let toHorizontal
        (latitudeDeg: float)
        (localSiderealDeg: float)
        (rightAscensionDeg: float)
        (declinationDeg: float)
        : HorizontalPosition =

        let latitude = latitudeDeg * deg
        let declination = declinationDeg * deg
        let hourAngle = (localSiderealDeg - rightAscensionDeg) * deg

        let altitude =
            Math.Asin(
                Math.Sin latitude * Math.Sin declination
                + Math.Cos latitude * Math.Cos declination * Math.Cos hourAngle)

        let azimuth =
            Math.Atan2(
                -Math.Cos declination * Math.Sin hourAngle,
                Math.Cos latitude * Math.Sin declination
                - Math.Sin latitude * Math.Cos declination * Math.Cos hourAngle)

        let azimuthFromNorth = (azimuth / deg + 360.0) % 360.0

        { AltitudeDeg = altitude / deg
          AzimuthDeg = azimuthFromNorth }
