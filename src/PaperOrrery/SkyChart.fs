namespace PaperOrrery

open System
open System.Globalization
open System.IO
open System.Text

/// Renders an all-sky chart as a vector (SVG) image.
/// The chart shows the sky above the horizon for one observer and one instant.
module SkyChart =

    /// Kind of a plotted body.
    type BodyKind =
        | Star
        | Planet
        | Sun

    /// A body to plot, already converted to the horizontal frame.
    type ChartBody =
        { Label: string
          AltitudeDeg: float
          AzimuthDeg: float
          Kind: BodyKind
          Magnitude: float option }

    /// Chart caption and layout settings.
    type ChartSettings =
        { Title: string
          Subtitle: string
          Size: int }

    let private deg = Math.PI / 180.0

    let private invariant (format: string) (value: float) : string =
        value.ToString(format, CultureInfo.InvariantCulture)

    let private formatAngle (value: float) : string =
        sprintf "%s deg" (invariant "0.#" value)

    let private project (cx: float) (cy: float) (horizonRadius: float) (altitude: float) (azimuth: float) : float * float =
        let radius = horizonRadius * (90.0 - altitude) / 90.0
        let angle = azimuth * deg
        cx + radius * Math.Sin angle, cy - radius * Math.Cos angle

    let private starRadius (magnitude: float) : float =
        Math.Clamp(1.7 + (1.6 - magnitude) * 1.15, 1.2, 4.6)

    let private starOpacity (magnitude: float) : float =
        Math.Clamp(0.9 - 0.25 * (magnitude - 1.0), 0.35, 1.0)

    let private renderStar (x: float) (y: float) (body: ChartBody) : string =
        let magnitude = body.Magnitude |> Option.defaultValue 2.0
        let radius = starRadius magnitude
        let opacity = starOpacity magnitude
        let fx = invariant "0.0" x
        let fy = invariant "0.0" y

        let glow =
            if magnitude < 1.0 then
                sprintf
                    "<circle cx=\"%s\" cy=\"%s\" r=\"%s\" fill=\"#ffd97a\" fill-opacity=\"0.10\"/>"
                    fx
                    fy
                    (invariant "0.0" (radius * 3.2))
            else
                ""

        let label =
            if magnitude <= 1.8 then
                sprintf
                    "<text x=\"%s\" y=\"%s\" font-family=\"Arial, sans-serif\" font-size=\"10\" fill=\"#c6cfe2\">%s</text>"
                    (invariant "0.0" (x + radius + 4.0))
                    (invariant "0.0" (y - radius - 2.0))
                    body.Label
            else
                ""

        sprintf
            "%s<circle cx=\"%s\" cy=\"%s\" r=\"%s\" fill=\"#f4f7ff\" fill-opacity=\"%s\"/>%s"
            glow
            fx
            fy
            (invariant "0.0" radius)
            (invariant "0.00" opacity)
            label

    let private renderPlanet (x: float) (y: float) (body: ChartBody) : string =
        let fx = invariant "0.0" x
        let fy = invariant "0.0" y

        sprintf
            "<g><circle cx=\"%s\" cy=\"%s\" r=\"7\" fill=\"none\" stroke=\"#f5c15c\" stroke-width=\"1.5\"/><circle cx=\"%s\" cy=\"%s\" r=\"7\" fill=\"#f5c15c\" fill-opacity=\"0.35\"/><circle cx=\"%s\" cy=\"%s\" r=\"3.4\" fill=\"#f5c15c\"/><text x=\"%s\" y=\"%s\" font-family=\"Arial, sans-serif\" font-size=\"13\" font-weight=\"bold\" fill=\"#ffd97a\">%s</text></g>"
            fx fy fx fy fx fy (invariant "0.0" (x + 11.0)) (invariant "0.0" (y + 5.0)) body.Label

    let private renderSun (x: float) (y: float) (body: ChartBody) : string =
        let fx = invariant "0.0" x
        let fy = invariant "0.0" y

        sprintf
            "<g><circle cx=\"%s\" cy=\"%s\" r=\"12\" fill=\"#ffd76a\" fill-opacity=\"0.25\"/><circle cx=\"%s\" cy=\"%s\" r=\"8\" fill=\"#ffd76a\"/><text x=\"%s\" y=\"%s\" font-family=\"Arial, sans-serif\" font-size=\"13\" font-weight=\"bold\" fill=\"#ffd76a\">%s</text></g>"
            fx fy fx fy (invariant "0.0" (x + 12.0)) (invariant "0.0" (y + 5.0)) body.Label

    let private renderBody (cx: float) (cy: float) (horizonRadius: float) (body: ChartBody) : string =
        let x, y = project cx cy horizonRadius body.AltitudeDeg body.AzimuthDeg

        match body.Kind with
        | Star -> renderStar x y body
        | Planet -> renderPlanet x y body
        | Sun -> renderSun x y body

    let private renderTicks (cx: float) (cy: float) (horizonRadius: float) (major: bool) : string =
        let renderOne i =
            let angle = float i * deg
            let tickIn = if major then horizonRadius - 6.0 else horizonRadius - 3.5
            let x1 = invariant "0.0" (cx + tickIn * Math.Sin angle)
            let y1 = invariant "0.0" (cy - tickIn * Math.Cos angle)
            let x2 = invariant "0.0" (cx + horizonRadius * Math.Sin angle)
            let y2 = invariant "0.0" (cy - horizonRadius * Math.Cos angle)
            let stroke = if major then "#8fa1c9" else "#3c4a6e"

            sprintf
                "<line x1=\"%s\" y1=\"%s\" x2=\"%s\" y2=\"%s\" stroke=\"%s\" stroke-width=\"1\"/>"
                x1 y1 x2 y2 stroke

        let step = if major then 30 else 15
        [ 0 .. step .. 345 ] |> List.map renderOne |> String.concat ""

    let private renderCardinals (cx: float) (cy: float) (horizonRadius: float) : string =
        let cardinal (name: string) (angle: float) =
            let rad = angle * deg
            let x = cx + (horizonRadius + 22.0) * Math.Sin rad
            let y = cy - (horizonRadius + 22.0) * Math.Cos rad
            let anchor = if Math.Abs(Math.Sin rad) > 0.9 then "middle" else "start"

            sprintf
                "<text x=\"%s\" y=\"%s\" text-anchor=\"%s\" font-family=\"Georgia, serif\" font-size=\"16\" font-style=\"italic\" fill=\"#cdd8ee\">%s</text>"
                (invariant "0.0" x)
                (invariant "0.0" (y + 5.0))
                anchor
                name

        String.concat "" [ cardinal "N" 0.0; cardinal "E" 90.0; cardinal "S" 180.0; cardinal "W" 270.0 ]

    let private renderAltRing (cx: float) (cy: float) (horizonRadius: float) (altitude: float) : string =
        let radius = horizonRadius * (90.0 - altitude) / 90.0
        let label = sprintf "alt %d" (int altitude)

        sprintf
            "<circle cx=\"%s\" cy=\"%s\" r=\"%s\" fill=\"none\" stroke=\"#2b3a5c\" stroke-width=\"1\"/><text x=\"%s\" y=\"%s\" font-family=\"Arial, sans-serif\" font-size=\"10\" fill=\"#5c6d96\">%s</text>"
            (invariant "0.0" cx)
            (invariant "0.0" cy)
            (invariant "0.0" radius)
            (invariant "0.0" (cx + 4.0))
            (invariant "0.0" (cy - radius - 5.0))
            label

    let private renderLegend (cx: float) (size: float) (visible: ChartBody list) : string =
        let startY = size * 0.86
        let rowHeight = 17.0
        let columns = 6
        let colWidth = size / float columns

        visible
        |> List.chunkBySize columns
        |> List.mapi (fun rowIndex row ->
            let rowY = startY + float rowIndex * rowHeight

            row
            |> List.mapi (fun columnIndex body ->
                let bx = colWidth * (float columnIndex + 0.5)

                sprintf
                    "<text x=\"%s\" y=\"%s\" text-anchor=\"middle\" font-family=\"Arial, sans-serif\" font-size=\"11\" fill=\"#9fb0d0\">%s</text>"
                    (invariant "0.0" bx)
                    (invariant "0.0" rowY)
                    (sprintf "%s  %s  %s" body.Label (formatAngle body.AltitudeDeg) (formatAngle body.AzimuthDeg)))
            |> String.concat "")
        |> String.concat ""

    /// Renders an SVG chart for the given bodies and settings.
    /// Only bodies at or above the horizon are plotted.
    let render (settings: ChartSettings) (bodies: ChartBody list) : string =
        let size = float settings.Size
        let cx = size / 2.0
        let cy = size * 0.46
        let horizonRadius = size * 0.34

        let visible =
            bodies
            |> List.filter (fun body -> body.AltitudeDeg >= 0.0)
            |> List.sortByDescending (fun body -> body.AltitudeDeg)

        let bodyElements =
            visible
            |> List.map (renderBody cx cy horizonRadius)
            |> String.concat "\n"

        let builders = StringBuilder()

        builders.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>") |> ignore
        builders.AppendLine(sprintf "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 %d %d\">" settings.Size settings.Size) |> ignore
        builders.AppendLine("<defs>") |> ignore
        builders.AppendLine("<linearGradient id=\"sky\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#0a0f24\"/><stop offset=\"1\" stop-color=\"#182a4d\"/></linearGradient>") |> ignore
        builders.AppendLine("<radialGradient id=\"zenith\" cx=\"0.5\" cy=\"0.5\" r=\"0.5\"><stop offset=\"0\" stop-color=\"#16223f\" stop-opacity=\"0.8\"/><stop offset=\"1\" stop-color=\"#0a0f24\" stop-opacity=\"0\"/></radialGradient>") |> ignore
        builders.AppendLine("</defs>") |> ignore
        builders.AppendLine(sprintf "<rect width=\"%d\" height=\"%d\" fill=\"url(#sky)\"/>" settings.Size settings.Size) |> ignore
        builders.AppendLine(sprintf "<circle cx=\"%s\" cy=\"%s\" r=\"%s\" fill=\"url(#zenith)\"/>" (invariant "0.0" cx) (invariant "0.0" cy) (invariant "0.0" (horizonRadius * 1.05))) |> ignore
        builders.AppendLine(sprintf "<text x=\"%s\" y=\"%s\" text-anchor=\"middle\" font-family=\"Georgia, serif\" font-size=\"26\" font-style=\"italic\" fill=\"#e8eefb\">%s</text>" (invariant "0.0" cx) (invariant "0.0" (size * 0.07)) settings.Title) |> ignore
        builders.AppendLine(sprintf "<text x=\"%s\" y=\"%s\" text-anchor=\"middle\" font-family=\"Arial, sans-serif\" font-size=\"13\" fill=\"#8b9cc4\">%s</text>" (invariant "0.0" cx) (invariant "0.0" (size * 0.105)) settings.Subtitle) |> ignore
        builders.AppendLine(renderTicks cx cy horizonRadius false) |> ignore
        builders.AppendLine(renderTicks cx cy horizonRadius true) |> ignore
        builders.AppendLine(renderAltRing cx cy horizonRadius 60.0) |> ignore
        builders.AppendLine(renderAltRing cx cy horizonRadius 30.0) |> ignore
        builders.AppendLine(sprintf "<circle cx=\"%s\" cy=\"%s\" r=\"%s\" fill=\"none\" stroke=\"#7e90bb\" stroke-width=\"2\"/>" (invariant "0.0" cx) (invariant "0.0" cy) (invariant "0.0" horizonRadius)) |> ignore
        builders.AppendLine(renderCardinals cx cy horizonRadius) |> ignore
        builders.AppendLine(bodyElements) |> ignore
        builders.AppendLine(sprintf "<text x=\"%s\" y=\"%s\" text-anchor=\"middle\" font-family=\"Arial, sans-serif\" font-size=\"11\" fill=\"#5c6d96\">Positions are geometric and refer to the mean equinox of date. Refraction is not applied. Time scale: UTC.</text>" (invariant "0.0" cx) (invariant "0.0" (size * 0.975))) |> ignore
        builders.AppendLine(sprintf "<text x=\"%s\" y=\"%s\" text-anchor=\"middle\" font-family=\"Arial, sans-serif\" font-size=\"12\" font-weight=\"bold\" fill=\"#c6cfe2\">Visible bodies</text>" (invariant "0.0" cx) (invariant "0.0" (size * 0.83 + 15.0))) |> ignore
        builders.AppendLine(renderLegend cx size visible) |> ignore
        builders.AppendLine("</svg>") |> ignore
        builders.ToString()

    /// Writes an SVG document to a file.
    let writeToFile (path: string) (svg: string) : unit =
        File.WriteAllText(path, svg, new UTF8Encoding(false))
