module PaperOrreryCli

open System
open System.Globalization
open PaperOrrery
open PaperOrrery.Ephemeris
open PaperOrrery.Horizon
open PaperOrrery.SkyChart


type Options =
    { Date: DateTime
      LatitudeDeg: float
      LongitudeDeg: float
      OutputPath: string
      Title: string }

let private invariant (format: string) (value: float) : string =
    value.ToString(format, CultureInfo.InvariantCulture)

let private printUsage () =
    printfn "Paper Orrery - planet positions and sky charts."
    printfn ""
    printfn "Usage:"
    printfn "  dotnet run --project src/PaperOrrery.Cli -- [chart|positions] [options]"
    printfn ""
    printfn "Options:"
    printfn "  --date <iso-utc>   Instant, for example 2025-06-01T00:00:00. Default: now."
    printfn "  --lat <degrees>    Site latitude, north positive. Default: 51.4769."
    printfn "  --lon <degrees>    Site longitude, east positive. Default: 0."
    printfn "  --out <path>       Output file for the chart. Default: sky-chart.svg."
    printfn "  --title <text>     Chart title. Default: 'Paper Orrery sky chart'."
    printfn ""
    printfn "Subcommands:"
    printfn "  chart       Render an SVG sky chart. This is the default."
    printfn "  positions   Print a table of computed positions."

let private parseDate (text: string) : DateTime =
    let parsed, value =
        DateTime.TryParse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal ||| DateTimeStyles.AdjustToUniversal)

    if parsed then value else invalidArg "date" (sprintf "'%s' is not a valid date." text)

let private parseNumber (name: string) (text: string) : float =
    let parsed, value = Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture)
    if parsed then value else invalidArg name (sprintf "'%s' is not a number." text)

let private parseArguments (argv: string []) (defaults: Options) : Options =
    let rec loop index (options: Options) =
        if index >= argv.Length then
            options
        else
            match argv.[index] with
            | "--date" when index + 1 < argv.Length -> loop (index + 2) { options with Date = parseDate argv.[index + 1] }
            | "--lat" when index + 1 < argv.Length -> loop (index + 2) { options with LatitudeDeg = parseNumber "lat" argv.[index + 1] }
            | "--lon" when index + 1 < argv.Length -> loop (index + 2) { options with LongitudeDeg = parseNumber "lon" argv.[index + 1] }
            | "--out" when index + 1 < argv.Length -> loop (index + 2) { options with OutputPath = argv.[index + 1] }
            | "--title" when index + 1 < argv.Length -> loop (index + 2) { options with Title = argv.[index + 1] }
            | "--help" | "-h" ->
                printUsage ()
                exit 0
            | unknown -> invalidArg "arguments" (sprintf "Unknown option '%s'." unknown)

    loop 0 defaults

let private observe (options: Options) (elements: ElementSet list) : ObservedBody list =
    Observation.calculate
        elements
        { InstantUtc = options.Date
          LatitudeDeg = options.LatitudeDeg
          LongitudeDeg = options.LongitudeDeg }

let private runPositions (options: Options) (elements: ElementSet list) : int =
    let bodies = observe options elements

    printfn "%-6s %10s %10s %8s %9s %9s" "Body" "RA(deg)" "Dec(deg)" "Alt(deg)" "Az(deg)" "Dist(AU)"
    printfn "%s" (String('-', 58))

    bodies
    |> List.filter (fun body -> body.DistanceAu.IsSome)
    |> List.iter (fun body ->
        printfn
            "%-6s %10s %10s %8s %9s %9s"
            body.Name
            (invariant "0.000" body.RightAscensionDeg)
            (invariant "0.000" body.DeclinationDeg)
            (invariant "0.0" body.AltitudeDeg)
            (invariant "0.0" body.AzimuthDeg)
            (body.DistanceAu |> Option.defaultValue 0.0 |> invariant "0.000"))

    let visible = bodies |> List.filter (fun body -> body.AltitudeDeg >= 0.0) |> List.map (fun body -> body.Name)

    printfn ""
    printfn "Above horizon: %s" (String.concat ", " visible)
    0

let private runChart (options: Options) (elements: ElementSet list) : int =
    let bodies = observe options elements

    let chartBodies =
        bodies
        |> List.choose (fun body ->
            let kind =
                match body.Kind with
                | ObservedKind.Star -> Star
                | ObservedKind.Planet -> Planet
                | ObservedKind.Sun -> Sun

            if body.AltitudeDeg >= 0.0 then
                Some
                    { Label = body.Name
                      AltitudeDeg = body.AltitudeDeg
                      AzimuthDeg = body.AzimuthDeg
                      Kind = kind
                      Magnitude = body.Magnitude }
            else
                None)

    let subtitle =
        sprintf
            "%s  |  lat %s  lon %s"
            (options.Date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
            (invariant "0.0000" options.LatitudeDeg)
            (invariant "0.0000" options.LongitudeDeg)

    let settings =
        { Title = options.Title
          Subtitle = subtitle
          Size = 1000 }

    let svg = SkyChart.render settings chartBodies
    SkyChart.writeToFile options.OutputPath svg

    printfn "Wrote %s (%d bodies above horizon)." options.OutputPath (List.length chartBodies)
    0

[<EntryPoint>]
let main (argv: string []) : int =
    let command, rest =
        match argv with
        | [||] -> "chart", [||]
        | [| "chart" |] -> "chart", [||]
        | [| "positions" |] -> "positions", [||]
        | [| first; _ |] when first = "chart" || first = "positions" -> first, argv.[1..]
        | _ when argv.[0].StartsWith("--") -> "chart", argv
        | [| first |] -> first, [||]
        | _ -> argv.[0], argv.[1..]

    try
        let defaults =
            { Date = DateTime.UtcNow.AddMinutes(-float (DateTime.UtcNow.Minute))
              LatitudeDeg = 51.4769
              LongitudeDeg = 0.0
              OutputPath = "sky-chart.svg"
              Title = "Paper Orrery sky chart" }

        let options = parseArguments rest defaults
        let elements = Data.planetElements ()

        match command with
        | "positions" -> runPositions options elements
        | "chart" | _ -> runChart options elements
    with
    | :? ArgumentException as error ->
        eprintfn "%s" error.Message
        printUsage ()
        2
    | error ->
        eprintfn "Unexpected error: %s" error.Message
        1
