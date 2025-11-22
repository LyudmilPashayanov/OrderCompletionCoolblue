using OrderCompletion.Api.Models;
using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api.Unit.Tests.BehaviourTests.Fakes;

    public class InMemoryOrderRepository : IOrderCompletionRepository
    {
        private readonly Dictionary<int, Order> _store = new Dictionary<int, Order>();
        private readonly object _sync = new object();

        /// <summary>
        /// Add or replace an order in the in-memory store. Stored object is a clone to avoid shared mutable state across tests.
        /// </summary>
        public void Populate(Order order)
        {
            if (order is null) throw new ArgumentNullException(nameof(order));
            lock (_sync)
            {
                _store[order.Id] = Clone(order);
            }
        }

        /// <summary>
        /// Returns a list of orders matching the requested ids. If none found, returns an empty list.
        /// </summary>
        public Task<List<Order>> GetOrdersByIdAsync(IReadOnlyCollection<int> orderIds, CancellationToken ct = default)
        {
            if (orderIds is null) throw new ArgumentNullException(nameof(orderIds));
            ct.ThrowIfCancellationRequested();

            List<Order> result;
            lock (_sync)
            {
                result = orderIds
                    .Where(id => _store.TryGetValue(id, out _))
                    .Select(id => Clone(_store[id]))
                    .ToList();
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Marks matched orders as Finished and returns the number of orders actually updated.
        /// Only updates orders that currently exist (and optionally are in Submitted state).
        /// </summary>
        public Task<int> CompleteOrdersAsync(IReadOnlyCollection<int> orderIds, CancellationToken ct = default)
        {
            if (orderIds is null) throw new ArgumentNullException(nameof(orderIds));
            ct.ThrowIfCancellationRequested();

            int updated = 0;
            lock (_sync)
            {
                foreach (var id in orderIds)
                {
                    if (_store.TryGetValue(id, out var order))
                    {
                        // Mirror the repository's WHERE OrderStateId = Submitted semantics if desired:
                        if (order.OrderState == OrderState.Submitted)
                        {
                            // mutate stored order to represent DB update
                            order.OrderState = OrderState.Finished;
                            updated++;
                        }
                    }
                }
            }

            return Task.FromResult(updated);
        }

        /// <summary>
        /// Clone an Order to ensure callers can't mutate the internal in-memory store.
        /// </summary>
        private static Order Clone(Order src)
        {
            if (src is null) return null;

            return new Order
            {
                Id = src.Id,
                OrderState = src.OrderState,
                OrderDate = src.OrderDate,
                OrderLines = src.OrderLines?
                    .Select(ol => new OrderLine
                    {
                        ProductId = ol.ProductId,
                        OrderedQuantity = ol.OrderedQuantity,
                        DeliveredQuantity = ol.DeliveredQuantity
                    })
                    .ToList() ?? new List<OrderLine>()
            };
        }
    }