using Dnr.Service.Game.Models.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dnr.Service.Game.Models
{
    public class GameMap : IGameMap
    {
        public IEnumerable<ILocation> Locations { get; }

        public IEnumerable<IRoad> Roads { get; }

        public List<IArmy> Armies { get; set; }

        public GameMap(IEnumerable<IPlayer> players)
        {
            var playersLocal = players.ToList();
            var player1Index = new Random().Next(0, 2);
            var player1 = playersLocal.ElementAt(player1Index);
            playersLocal.RemoveAt(player1Index);
            var player2 = playersLocal.Single();

            Locations = new List<ILocation>
            {
                new Castle(
                    name: "Nightless City",
                    x: 100,
                    y: 100),
                new Village(
                    name: "Unclean Realm",
                    x: 50,
                    y: 50,
                    owner: player1),
                new Village(
                    name: "Cloud Recesses",
                    x: 150,
                    y: 100),
                new Village(
                    name: "Lotus Pier",
                    x: 50,
                    y: 150,
                    owner: player2),
            };

            Roads = new List<IRoad>
            {
                new Road(
                    end1: Locations.Single(_ => _.Name == "Unclean Realm"),
                    end2: Locations.Single(_ => _.Name == "Nightless City"),
                    speedModifier: 1),
                new Road(
                    end1: Locations.Single(_ => _.Name == "Unclean Realm"),
                    end2: Locations.Single(_ => _.Name == "Cloud Recesses"),
                    speedModifier: 0.5),
                new Road(
                    end1: Locations.Single(_ => _.Name == "Lotus Pier"),
                    end2: Locations.Single(_ => _.Name == "Nightless City"),
                    speedModifier: 1),
                new Road(
                    end1: Locations.Single(_ => _.Name == "Lotus Pier"),
                    end2: Locations.Single(_ => _.Name == "Cloud Recesses"),
                    speedModifier: 0.5),
                new Road(
                    end1: Locations.Single(_ => _.Name == "Cloud Recesses"),
                    end2: Locations.Single(_ => _.Name == "Nightless City"),
                    speedModifier: 1),
            };

            foreach (var location in Locations)
            {
                location.Roads.AddRange(
                    Roads.Where(_ => _.Ends.Item1.Name == location.Name || _.Ends.Item2.Name == location.Name));
            }

            Armies = new List<IArmy>();
        }
    }
}