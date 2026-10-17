namespace PaperOrrery.Tests

open System
open Xunit
open PaperOrrery

module ObservationTests =

    let private request =
        { InstantUtc = DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc)
          LatitudeDeg = 51.4769
          LongitudeDeg = 0.0 }

    let private calculate () =
        Observation.calculate (Data.planetElements ()) request

    [<Fact>]
    let ``Observation assembles planets Sun and catalog stars`` () =
        let bodies = calculate ()
        let names = bodies |> List.map (fun body -> body.Name)

        for name in [ "Mercury"; "Venus"; "Mars"; "Jupiter"; "Saturn"; "Uranus"; "Neptune"; "Pluto"; "Sun" ] do
            Assert.Contains(name, names)

        let sun = bodies |> List.find (fun body -> body.Name = "Sun")
        Assert.Equal(ObservedKind.Sun, sun.Kind)
        Assert.True(sun.DistanceAu.IsSome)

        let mars = bodies |> List.find (fun body -> body.Name = "Mars")
        Assert.Equal(ObservedKind.Planet, mars.Kind)

        let sirius = bodies |> List.find (fun body -> body.Name = "Sirius")
        Assert.Equal(ObservedKind.Star, sirius.Kind)
        Assert.Equal(Some -1.46, sirius.Magnitude)
        Assert.Equal(None, sirius.DistanceAu)

    [<Fact>]
    let ``Observation results are deterministic for a fixed request`` () =
        let first = calculate ()
        let second = calculate ()
        Assert.Equal<ObservedBody list>(first, second)
