using System.Collections.Generic;

namespace Dnr.Web.Api.Models
{
    public class GameMapGet
    {
        public CastleGet? Castle { get; set; }

        public IEnumerable<VillageGet>? Vilages { get; set; }

        public IEnumerable<RoadGet>? Roads { get; set; }

        public IEnumerable<ArmyGet>? Armies { get; set; }
    }
}