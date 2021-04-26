using System.Collections.Generic;

namespace Dnr.Service.Game.Models.Abstractions
{
    public interface IGameMap
    {
        IEnumerable<ILocation> Locations { get; }

        IEnumerable<IRoad> Roads { get; }

        List<IArmy> Armies { get; set; }
    }
}