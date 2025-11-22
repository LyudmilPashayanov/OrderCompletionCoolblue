
![happy path overview service.drawio.png](happy%20path%20overview%20service.drawio.png)

1. Scheduled job service -> Running for example every day processing a batch of orders.
   - Initially the call was synchronous, but now it is async. When dealing with I/O operations, async is preferred for performance, as it doesn't block a thread while awaiting. ASP.NET automatically releases the thread when it is being awaited and it can be used for some other operation.
   - The call is expected to return either OK(body with possible fail reasons) or BadRequest(body with possible fail reasons).
   - Inside the body of each response there is a custom class (CompleteOrderResponse) with some information about which order Ids failed and which succeeded.
   - The scheduled job will retry the failed Ids, while the logs will provide more information on why the initial request failed.
2. The Controller calls the "CompleteOrderAsync" method in OrderCompletionUseCase.
   - This function is the main orchestrator of the service and has a simple step by step structure, explained below.
3. First we fetch all orders with one query. Using a query to fetch each order one by one will be slower and less efficient.
   - If no orders are returned from the Database, we throw custom exception- NoOrdersFoundException, and we log an error.
   - If NoOrdersFoundException was caught, we return BadRequest.
4. If no errors, then we check which of the fetched orders are meeting the business requirements.
   - All business requirements are injected, using DI of the framework. 
   - All business requirements inherit from IOrderRequirement and each requirement is its own class.
   - When an order needs to be completed, it checks against all requirements.
   - As we don't know if more requirements will be added in the future, adding a new requirement is easy. Just a matter of adding a new IOrderRequirement class and injecting it.
   - For each order that doesn't meet the requirements is added to a UnsuccessfulOrders list which will be returned to the scheduled job.
5. For each order meeting the requirements, we try to send a notification. 
   - Notifications are sent, one by one per order (initial design of the service). Maybe a good optimization of the notifications service will be to make it send notifications to a batch of orders.
   - Retry mechanism from a 3rd party library ("Polly") is used when sending notifications.
   - You can modify and control some of the retry settings from the appsettings.json of the OrderComplete service.
   - For each order that failed to be notified by the Notification service, is added to a UnsuccessfullyNotifiedOrders list which will be returned to the scheduled job.
   - If all notifications fail, then we throw NoOrdersSuccessfullyNotifiedException, and a BadRequest is returned.
6. For all orders that meet all requirements and are successfully notified, we try to update their status to finished.
   - We use a single query to batch update all good orders.
   - If this fails, we throw NoOrdersSuccessfullyUpdated and return BadRequest(body with fail reasons).
7. I generate the CompleteOrderResponse, which I will send in the response body.
   - If this last query was successful I send Result.OK(body with some failed ids).

General Improvements:
- 
- Factory responsible only for generating the MySQL connection string.
- 