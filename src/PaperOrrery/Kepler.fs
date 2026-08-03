namespace PaperOrrery

open System

/// Solves the equation of the center for Keplerian motion.
module Kepler =

    /// Solves Kepler's equation  M = E - e sin(E) for E.
    /// Uses Newton-Raphson iteration seeded from the mean anomaly.
    /// Parameters: meanAnomaly in radians, eccentricity in [0, 1).
    let solve (meanAnomaly: float) (eccentricity: float) : float =
        let tolerance = 1.0e-12

        let rec iterate eccentricAnomaly steps =
            if steps = 0 then
                eccentricAnomaly
            else
                let error =
                    eccentricAnomaly
                    - eccentricity * sin eccentricAnomaly
                    - meanAnomaly

                let derivative = 1.0 - eccentricity * cos eccentricAnomaly
                let next = eccentricAnomaly - error / derivative

                if abs (next - eccentricAnomaly) < tolerance then
                    next
                else
                    iterate next (steps - 1)

        iterate meanAnomaly 25
