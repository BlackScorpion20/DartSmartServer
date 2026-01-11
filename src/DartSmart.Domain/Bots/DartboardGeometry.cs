using DartSmart.Domain.Entities;
using DartSmart.Domain.ValueObjects;

namespace DartSmart.Domain.Bots;

public record ThrowCoordinates(double X, double Y);

public static class DartboardGeometry
{
    // Constants in millimeters (standard dartboard)
    private const double BullseyeRadius = 6.35;
    private const double DoubleBullRadius = 15.9;
    private const double TripleRingInnerRadius = 99.0;
    private const double TripleRingOuterRadius = 107.0;
    private const double DoubleRingInnerRadius = 162.0;
    private const double DoubleRingOuterRadius = 170.0;
    
    // Convert Segment/Multiplier to Target (Center) X,Y
    public static ThrowCoordinates GetTargetCoordinates(int segment, int multiplier)
    {
        if (segment == 25)
        {
            return new ThrowCoordinates(0, 0); // Bull center
        }

        // Angles: 20 is at 90 degrees (top). Each segment is 18 degrees (360/20).
        // 20 = 90°, 1 = 72°, 18 = 54°, ..., 5 = 108°.
        // Standard order: 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5
        var angleDegrees = GetSegmentAngle(segment);
        var angleRadians = angleDegrees * (Math.PI / 180.0);

        var radius = multiplier switch
        {
            1 => (DoubleBullRadius + TripleRingInnerRadius) / 2.0, // Middle of single
            2 => (DoubleRingInnerRadius + DoubleRingOuterRadius) / 2.0, // Middle of double
            3 => (TripleRingInnerRadius + TripleRingOuterRadius) / 2.0, // Middle of triple
            _ => throw new ArgumentOutOfRangeException(nameof(multiplier))
        };

        var x = radius * Math.Cos(angleRadians);
        var y = radius * Math.Sin(angleRadians);

        return new ThrowCoordinates(x, y);
    }

    public static (int Segment, int Multiplier) GetScoreFromCoordinates(ThrowCoordinates coords)
    {
        var radius = Math.Sqrt(coords.X * coords.X + coords.Y * coords.Y);

        // Bullseye check
        if (radius <= BullseyeRadius) return (25, 2); // Double Bull
        if (radius <= DoubleBullRadius) return (25, 1); // Single Bull

        // Outside board
        if (radius > DoubleRingOuterRadius) return (0, 0); // Miss

        // Calculate Angle
        // Atan2 returns angle in radians between -PI and PI
        var angleRadians = Math.Atan2(coords.Y, coords.X);
        var angleDegrees = angleRadians * (180.0 / Math.PI);
        
        // Normalize to 0-360
        if (angleDegrees < 0) angleDegrees += 360.0;

        // Determine Segment
        var segment = GetSegmentFromAngle(angleDegrees);

        // Determine Multiplier
        if (radius >= TripleRingInnerRadius && radius <= TripleRingOuterRadius) return (segment, 3);
        if (radius >= DoubleRingInnerRadius && radius <= DoubleRingOuterRadius) return (segment, 2);
        
        return (segment, 1);
    }

    private static double GetSegmentAngle(int segment)
    {
        // Segment 20 is at 90 degrees (Top)
        // Each segment is 18 degrees wide.
        // We need to map segment number to its center angle.
        // Order clockwise from 20: 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5
        
        int[] segmentsClockwise = { 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };
        int index = Array.IndexOf(segmentsClockwise, segment);
        
        if (index == -1) // 0 or invalid
             return 0; // Irrelevant for miss

        // 20 starts at 90 - 9 = 81 to 90 + 9 = 99. Center is 90.
        // Clockwise means angle decreases.
        // Index 0 (20): 90
        // Index 1 (1): 90 - 18 = 72
        // Index 2 (18): 72 - 18 = 54
        
        return 90.0 - (index * 18.0);
    }

    private static int GetSegmentFromAngle(double angleDegrees)
    {
        // Adjust angle so 20 (90 deg) is at index 0
        // We want to map 81-99 to 20.
        // (90 - angle + 9) / 18 should allow mapping?
        // Let's use a simpler way: normalize so 20 is at 0 degrees, rotating everything.
        
        // Current: 90 is 20. 72 is 1.
        // Shift: valid angle = 90 - angle. 
        // If angle is 90, result 0.
        // If angle is 72, result 18.
        
        double shifted = 90.0 - angleDegrees;
        if (shifted < 0) shifted += 360.0;
        
        // Now: 0 deg = 20, 18 deg = 1, ...
        // We add 9 to handle the half-segment offset (18/2)
        int index = (int)((shifted + 9.0) / 18.0) % 20;

        int[] segmentsClockwise = { 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };
        return segmentsClockwise[index];
    }
}
