using Dnr.Service.Game.Models.Abstractions;
using System;
using System.Collections.Generic;

namespace Dnr.Service.Game.Models
{
    public class Road : IRoad
    {
        public Tuple<ILocation, ILocation> Ends { get; }

        public double SpeedModifier { get; }

        public double Length => Math.Sqrt(Math.Pow(Ends.Item1.X - Ends.Item2.X, 2) + Math.Pow(Ends.Item1.Y - Ends.Item2.Y, 2));

        public List<IArmy> Armies { get; set; }

        public Road(
            ILocation end1,
            ILocation end2,
            double speedModifier)
        {
            Ends = new Tuple<ILocation, ILocation>(end1, end2);
            SpeedModifier = speedModifier;
            Armies = new List<IArmy>();
        }
    }
}
