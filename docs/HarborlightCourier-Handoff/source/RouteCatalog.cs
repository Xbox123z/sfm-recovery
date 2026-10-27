using System;
using System.Text.Json;

/// <summary>
/// Runtime DTO for map data generated and reachability-checked by native/route_compiler.cpp.
/// C# owns gameplay; this catalog owns the authored route geometry and patrol tuning.
/// </summary>
public sealed class RouteCatalog
{
    public int Version { get; set; }
    public CourierRoute[] Levels { get; set; } = Array.Empty<CourierRoute>();

    public static RouteCatalog Parse(string json)
    {
        RouteCatalog? catalog = JsonSerializer.Deserialize<RouteCatalog>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (catalog is null)
            throw new JsonException("Route catalog is empty.");
        if (!catalog.IsValid(out string error))
            throw new JsonException(error);
        return catalog;
    }

    public bool IsValid(out string error)
    {
        if (Version != 1)
        {
            error = $"Unsupported route catalog version {Version}.";
            return false;
        }
        if (Levels.Length != 2)
        {
            error = $"Expected two levels, found {Levels.Length}.";
            return false;
        }
        for (int index = 0; index < Levels.Length; index++)
        {
            CourierRoute level = Levels[index];
            if (level.Id != index || string.IsNullOrWhiteSpace(level.Name) ||
                level.Parcels.Length != level.RequiredParcels || level.Patrols.Length == 0 ||
                level.Blocks.Length == 0 || level.VisionRange <= 0f || level.HearingRange <= 0f ||
                level.WalkSpeed <= 0f || level.RunSpeed < level.WalkSpeed)
            {
                error = $"Route {index} has missing or inconsistent geometry/tuning.";
                return false;
            }
        }
        error = string.Empty;
        return true;
    }
}

public sealed class CourierRoute
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int RequiredParcels { get; set; }
    public int ParcelPay { get; set; }
    public int FullBonus { get; set; }
    public float WalkSpeed { get; set; }
    public float RunSpeed { get; set; }
    public float VisionRange { get; set; }
    public float HearingRange { get; set; }
    public float VisionRate { get; set; }
    public float NoiseRate { get; set; }
    public float SuspicionDecay { get; set; }
    public RoutePoint Spawn { get; set; } = new RoutePoint();
    public RoutePoint Exit { get; set; } = new RoutePoint();
    public RouteRect[] Blocks { get; set; } = Array.Empty<RouteRect>();
    public RoutePoint[] Parcels { get; set; } = Array.Empty<RoutePoint>();
    public RoutePatrol[] Patrols { get; set; } = Array.Empty<RoutePatrol>();
}

public sealed class RoutePoint
{
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class RouteRect
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}

public sealed class RoutePatrol
{
    public RoutePoint Origin { get; set; } = new RoutePoint();
    public RoutePoint Axis { get; set; } = new RoutePoint();
    public float Amplitude { get; set; }
    public float Phase { get; set; }
    public float Frequency { get; set; }
}
