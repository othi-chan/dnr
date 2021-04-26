using System;

namespace Dnr.Service.Game.Models.Abstractions
{
    public interface IPlayer
    {
        long Id { get; }

        string Name { get; }

        int Influence { get; set; }
    }
}