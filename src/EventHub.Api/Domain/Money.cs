namespace EventHub.Api.Domain
{
    public record Money(decimal Amount, string Currency)
    {
        public static Money Zero(string currency) => new(0, currency);

        public Money Add(Money other)
        {
            return other.Currency != Currency
                ? throw new InvalidOperationException("The currencies are different")
                : this with
                  {
                        Amount = Amount + other.Amount
                  };
        }
    }
}
