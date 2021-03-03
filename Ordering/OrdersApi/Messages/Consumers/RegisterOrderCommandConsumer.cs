using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using MassTransit;
using Messaging.InterfacesConstants.Commands;
using Newtonsoft.Json;
using OrdersApi.Models;
using OrdersApi.Persistence;

namespace OrdersApi.Messages.Consumers
{
    public class RegisterOrderCommandConsumer : IConsumer<IRegisterOrderCommand>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IHttpClientFactory _clientFactory;
        public RegisterOrderCommandConsumer(IOrderRepository orderRepository, IHttpClientFactory clientFactory)
        {
            _orderRepository = orderRepository;
            _clientFactory = clientFactory;
        }

        public async Task Consume(ConsumeContext<IRegisterOrderCommand> context)
        {
            var result = context.Message;
            if (result.OrderId != null &&
                result.PictureUrl != null &&
                result.UserEmail != null &&
                result.ImageData != null)
            {
                SaveOrder(result);

                var client = _clientFactory.CreateClient();
                var orderDetailsData = await GetFacesFromFaceApiAsync(client, result.ImageData, result.OrderId);
                List<byte[]> faces = orderDetailsData.Item1;
                var orderId = orderDetailsData.Item2;
                SaveOrderDetails(orderId, faces);
            }
        }

        private void SaveOrder(IRegisterOrderCommand result)
        {
            Order order = new Order
            {
                OrderId = result.OrderId,
                UserEmail = result.UserEmail,
                Status = Status.Registered,
                PictureUrl = result.PictureUrl,
                ImageData = result.ImageData
            };
            _orderRepository.RegisterOrder(order);
        }

        private async Task<Tuple<List<byte[]>, Guid>> GetFacesFromFaceApiAsync(HttpClient client, byte[] imageData, Guid orderId)
        {
            var byteContent = new ByteArrayContent(imageData);
            Tuple<List<byte[]>, Guid> orderDetailData = null;
            byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            using (var response = await client.PostAsync($"http:6000/api/faces?orderId={orderId}", byteContent))
            {
                string apiResponse = await response.Content.ReadAsStringAsync();
                orderDetailData = JsonConvert.DeserializeObject<Tuple<List<byte[]>, Guid>>(apiResponse);
            }
            return orderDetailData;
        }

        private void SaveOrderDetails(Guid orderId, List<byte[]> faces)
        {
            var order = _orderRepository.GetOrderAsync(orderId).Result;

            if (order == null)
            {
                order.Status = Status.Processed;

                foreach (var face in faces)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = orderId,
                        FaceData = face
                    };
                    order.OrderDetails.Add(orderDetail);
                }

                _orderRepository.UpdateOrder(order);
            }
        }
    }
}
