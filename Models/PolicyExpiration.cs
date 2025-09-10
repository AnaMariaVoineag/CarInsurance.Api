namespace CarInsurance.Api.Models
{
    public class PolicyExpiration
    {
        public long Id { get; set; }
        public long PolicyId { get; set; }
        public DateTime LoggedAt { get; set; }
    }
}
