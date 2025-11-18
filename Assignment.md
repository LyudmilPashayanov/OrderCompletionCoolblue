# The take-home assignment

_A brilliant Coolblue developer started a new service. Unfortunately, they got lost in our warehouse before they were able to finish it…_

Together with this document, you should have received a .Net solution. The solution contains the start of an API service but is missing quite a few crucial parts.
Your task is to fill in these missing parts based on the business requirements below.

### Business Requirements

Part of managing stock and making sales predictions is knowing exactly how much of each product was sold. When an order has been completely processed, the Coolblue sales records and prediction models can be updated. This new service will be responsible for deciding if an order is 'complete'.

An order is complete if:
- for every separate product that is part of the order (an orderline), Coolblue delivered the quantity that the customer ordered
- the date that the order was placed is at least six months in the past. This extra time is needed to give Coolblue the opportunity to fix small administrative mistakes

### Additional Behaviours

- If the new service decides that an order can safely be completed, it should notify another service using the notification client
- Be aware that notifying can fail! One way to handle this could be to retry calling the notification service, but there could be problems here as well
- If the call fails permanently, the order should not be completed. Assume that the Order Completion Service will be run as a scheduled job and the order will be picked up later. No further action from you, the candidate, is required
- If an order has been completed and the notification has been successfully sent, only then the local database should be updated to register the completion. The same database can provide you the exact order that was completed

### What we expect

See the README.md for our technical values and expectations. There is one thing we want to stress:
- Complete the use case and ensure its tests all pass. You should not change the unit tests themselves!


# Getting started

This .NET solution includes multiple services that need to be spun up using Docker Compose before running the `OrderCompletion.Api` project. The services include:
- Notification service
- MySQL database
- Flyway migration service

## Prerequisites

- Docker installed on your machine
- .NET SDK installed on your machine

## Build and Start the Services

**WARNING**: The `docker-compose` configuration assumes there's no other services running on the following ports: `5002` (notification service), `3306` (MySQL 8.0 instance). You might need to change the ports in the `docker-compose.yml` file if these ports are already in use on your machine.

The same logic also applies to the `OrderCompletion.Api` project, which is configured to run on port `5001` by default.

**WARNING**: If you use ARM-based machine, you need to update `docker-compose.yml` with the following information:
- `notification` service build argument with `linux-arm64`.
- `notification` service port mapping with `5002:8080`.

Open a terminal window and run the following command to build and start the necessary services:

```sh
docker compose up --build -d
```

The following service will be spun up and will be ready for you:

- **Notification Service**: A .NET Core API running on port 5002
  - This service can be accessed on http://localhost:5002/swagger/index.html
- **MySQL Database**: A MySQL 8.0 instance running on port 3306
- **Flyway Migration**: A Flyway service that runs database migrations
  - You don't manually utilize this service; it will execute the required DB migration scripts and will stop automatically

## Running the OrderCompletion.Api Project

After the services are up and running, you can execute the `OrderCompletion.Api` project. This service provides you with a Swagger interface for your convenience, allowing easy access to the order completion endpoint.

The order completion end-point can be accessed on:
https://localhost:5001/swagger/index.html

If the service fails to start, try changing to another port number on your machine.
