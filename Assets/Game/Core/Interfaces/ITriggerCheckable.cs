namespace Game.Core.Interfaces
{
    public interface ITriggerCheckable 
    {
    bool IsAggroed { get; set; }
    bool IsWithinStrickingDistance {get; set;}
    void SetAggroStatus(bool isAggored);
    void SetStrikingDistanceBool(bool isWithinStrikingDistance);
    }
}