using Dnr.Service.Game.Models.Abstractions;
using System.Collections.Generic;

namespace Dnr.Service.Game.Models.Infrastracture
{
    public abstract class LocationBase : ILocation
    {
        public string Name { get; }

        public double X { get; }

        public double Y { get; }

        public List<IRoad> Roads { get; }

        public IPlayer? Owner { get; set; }

        public int ArmyCount { get; set; }

        public LocationBase(
            string name,
            double x,
            double y,
            IPlayer? owner)
        {
            Name = name;
            X = x;
            Y = y;
            Roads = new List<IRoad>();
            Owner = owner;
            ArmyCount = 0;
        }
    }
}