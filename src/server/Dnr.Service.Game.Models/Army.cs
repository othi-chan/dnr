using Dnr.Service.Game.Models.Abstractions;
using System;

namespace Dnr.Service.Game.Models
{
    public class Army : IArmy
    {
        private const int DefaultArmyCount = 100;

        private const double DefaultArmySpeed = 10;

        public IPlayer Owner { get; }

        public ILocation Source { get; }

        public ILocation Target { get; }

        public IRoad Road { get; }

        public DateTime StartTime { get; }

        public DateTime FinishTime { get; }

        public double SpeedModifier =>
            DefaultArmyCount / Count > 4
            ? 4
            : DefaultArmyCount / Count < 0.25
                ? 0.25
                : DefaultArmyCount / Count;

        public int Count { get; set; }

        public Army(
            IPlayer owner,
            ILocation source,
            ILocation target,
            IRoad road,
            int count)
        {
            Owner = owner;
            Source = source;
            Target = target;
            Road = road;
            Count = count;
            StartTime = DateTime.UtcNow;
            FinishTime = StartTime.Add(
                TimeSpan.FromSeconds(road.Length / (SpeedModifier * road.SpeedModifier * DefaultArmySpeed)));
        }
    }
}