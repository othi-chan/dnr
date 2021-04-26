namespace Dnr.Service.Game.Models.Abstractions
{
    public interface ICastle : ILocation
    {
        double DefenseModifier { get; }

        int ArmyGrowth { get; }

        int InfluenceGrowth { get; }

        int ArmyCapacity { get; }

        int ArmyCount { get; set; }
    }
}