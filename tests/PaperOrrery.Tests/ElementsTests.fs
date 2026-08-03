namespace PaperOrrery.Tests

open System
open Xunit
open PaperOrrery
open PaperOrrery

module ElementsTests =

    [<Fact>]
    let ``Bundled elements load for every planet`` () =
        let elements = Data.planetElements ()
        Assert.Equal(9, elements.Length)

        for body in [ "Mercury"; "Venus"; "Earth"; "Mars"; "Jupiter"; "Saturn"; "Uranus"; "Neptune"; "Pluto" ] do
            let matchFound =
                elements
                |> List.exists (fun entry -> String.Equals(entry.Body, body, StringComparison.OrdinalIgnoreCase))

            Assert.True(matchFound, sprintf "Missing elements for %s" body)

    [<Fact>]
    let ``Earth orbit is nearly circular with semi-major axis of one AU`` () =
        let earth =
            Data.planetElements ()
            |> List.find (fun entry -> entry.Body = "Earth")

        let osculating = earth.Evaluate(0.0)
        Assert.Equal(1.00000261, osculating.SemiMajorAxis, 6)
        Assert.InRange(osculating.Eccentricity, 0.015, 0.018)

    [<Fact>]
    let ``Element polynomials reproduce the epoch values at J2000`` () =
        let jupiter =
            Data.planetElements ()
            |> List.find (fun entry -> entry.Body = "Jupiter")

        let osculating = jupiter.Evaluate(0.0)
        Assert.Equal(5.20288700, osculating.SemiMajorAxis, 7)
        Assert.Equal(34.39644051, osculating.MeanLongitude, 7)

    [<Fact>]
    let ``Bright star catalog parses with plausible coordinates`` () =
        let stars = Data.brightStars ()
        Assert.True(stars.Length >= 50)

        for star in stars do
            Assert.InRange(star.RightAscensionDeg, 0.0, 360.0)
            Assert.InRange(star.DeclinationDeg, -90.0, 90.0)

        let sirius = stars |> List.find (fun star -> star.Name = "Sirius")
        Assert.Equal(101.287083, sirius.RightAscensionDeg, 5)
        Assert.Equal(-16.716111, sirius.DeclinationDeg, 5)
        Assert.True(sirius.Magnitude < 0.0)

    [<Fact>]
    let ``Malformed rows are rejected`` () =
        let csv =
            "Body,a0,a1\nMercury,0.38,x"

        Assert.Throws<FormatException>(fun () -> Data.parseElementSets csv |> ignore)
        |> ignore
