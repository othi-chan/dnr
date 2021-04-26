using Dnr.Service.Game.Models.Abstractions;
using Dnr.Service.Game.Models.Infrastracture;
using System.Collections.Generic;
using System;

namespace Dnr.Service.Game.Models
{
    public class Castle : LocationBase, ICastle
    {
        private const int DefaultCastleLevel = 10;

        private const int DefaultArmyCount = 100;

        public double DefenseModifier => Math.Log(DefaultCastleLevel);

        public int ArmyGrowth => Owner == null && ArmyCount < ArmyCapacity ? 1 : 0;

        public int InfluenceGrowth => DefaultCastleLevel;

        public int ArmyCapacity => Owner == null ? DefaultArmyCount : int.MaxValue;

        public int ArmyCount { get; set; }

        public Castle(
            string name,
            double x,
            double y)
            : base(name, x, y, owner: null)
        {
            ArmyCount = DefaultArmyCount;
        }
    }
}