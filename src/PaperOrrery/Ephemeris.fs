namespace PaperOrrery

open System

/// Computes planet and Sun positions from Keplerian elements.
module Ephemeris =

    /// Mean obliquity of the ecliptic at J2000.0, in degrees.
    let obliquityJ2000Deg = 23.4392911

    /// Light travel time per astronomical unit, in days.
    let lightTimePerAuDays = 499.004786 / 86400.0

    /// A vector in the J2000 ecliptic frame, in astronomical units.
    type EclipticVector =
        { X: float
          Y: float
          Z: float }

    /// An equatorial position in the mean equator and equinox of date.
    type EquatorialOfDate =
        { RightAscensionDeg: float
          DeclinationDeg: float
          DistanceAu: float }

    /// A computed geocentric body position.
    type GeocentricBody =
        { Name: string
          EclipticLongitudeJ2000Deg: float
          EclipticLatitudeJ2000Deg: float
          EquatorialOfDate: EquatorialOfDate }

    let private deg = Math.PI / 180.0

    let private scale (k: float) (v: EclipticVector) : EclipticVector =
        { X = k * v.X; Y = k * v.Y; Z = k * v.Z }

    let private subtract (a: EclipticVector) (b: EclipticVector) : EclipticVector =
        { X = a.X - b.X; Y = a.Y - b.Y; Z = a.Z - b.Z }

    let private norm (v: EclipticVector) : float =
        sqrt (v.X * v.X + v.Y * v.Y + v.Z * v.Z)

    /// Converts osculating elements to a heliocentric ecliptic vector.
    let heliocentric (elements: OsculatingElements) : EclipticVector =
        let eccentricity = max 0.0 elements.Eccentricity
        let meanAnomaly = (elements.MeanLongitude - elements.Perihelion) * deg

        let eccentricAnomaly =
            Kepler.solve meanAnomaly eccentricity

        let trueAnomaly =
            2.0
            * Math.Atan2(
                Math.Sqrt(1.0 + eccentricity) * Math.Sin(eccentricAnomaly / 2.0),
                Math.Sqrt(1.0 - eccentricity) * Math.Cos(eccentricAnomaly / 2.0))

        let radius =
            elements.SemiMajorAxis * (1.0 - eccentricity * Math.Cos eccentricAnomaly)

        let argumentOfPerihelion =
            (elements.Perihelion - elements.Node) * deg

        let node = elements.Node * deg
        let inclination = elements.Inclination * deg
        let argumentOfLatitude = argumentOfPerihelion + trueAnomaly

        { X =
            radius
            * (Math.Cos node * Math.Cos argumentOfLatitude
               - Math.Sin node * Math.Sin argumentOfLatitude * Math.Cos inclination)
          Y =
            radius
            * (Math.Sin node * Math.Cos argumentOfLatitude
               + Math.Cos node * Math.Sin argumentOfLatitude * Math.Cos inclination)
          Z = radius * Math.Sin argumentOfLatitude * Math.Sin inclination }

    let private normalizeDegrees (value: float) : float =
        let wrapped = value % 360.0
        if wrapped < 0.0 then wrapped + 360.0 else wrapped

    /// Converts J2000 ecliptic longitude/latitude to J2000 equatorial angles.
    let private eclipticToEquatorialJ2000 (longitudeDeg: float) (latitudeDeg: float) : float * float =
        let epsilon = obliquityJ2000Deg * deg
        let lambda = longitudeDeg * deg
        let beta = latitudeDeg * deg

        let rightAscension =
            Math.Atan2(
                Math.Sin lambda * Math.Cos epsilon
                - Math.Tan beta * Math.Sin epsilon,
                Math.Cos lambda)

        let declination =
            Math.Asin(
                Math.Sin beta * Math.Cos epsilon
                + Math.Cos beta * Math.Sin epsilon * Math.Sin lambda)

        normalizeDegrees (rightAscension / deg), declination / deg

    /// Precesses J2000 equatorial coordinates to the mean equinox of date.
    /// Uses the IAU 1976 precession angles (USNO Circular 179, Newcomb).
    let precessJ2000ToDate (jdTt: float) (rightAscensionJ2000Deg: float) (declinationJ2000Deg: float) : float * float =
        let t = Time.julianCenturies jdTt
        let arcsec = Math.PI / (180.0 * 3600.0)

        let zeta =
            ((2306.2181 + 0.30188 * t + 0.017998 * t * t) * t) * arcsec

        let z =
            ((2306.2181 + 1.09468 * t + 0.018203 * t * t) * t) * arcsec

        let theta =
            ((2004.3109 - 0.42665 * t - 0.041833 * t * t) * t) * arcsec

        let alpha1 = rightAscensionJ2000Deg * deg
        let delta1 = declinationJ2000Deg * deg

        let a =
            Math.Cos delta1 * Math.Sin(alpha1 + zeta)

        let b =
            Math.Cos theta * Math.Cos delta1 * Math.Cos(alpha1 + zeta)
            - Math.Sin theta * Math.Sin delta1

        let c =
            Math.Sin theta * Math.Cos delta1 * Math.Cos(alpha1 + zeta)
            + Math.Cos theta * Math.Sin delta1

        let rightAscensionOfDate = z + Math.Atan2(a, b)
        let declinationOfDate = Math.Asin(Math.Clamp(c, -1.0, 1.0))
        normalizeDegrees (rightAscensionOfDate / deg), declinationOfDate / deg

    let private heliocentricAt (elements: ElementSet list) (body: string) (jdTt: float) : EclipticVector =
        elements
        |> List.find (fun entry -> String.Equals(entry.Body, body, StringComparison.OrdinalIgnoreCase))
        |> fun entry -> entry.Evaluate(Time.julianCenturies jdTt)
        |> heliocentric

    /// Computes a geocentric ecliptic vector with down-leg light-time correction.
    /// planetAt returns the heliocentric planet position at a given time.
    /// earthAt returns the heliocentric Earth position at a given time.
    let geocentricVector (earthAt: float -> EclipticVector) (planetAt: float -> EclipticVector) (jdTt: float) : EclipticVector =
        let rec iterate current steps =
            if steps = 0 then
                current
            else
                let tau = norm current * lightTimePerAuDays
                let corrected = subtract (planetAt (jdTt - tau)) (earthAt jdTt)
                iterate corrected (steps - 1)

        iterate (subtract (planetAt jdTt) (earthAt jdTt)) 3

    /// Computes the geocentric position of a planet for a Terrestrial Time Julian day.
    let geocentricPlanet (elements: ElementSet list) (planet: string) (jdTt: float) : GeocentricBody =
        let earthAt time = heliocentricAt elements "Earth" time
        let planetAt time = heliocentricAt elements planet time
        let geocentric = geocentricVector earthAt planetAt jdTt

        let distance = norm geocentric
        let longitude = normalizeDegrees (Math.Atan2(geocentric.Y, geocentric.X) / deg)
        let latitude = Math.Asin(Math.Clamp(geocentric.Z / distance, -1.0, 1.0)) / deg
        let raJ2000, decJ2000 = eclipticToEquatorialJ2000 longitude latitude
        let raOfDate, decOfDate = precessJ2000ToDate jdTt raJ2000 decJ2000

        { Name = planet
          EclipticLongitudeJ2000Deg = longitude
          EclipticLatitudeJ2000Deg = latitude
          EquatorialOfDate =
            { RightAscensionDeg = raOfDate
              DeclinationDeg = decOfDate
              DistanceAu = distance } }

    /// Computes the geocentric position of the Sun from supplied orbital elements.
    let geocentricSunWithElements (elements: ElementSet list) (jdTt: float) : GeocentricBody =
        let earthAt time = heliocentricAt elements "Earth" time

        let sunAt time =
            scale -1.0 (heliocentricAt elements "Earth" time)

        let geocentric =
            geocentricVector earthAt sunAt jdTt

        let distance = norm geocentric
        let longitude = normalizeDegrees (Math.Atan2(geocentric.Y, geocentric.X) / deg)
        let latitude = Math.Asin(Math.Clamp(geocentric.Z / distance, -1.0, 1.0)) / deg
        let raJ2000, decJ2000 = eclipticToEquatorialJ2000 longitude latitude
        let raOfDate, decOfDate = precessJ2000ToDate jdTt raJ2000 decJ2000

        { Name = "Sun"
          EclipticLongitudeJ2000Deg = longitude
          EclipticLatitudeJ2000Deg = latitude
          EquatorialOfDate =
            { RightAscensionDeg = raOfDate
              DeclinationDeg = decOfDate
              DistanceAu = distance } }

    /// Computes the geocentric position of the Sun from the bundled orbital elements.
    let geocentricSun (jdTt: float) : GeocentricBody =
        geocentricSunWithElements (Data.planetElements ()) jdTt
