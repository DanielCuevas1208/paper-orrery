namespace PaperOrrery.Tests

open System
open Xunit
open PaperOrrery

module KeplerTests =

    [<Fact>]
    let ``Kepler equation is satisfied to machine precision`` () =
        let meanAnomalies = [ 0.0; 0.1; 1.7; 3.14; 4.2; 5.9; 6.2 ]
        let eccentricities = [ 0.0; 0.1; 0.35; 0.7; 0.95 ]

        for meanAnomaly in meanAnomalies do
            for eccentricity in eccentricities do
                let eccentricAnomaly = Kepler.solve meanAnomaly eccentricity
                let residual = eccentricAnomaly - eccentricity * sin eccentricAnomaly - meanAnomaly
                Assert.True(abs residual < 1.0e-10, sprintf "Residual %f for M=%f e=%f" residual meanAnomaly eccentricity)

    [<Fact>]
    let ``Circular orbit returns the mean anomaly`` () =
        let meanAnomaly = 2.0
        let eccentricAnomaly = Kepler.solve meanAnomaly 0.0
        Assert.Equal(meanAnomaly, eccentricAnomaly, 12)

    [<Fact>]
    let ``Zero mean anomaly returns zero`` () =
        let eccentricAnomaly = Kepler.solve 0.0 0.9
        Assert.Equal(0.0, eccentricAnomaly, 12)
