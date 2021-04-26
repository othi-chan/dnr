using System;
namespace Dnr.Service.Game.Models.Abstractions
{
    public interface IVillage : ILocation
    {
        double DefenseModifier { get; }

        int ArmyGrowth { get; }

        int InfluenceGrowth { get; }

        int LevelUpCost { get; }

        int Level { get; set; }

        int ArmyCount { get; set; }
    }
}