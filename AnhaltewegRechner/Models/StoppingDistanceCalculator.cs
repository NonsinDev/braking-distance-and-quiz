using System;

namespace AnhaltewegRechner.Models;

/// <summary>
/// Enthält die komplette Berechnungslogik, losgelöst von der Oberfläche.
/// Die Formeln wurden durch systematisches Testen der Altanwendung
/// (unterschiedliche Eingabewerte, Abgleich der Ausgaben) rekonstruiert,
/// da für diese keine Dokumentation vorlag.
/// </summary>
public static class StoppingDistanceCalculator
{
    // Angenommene Bremsverzögerung in m/s² (typische Richtwerte aus der
    // Verkehrspädagogik für eine Vollbremsung; siehe FormulasWindow für Details).
    private const double DryDecelerationMs2 = 7.5;
    private const double WetDecelerationMs2 = 5.0;

    public static double KmhToMs(double speedKmh) => speedKmh / 3.6;

    public static double GetDeceleration(RoadCondition condition) =>
        condition == RoadCondition.Wet ? WetDecelerationMs2 : DryDecelerationMs2;

    /// <summary>Reaktionsweg = Geschwindigkeit [m/s] × Reaktionszeit [s].</summary>
    public static double ReactionDistanceMeters(double speedKmh, double reactionTimeSeconds)
        => KmhToMs(speedKmh) * reactionTimeSeconds;

    /// <summary>Bremsweg = v² / (2 × a).</summary>
    public static double BrakingDistanceMeters(double speedKmh, RoadCondition condition)
    {
        double speedMs = KmhToMs(speedKmh);
        double deceleration = GetDeceleration(condition);
        return (speedMs * speedMs) / (2 * deceleration);
    }

    /// <summary>Anhalteweg = Reaktionsweg + Bremsweg.</summary>
    public static double StoppingDistanceMeters(double speedKmh, double reactionTimeSeconds, RoadCondition condition)
        => ReactionDistanceMeters(speedKmh, reactionTimeSeconds) + BrakingDistanceMeters(speedKmh, condition);

    /// <summary>
    /// Aufprallgeschwindigkeit: Die Geschwindigkeit, mit der das eigene Fahrzeug
    /// genau an der Stelle noch fährt, an der ein mit zulässiger Höchstgeschwindigkeit
    /// fahrendes Fahrzeug bereits vollständig zum Stillstand gekommen wäre.
    ///
    /// Liegt dieser Vergleichspunkt noch innerhalb der eigenen Reaktionsstrecke,
    /// hat das eigene Fahrzeug dort noch gar nicht zu bremsen begonnen – die
    /// Aufprallgeschwindigkeit entspricht dann exakt der Ausgangsgeschwindigkeit.
    /// </summary>
    public static double ImpactSpeedKmh(
        double ownSpeedKmh,
        double speedLimitKmh,
        double reactionTimeSeconds,
        RoadCondition condition)
    {
        double referenceStoppingDistance = StoppingDistanceMeters(speedLimitKmh, reactionTimeSeconds, condition);
        double ownReactionDistance = ReactionDistanceMeters(ownSpeedKmh, reactionTimeSeconds);

        if (referenceStoppingDistance <= ownReactionDistance)
        {
            // Das Vergleichsfahrzeug steht bereits, bevor das eigene Fahrzeug
            // überhaupt zu bremsen beginnt.
            return ownSpeedKmh;
        }

        double distanceCoveredWhileBraking = referenceStoppingDistance - ownReactionDistance;
        double ownSpeedMs = KmhToMs(ownSpeedKmh);
        double deceleration = GetDeceleration(condition);

        double impactSpeedSquared = (ownSpeedMs * ownSpeedMs) - (2 * deceleration * distanceCoveredWhileBraking);
        if (impactSpeedSquared <= 0)
        {
            // Das eigene Fahrzeug wäre an dieser Stelle ebenfalls schon zum Stehen gekommen.
            return 0;
        }

        double impactSpeedMs = Math.Sqrt(impactSpeedSquared);
        return impactSpeedMs * 3.6;
    }
}
