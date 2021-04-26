namespace Dnr.Service.Auth.Models.Abstractions
{
    public interface IEntity<TKey>
    {
        TKey Id { get; set; }
    }
}