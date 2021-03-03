using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Faces.WebMvc.ViewModels;
using Microsoft.Extensions.Configuration;
using Refit;

namespace Faces.WebMvc.RestClients
{
    public class OrderManagementApi : IOrderManagementApi
    {
        private IOrderManagementApi _restClient;
        public OrderManagementApi(IConfiguration configuration, HttpClient httpClient)
        {
            string apiHostAndPort = configuration.GetSection("ApiServiceLocation")
                .GetValue<string>("OrdersApiLocation");

            httpClient.BaseAddress = new Uri($"https://{apiHostAndPort}/api");
            _restClient = RestService.For<IOrderManagementApi>(httpClient);
        }

        public async Task<OrderViewModel> GetOrder(Guid orderId)
        {
            try
            {
                return await _restClient.GetOrder(orderId);
            }
            catch (ApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                else
                    throw;
            }
        }

        public async Task<List<OrderViewModel>> GetOrders()
        {
            return await _restClient.GetOrders();
        }
    }
}
