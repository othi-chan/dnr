using System;
using System.Collections.Generic;

namespace Dnr.Service.Game.Models.Abstractions
{
    public interface IRoad
    {
        Tuple<ILocation, ILocation> Ends { get; }

        double SpeedModifier { get; }

        double Length { get; }

        List<IArmy> Armies { get; set; }
    }
}