CREATE TABLE IF NOT EXISTS ORDER_LINES
(
	Id                  int auto_increment primary key,
	OrderId             int not null,
	ProductId           int not null,
	OrderedQuantity     int not null,
	DeliveredQuantity   int,	
	constraint OrderLines_Orders_FK
		foreign key (OrderId) references ORDERS (Id),
	constraint OrderLines_Products_FK
		foreign key (ProductId) references PRODUCTS (Id)
);