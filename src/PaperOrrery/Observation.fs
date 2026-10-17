namespace PaperOrrery

open System

/// Inputs for one local-sky observation.
type ObservationRequest =
    { InstantUtc: DateTime
      LatitudeDeg: float
      LongitudeDeg: float }

/// The kind of body returned by an observation.
type ObservedKind =
    | Star
    | Planet
    | Sun

/// A body with equatorial and local-horizon coordinates.
type ObservedBody =
    { Name: string
      RightAscensionDeg: float
      DeclinationDeg: float
      AltitudeDeg: float
      AzimuthDeg: float
      DistanceAu: float option
      Kind: ObservedKind
      Magnitude: float option }

/// Builds the complete body list used by the command line tool and chart renderer.
module Observation =

    let private planetNames =
        [ "Mercury"; "Venus"; "Mars"; "Jupiter"; "Saturn"; "Uranus"; "Neptune"; "Pluto" ]

    let private observeCoordinates
        (request: ObservationRequest)
        (lstDeg: float)
        (kind: ObservedKind)
        (magnitude: float option)
        (name: string)
        (rightAscensionDeg: float)
        (declinationDeg: float)
        (distanceAu: float option)
        : ObservedBody =
        let horizontal =
            Horizon.toHorizontal request.LatitudeDeg lstDeg rightAscensionDeg declinationDeg

        { Name = name
          RightAscensionDeg = rightAscensionDeg
          DeclinationDeg = declinationDeg
          AltitudeDeg = horizontal.AltitudeDeg
          AzimuthDeg = horizontal.AzimuthDeg
          DistanceAu = distanceAu
          Kind = kind
          Magnitude = magnitude }

    let private observeBody
        (request: ObservationRequest)
        (lstDeg: float)
        (kind: ObservedKind)
        (magnitude: float option)
        (body: Ephemeris.GeocentricBody)
        : ObservedBody =
        observeCoordinates
            request
            lstDeg
            kind
            magnitude
            body.Name
            body.EquatorialOfDate.RightAscensionDeg
            body.EquatorialOfDate.DeclinationDeg
            (Some body.EquatorialOfDate.DistanceAu)

    /// Computes planets, the Sun, and bundled bright stars for one observer and instant.
    let calculate (elements: ElementSet list) (request: ObservationRequest) : ObservedBody list =
        let jdUt = Time.julianDayOfUtc request.InstantUtc
        let jdTt = Time.jdTtOfUtc request.InstantUtc
        let lstDeg = Horizon.localSiderealDeg jdUt request.LongitudeDeg

        let planets =
            planetNames
            |> List.map (fun name ->
                Ephemeris.geocentricPlanet elements name jdTt
                |> observeBody request lstDeg Planet None)

        let sun =
            Ephemeris.geocentricSunWithElements elements jdTt
            |> observeBody request lstDeg Sun None

        let stars =
            Data.brightStars ()
            |> List.map (fun star ->
                let raDate, decDate =
                    Ephemeris.precessJ2000ToDate jdTt star.RightAscensionDeg star.DeclinationDeg

                observeCoordinates
                    request
                    lstDeg
                    Star
                    (Some star.Magnitude)
                    star.Name
                    raDate
                    decDate
                    None)

        planets @ [ sun ] @ stars
