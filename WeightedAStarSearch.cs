using System;
using System.Collections.Generic;
using Priority_Queue;
using UnityEngine;
using Ostranauts.Pathing;
using HarmonyLib;
using System.Reflection;

namespace TessPaths
{
    public class WeightedAStarSearch : IPathSearchProvider
    {
        public CondOwner coUs { get; set; }
        public Pathfinder pf { get; set; }
        public CondOwner coDest { get; set; }

        private Dictionary<Tile, Tile> tilesSearched = new Dictionary<Tile, Tile>();
        private Dictionary<Tile, float> tileCosts = new Dictionary<Tile, float>();

        private MethodInfo buildListMethod = AccessTools.Method(typeof(Pathfinder), "BuildListFromTilesSearched");

        public PathResult GetPath(Tile destination, bool bAllowAirlocks, Tile origin)
        {
            bool flag = false;
            bool flag2 = false;
            bool flag3 = false;
            SimplePriorityQueue<Tile> simplePriorityQueue = new SimplePriorityQueue<Tile>();
            this.tilesSearched.Clear();
            this.tileCosts.Clear();
            simplePriorityQueue.Enqueue(origin, 0f);
            this.tileCosts.Add(origin, 0f);
            Tile[] array = new Tile[8];
            string text = ((destination.coProps != null && destination.coProps.ship != null) ? destination.coProps.ship.strRegID : "");

            float evaPenalty = Plugin.Instance.EvaTilePenalty.Value;
            float doorPenalty = Plugin.Instance.DoorOpeningPenalty.Value;
            float firePenalty = Plugin.Instance.FireHazardPenalty.Value;

            for (int i = 0; i < 3333; i++)
            {
                if (i == 3332 || simplePriorityQueue.Count == 0)
                {
                    if (i > 1511)
                    {
                        Plugin.Log.LogWarning($"TessPaths: Pathfinder searched {i} tiles for {this.coUs?.strName ?? "Unknown"}. High tile search count!");
                    }
                    PathResult pathResult = new PathResult(origin, destination);
                    pathResult.SetTiles(null);
                    pathResult.bGravBlocked = flag;
                    pathResult.bForbidZoneBlocked = flag2;
                    pathResult.bAirlockBlocked = flag3;
                    Plugin.Log.LogWarning($"TessPaths: Path failed for '{this.coUs?.strName ?? "Unknown"}'. Searched {i} tiles. Blocked: Airlock={flag3}, Grav={flag}, Forbidden={flag2}");
                    return pathResult;
                }
                Tile tile = simplePriorityQueue.Dequeue();
                if (tile == destination)
                {
                    break;
                }
                bool flag4 = tile.coProps != null && tile.coProps.ship != null && tile.coProps.ship.strRegID != text;
                TileUtils.GetSurroundingTiles(ref array, tile, flag4);
                int num = 0;
                for (int j = 0; j < 8; j++)
                {
                    Tile tile2 = array[j];
                    if (!(tile2 == null))
                    {
                        float num2 = 1f;
                        if (j > 3)
                        {
                            num2 = 1.4142135f;
                        }
                        if ((j != 4 || (num & 3) == 0) && (j != 5 || (num & 6) == 0) && (j != 6 || (num & 9) == 0) && (j != 7 || (num & 12) == 0))
                        {
                            if (tile2.IsPortal && tile2.IsWall)
                            {
                                num2 += doorPenalty;
                                if (Pathfinder.CheckDoorPressure(tile2.tf.position, tile2.coProps.ship, tile2.room) && !bAllowAirlocks)
                                {
                                    num |= 1 << j;
                                    flag3 = true;
                                    goto IL_30F;
                                }
                            }

                            if (tile2.IsBurningHazard(this.coUs))
                            {
                                num2 += firePenalty;
                            }

                            bool isGrounded = tile2.coProps != null && TileUtils.CTShipTile.Triggered(tile2.coProps, null, true) && !Tile.IsEVATile(tile2);
                            if (!isGrounded)
                            {
                                num2 *= evaPenalty;
                                flag = true;
                            }

                            float num3 = this.tileCosts[tile] + num2;
                            float num4 = 100000000f;
                            if (!this.tileCosts.TryGetValue(tile2, out num4))
                            {
                                num4 = 100000000f;
                            }
                            if (j < 3 || num4 > num3)
                            {
                                if (tile2.IsForbidden(this.coUs))
                                {
                                    flag2 = true;
                                    num |= 1 << j;
                                }
                                else if (!tile2.bPassable && (!tile2.IsPortal || tile2.coProps.HasCond("IsPortalStuck", false)))
                                {
                                    num |= 1 << j;
                                }
                                else if (num4 > num3)
                                {
                                    this.tileCosts[tile2] = num3;
                                    float num5 = num3 + this.Heuristic(destination, tile2);
                                    simplePriorityQueue.Enqueue(tile2, num5);
                                    this.tilesSearched[tile2] = tile;
                                }
                            }
                        }
                    }
                    IL_30F:;
                }
            }

            List<Tile> list = null;
            if (buildListMethod != null)
            {
                list = buildListMethod.Invoke(null, new object[] { this.tilesSearched, origin, destination }) as List<Tile>;
            }
            else
            {
                Plugin.Log.LogError("TessPaths: Could not find BuildListFromTilesSearched method!");
            }
            
            PathResult pathResult2 = new PathResult(origin, destination);
            pathResult2.SetTiles(list);
            pathResult2.bAirlockBlocked = flag3;
            pathResult2.bGravBlocked = flag;
            pathResult2.bForbidZoneBlocked = flag2;
            
            if (list == null)
            {
                Plugin.Log.LogWarning($"TessPaths: Path succeeded algorithm but list build was empty for '{this.coUs?.strName ?? "Unknown"}'.");
            }
            else
            {
                float finalCost = 0f;
                this.tileCosts.TryGetValue(destination, out finalCost);
                Plugin.Log.LogInfo($"TessPaths: Path found for '{this.coUs?.strName ?? "Unknown"}' (Tiles: {list.Count}, Final Cost: {finalCost:F1}, EVA used: {flag}).");
            }
            return pathResult2;
        }

        private float Heuristic(Tile a, Tile b)
        {
            float num = a.tf.position.x - b.tf.position.x;
            float num2 = a.tf.position.y - b.tf.position.y;
            return Mathf.Sqrt(num * num + num2 * num2);
        }
    }
}
