using Dnr.Service.Game.Models;

namespace Dnr.Web.Api.Models
{
    public class CastleGet
    {
        public string? Name { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public Player? Owner { get; set; }

        public double DefenseModifier { get; set; }

        public int ArmyGrowth { get; set; }

        public int InfluenceGrowth { get; set; }

        public int ArmyCount { get; set; }
    }
}