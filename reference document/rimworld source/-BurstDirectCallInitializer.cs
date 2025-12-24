using RimWorld;
using RimWorld.Planet;
using UnityEngine;

internal static class _0024BurstDirectCallInitializer
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	private static void Initialize()
	{
		MapGenUtility.ComputeLargestRects_0000B754_0024BurstDirectCall.Initialize();
		MapGenUtility.RectsComputeSpaces_0000B755_0024BurstDirectCall.Initialize();
		FastTileFinder.Initialize_0024ComputeQueryJob_SphericalDistance_00014F3A_0024BurstDirectCall();
		PlanetLayer.CalculateAverageTileSize_000153FD_0024BurstDirectCall.Initialize();
		PlanetLayer.IntGetTileSize_000153FF_0024BurstDirectCall.Initialize();
		PlanetLayer.IntGetTileCenter_00015402_0024BurstDirectCall.Initialize();
	}
}
