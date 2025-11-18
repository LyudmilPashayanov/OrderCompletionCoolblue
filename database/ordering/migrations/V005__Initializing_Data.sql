insert into ORDER_STATES (OrderState) values ('SUBMITTED');
SET @OrderStateId1 = LAST_INSERT_ID();

insert into ORDER_STATES (OrderState) values ('FINISHED');

insert into PRODUCTS (ProductName) values ('Apple iPhone 15 pro max');
SET @ProductId1 = LAST_INSERT_ID();

insert into PRODUCTS (ProductName) values ('Samsung s24');
SET @ProductId2 = LAST_INSERT_ID();

insert into PRODUCTS (ProductName) values ('Samsung Laptop');
SET @ProductId3 = LAST_INSERT_ID();

insert into PRODUCTS (ProductName) values ('AirPod 2nd Gen');
SET @ProductId4 = LAST_INSERT_ID();

insert into PRODUCTS (ProductName) values ('USB-C cable');
SET @ProductId5 = LAST_INSERT_ID();

insert into ORDERS (OrderDate,OrderStateId) values (DATE_SUB(NOW(), INTERVAL 7 MONTH ),@OrderStateId1);
SET @OrderID1 = LAST_INSERT_ID();
insert into ORDER_LINES (OrderId,ProductId,OrderedQuantity,DeliveredQuantity) values (@OrderID1,@ProductId1,10,5);
insert into ORDER_LINES (OrderId,ProductId,OrderedQuantity,DeliveredQuantity) values (@OrderID1,@ProductId3,20,20);

insert into ORDERS (OrderDate,OrderStateId) values (DATE_SUB(NOW(), INTERVAL 7 MONTH ),@OrderStateId1);
SET @OrderID2 = LAST_INSERT_ID();
insert into ORDER_LINES (OrderId,ProductId,OrderedQuantity,DeliveredQuantity) values (@OrderID2,@ProductId1,25,25);
insert into ORDER_LINES (OrderId,ProductId,OrderedQuantity,DeliveredQuantity) values (@OrderID2,@ProductId2,20,20);

insert into ORDERS (OrderDate,OrderStateId) values (DATE_SUB(NOW(), INTERVAL 10 MONTH ),@OrderStateId1);
SET @OrderID3 = LAST_INSERT_ID();
insert into ORDER_LINES (OrderId,ProductId,OrderedQuantity,DeliveredQuantity) values (@OrderID3,@ProductId3,15,15);
insert into ORDER_LINES (OrderId,ProductId,OrderedQuantity,DeliveredQuantity) values (@OrderID3,@ProductId4,50,50);

insert into ORDERS (OrderDate,OrderStateId) values (DATE_SUB(NOW(), INTERVAL 1 MONTH ),@OrderStateId1);
SET @OrderID4 = LAST_INSERT_ID();
insert into ORDER_LINES (OrderId,ProductId,OrderedQuantity,DeliveredQuantity) values (@OrderID4,@ProductId1,30,30);
insert into ORDER_LINES (OrderId,ProductId,OrderedQuantity,DeliveredQuantity) values (@OrderID4,@ProductId5,25,25);

insert into ORDERS (OrderDate,OrderStateId) values (DATE_SUB(NOW(), INTERVAL 5 MONTH ),@OrderStateId1);
SET @OrderID5 = LAST_INSERT_ID();
insert into ORDER_LINES (OrderId,ProductId,OrderedQuantity,DeliveredQuantity) values (@OrderID5,@ProductId4,30,10);
insert into ORDER_LINES (OrderId,ProductId,OrderedQuantity,DeliveredQuantity) values (@OrderID5,@ProductId5,25,25);