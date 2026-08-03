namespace PaperOrrery.Tests

open System
open Xunit
open PaperOrrery

module HorizonTests =

    [<Fact>]
    let ``Greenwich mean sidereal time at J2000 equals its defining value`` () =
        let j2000Jd = 2451545.0
        let gmst = Horizon.greenwichMeanSiderealDeg j2000Jd
        Assert.Equal(280.46061837, gmst, 6)

    [<Fact>]
    let ``Greenwich mean sidereal time advances one sidereal day per day`` () =
        let start = Horizon.greenwichMeanSiderealDeg 2460000.5
        let next = Horizon.greenwichMeanSiderealDeg 2460001.5
        let delta = (next - start + 360.0) % 360.0
        Assert.Equal(0.98564736629, delta, 6)

    [<Fact>]
    let ``Local sidereal time shifts with longitude`` () =
        let jdUt = 2460827.5
        let atGreenwich = Horizon.localSiderealDeg jdUt 0.0
        let atNinetyEast = Horizon.localSiderealDeg jdUt 90.0
        let delta = (atNinetyEast - atGreenwich + 360.0) % 360.0
        Assert.Equal(90.0, delta, 6)

    [<Fact>]
    let ``Object at the meridian due south has azimuth 180 degrees`` () =
        let latitude = 51.4769
        let lst = 100.0
        let dec = 10.0
        let horizontal = Horizon.toHorizontal latitude lst lst dec
        Assert.Equal(48.52, horizontal.AltitudeDeg, 2)
        Assert.Equal(180.0, horizontal.AzimuthDeg, 2)

    [<Fact>]
    let ``Object rising in the east has azimuth 90 degrees`` () =
        let latitude = 51.4769
        let lst = 100.0
        let hourAngle = -90.0
        let ra = lst - hourAngle
        let horizontal = Horizon.toHorizontal latitude lst ra 0.0
        Assert.Equal(0.0, horizontal.AltitudeDeg, 3)
        Assert.Equal(90.0, horizontal.AzimuthDeg, 2)

    [<Fact>]
    let ``Object setting in the west has azimuth 270 degrees`` () =
        let latitude = 51.4769
        let lst = 100.0
        let hourAngle = 90.0
        let ra = lst - hourAngle
        let horizontal = Horizon.toHorizontal latitude lst ra 0.0
        Assert.Equal(0.0, horizontal.AltitudeDeg, 3)
        Assert.Equal(270.0, horizontal.AzimuthDeg, 2)

    [<Fact>]
    let ``Polaris stays close to north at all hours`` () =
        let latitude = 51.4769
        let dec = 89.2642

        for hour in [ 0.0; 3.0; 6.0; 9.0; 12.0; 15.0; 18.0; 21.0 ] do
            let lst = 100.0
            let ra = lst - hour * 15.0
            let horizontal = Horizon.toHorizontal latitude lst ra dec
            let offset = min horizontal.AzimuthDeg (360.0 - horizontal.AzimuthDeg)
            Assert.True(offset < 3.0, sprintf "Polaris azimuth was %f" horizontal.AzimuthDeg)
            Assert.True(horizontal.AltitudeDeg > 50.0)
