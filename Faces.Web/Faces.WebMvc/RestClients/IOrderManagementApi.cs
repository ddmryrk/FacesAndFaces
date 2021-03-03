using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faces.WebMvc.ViewModels;
using Refit;

namespace Faces.WebMvc.RestClients
{
    public interface IOrderManagementApi
    {
        [Get("/orders")]
        Task<List<OrderViewModel>> GetOrders();

        [Get("/orders/{orderId}")]
        Task<OrderViewModel> GetOrder(Guid orderId);
    }
}
