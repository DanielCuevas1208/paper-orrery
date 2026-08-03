namespace PaperOrrery

open System
open System.Globalization
open System.IO
open System.Reflection

/// Orbital element catalog for a body, as linear polynomials in T.
/// T is Julian centuries of Terrestrial Time since J2000.0.
/// Angles are in degrees, semi-major axis in astronomical units.
type ElementSet =
    { Body: string
      SemiMajorAxis0: float
      SemiMajorAxisRate: float
      Eccentricity0: float
      EccentricityRate: float
      Inclination0: float
      InclinationRate: float
      MeanLongitude0: float
      MeanLongitudeRate: float
      Perihelion0: float
      PerihelionRate: float
      Node0: float
      NodeRate: float }

    /// Evaluates the osculating elements at a given epoch.
    member this.Evaluate(t: float) : OsculatingElements =
        { SemiMajorAxis = this.SemiMajorAxis0 + this.SemiMajorAxisRate * t
          Eccentricity = this.Eccentricity0 + this.EccentricityRate * t
          Inclination = this.Inclination0 + this.InclinationRate * t
          MeanLongitude = this.MeanLongitude0 + this.MeanLongitudeRate * t
          Perihelion = this.Perihelion0 + this.PerihelionRate * t
          Node = this.Node0 + this.NodeRate * t }

/// Osculating elements of a body at a single epoch.
and OsculatingElements =
    { SemiMajorAxis: float
      Eccentricity: float
      Inclination: float
      MeanLongitude: float
      Perihelion: float
      Node: float }

/// Loads the bundled element and star catalog files.
module Data =

    let private planetElementsResource = "PaperOrrery.data.planet-elements.csv"
    let private brightStarsResource = "PaperOrrery.data.bright-stars.csv"

    let private readEmbedded (resourceName: string) : string =
        let assembly = Assembly.GetExecutingAssembly()

        use stream =
            assembly.GetManifestResourceStream resourceName

        if isNull stream then
            invalidOp (sprintf "Embedded resource '%s' was not found." resourceName)

        use reader = new StreamReader(stream)
        reader.ReadToEnd()

    let private parseFloat (text: string) : float =
        Double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture)

    let private dataLines (csv: string) : string [] =
        csv.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun line -> line.Trim())
        |> Array.filter (fun line -> line.Length > 0 && not (line.StartsWith("#")))

    /// Parses planet element rows from CSV text.
    let parseElementSets (csv: string) : ElementSet list =
        dataLines csv
        |> Array.skip 1
        |> Array.map (fun line ->
            let fields = line.Split(',')
            { Body = fields.[0]
              SemiMajorAxis0 = parseFloat fields.[1]
              SemiMajorAxisRate = parseFloat fields.[2]
              Eccentricity0 = parseFloat fields.[3]
              EccentricityRate = parseFloat fields.[4]
              Inclination0 = parseFloat fields.[5]
              InclinationRate = parseFloat fields.[6]
              MeanLongitude0 = parseFloat fields.[7]
              MeanLongitudeRate = parseFloat fields.[8]
              Perihelion0 = parseFloat fields.[9]
              PerihelionRate = parseFloat fields.[10]
              Node0 = parseFloat fields.[11]
              NodeRate = parseFloat fields.[12] })
        |> Array.toList

    /// A bright star with J2000 equatorial coordinates.
    type BrightStar =
        { Name: string
          RightAscensionDeg: float
          DeclinationDeg: float
          Magnitude: float }

    /// Parses bright star rows from CSV text.
    let parseBrightStars (csv: string) : BrightStar list =
        dataLines csv
        |> Array.skip 1
        |> Array.map (fun line ->
            let fields = line.Split(',')
            { Name = fields.[0]
              RightAscensionDeg = parseFloat fields.[2]
              DeclinationDeg = parseFloat fields.[3]
              Magnitude = parseFloat fields.[4] })
        |> Array.toList

    /// The bundled planet elements.
    let planetElements () : ElementSet list =
        parseElementSets (readEmbedded planetElementsResource)

    /// The bundled bright star catalog.
    let brightStars () : BrightStar list =
        parseBrightStars (readEmbedded brightStarsResource)
