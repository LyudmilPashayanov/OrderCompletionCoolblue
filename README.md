
![happy path overview service.drawio.png](happy%20path%20overview%20service.drawio.png)

1. Scheduled job service -> Running for example every day processing a batch of orders.
   - The call is expected to return either OK(CompleteOrderResponse) or BadRequest(CompleteOrderResponse).
   - Inside the body of each response there is a custom class (CompleteOrderResponse) with some information about which order Ids failed and which succeeded (details in the diagram).
   - The scheduled job will retry the failed Ids, while the logs will provide more information on why the initial request failed.
2. The Controller calls the "CompleteOrderAsync" method in OrderCompletionUseCase.
   - This function is the main orchestrator of the service and has a simple step by step structure, explained below.
3. First we fetch all orders with one query. Using a query to fetch each order one by one will be slower and less efficient.
   - If no orders are returned from the Database, we throw custom exception- NoOrdersFoundException, and we log an error. In that case, we exit the service by returning BadRequest.
4. If no errors, then we check which of the fetched orders are meeting the business requirements.
   - So that an order can be completed, it must check and pass against all requirements.
   - For each order that doesn't meet the requirements is added to a UnsuccessfulOrders list which will be returned to the scheduled job.
5. For each order meeting all the requirements, we try to send a notification. 
   - Notifications are sent, one by one per order (initial design of the service). Maybe a good optimization of the notifications service will be to make it send notifications to a batch of orders.
   - Retry mechanism from a 3rd party library ("Polly") is used when sending notifications. Setup is in NotificationAdapter and you can modify and control some of the retry settings from the appsettings.json of the OrderComplete service.
   - For each order that failed to be notified by the Notification service, is added to a UnsuccessfullyNotifiedOrders list which will be returned to the scheduled job.
   - If all notifications fail, then we throw NoOrdersSuccessfullyNotifiedException, and exit the service by returning BadRequest
6. For all orders that meet all requirements and are successfully notified, we try to update their status to finished.
   - We use a single query to batch update all good orders.
   - If this fails, we throw NoOrdersSuccessfullyUpdated and exit the service by returning BadRequest.
7. At last the CompleteOrderResponse is completely populated.
   - If this last query was successful we exit the service by sending Result.OK(body with some possible failed ids).

Managers:
- Singletons (all of them stateless): IOrderRequirements, IDbConnectionFactory, ISqlDialect, ISystemClock, IAsyncPolicy 
- Scoped : IOrderCompletionRepository, IOrderCompletionUseCase (in theory currently both can be singletons, as both are stateless, but prone to error in the future if refactored by using scoped services)
- Transient: ---

General Improvements:
- If we want to switch the db we are connecting to, we can do that easily. It just needs to inherit from IDbConnectionFactory and implement the new DB query as a ISqlDialect class. Setup is in OrderCompletionAdapter.
- Initially the call was synchronous, but now it is async. When dealing with I/O operations, async is preferred for performance, as it doesn't block a thread while awaiting. ASP.NET automatically releases the thread when it is being awaited and it can be used for some other operation.
- All business requirements are injected, using DI of the framework. they all inherit from IOrderRequirement and each requirement is its own class. As we don't know if more requirements will be added in the future, adding a new requirement is easy. Just a matter of adding a new IOrderRequirement class and injecting it.
- Improved security, by removing credentials (username and password for DB) from appsettings. 
- All queries are idempotent, meaning that if we call the same query multiple times, the final state will be the same as executing it once. 

Edge cases:
- If there is an old order, but without any order lines, the service will NOT mark it as complete. It will log a warning with the failure reason.
- If there is an old order, with more delivered items, than requested, the service will mark the order as complete.

TODO:
 - Add retry mechanism to the OrderCompletionRepository class, when we do the querying the database.
 - In case we expect a lot of orders in the initial request, batch them and place them to be checked on different threads. 
 - Notifications are sent, one by one per order id (initial design of the service). Maybe a good optimization of the notifications service will be to make it send notifications to a batch of orders.
 - Add behaviour tests for the NotificationService and the querying the database logic. 
