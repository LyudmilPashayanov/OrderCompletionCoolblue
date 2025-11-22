namespace OrderCompletion.Api.CustomExceptions;

public class NoOrdersSuccessfullyUpdated : Exception
{
    public NoOrdersSuccessfullyUpdated() : base("No orders were successfully updated in the database.")
    {
        
    }
}