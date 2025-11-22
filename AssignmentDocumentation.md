Scheduled job service -> Running for example every day or every week and processing a batch of orders.
First we fetch all orders with one query (because using a different query to fetch one by one each order will be less efficient)
Then decide which orders are meeting the business requirements.
For each order meeting the requirements, send a notification. Notifications are sent, one by one. (Maybe a good optimization of the notifications service will be to make it send notification to a batch of orders)
I check for which orders the notifications failed and remove them from my list of orders.
For my new list of orders I make one query in batch to change their status to processedAndFinished.
If this last query is successful I send Result.OK().
If the query to change their status fails, I send Result(400) of the Complete http request and I send the reason.
All business requirements are injected in the using DI of the framework.  All business requirements inherit from IBusinessRequirement and each requirement is its own class. When an order needs to be completed it checks against all requirements. Adding a new requirement is now easy, just a matter of adding a new class and injecting it.
Factory responsible only for generating the MySQL connection string.
I have decided to make all operation async so that they can be awaited. ASP.NET automatically releases the thread when it is being awaited and it can be used for some other operation. In this case, when we have I/O operations, this is improving the performance as the service is running more efficiently releasing threads, while they are not used.