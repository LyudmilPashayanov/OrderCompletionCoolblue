CREATE TABLE IF NOT EXISTS ORDERS
(
	Id              int auto_increment primary key,
	OrderDate       datetime not null,
	OrderStateId    int not null,	
	constraint Orders_OrderStates_FK
		foreign key (OrderStateId) references ORDER_STATES (Id)
);