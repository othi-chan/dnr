namespace Dnr.Web.Api.Models
{
    public class SendArmyPut
    {
        public string? SourceVillage { get; set; }

        public string? TargetVillage { get; set; }

        public int ArmyCount { get; set; }
    }
}