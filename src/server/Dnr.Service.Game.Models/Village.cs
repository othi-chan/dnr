using Dnr.Service.Game.Models.Abstractions;
using Dnr.Service.Game.Models.Infrastracture;
using System;
using System.Collections.Generic;

namespace Dnr.Service.Game.Models
{
    public class Village : LocationBase, IVillage
    {
        private const int DefaultLevelUpCost = 10;

        public double DefenseModifier => Math.Log(Level);

        public int ArmyGrowth => Owner == null ? 0 : Level;

        public int InfluenceGrowth => Owner == null ? 0 : Level;

        public int LevelUpCost => (int)Math.Pow(DefaultLevelUpCost, Level);

        public int Level { get; set; }

        public int ArmyCount { get; set; }

        public Village(
            string name,
            double x,
            double y,
            IPlayer? owner = null)
            : base(name, x, y, owner)
        {
            Level = owner == null ? new Random().Next(1, 10) : 1;
            ArmyCount = owner == null ? new Random().Next(1, 100) : 0;
        }
    }
}