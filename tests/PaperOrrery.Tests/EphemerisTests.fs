namespace PaperOrrery.Tests

open System
open Xunit
open PaperOrrery
open PaperOrrery
open PaperOrrery.Ephemeris
open PaperOrrery.Horizon

module EphemerisTests =

    /// UTC instant of the reference epoch for all almanac fixtures.
    let referenceEpoch = DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc)

    /// Angular separation between two equatorial positions, in degrees.
    let private separationDeg (ra1: float) (dec1: float) (ra2: float) (dec2: float) : float =
        let deg = Math.PI / 180.0
        let d1 = dec1 * deg
        let d2 = dec2 * deg
        let dra = (ra1 - ra2) * deg
        Math.Acos(Math.Clamp(Math.Sin d1 * Math.Sin d2 + Math.Cos d1 * Math.Cos d2 * Math.Cos dra, -1.0, 1.0)) / deg

    let private almanacData : obj [] seq =
        seq {
            yield [| "Mercury"; 71.4908; 23.2219; 0.12 |]
            yield [| "Venus"; 23.8876; 7.6499; 0.12 |]
            yield [| "Mars"; 143.9282; 15.8536; 0.12 |]
            yield [| "Jupiter"; 87.8226; 23.2444; 0.15 |]
            yield [| "Saturn"; 1.2840; -1.7397; 0.15 |]
            yield [| "Uranus"; 55.9057; 19.5324; 0.15 |]
            yield [| "Neptune"; 2.2279; -0.4493; 0.15 |]
        }

    /// The values are airless apparent RA and Dec from the JPL Horizons system,
    /// geocentric, for 2025-06-01 00:00 UT.
    /// Horizons query: EPHEM_TYPE=OBSERVER, CENTER=500@399, QUANTITIES='1,2'.
    [<Theory>]
    [<InlineData("Mercury", 71.4908, 23.2219, 0.03)>]
    [<InlineData("Venus", 23.8876, 7.6499, 0.03)>]
    [<InlineData("Mars", 143.9282, 15.8536, 0.03)>]
    [<InlineData("Jupiter", 87.8226, 23.2444, 0.05)>]
    [<InlineData("Saturn", 1.2840, -1.7397, 0.15)>]
    [<InlineData("Uranus", 55.9057, 19.5324, 0.05)>]
    [<InlineData("Neptune", 2.2279, -0.4493, 0.05)>]
    let ``Geocentric planet positions match the published almanac``
        (body: string)
        (referenceRaDeg: float)
        (referenceDecDeg: float)
        (toleranceDeg: float)
        =
        let elements = Data.planetElements ()
        let jdTt = Time.jdTtOfUtc referenceEpoch
        let computed = geocentricPlanet elements body jdTt
        let equatorial = computed.EquatorialOfDate
        let separation = separationDeg equatorial.RightAscensionDeg equatorial.DeclinationDeg referenceRaDeg referenceDecDeg

        Assert.True(
            separation <= toleranceDeg,
            sprintf "%s is off by %.4f deg (tolerance %.2f)." body separation toleranceDeg)

    [<Fact>]
    let ``Sun position matches the published almanac`` () =
        let jdTt = Time.jdTtOfUtc referenceEpoch
        let sun = geocentricSun jdTt
        let equatorial = sun.EquatorialOfDate
        let separation = separationDeg equatorial.RightAscensionDeg equatorial.DeclinationDeg 69.1848 22.0597
        Assert.True(separation < 0.10, sprintf "Sun is off by %.4f deg." separation)

    [<Fact>]
    let ``Sun longitude is near zero at the March equinox`` () =
        let equinox = DateTime(2025, 3, 20, 9, 1, 0, DateTimeKind.Utc)
        let jdTt = Time.jdTtOfUtc equinox
        let sun = geocentricSun jdTt
        let equatorial = sun.EquatorialOfDate
        let separation = separationDeg equatorial.RightAscensionDeg equatorial.DeclinationDeg 0.0 0.0
        Assert.True(separation < 0.5, sprintf "Sun is %.4f deg from the equinox point." separation)

    /// Airless apparent azimuth and elevation of Mars at the Royal Observatory,
    /// Greenwich (lat 51.4769 N, lon 0, elevation 46 m), for 2025-06-01 00:00 UT.
    /// Horizons query: EPHEM_TYPE=OBSERVER, CENTER=coord@399,
    /// SITE_COORD='0.0000,51.4769,0.046', QUANTITIES='2,4'.
    [<Fact>]
    let ``Mars altitude and azimuth match the published almanac`` () =
        let elements = Data.planetElements ()
        let jdUt = Time.julianDayOfUtc referenceEpoch
        let jdTt = Time.jdTtOfUtc referenceEpoch
        let latitude = 51.4769
        let longitude = 0.0
        let lst = localSiderealDeg jdUt longitude
        let mars = geocentricPlanet elements "Mars" jdTt
        let horizontal = toHorizontal latitude lst mars.EquatorialOfDate.RightAscensionDeg mars.EquatorialOfDate.DeclinationDeg

        Assert.True(abs (horizontal.AzimuthDeg - 292.0616) < 0.20, sprintf "Mars azimuth was %f" horizontal.AzimuthDeg)
        Assert.True(abs (horizontal.AltitudeDeg - 2.8957) < 0.20, sprintf "Mars altitude was %f" horizontal.AltitudeDeg)

    [<Fact>]
    let ``Geocentric longitude is continuous and within range`` () =
        let elements = Data.planetElements ()

        for body in [ "Mercury"; "Venus"; "Mars"; "Jupiter"; "Saturn"; "Uranus"; "Neptune" ] do
            let computed = geocentricPlanet elements body (Time.jdTtOfUtc referenceEpoch)
            Assert.InRange(computed.EclipticLongitudeJ2000Deg, 0.0, 360.0)
            Assert.InRange(computed.EclipticLatitudeJ2000Deg, -90.0, 90.0)
            Assert.True(computed.EquatorialOfDate.DistanceAu > 0.0)
