namespace PaperOrrery.Tests

open System
open System.IO
open Xunit
open PaperOrrery
open PaperOrrery.SkyChart

module SkyChartTests =

    let private sampleBodies () =
        [ { Label = "Vega"
            AltitudeDeg = 72.0
            AzimuthDeg = 355.0
            Kind = Star
            Magnitude = Some 0.03 }
          { Label = "Sirius"
            AltitudeDeg = 15.0
            AzimuthDeg = 135.0
            Kind = Star
            Magnitude = Some -1.46 }
          { Label = "Mars"
            AltitudeDeg = 2.9
            AzimuthDeg = 292.0
            Kind = Planet
            Magnitude = None }
          { Label = "Sun"
            AltitudeDeg = -12.0
            AzimuthDeg = 60.0
            Kind = Sun
            Magnitude = None } ]

    let private settings =
        { Title = "Test chart"
          Subtitle = "2025-06-01 00:00:00 | lat 51.4769 lon 0"
          Size = 1000 }

    [<Fact>]
    let ``Rendered chart is valid XML`` () =
        let svg = render settings (sampleBodies ())
        use reader = Xml.XmlReader.Create(new StringReader(svg))
        let mutable wellFormed = true
        try
            while reader.Read() do
                ()
        with :? Xml.XmlException ->
            wellFormed <- false
        Assert.True(wellFormed)

    [<Fact>]
    let ``Bodies below the horizon are omitted`` () =
        let svg = render settings (sampleBodies ())
        Assert.Contains("Mars", svg)
        Assert.DoesNotContain(">Sun</text>", svg)

    [<Fact>]
    let ``Chart contains structural elements`` () =
        let svg = render settings (sampleBodies ())
        Assert.Contains("N</text>", svg)
        Assert.Contains("E</text>", svg)
        Assert.Contains("alt 30", svg)
        Assert.Contains("Visible bodies", svg)

    [<Fact>]
    let ``Star and planet markers use distinct classes`` () =
        let svg = render settings (sampleBodies ())
        let starMarker = svg.Contains("fill=\"#f4f7ff\"")
        let planetMarker = svg.Contains("fill=\"#f5c15c\"")
        Assert.True(starMarker && planetMarker)

    [<Fact>]
    let ``Rendered chart is deterministic`` () =
        let first = render settings (sampleBodies ())
        let second = render settings (sampleBodies ())
        Assert.Equal(first, second)
