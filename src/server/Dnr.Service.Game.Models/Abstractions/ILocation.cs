using System.Collections.Generic;

namespace Dnr.Service.Game.Models.Abstractions
{
    public interface ILocation
    {
        string Name { get; }

        double X { get; }

        double Y { get; }

        List<IRoad> Roads { get; }

        public int ArmyCount { get; set; }

        IPlayer? Owner { get; set; }
    }
}