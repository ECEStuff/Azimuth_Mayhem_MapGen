using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azimuth
{
    class X3MapPath
    {
        class Location
        {
            public int X;
            public int Y;
            public int F;
            public int G;
            public int H;
            public Location Parent;
        }

        public static bool HasPath(X3Sector source, X3Sector dest)
        {
            Location current = null;
            var start = new Location { X = source.x, Y = source.y };
            var target = new Location { X = dest.x, Y = dest.y };
            var openList = new List<Location>();
            var closedList = new List<Location>();
            int g = 0;

            // start by adding the original position to the open list
            openList.Add(start);

            while (openList.Count > 0)
            {
                // get the square with the lowest F score
                var lowest = openList.Min(l => l.F);
                current = openList.First(l => l.F == lowest);

                // Found the target, we are done.  There is a path.
                if (current.X == target.X && current.Y == target.Y)
                    return true;

                // add the current square to the closed list
                closedList.Add(current);

                // remove it from the open list
                openList.Remove(current);

                var adjacentSquares = GetGateConnectedSystems(current.X, current.Y);
                g = current.G + 1;

                foreach (var adjacentSquare in adjacentSquares)
                {
                    // if this adjacent square is already in the closed list, ignore it
                    if (closedList.Exists(item => item.X == adjacentSquare.X && item.Y == adjacentSquare.Y)) continue;

                    // if it's not in the open list set params and add it.
                    if (!openList.Exists(item => item.X == adjacentSquare.X && item.Y == adjacentSquare.Y))
                    {
                        // compute its score, set the parent and add it to the open list.
                        adjacentSquare.G = g;
                        adjacentSquare.H = ComputeHScore(adjacentSquare.X, adjacentSquare.Y, target.X, target.Y);
                        adjacentSquare.F = adjacentSquare.G + adjacentSquare.H;
                        adjacentSquare.Parent = current;
                        openList.Insert(0, adjacentSquare);
                    }
                    else
                    {

                        // test if using the current G score makes the adjacent square's F score
                        // lower, if yes update the parent because it means it's a better path
                        if (g + adjacentSquare.H < adjacentSquare.F)
                        {
                            adjacentSquare.G = g;
                            adjacentSquare.F = adjacentSquare.G + adjacentSquare.H;
                            adjacentSquare.Parent = current;
                        }
                    }
                }
            }
            return false;
        }

        static int ComputeHScore(int x, int y, int targetX, int targetY)
        {
            return Math.Abs(targetX - x) + Math.Abs(targetY - y);
        }

        static List<Location> GetGateConnectedSystems(int x, int y)
        {
            var proposedLocations = new List<Location>();

            X3Sector source = X3Galaxy.Instance.GetSectorAt(x, y);
            if (source == null) throw new Exception($"GetWalkableAdjacentSquares() - sector at {x}:{y} does not exist");

            // return list of connected sectors. Note, these are not always 1 jump away.
            foreach(X3Gate gate in source.Gates)
            {
                proposedLocations.Add(new Location { X = gate.gx, Y = gate.gy });
            }

            return proposedLocations;
        }
    }
}
